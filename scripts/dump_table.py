boot = open("games/boot_dump.bin", "rb").read()
off = 0x6610 - 0x6000
for i in range(10):
    chunk = boot[off + i*4 : off + i*4 + 4]
    lo, hi = chunk[0], chunk[1]
    addr = lo | (hi << 8)
    print(f"Entry {i}: Routine=0x{addr:04X}, bytes: {chunk.hex()}")
