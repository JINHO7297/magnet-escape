"""Original pixel art for Magnet Skater (legacy asset filenames preserve GUID references). Optional authoring dependency: Pillow.
Run from any directory; generated PNGs are committed and require no Python at runtime.
"""
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parents[1] / 'Assets/_MagnetEscape/Resources/Art'
OUT.mkdir(parents=True, exist_ok=True)
INK = '#101d2b'
STEEL = '#547180'
LIGHT = '#9db6bf'
CYAN = '#5af3df'
YELLOW = '#ffc65a'


def canvas(size):
    image = Image.new('RGBA', size, (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def save(image, name):
    image.save(OUT / (name + '.png'))


def skater(name, pose, frame=0):
    image, d = canvas((48, 64))
    bob = frame % 2 if pose == 'run' else 0
    crouch = 7 if pose == 'dash' else 0
    head = 6 + bob + crouch
    # Helmet, face and jacket: original skater silhouette facing right.
    d.ellipse((15, head, 33, head + 17), fill=INK)
    d.rectangle((18, head + 9, 33, head + 20), fill='#efbb91')
    d.rectangle((15, head + 3, 34, head + 10), fill=CYAN)
    d.rectangle((29, head + 12, 32, head + 14), fill=INK)
    d.line((20, head + 20, 28, 40), fill=CYAN, width=11)
    d.line((24, head + 23, 36, 36 if pose != 'jump' else 25), fill=LIGHT, width=5)
    if pose == 'fall':
        d.line((22, 31, 9, 18), fill=LIGHT, width=5)
        d.rectangle((6, 14, 11, 20), fill='#efbb91')
    d.line((25, 39, 18, 52), fill=INK, width=7)
    d.line((27, 40, 33, 52), fill=INK, width=7)
    if pose == 'run' and frame % 2:
        d.line((24, 40, 9, 53), fill=INK, width=6)
    d.rectangle((13, 51, 22, 54), fill=LIGHT)
    d.rectangle((28, 51, 38, 54), fill=LIGHT)
    # Magnetic board remains visible in every airborne pose.
    board = 55 if pose != 'jump' else 54
    d.rounded_rectangle((6, board, 43, board + 5), radius=2, fill=CYAN)
    d.rectangle((11, board + 5, 15, board + 8), fill=INK)
    d.rectangle((33, board + 5, 37, board + 8), fill=INK)
    if pose == 'dash':
        d.line((0, 36, 11, 36), fill=CYAN, width=3)
        d.line((0, 45, 9, 45), fill=YELLOW, width=2)
    save(image, name)


# Names are kept for serialized/prefab compatibility; these are now skater frames.
skater('robot_idle', 'idle')
for i in range(4):
    skater('robot_run_' + str(i), 'run', i)
for pose in ['jump', 'fall', 'dash']:
    skater('robot_' + pose, pose)

image, d = canvas((64, 32))
d.rectangle((0, 0, 63, 31), fill=INK)
d.rectangle((1, 3, 62, 30), fill='#344c5d')
d.rectangle((1, 1, 62, 4), fill=LIGHT)
d.rectangle((1, 5, 62, 9), fill=YELLOW)
for x in range(-8, 72, 16):
    d.polygon([(x, 5), (x + 7, 5), (x + 2, 9), (x - 5, 9)], fill=INK)
for x in [6, 57]:
    d.rectangle((x, 17, x + 2, 19), fill=LIGHT)
d.line((0, 30, 63, 30), fill='#223544', width=2)
save(image, 'platform')

image, d = canvas((32, 32))
d.rectangle((0, 26, 31, 31), fill=INK)
d.polygon([(2, 25), (15, 2), (28, 25)], fill='#ef7f3e')
d.line((10, 12, 20, 12), fill='#fff0ce', width=3)
d.line((6, 20, 25, 20), fill='#fff0ce', width=3)
save(image, 'spike')

image, d = canvas((64, 64))
# Patrol drone. Its circular sensor defines the existing hazard radius.
d.ellipse((5, 5, 59, 59), fill='#d45e64', outline=INK, width=3)
d.rounded_rectangle((13, 19, 51, 44), radius=6, fill=INK)
d.rectangle((18, 24, 46, 32), fill=LIGHT)
d.rectangle((26, 25, 35, 30), fill=YELLOW)
d.line((1, 10, 25, 10), fill=INK, width=4)
d.line((39, 10, 62, 10), fill=INK, width=4)
save(image, 'saw')

image, d = canvas((64, 64))
d.ellipse((2, 2, 61, 61), fill=INK)
d.ellipse((5, 5, 58, 58), fill=STEEL)
d.ellipse((10, 10, 53, 53), fill=INK)
d.rectangle((17, 16, 26, 38), fill='#ff6d7a')
d.rectangle((38, 16, 47, 38), fill=CYAN)
d.rectangle((19, 34, 45, 46), fill=LIGHT)
d.rectangle((17, 14, 26, 21), fill=LIGHT)
d.rectangle((38, 14, 47, 21), fill=LIGHT)
for x, y in [(29, 6), (29, 54), (6, 29), (54, 29)]:
    d.rectangle((x, y, x + 3, y + 3), fill=YELLOW)
save(image, 'anchor')

for active in [False, True]:
    image, d = canvas((32, 64))
    d.rectangle((12, 6, 18, 59), fill=STEEL)
    d.rectangle((6, 57, 26, 63), fill=INK)
    d.rectangle((7, 57, 25, 59), fill=LIGHT)
    d.rectangle((15, 7, 29, 26), fill=CYAN if active else YELLOW)
    d.polygon([(19, 11), (19, 22), (26, 16)], fill=INK)
    save(image, 'checkpoint_on' if active else 'checkpoint_off')

image, d = canvas((64, 128))
d.rectangle((8, 0, 13, 127), fill=LIGHT)
d.rectangle((13, 4, 61, 45), fill=CYAN)
for y in range(4, 45, 8):
    for x in range(13, 61, 8):
        if ((x - 13) // 8 + (y - 4) // 8) % 2 == 0:
            d.rectangle((x, y, min(x + 7, 61), min(y + 7, 45)), fill=INK)
d.rectangle((0, 123, 30, 127), fill=INK)
save(image, 'exit')

image, d = canvas((512, 256))
d.rectangle((0, 0, 511, 255), fill='#14283e')
# Repeating city facades: decorative and non-colliding.
for x, top, width in [(0, 20, 80), (86, 65, 66), (158, 5, 100), (264, 48, 76), (346, 12, 94), (446, 82, 65)]:
    d.rectangle((x, top, x + width, 255), fill='#20394f')
    d.rectangle((x, top, x + width, top + 3), fill='#31576c')
    for wy in range(top + 16, 250, 28):
        for wx in range(x + 10, x + width - 5, 20):
            d.rectangle((wx, wy, wx + 7, wy + 10), fill='#427579' if (wx + wy) % 3 else '#bd9e64')
d.line((0, 253, 511, 253), fill='#28485f', width=3)
save(image, 'factory_backdrop')
print('Generated', len(list(OUT.glob('*.png'))), 'original skater/city sprites; legacy GUIDs retained.')
