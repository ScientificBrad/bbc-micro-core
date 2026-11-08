import os

def analyze_strings(path, min_len=4):
    data = open(path, "rb").read()
    strings = []
    current = []
    curr_addr = 0
    
    for i, b in enumerate(data):
        if 32 <= b < 127:
            if not current:
                curr_addr = i
            current.append(chr(b))
        else:
            if len(current) >= min_len:
                strings.append((curr_addr, "".join(current)))
            current = []
    if len(current) >= min_len:
        strings.append((curr_addr, "".join(current)))
    return strings

print("--- BASIC II Strings ---")
basic_strings = analyze_strings("roms/basic2.rom", min_len=4)
for off, s in basic_strings[:30]:
    print(f"${0x8000+off:04X} (off 0x{off:04X}): {s}")

print("\n--- DFS 0.90 Strings ---")
dfs_strings = analyze_strings("roms/dfs09.rom", min_len=4)
for off, s in dfs_strings[:30]:
    print(f"${0x8000+off:04X} (off 0x{off:04X}): {s}")
