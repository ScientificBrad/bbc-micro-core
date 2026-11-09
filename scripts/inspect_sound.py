with open('games/Disc002-ChuckieEgg.ssd', 'rb') as f:
    data = f.read()

def disasm_around(offset, count=10):
    start = max(0, offset - 10)
    end = min(len(data), offset + count)
    print(f"Around 0x{offset:04X}:")
    for i in range(start, end):
        b = data[i]
        print(f" {b:02X}", end="")
    print("\n")

for off in [0x1B17, 0x1B30, 0x221D, 0x2254, 0x2927, 0x2A2A, 0x2FA9, 0x3395]:
    # Print 10 bytes before the call
    code = data[off-8 : off+3]
    hex_str = " ".join(f"{b:02X}" for b in code)
    print(f"Call at 0x{off:04X}: {hex_str}")
