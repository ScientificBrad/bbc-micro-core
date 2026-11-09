import os

data = open("games/boot_dump.bin", "rb").read()

# Let's inspect from 0x6000 onwards
print("Boot code length:", len(data))

# Let's search where the subroutines at 0x600C-0x601E are:
# 600C: JSR $66EA
# 600F: JSR $6620
# 6012: JSR $6643
# 6015: JSR $6773
# 6018: JSR $66BE
# 601B: JSR $66FA
# 601E: JMP $601E

for target in [0x66EA, 0x6620, 0x6643, 0x6773, 0x66BE, 0x66FA]:
    off = target - 0x6000
    print(f"\n--- Routine at ${target:04X} (offset 0x{off:04X}) ---")
    chunk = data[off : off + 64]
    print(" ".join(f"{b:02X}" for b in chunk[:32]))
