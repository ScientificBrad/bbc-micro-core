import struct

def analyze_all():
    print("=== Analyzing ROM internals ===")
    
    # 1. MOS 1.20 Entry Points & Jump Table
    os_data = open("roms/os12.rom", "rb").read()
    print("MOS Hardware vectors:")
    print(f"  NMI:   0x{struct.unpack('<H', os_data[0x3FFA:0x3FFC])[0]:04X}")
    print(f"  RESET: 0x{struct.unpack('<H', os_data[0x3FFC:0x3FFE])[0]:04X}")
    print(f"  IRQ:   0x{struct.unpack('<H', os_data[0x3FFE:0x4000])[0]:04X}")
    
    # Standard entry points at top of ROM
    # $FFB6 - $FFF7
    os_api = {
        0xFFB6: "OSDATA",
        0xFFB9: "OSEVEN",
        0xFFBC: "NVWRCH",
        0xFFBF: "NVRDCH",
        0xFFC2: "OSFIND_OLD",
        0xFFC5: "OSGBPB_OLD",
        0xFFC8: "OSBPUT_OLD",
        0xFFCB: "OSBGET_OLD",
        0xFFCE: "OSFIND",
        0xFFD1: "OSGBPB",
        0xFFD4: "OSBPUT",
        0xFFD7: "OSBGET",
        0xFFDA: "OSARGS",
        0xFFDD: "OSFILE",
        0xFFE0: "OSRDCH",
        0xFFE3: "OSASCI",
        0xFFE7: "OSNEWL",
        0xFFEE: "OSWRCH",
        0xFFF1: "OSWORD",
        0xFFF4: "OSBYTE",
        0xFFF7: "OSCLI"
    }
    print("\nMOS Vector Entry Points ($FFB6-$FFF7):")
    for addr in sorted(os_api.keys()):
        off = addr - 0xC000
        op = os_data[off]
        tgt = os_data[off+1] | (os_data[off+2] << 8) if off + 2 < len(os_data) else 0
        print(f"  ${addr:04X} ({os_api[addr]}): op 0x{op:02X}, target 0x{tgt:04X}")

    # 2. Sideways ROM calls into MOS
    # We scan basic2.rom and dfs09.rom for JSR / JMP to $FFxx
    for rom_name, path in [("BASIC II", "roms/basic2.rom"), ("DFS 0.90", "roms/dfs09.rom")]:
        data = open(path, "rb").read()
        os_calls = {}
        for i in range(len(data)-2):
            op = data[i]
            if op in (0x20, 0x4C): # JSR or JMP absolute
                tgt = data[i+1] | (data[i+2] << 8)
                if 0xFF00 <= tgt <= 0xFFFF:
                    sym = os_api.get(tgt, f"${tgt:04X}")
                    os_calls[sym] = os_calls.get(sym, 0) + 1
        print(f"\n{rom_name} OS calls:")
        for sym, count in sorted(os_calls.items()):
            print(f"  {sym}: {count} calls")

analyze_all()
