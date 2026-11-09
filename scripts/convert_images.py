import os
from PIL import Image

images_dir = r"D:\Emulation\BBCMicro\docs\images"
bmps = [f for f in os.listdir(images_dir) if f.endswith(".bmp")]

for bmp in sorted(bmps):
    bmp_path = os.path.join(images_dir, bmp)
    png_path = os.path.join(images_dir, os.path.splitext(bmp)[0] + ".png")
    im = Image.open(bmp_path)
    im.save(png_path, "PNG", optimize=True)
    print(f"Converted {bmp} -> {os.path.basename(png_path)} ({im.size})")

# Also copy 03_chuckie_egg_gameplay.png to screenshots/chuckie_egg_gameplay.png
im_chuckie = Image.open(os.path.join(images_dir, "03_chuckie_egg_gameplay.bmp"))
im_chuckie.save(r"D:\Emulation\BBCMicro\screenshots\chuckie_egg_gameplay.png", "PNG", optimize=True)
print("Updated screenshots/chuckie_egg_gameplay.png")
