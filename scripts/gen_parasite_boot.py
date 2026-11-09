# Python assembler for Parasite bootstrap
lines = """
F800: 78          SEI
F801: D8          CLD
F802: A2 FF       LDX #$FF
F804: 9A          TXS
F805: A9 00       LDA #$00
F807: 85 15       STA $15
F809: A9 08       LDA #$08
F80B: 85 16       STA $16

WaitR4:
F80D: 2C FE FE    BIT $FEFE
F810: 10 FB       BPL WaitR4
F812: AD FF FE    LDA $FEFF
F815: C9 01       CMP #$01
F817: F0 07       BEQ DoTransfer1
F819: C9 04       CMP #$04
F81B: F0 54       BEQ DoRelease
F81D: 4C 0D F8    JMP WaitR4

DoTransfer1:
F820: 2C FE FE    BIT $FEFE
F823: 10 FB       BPL F820
F825: AD FF FE    LDA $FEFF

F828: 2C FE FE    BIT $FEFE
F82B: 10 FB       BPL F828
F82D: AD FF FE    LDA $FEFF

F830: 2C FE FE    BIT $FEFE
F833: 10 FB       BPL F830
F835: AD FF FE    LDA $FEFF

F838: 2C FE FE    BIT $FEFE
F83B: 10 FB       BPL F838
F83D: AD FF FE    LDA $FEFF

F840: 2C FE FE    BIT $FEFE
F843: 10 FB       BPL F840
F845: AD FF FE    LDA $FEFF

F848: 2C FE FE    BIT $FEFE
F84B: 10 FB       BPL F848
F84D: AD FF FE    LDA $FEFF

LoopR3:
F850: 2C FC FE    BIT $FEFC
F853: 10 FB       BPL LoopR3
F855: AD FD FE    LDA $FEFD
F858: A0 00       LDY #$00
F85A: 91 15       STA ($15),Y
F85C: E6 15       INC $15
F85E: D0 02       BNE +2
F860: E6 16       INC $16
F862: A5 15       LDA $15
F864: C9 36       CMP #$36
F866: D0 E8       BNE LoopR3
F868: A5 16       LDA $16
F86A: C9 0A       CMP #$0A
F86C: D0 E2       BNE LoopR3
F86E: 4C 0D F8    JMP WaitR4

DoRelease:
F871: 2C FE FE    BIT $FEFE
F874: 10 FB       BPL F871
F876: AD FF FE    LDA $FEFF

F879: 2C FE FE    BIT $FEFE
F87C: 10 FB       BPL F879
F87E: AD FF FE    LDA $FEFF

F881: 2C FE FE    BIT $FEFE
F884: 10 FB       BPL F881
F886: AD FF FE    LDA $FEFF

F889: 2C FE FE    BIT $FEFE
F88C: 10 FB       BPL F889
F88E: AD FF FE    LDA $FEFF

F891: 2C FE FE    BIT $FEFE
F894: 10 FB       BPL F891
F896: AD FF FE    LDA $FEFF

F899: 2C FE FE    BIT $FEFE
F89C: 10 FB       BPL F899
F89E: AD FF FE    LDA $FEFF

F8A1: 2C FE FE    BIT $FEFE
F8A4: 50 FB       BVC F8A1
F8A6: A9 00       LDA #$00
F8A8: 8D FF FE    STA $FEFF

F8AB: 4C 00 08    JMP $0800
"""

bytes_list = []
for line in lines.strip().splitlines():
    line = line.strip()
    if not line or line.endswith(":"): continue
    parts = line.split(":")
    if len(parts) < 2: continue
    hex_part = parts[1].split()[0:parts[1].split().index(parts[1].split()[-1])]
    # collect hex bytes
    words = parts[1].strip().split()
    b_words = []
    for w in words:
        if len(w) == 2 and all(c in "0123456789ABCDEFabcdef" for c in w):
            b_words.append(f"0x{w.upper()}")
        else:
            break
    bytes_list.extend(b_words)

print(f"Total bytes: {len(bytes_list)}")
print(", ".join(bytes_list))
