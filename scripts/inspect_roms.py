import os
import hashlib
import zlib
import struct

def crc32_file(filepath):
    prev = 0
    with open(filepath, 'rb') as f:
        while chunk := f.read(65536):
            prev = zlib.crc32(chunk, prev)
    return f"{prev & 0xFFFFFFFF:08X}"

def analyze_rom(filename, base_dir="roms"):
    filepath = os.path.join(base_dir, filename)
    data = open(filepath, "rb").read()
    size = len(data)
    md5 = hashlib.md5(data).hexdigest()
    sha256 = hashlib.sha256(data).hexdigest()
    crc = crc32_file(filepath)
    print("=" * 60)
    print(f"ROM File: {filename}")
    print(f"Size: {size} bytes (0x{size:04X})")
    print(f"MD5: {md5}")
    print(f"SHA-256: {sha256}")
    print(f"CRC32: {crc}")

    if filename == "os12.rom":
        # MOS ROM mapped $C000-$FFFF
        nmi = data[0x3FFA] | (data[0x3FFB] << 8)
        reset = data[0x3FFC] | (data[0x3FFD] << 8)
        irq = data[0x3FFE] | (data[0x3FFF] << 8)
        print(f"Architecture: MOS 6502 Operating System ($C000-$FFFF)")
        print(f"Hardware Vectors:")
        print(f"  NMI:   0x{nmi:04X} (offset 0x3FFA)")
        print(f"  RESET: 0x{reset:04X} (offset 0x3FFC)")
        print(f"  IRQ:   0x{irq:04X} (offset 0x3FFE)")
        
        # Check standard MOS entry points (at top of MOS $FFxx)
        entries = [
            ("OSFIND", 0x3FCE, 0xFFCE),
            ("OSGBPB", 0x3FD1, 0xFFD1),
            ("OSBPUT", 0x3FD4, 0xFFD4),
            ("OSBGET", 0x3FD7, 0xFFD7),
            ("OSARGS", 0x3FDA, 0xFFDA),
            ("OSFILE", 0x3FDD, 0xFFDD),
            ("OSRDCH", 0x3FE0, 0xFFE0),
            ("OSASCI", 0x3FE3, 0xFFE3),
            ("OSNEWL", 0x3FE7, 0xFFE7),
            ("OSWRCH", 0x3EEB, 0xFFEE), # wait, let's check standard table
            ("OSWORD", 0x3FF1, 0xFFF1),
            ("OSBYTE", 0x3FF4, 0xFFF4),
            ("OSCLI",  0x3FF7, 0xFFF7),
        ]
        print("OS Entry Point Vectors:")
        for name, off, addr in [
            ("OSVDU",  0x3FD7, 0xFFD7), # wait let's look at exact addresses
        ]:
            pass
        # Let's inspect bytes from 0x3FCE to 0x3FFA
        print("Standard Jump Table ($FFB6-$FFF7):")
        for addr in range(0xFFB6, 0xFFFA, 3):
            off = addr - 0xC000
            op = data[off]
            target = data[off+1] | (data[off+2] << 8)
            print(f"  ${addr:04X}: {op:02X} {target:04X}")

    else:
        # Sideways ROM mapped $8000-$BFFF
        print(f"Architecture: Sideways ROM ($8000-$BFFF / {0x8000+size-1:04X})")
        lang_op = data[0]
        lang_target = data[1] | (data[2] << 8)
        serv_op = data[3]
        serv_target = data[4] | (data[5] << 8)
        rom_type = data[6]
        copy_off = data[7]
        version = data[8]
        
        title_chars = []
        i = 9
        while i < len(data) and data[i] != 0:
            title_chars.append(chr(data[i]))
            i += 1
        title = "".join(title_chars)

        copy_chars = []
        i = copy_off
        while i < len(data) and data[i] != 0:
            copy_chars.append(chr(data[i]))
            i += 1
        copy = "".join(copy_chars)

        print(f"Language Entry: 0x{lang_op:02X} target 0x{lang_target:04X}")
        print(f"Service Entry:  0x{serv_op:02X} target 0x{serv_target:04X}")
        print(f"ROM Type: 0x{rom_type:02X} (Language={bool(rom_type & 0x80)}, Service={bool(rom_type & 0x40)}, 6502={bool(rom_type & 0x04 or not (rom_type & 0x3F))})")
        print(f"Copyright Offset: 0x{copy_off:02X} (Address ${0x8000 + copy_off:04X})")
        print(f"Binary Version: {version}")
        print(f"Title: '{title}'")
        print(f"Copyright String: '{copy}'")

if __name__ == "__main__":
    for r in ["os12.rom", "basic2.rom", "dfs09.rom"]:
        analyze_rom(r)
