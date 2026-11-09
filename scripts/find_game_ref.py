import os

data = open("games/boot_dump.bin", "rb").read()

# Let's search for references to string "Game"
game_pos = data.find(b'Game')
print("Position of 'Game':", hex(game_pos), "addr:", hex(0x6000 + game_pos))

# Dump 64 bytes before and after
print("Surrounding bytes:")
start = max(0, game_pos - 64)
end = min(len(data), game_pos + 64)
print(" ".join(f"{b:02X}" for b in data[start:end]))
