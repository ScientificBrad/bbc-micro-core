with open('games/Disc999-StuntCarRacerSTD.ssd', 'rb') as f:
    data = f.read()

boot = data[2 * 256 : 2 * 256 + 0x100]

pos = 0
while pos < 0x80:
    pc = 0x1900 + pos
    op = boot[pos]
    if op == 0x20:
        tgt = boot[pos+1] | (boot[pos+2] << 8)
        print(f"${pc:04X}: JSR ${tgt:04X}")
        pos += 3
    elif op == 0xA9:
        print(f"${pc:04X}: LDA #${boot[pos+1]:02X}")
        pos += 2
    elif op == 0xA2:
        print(f"${pc:04X}: LDX #${boot[pos+1]:02X}")
        pos += 2
    elif op == 0xA0:
        print(f"${pc:04X}: LDY #${boot[pos+1]:02X}")
        pos += 2
    elif op == 0xE0:
        print(f"${pc:04X}: CPX #${boot[pos+1]:02X}")
        pos += 2
    elif op == 0x90:
        offset = boot[pos+1]
        tgt = pc + 2 + (offset if offset < 128 else offset - 256)
        print(f"${pc:04X}: BCC ${tgt:04X}")
        pos += 2
    elif op == 0xF0:
        offset = boot[pos+1]
        tgt = pc + 2 + (offset if offset < 128 else offset - 256)
        print(f"${pc:04X}: BEQ ${tgt:04X}")
        pos += 2
    elif op == 0xD0:
        offset = boot[pos+1]
        tgt = pc + 2 + (offset if offset < 128 else offset - 256)
        print(f"${pc:04X}: BNE ${tgt:04X}")
        pos += 2
    elif op == 0x4C:
        tgt = boot[pos+1] | (boot[pos+2] << 8)
        print(f"${pc:04X}: JMP ${tgt:04X}")
        pos += 3
    elif op == 0x60:
        print(f"${pc:04X}: RTS")
        pos += 1
    elif op == 0x00:
        print(f"${pc:04X}: BRK")
        pos += 1
    else:
        print(f"${pc:04X}: byte 0x{op:02X}")
        pos += 1
