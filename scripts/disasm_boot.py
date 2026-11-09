import os

OPCODES = {
    0x00: ("BRK", 1), 0x01: ("ORA ($%02X,X)", 2), 0x05: ("ORA $%02X", 2), 0x06: ("ASL $%02X", 2),
    0x08: ("PHP", 1), 0x09: ("ORA #$%02X", 2), 0x0A: ("ASL A", 1), 0x0D: ("ORA $%04X", 3),
    0x0E: ("ASL $%04X", 3), 0x10: ("BPL $%04X", 2), 0x18: ("CLC", 1), 0x1D: ("ORA $%04X,X", 3),
    0x20: ("JSR $%04X", 3), 0x24: ("BIT $%02X", 2), 0x25: ("AND $%02X", 2), 0x26: ("ROL $%02X", 2),
    0x28: ("PLP", 1), 0x29: ("AND #$%02X", 2), 0x2A: ("ROL A", 1), 0x2C: ("BIT $%04X", 3),
    0x2D: ("AND $%04X", 3), 0x30: ("BMI $%04X", 2), 0x38: ("SEC", 1), 0x40: ("RTI", 1),
    0x48: ("PHA", 1), 0x4C: ("JMP $%04X", 3), 0x58: ("CLI", 1), 0x60: ("RTS", 1),
    0x68: ("PLA", 1), 0x6C: ("JMP ($%04X)", 3), 0x78: ("SEI", 1), 0x84: ("STY $%02X", 2),
    0x85: ("STA $%02X", 2), 0x86: ("STX $%02X", 2), 0x88: ("DEY", 1), 0x8A: ("TXA", 1),
    0x8C: ("STY $%04X", 3), 0x8D: ("STA $%04X", 3), 0x8E: ("STX $%04X", 3), 0x90: ("BCC $%04X", 2),
    0x91: ("STA ($%02X),Y", 2), 0x94: ("STY $%02X,X", 2), 0x95: ("STA $%02X,X", 2),
    0x98: ("TYA", 1), 0x99: ("STA $%04X,Y", 3), 0x9A: ("TXS", 1), 0x9D: ("STA $%04X,X", 3),
    0xA0: ("LDY #$%02X", 2), 0xA2: ("LDX #$%02X", 2), 0xA4: ("LDY $%02X", 2), 0xA5: ("LDA $%02X", 2),
    0xA6: ("LDX $%02X", 2), 0xA8: ("TAY", 1), 0xA9: ("LDA #$%02X", 2), 0xAA: ("TAX", 1),
    0xAC: ("LDY $%04X", 3), 0xAD: ("LDA $%04X", 3), 0xAE: ("LDX $%04X", 3), 0xB0: ("BCS $%04X", 2),
    0xB1: ("LDA ($%02X),Y", 2), 0xB5: ("LDA $%02X,X", 2), 0xB8: ("CLV", 1), 0xB9: ("LDA $%04X,Y", 3),
    0xBA: ("TSX", 1), 0xBD: ("LDA $%04X,X", 3), 0xBE: ("LDX $%04X,Y", 3), 0xC0: ("CPY #$%02X", 2),
    0xC6: ("DEC $%02X", 2), 0xC8: ("INY", 1), 0xC9: ("CMP #$%02X", 2), 0xCA: ("DEX", 1),
    0xCE: ("DEC $%04X", 3), 0xD0: ("BNE $%04X", 2), 0xD8: ("CLD", 1), 0xE0: ("CPX #$%02X", 2),
    0xE6: ("INC $%02X", 2), 0xE8: ("INX", 1), 0xEA: ("NOP", 1), 0xEE: ("INC $%04X", 3),
    0xF0: ("BEQ $%04X", 2), 0xF8: ("SED", 1)
}

data = open("games/boot_dump.bin", "rb").read()
pc = 0x6000
idx = 0
while idx < min(200, len(data)):
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
