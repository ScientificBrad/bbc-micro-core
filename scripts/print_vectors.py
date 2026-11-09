mos = open("roms/os12.rom", "rb").read()
for vec, name in [
    (0xFFF4, "OSBYTE"),
    (0xFFF1, "OSWORD"),
    (0xFFEE, "OSWRCH"),
    (0xFFE7, "OSNEWL"),
    (0xFFE3, "OSASCI"),
    (0xFFE0, "OSRDCH"),
    (0xFFCE, "OSFIND"),
    (0xFFDA, "OSARGS"),
    (0xFFD7, "OSBGET"),
    (0xFFD4, "OSBPUT"),
    (0xFFD1, "OSGBPB"),
]:
    lo = mos[vec - 0xC000]
    hi = mos[vec + 1 - 0xC000]
    target = lo | (hi << 8)
    print(f"{name} ({hex(vec)}): target = {hex(target)}")
