import sys
sys.path.append('scripts')
from disasm_boot import OPCODES

os_rom = open('roms/os12.rom', 'rb').read()

# Let's find OSBYTE table or handler for 129 (0x81)
# In MOS 1.20, OSBYTE dispatcher is around $DF00-$E000 or table of handlers
# Let's search for CPX #$FF or similar in OSBYTE
# Or let's search for 0x81 in os_rom
pos = 0
while True:
    pos = os_rom.find(bytes([0xC9, 0x81]), pos) # CMP #$81
    if pos == -1: break
    addr = 0xC000 + pos
    print(f"Found CMP #$81 at 0x{addr:04X}")
    pos += 2
