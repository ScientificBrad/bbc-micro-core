import os

data = open("roms/basic2.rom", "rb").read()

# Let's search for '>' (0x3E) in basic2.rom
for i, b in enumerate(data):
    if b == ord('>'):
        print(f"Found '>' at offset 0x{i:04X} ($80{i:02X})")
        # Dump surrounding instructions
        start = max(0, i-10)
        end = min(len(data), i+15)
        print(" ", " ".join(f"{x:02X}" for x in data[start:end]))
