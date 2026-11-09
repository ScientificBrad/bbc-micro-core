import os
from disasm_boot import OPCODES

data = open("games/boot_dump.bin", "rb").read()
pc = 0x66FA
idx = pc - 0x6000
while idx < min(len(data), idx + 250):
    b = data[idx]
    if b in OPCODES:
        name, length = OPCODES[b]
        if length == 1:
            print(f"${pc:04X}: {b:02X}        {name}")
        elif length == 2:
            op1 = data[idx+1]
            if name.startswith("B"): # branch relative
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
