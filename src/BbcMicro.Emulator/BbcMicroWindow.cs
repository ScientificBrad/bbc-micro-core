using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using BbcMicro.Core;

namespace BbcMicro.Emulator
{
    public sealed class BbcMicroWindow : Form
    {
        private readonly BbcMachine _machine;
        private readonly string? _initialCommand;

        // Signed int pixels allow Marshal.Copy without an invalid uint[] to int[] cast.
        private readonly int[] _framebuffer = new int[640 * 512];
        private readonly object _frameLock = new();

        private readonly Bitmap _displayBitmap;
        private Thread? _emuThread;
        private volatile bool _running = true;
        private volatile bool _paused = false;

        private int _currentMode = 7;
        private int _scale = 2; // Default 2x (1280x1024) or 1.5x (960x768)
        private double _fps = 50.0;
        private int _frameCount = 0;
        private readonly Stopwatch _fpsStopwatch = Stopwatch.StartNew();

        public BbcMicroWindow(BbcMachine machine, string? initialCommand = null, int scale = 2)
        {
            _machine = machine;
            _initialCommand = initialCommand;
            _scale = Math.Clamp(scale, 1, 3);

            Text = "Acorn BBC Micro Model B (1981) - Project 003";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            BackColor = Color.Black;
            KeyPreview = true;

            int targetW = 640 * _scale / 2; // scale=2 -> 640, scale=3 -> 960, scale=4 -> 1280
            int targetH = 512 * _scale / 2;
            if (_scale == 2) { targetW = 960; targetH = 768; } // comfortable 960x768 default
            else if (_scale == 1) { targetW = 640; targetH = 512; }
            else if (_scale == 3) { targetW = 1280; targetH = 1024; }

            ClientSize = new Size(targetW, targetH);
            _displayBitmap = new Bitmap(640, 512, PixelFormat.Format32bppRgb);

            // Hook Form events
            Load += OnWindowLoad;
            FormClosing += OnWindowClosing;
            MouseDown += (s, e) => Focus();
            KeyDown += OnWindowKeyDown;
            KeyUp += OnWindowKeyUp;
            Paint += OnWindowPaint;
        }

        private void OnWindowLoad(object? sender, EventArgs e)
        {
            _emuThread = new Thread(EmulationWorker)
            {
                Name = "BBCMicro_EmuThread",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            _emuThread.Start();
        }

        private void OnWindowClosing(object? sender, FormClosingEventArgs e)
        {
            _running = false;
            _emuThread?.Join(500);
            _displayBitmap.Dispose();
        }

        private void EmulationWorker()
        {
            // If an initial command was provided (e.g. *RUN CH_EGG), inject it
            if (!string.IsNullOrEmpty(_initialCommand))
            {
                Thread.Sleep(100);
                _machine.TypeCommand(_initialCommand);
            }

            WindowsWaveOut? audio = null;
            try
            {
                audio = new WindowsWaveOut();
                Console.WriteLine("[AUDIO] BBC SN76489AN -> Windows 44100 Hz mono output active");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[AUDIO] Windows playback unavailable: " + ex.Message);
            }
            // Boot happened before the audio device opened; discard that historic PCM.
            _machine.Bus.Sound.DiscardSamples();
            bool wasPaused = false;

            const int CYCLES_PER_FRAME = 40_000; // 40,000 cycles at 2 MHz = exactly 20 ms = 50 Hz
            long ticksPerFrame = Stopwatch.Frequency / 50; // 50 Hz display refresh
            Stopwatch frameTimer = Stopwatch.StartNew();

            while (_running)
            {
                long frameStart = frameTimer.ElapsedTicks;

                if (!_paused)
                {
                    _machine.RunCycles(CYCLES_PER_FRAME);
                    wasPaused = false;
                    // Sound is generated from actual emulated CPU-cycle PSG state.
                    short[] pcm = _machine.Bus.Sound.DrainSamples();
                    audio?.Queue(pcm);

                    byte mode = _machine.Bus.Ram[0x0355];
                    ushort screenStart = (ushort)(_machine.Bus.Crtc.ScreenStartAddress * 8);

                    uint[] frame = ScreenFramebufferRenderer.RenderScreen(
                        _machine.Bus.Ram,
                        mode,
                        _machine.Bus.MosRom,
                        _machine.Bus.Ula.Palette,
                        screenStart);

                    lock (_frameLock)
                    {
                        // MODE 7 renders 640x500; bitmap modes render 640x512.
                        // Clear any unused rows and only copy the pixels provided.
                        Array.Clear(_framebuffer, 0, _framebuffer.Length);
                        Buffer.BlockCopy(frame, 0, _framebuffer, 0,
                            Math.Min(frame.Length, _framebuffer.Length) * sizeof(int));
                        _currentMode = mode;
                    }

                    _frameCount++;
                    if (_fpsStopwatch.ElapsedMilliseconds >= 1000)
                    {
                        _fps = _frameCount * 1000.0 / _fpsStopwatch.ElapsedMilliseconds;
                        _frameCount = 0;
                        _fpsStopwatch.Restart();
                        UpdateTitle();
                    }

                    try
                    {
                        if (IsHandleCreated && !IsDisposed)
                        {
                            BeginInvoke(new Action(Invalidate));
                        }
                    }
                    catch (ObjectDisposedException) { }
                }

                if (_paused && !wasPaused)
                {
                    audio?.Stop();
                    _machine.Bus.Sound.DiscardSamples();
                    wasPaused = true;
                }

                // High-precision 50 Hz synchronization
                long targetTicks = frameStart + ticksPerFrame;
                while (frameTimer.ElapsedTicks < targetTicks)
                {
                    Thread.Yield();
                }
            }
            audio?.Dispose();
            Console.WriteLine($"[AUDIO] PSG writes: {_machine.Bus.Sound.WriteCount:N0}, nonzero PCM samples: {_machine.Bus.Sound.NonSilentSamplesEmitted:N0}, buffers queued: {audio?.Submitted ?? 0:N0}, dropped: {audio?.Dropped ?? 0:N0}");
        }

        private void UpdateTitle()
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(UpdateTitle)); } catch { }
                return;
            }

            string status = _paused ? "[PAUSED]" : "[RUNNING]";
            Text = $"Acorn BBC Micro Model B (1981) - MODE {_currentMode} | {_fps:F1} FPS | 2.00 MHz {status} | [ESC] Exit [Pause/Ctrl+P] Pause [F10] Reset";
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            // Do not consume arrow keys or Tab as Windows Forms dialog navigation keys;
            // pass them down to standard KeyDown/KeyUp handlers so the emulated machine receives them.
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || key == Keys.Tab)
            {
                return false;
            }
            return base.ProcessDialogKey(keyData);
        }

        private void OnWindowPaint(object? sender, PaintEventArgs e)
        {
            BitmapData data = _displayBitmap.LockBits(
                new Rectangle(0, 0, 640, 512),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppRgb);

            lock (_frameLock)
            {
                Marshal.Copy(_framebuffer, 0, data.Scan0, _framebuffer.Length);
            }

            _displayBitmap.UnlockBits(data);

            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.DrawImage(_displayBitmap, 0, 0, ClientSize.Width, ClientSize.Height);
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            // System Control Keys
            if (e.KeyCode == Keys.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Pause || (e.KeyCode == Keys.P && e.Control))
            {
                _paused = !_paused;
                UpdateTitle();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.F10 || e.KeyCode == Keys.F12)
            {
                _machine.Reset();
                UpdateTitle();
                e.Handled = true;
                return;
            }

            // Map physical key to BBC Micro internal keyboard matrix
            byte ikn = MapFormsKeyToInternalKey(e.KeyCode);
            if (ikn != 0xFF)
            {
                _machine.Bus.Keyboard.SetKeyDown(ikn, true);
                e.Handled = true;
            }

            // Also map PC Arrow keys to Chuckie Egg default movement keys (, . Z A)
            // Left  = Comma  (0x66)
            // Right = Period (0x67)
            // Up    = Z      (0x61)
            // Down  = A      (0x41)
            if (e.KeyCode == Keys.Left) _machine.Bus.Keyboard.SetKeyDown(0x66, true);
            else if (e.KeyCode == Keys.Right) _machine.Bus.Keyboard.SetKeyDown(0x67, true);
            else if (e.KeyCode == Keys.Up) _machine.Bus.Keyboard.SetKeyDown(0x61, true);
            else if (e.KeyCode == Keys.Down) _machine.Bus.Keyboard.SetKeyDown(0x41, true);
        }

        private void OnWindowKeyUp(object? sender, KeyEventArgs e)
        {
            byte ikn = MapFormsKeyToInternalKey(e.KeyCode);
            if (ikn != 0xFF)
            {
                _machine.Bus.Keyboard.SetKeyDown(ikn, false);
                e.Handled = true;
            }

            if (e.KeyCode == Keys.Left) _machine.Bus.Keyboard.SetKeyDown(0x66, false);
            else if (e.KeyCode == Keys.Right) _machine.Bus.Keyboard.SetKeyDown(0x67, false);
            else if (e.KeyCode == Keys.Up) _machine.Bus.Keyboard.SetKeyDown(0x61, false);
            else if (e.KeyCode == Keys.Down) _machine.Bus.Keyboard.SetKeyDown(0x41, false);
        }

        public static byte MapFormsKeyToInternalKey(Keys key)
        {
            return key switch
            {
                // Letters
                Keys.Q => 0x10,
                Keys.W => 0x21,
                Keys.E => 0x22,
                Keys.R => 0x33,
                Keys.T => 0x23,
                Keys.Y => 0x44,
                Keys.U => 0x35,
                Keys.I => 0x25,
                Keys.O => 0x36,
                Keys.P => 0x37,
                Keys.A => 0x41,
                Keys.S => 0x51,
                Keys.D => 0x32,
                Keys.F => 0x14,
                Keys.G => 0x53,
                Keys.H => 0x54,
                Keys.J => 0x45,
                Keys.K => 0x46,
                Keys.L => 0x56,
                Keys.Z => 0x61,
                Keys.X => 0x42,
                Keys.C => 0x52,
                Keys.V => 0x63,
                Keys.B => 0x55,
                Keys.N => 0x64,
                Keys.M => 0x65,

                // Digits
                Keys.D0 or Keys.NumPad0 => 0x27,
                Keys.D1 or Keys.NumPad1 => 0x30,
                Keys.D2 or Keys.NumPad2 => 0x31,
                Keys.D3 or Keys.NumPad3 => 0x11,
                Keys.D4 or Keys.NumPad4 => 0x12,
                Keys.D5 or Keys.NumPad5 => 0x13,
                Keys.D6 or Keys.NumPad6 => 0x34,
                Keys.D7 or Keys.NumPad7 => 0x24,
                Keys.D8 or Keys.NumPad8 => 0x15,
                Keys.D9 or Keys.NumPad9 => 0x26,

                // Directional / Cursor Keys
                Keys.Left => 0x19,
                Keys.Right => 0x79,
                Keys.Up => 0x39,
                Keys.Down => 0x29,

                // Control / Action Keys
                Keys.Space => 0x62,
                Keys.Enter or Keys.Return => 0x49,
                Keys.Tab => 0x60,
                Keys.Back => 0x59, // Delete
                Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => 0x00,
                Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => 0x01,

                // Punctuation
                Keys.Oemcomma => 0x66,
                Keys.OemPeriod => 0x67,
                Keys.OemQuestion => 0x68,
                Keys.OemSemicolon => 0x57,
                Keys.OemQuotes => 0x58,
                Keys.OemMinus => 0x17,
                Keys.Oemplus => 0x57,

                _ => 0xFF
            };
        }
    }
}
