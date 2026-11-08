data = open("roms/basic2.rom", "rb").read()
off = 0x3558
print("Bytes at $B558:")
print(" ".join(f"{b:02X}" for b in data[off:off+30]))
