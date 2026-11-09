with open('games/Disc999-StripPoker.ssd', 'rb') as f:
    data = f.read()

start = 2 * 256
load_data = data[start : start + 0x03CD]

pos = 0
while pos < len(load_data):
    if load_data[pos] != 0x0D:
        pos += 1
        continue
    if pos + 3 >= len(load_data):
        break
    line_num = (load_data[pos+1] << 8) | load_data[pos+2]
    line_len = load_data[pos+3]
    if line_num >= 0xFF00 or line_len == 0:
        break
    content = load_data[pos+4 : pos+line_len]
    text = ""
    for b in content:
        if 32 <= b <= 126:
            text += chr(b)
        elif b >= 0x80:
            text += f" <T_{b:02X}> "
        else:
            text += f" [{b:02X}] "
    print(f"{line_num}: {text}")
    pos += line_len
