import sys
sys.path.append('scripts')
from disasm_boot import OPCODES

os_rom = open('roms/os12.rom', 'rb').read()

pos = 0
while True:
    pos = os_rom.find(bytes([0x79, 0x02]), pos) # $0279
    if pos == -1: break
    addr = 0xC000 + pos - 1
    opcode = os_rom[pos - 1]
    print(f"Ref to 0x0279 at 0x{addr:04X}: opcode=0x{opcode:02X}")
    pos += 2
