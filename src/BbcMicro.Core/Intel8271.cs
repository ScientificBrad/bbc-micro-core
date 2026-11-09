using System;
using System.Collections.Generic;

namespace BbcMicro.Core
{
    public class Intel8271
    {
        private readonly BbcBus _bus;
        public readonly FloppyDrive[] Drives = new FloppyDrive[2];
        public int SelectedDriveIndex = 0;

        // Status register flags (0xFE80 Read):
        // Bit 7: Busy (0x80)
        // Bit 6: Command Full (0x40)
        // Bit 5: Parameter Full (0x20) - 0 when ready for next parameter
        // Bit 4: Result Full (0x10) - 1 when result byte ready to read
        // Bit 3: Interrupt Request / INT (0x08)
        // Bit 2: Non-DMA Data Request / DRQ (0x04)
        private byte _status = 0x00;

        private byte _commandRegister = 0x00;
        private byte _dataRegister = 0x00;
        private readonly Queue<byte> _resultQueue = new();
        private readonly List<byte> _parameters = new();
        private int _expectedParams = 0;

        private readonly byte[] _specialRegisters = new byte[64];

        // Streaming state for Read/Write Sectors
        private byte[]? _transferBuffer;
        private int _transferIndex = 0;
        private int _transferTotal = 0;
        private int _transferDelayCycles = 0;
        private bool _isReadTransfer = false;
        private bool _isWriteTransfer = false;
        private bool _completionPending = false;
        private int _completionDelayCycles = 0;

        public bool EnableTracing { get; set; } = false;
        public readonly List<string> TraceLog = new();

        public FloppyDrive Drive0 => Drives[0];
        public FloppyDrive Drive1 => Drives[1];

        public Intel8271(BbcBus bus)
        {
            _bus = bus;
            Drives[0] = new FloppyDrive();
            Drives[1] = new FloppyDrive();
            Reset();
        }

        public void Reset()
        {
            _status = 0x00;
            _commandRegister = 0x00;
            _dataRegister = 0x00;
            _resultQueue.Clear();
            _parameters.Clear();
            _expectedParams = 0;
            _transferBuffer = null;
            _transferIndex = 0;
            _transferTotal = 0;
            _transferDelayCycles = 0;
            _isReadTransfer = false;
            _isWriteTransfer = false;
            _completionPending = false;
            _completionDelayCycles = 0;
            Array.Clear(_specialRegisters, 0, _specialRegisters.Length);
            // Default 8271 mode registers
            _specialRegisters[0x17] = 0xC1; // Mode register
            _specialRegisters[0x22] = 0x00; // Drive 0 status
            _specialRegisters[0x23] = 0x00; // Drive 1 status
        }

        public void Tick(int cycles)
        {
            if (_transferDelayCycles > 0)
            {
                _transferDelayCycles -= cycles;
                if (_transferDelayCycles <= 0)
                {
                    _transferDelayCycles = 0;
                    if (_isReadTransfer && _transferBuffer != null && _transferIndex < _transferTotal)
                    {
                        _dataRegister = _transferBuffer[_transferIndex];
                        _status |= 0x04; // DRQ high
                        _bus.NmiPending = true; // Assert NMI
                    }
                    else if (_isWriteTransfer && _transferBuffer != null && _transferIndex < _transferTotal)
                    {
                        _status |= 0x04; // DRQ high
                        _bus.NmiPending = true; // Assert NMI
                    }
                }
            }

            if (_completionPending)
            {
                _completionDelayCycles -= cycles;
                if (_completionDelayCycles <= 0)
                {
                    _completionPending = false;
                    _completionDelayCycles = 0;
                    _status = (byte)(_status & ~0x80); // Clear Busy
                    _status = (byte)(_status & ~0x04); // Clear DRQ
                    _status |= 0x08;  // Set INT (completion interrupt)
                    _status |= 0x10;  // Set Result Full
                    _resultQueue.Enqueue(0x00); // 0x00 = success code
                    _bus.NmiPending = true; // Assert NMI for completion

                    if (EnableTracing)
                    {
                        TraceLog.Add($"[8271] Command completed ({_transferTotal} bytes)");
                    }
                }
            }
        }

        public byte Read(byte offset)
        {
            // Decoding: $FE80 - $FE84 (mirrored up to $FE9F)
            int reg = offset & 0x07;
            switch (reg)
            {
                case 0: // $FE80: Status Register
                    return _status;

                case 1: // $FE81: Result Register
                    if (_resultQueue.Count > 0)
                    {
                        byte res = _resultQueue.Dequeue();
                        if (_resultQueue.Count == 0)
                        {
                            _status = (byte)(_status & ~0x10); // Clear Result Full
                            _status = (byte)(_status & ~0x08); // Clear INT
                        }
                        return res;
                    }
                    return 0x00;

                case 4: // $FE84: Data Register
                    if (_isReadTransfer)
                    {
                        byte b = _dataRegister;
                        _status = (byte)(_status & ~0x04); // Clear DRQ
                        _bus.ClearNmi();
                        _transferIndex++;

                        if (_transferIndex < _transferTotal)
                        {
                            // Authentic transfer pacing: 64 cycles delay before next byte ready
                            _transferDelayCycles = 64;
                        }
                        else
                        {
                            // All bytes transferred! Finish Read Sectors command after delay so NMI RTI completes
                            _isReadTransfer = false;
                            _completionPending = true;
                            _completionDelayCycles = 64;
                        }
                        return b;
                    }
                    return _dataRegister;

                default:
                    return 0x00;
            }
        }

        public void Write(byte offset, byte value)
        {
            int reg = offset & 0x07;
            switch (reg)
            {
                case 0: // $FE80: Command Register
                    HandleCommandWrite(value);
                    break;

                case 1: // $FE81: Parameter Register
                    HandleParameterWrite(value);
                    break;

                case 2: // $FE82: Reset Register
                    Reset();
                    break;

                case 4: // $FE84: Data Register
                    if (_isWriteTransfer)
                    {
                        _status = (byte)(_status & ~0x04); // Clear DRQ
                        _bus.ClearNmi();
                        if (_transferBuffer != null && _transferIndex < _transferBuffer.Length)
                        {
                            _transferBuffer[_transferIndex] = value;
                        }
                        _transferIndex++;

                        if (_transferIndex < _transferTotal)
                        {
                            _transferDelayCycles = 64;
                        }
                        else
                        {
                            // Finish Write Sectors command: commit buffer to drive after delay
                            CommitWriteBuffer();
                            _isWriteTransfer = false;
                            _completionPending = true;
                            _completionDelayCycles = 64;
                        }
                    }
                    else
                    {
                        _dataRegister = value;
                    }
                    break;
            }
        }

        private void HandleCommandWrite(byte val)
        {
            _commandRegister = val;
            _status |= 0x80;  // Busy
            _status = (byte)(_status & ~0x20); // Ready for parameter (Bit 5 = 0)
            _status = (byte)(_status & ~0x10); // Result not full
            _status = (byte)(_status & ~0x08); // Clear INT
            _status = (byte)(_status & ~0x04); // Clear DRQ
            _resultQueue.Clear();
            _parameters.Clear();

            // Drive selection bits:
            // Acorn DFS encodes drive in bits 6-7:
            // Bit 6: Drive 0, Bit 7: Drive 1
            SelectedDriveIndex = (val & 0x80) != 0 ? 1 : 0;

            uint op = (uint)(val & 0x3F);
            // 8271 parameter count determination
            if ((op & 0x18) != 0)
            {
                _expectedParams = (int)(op & 0x03);
            }
            else
            {
                _expectedParams = 5;
            }

            // Command-specific adjustments:
            uint cmdCode = (op & 0x3C) >> 2;
            switch (cmdCode)
            {
                case 14: // 0x38/0x3A: Write Special Register -> 2 params
                    _expectedParams = 2;
                    break;
                case 15: // 0x3C/0x3D: Read Special Register -> 1 param
                    _expectedParams = 1;
                    break;
                case 11: // 0x2C: Read Drive Status -> 0 params
                    _expectedParams = 0;
                    break;
                case 10: // 0x28/0x29: Seek -> 1 param
                    _expectedParams = 1;
                    break;
                case 4:  // 0x10-0x13: Read Sectors -> 3 params (Track, Sector, Count/Length)
                    _expectedParams = 3;
                    break;
                case 2:  // 0x08-0x0B: Write Sectors -> 3 params
                    _expectedParams = 3;
                    break;
                case 7:  // 0x1C-0x1F: Verify Sectors -> 3 params
                    _expectedParams = 3;
                    break;
                case 8:  // 0x20-0x23: Format Track -> 3 params
                    _expectedParams = 3;
                    break;
                case 13: // 0x34/0x35: Specify -> Variable params ending with $EA
                    _expectedParams = 4;
                    break;
            }

            if (EnableTracing)
            {
                TraceLog.Add($"[8271] Command 0x{val:X2} (code {cmdCode}), expecting {_expectedParams} params");
            }

            if (_expectedParams == 0)
            {
                ExecuteCommand();
            }
        }

        private void HandleParameterWrite(byte val)
        {
            _parameters.Add(val);

            if (EnableTracing)
            {
                TraceLog.Add($"[8271] Param[{_parameters.Count - 1}] = 0x{val:X2}");
            }

            uint cmdCode = ((uint)_commandRegister & 0x3C) >> 2;
            if (cmdCode == 13) // Specify
            {
                if (val == 0xEA || _parameters.Count >= 4)
                {
                    ExecuteCommand();
                }
            }
            else if (_parameters.Count >= _expectedParams)
            {
                ExecuteCommand();
            }
        }

        private void ExecuteCommand()
        {
            uint cmdCode = ((uint)_commandRegister & 0x3C) >> 2;

            switch (cmdCode)
            {
                case 4: // Read Sectors (0x10 - 0x13)
                    ExecuteReadSectors();
                    break;

                case 2: // Write Sectors (0x08 - 0x0B)
                    ExecuteWriteSectors();
                    break;

                case 10: // Seek (0x28 - 0x29)
                    ExecuteSeek();
                    break;

                case 11: // Read Drive Status (0x2C)
                    ExecuteReadDriveStatus();
                    break;

                case 13: // Specify (0x34 - 0x35)
                    ExecuteSpecify();
                    break;

                case 14: // Write Special Register (0x38 - 0x3A)
                    ExecuteWriteSpecialRegister();
                    break;

                case 15: // Read Special Register (0x3C - 0x3D)
                    ExecuteReadSpecialRegister();
                    break;

                case 7: // Verify Sectors
                case 8: // Format
                default:
                    // Generic success
                    _status = (byte)(_status & ~0x80);
                    _status |= 0x10;
                    _status |= 0x08;
                    _resultQueue.Enqueue(0x00);
                    _bus.NmiPending = true;
                    break;
            }
        }

        private void ExecuteReadSectors()
        {
            int track = _parameters.Count > 0 ? _parameters[0] : 0;
            int sector = _parameters.Count > 1 ? _parameters[1] : 0;
            int count = _parameters.Count > 2 ? (_parameters[2] & 0x1F) : 1;
            if (count == 0) count = 1;

            _transferTotal = count * 256;
            _transferBuffer = new byte[_transferTotal];
            var drive = Drives[SelectedDriveIndex];

            if (EnableTracing)
            {
                TraceLog.Add($"[8271] Executing Read Sectors: Drive {SelectedDriveIndex}, Track {track}, Sector {sector}, Count {count}");
            }

            int currTrack = track;
            int currSec = sector;
            for (int i = 0; i < count; i++)
            {
                drive.ReadSector(currTrack, currSec, _transferBuffer, i * 256);
                currSec++;
                if (currSec >= 10)
                {
                    currSec = 0;
                    currTrack++;
                }
            }
            drive.CurrentTrack = currTrack;

            _transferIndex = 0;
            _isReadTransfer = true;
            _status |= 0x80; // Busy
            // Initial delay of 32 cycles before first NMI byte
            _transferDelayCycles = 32;
        }

        private void ExecuteWriteSectors()
        {
            int track = _parameters.Count > 0 ? _parameters[0] : 0;
            int sector = _parameters.Count > 1 ? _parameters[1] : 0;
            int count = _parameters.Count > 2 ? (_parameters[2] & 0x1F) : 1;
            if (count == 0) count = 1;

            _transferTotal = count * 256;
            _transferBuffer = new byte[_transferTotal];
            _transferIndex = 0;
            _isWriteTransfer = true;
            _status |= 0x80;
            _transferDelayCycles = 32;
        }

        private void CommitWriteBuffer()
        {
            if (_transferBuffer == null) return;
            int track = _parameters.Count > 0 ? _parameters[0] : 0;
            int sector = _parameters.Count > 1 ? _parameters[1] : 0;
            int count = _parameters.Count > 2 ? (_parameters[2] & 0x1F) : 1;
            if (count == 0) count = 1;

            var drive = Drives[SelectedDriveIndex];
            int currTrack = track;
            int currSec = sector;
            for (int i = 0; i < count; i++)
            {
                drive.WriteSector(currTrack, currSec, _transferBuffer, i * 256);
                currSec++;
                if (currSec >= 10)
                {
                    currSec = 0;
                    currTrack++;
                }
            }
            drive.CurrentTrack = currTrack;
        }

        private void ExecuteSeek()
        {
            int targetTrack = _parameters.Count > 0 ? _parameters[0] : 0;
            Drives[SelectedDriveIndex].CurrentTrack = targetTrack;
            _specialRegisters[SelectedDriveIndex == 0 ? 0x0A : 0x0B] = (byte)targetTrack;

            _status = (byte)(_status & ~0x80); // Clear Busy
            _status |= 0x08;  // Interrupt Request
            _status |= 0x10;  // Result Ready
            _resultQueue.Enqueue(0x00); // Success
            _bus.NmiPending = true;

            if (EnableTracing)
            {
                TraceLog.Add($"[8271] Seek to Track {targetTrack} completed");
            }
        }

        private void ExecuteReadDriveStatus()
        {
            _status = (byte)(_status & ~0x80); // Clear Busy
            _status |= 0x10;  // Result Ready

            byte statusByte = 0x00;
            var drive0 = Drives[0];
            if (drive0.IsLoaded)
            {
                // Bit 2: Drive 0 Ready / Selected, Bit 3: Drive 0 Ready
                statusByte |= 0x0C;
                if (drive0.CurrentTrack == 0) statusByte |= 0x02; // Track 0
                if (drive0.WriteProtected) statusByte |= 0x01;
            }
            var drive1 = Drives[1];
            if (drive1.IsLoaded)
            {
                statusByte |= 0xC0;
                if (drive1.CurrentTrack == 0) statusByte |= 0x20;
                if (drive1.WriteProtected) statusByte |= 0x10;
            }

            _resultQueue.Enqueue(statusByte);

            if (EnableTracing)
            {
                TraceLog.Add($"[8271] Read Drive Status -> 0x{statusByte:X2}");
            }
        }

        private void ExecuteSpecify()
        {
            _status = (byte)(_status & ~0x80); // Clear Busy
            _status = (byte)(_status & ~0x08);
            if (EnableTracing)
            {
                TraceLog.Add("[8271] Specify command processed");
            }
        }

        private void ExecuteWriteSpecialRegister()
        {
            if (_parameters.Count >= 2)
            {
                byte regNum = (byte)(_parameters[0] & 0x3F);
                byte val = _parameters[1];
                _specialRegisters[regNum] = val;
                if (EnableTracing)
                {
                    TraceLog.Add($"[8271] Write Special Register 0x{regNum:X2} = 0x{val:X2}");
                }
            }
            _status = (byte)(_status & ~0x80); // Clear Busy
        }

        private void ExecuteReadSpecialRegister()
        {
            _status = (byte)(_status & ~0x80); // Clear Busy
            _status |= 0x10;  // Result Ready
            byte regNum = _parameters.Count > 0 ? (byte)(_parameters[0] & 0x3F) : (byte)0;
            byte val = _specialRegisters[regNum];
            _resultQueue.Enqueue(val);

            if (EnableTracing)
            {
                TraceLog.Add($"[8271] Read Special Register 0x{regNum:X2} -> 0x{val:X2}");
            }
        }
    }
}
