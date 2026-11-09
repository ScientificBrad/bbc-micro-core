import os

data = open("roms/dfs09.rom", "rb").read()

# Scan for references to 0xFE80 - 0xFE84
fe80_refs = []
for i in range(len(data)-2):
    addr = data[i+1] | (data[i+2] << 8)
    if 0xFE80 <= addr <= 0xFE8F:
        op = data[i]
        fe80_refs.append((0x8000 + i, op, addr))

print(f"Found {len(fe80_refs)} references to 8271 registers ($FE80-$FE8F) in DFS 0.90:")
for pc, op, target in fe80_refs[:40]:
    print(f"  ${pc:04X}: op 0x{op:02X} target ${target:04X}")
