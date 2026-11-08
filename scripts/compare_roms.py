import os

b1 = open(r'references\tobylobster\os120.bin', 'rb').read()
b2 = open(r'roms\os12.rom', 'rb').read()

diffs = []
for i in range(min(len(b1), len(b2))):
    if b1[i] != b2[i]:
        diffs.append((i, 0xC000 + i, b1[i], b2[i]))

print(f"Total byte differences: {len(diffs)}")
if diffs:
    addrs = [d[1] for d in diffs]
    print(f"Differences range: min=${min(addrs):04X}, max=${max(addrs):04X}")
    print("Address ranges of differences:")
    
    # Group contiguous diffs
    ranges = []
    start = diffs[0]
    prev = diffs[0]
    for d in diffs[1:]:
        if d[0] == prev[0] + 1:
            prev = d
        else:
            ranges.append((start, prev))
            start = d
            prev = d
    ranges.append((start, prev))
    
    for r in ranges:
        print(f"  ${r[0][1]:04X} - ${r[1][1]:04X} (offset 0x{r[0][0]:04X}-0x{r[1][0]:04X}, count={r[1][0]-r[0][0]+1})")
        # Sample text if printable
        s_toby = "".join(chr(b1[i]) if 32 <= b1[i] < 127 else "." for i in range(r[0][0], min(r[0][0]+64, r[1][0]+1)))
        s_os12 = "".join(chr(b2[i]) if 32 <= b2[i] < 127 else "." for i in range(r[0][0], min(r[0][0]+64, r[1][0]+1)))
        print(f"    Toby: {s_toby}")
        print(f"    os12: {s_os12}")
