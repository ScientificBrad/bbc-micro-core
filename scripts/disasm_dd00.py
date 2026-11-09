import sys
sys.path.append('scripts')
from disasm_boot import OPCODES

data = open('roms/os12.rom', 'rb').read()

def disasm(start_pc, max_len=60):
    pc = start_pc
    idx = pc - 0xC000
    end_idx = min(len(data), idx + max_len)
    print(f"\n=== Disassembly at 0x{start_pc:04X} ===")
    while idx < end_idx:
        b = data[idx]
        if b in OPCODES:
            name, length = OPCODES[b]
            if length == 1:
                print(f"${pc:04X}: {b:02X}        {name}")
            elif length == 2:
                op1 = data[idx+1]
                if name.startswith('B'):
                    offset = op1 if op1 < 128 else op1 - 256
                    target = pc + 2 + offset
                    print(f"${pc:04X}: {b:02X} {op1:02X}     {name.split()[0]} ${target:04X}")
                else:
                    print(f"${pc:04X}: {b:02X} {op1:02X}     {name % op1}")
            elif length == 3:
                op1 = data[idx+1]
                op2 = data[idx+2]
                target = op1 | (op2 << 8)
                print(f"${pc:04X}: {b:02X} {op1:02X} {op2:02X}  {name % target}")
            idx += length
            pc += length
        else:
            print(f"${pc:04X}: {b:02X}        .BYTE ${b:02X}")
            idx += 1
            pc += 1

disasm(0xDCF0, 60)
