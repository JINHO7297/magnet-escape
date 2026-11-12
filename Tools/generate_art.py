"""Original pixel art for Magnet Escape. Optional authoring dependency: Pillow.
Run from any directory; generated PNGs are committed and require no Python at runtime.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import math

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


def robot(name, pose, frame=0):
    image, d = canvas((48, 64))
    bob = frame % 2 if pose == 'run' else 0
    d.rectangle((12, 5 + bob, 35, 30 + bob), fill=INK)
    d.rectangle((14, 7 + bob, 33, 28 + bob), fill=LIGHT)
    d.rectangle((15, 8 + bob, 32, 12 + bob), fill='#d7e4e0')
    d.rectangle((15, 15 + bob, 35, 25 + bob), fill=INK)
    d.rectangle((22, 18 + bob, 26, 21 + bob), fill=CYAN)
    d.rectangle((30, 18 + bob, 34, 21 + bob), fill=CYAN)
    d.rectangle((21, 1 + bob, 24, 6 + bob), fill=STEEL)
    d.rectangle((20, 0 + bob, 25, 2 + bob), fill=YELLOW)
    d.rectangle((13, 30 + bob, 34, 48 + bob), fill=INK)
    d.rectangle((15, 32 + bob, 32, 46 + bob), fill=STEEL)
    # Horseshoe magnet on chest.
    d.rectangle((19, 34 + bob, 22, 42 + bob), fill='#ff6d7a')
    d.rectangle((27, 34 + bob, 30, 42 + bob), fill=CYAN)
    d.rectangle((20, 41 + bob, 29, 44 + bob), fill=LIGHT)
    d.rectangle((19, 33 + bob, 22, 35 + bob), fill=LIGHT)
    d.rectangle((27, 33 + bob, 30, 35 + bob), fill=LIGHT)
    if pose == 'dash':
        d.rectangle((1, 33, 13, 38), fill=CYAN)
        d.rectangle((3, 42, 12, 45), fill=LIGHT)
        d.rectangle((33, 32, 43, 37), fill=STEEL)
        legs = [(14, 49, 23, 54), (26, 47, 40, 52)]
    elif pose == 'jump':
        d.rectangle((7, 24, 12, 38), fill=STEEL)
        d.rectangle((35, 23, 40, 37), fill=STEEL)
        legs = [(13, 48, 21, 54), (27, 47, 35, 53)]
    elif pose == 'fall':
        d.rectangle((7, 17, 12, 35), fill=STEEL)
        d.rectangle((35, 17, 40, 35), fill=STEEL)
        legs = [(15, 48, 22, 59), (27, 48, 34, 59)]
    else:
        d.rectangle((7, 32 + bob, 12, 45 + bob), fill=STEEL)
        d.rectangle((35, 32 + bob, 40, 45 + bob), fill=STEEL)
        step = [0, 3, 0, -3][frame % 4] if pose == 'run' else 0
        legs = [(15 - step, 48 + bob, 22 - step, 59), (27 + step, 48 + bob, 34 + step, 59)]
    for x1, y1, x2, y2 in legs:
        d.rectangle((x1, y1, x2, y2), fill=INK)
        d.rectangle((x1 + 1, y1, x2 - 1, y2 - 3), fill=STEEL)
        d.rectangle((x1 - 2, y2 - 3, x2 + 2, y2), fill=LIGHT)
    save(image, name)


robot('robot_idle', 'idle')
for i in range(4):
    robot('robot_run_' + str(i), 'run', i)
for pose in ['jump', 'fall', 'dash']:
    robot('robot_' + pose, pose)

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
d.rectangle((0, 26, 31, 31), fill=STEEL)
d.polygon([(1, 25), (15, 2), (29, 25)], fill=INK)
d.polygon([(5, 24), (15, 5), (25, 24)], fill='#c7d6d9')
d.polygon([(15, 5), (25, 24), (15, 24)], fill='#f07277')
save(image, 'spike')

image, d = canvas((64, 64))
points = []
for i in range(48):
    angle = i * math.pi / 24
    radius = 30 if i % 4 < 2 else 23
    points.append((32 + math.cos(angle) * radius, 32 + math.sin(angle) * radius))
d.polygon(points, fill=LIGHT, outline=INK)
d.ellipse((13, 13, 51, 51), fill=STEEL, outline=INK, width=3)
d.ellipse((23, 23, 41, 41), fill=YELLOW, outline=INK, width=3)
d.rectangle((30, 27, 34, 37), fill=INK)
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
d.rectangle((4, 0, 59, 127), fill=INK)
d.rectangle((9, 5, 54, 126), fill=STEEL)
d.rectangle((15, 20, 48, 124), fill='#153f43')
d.rectangle((18, 22, 45, 122), fill='#236c67')
for y in range(24, 124, 8):
    d.line((18, y, 45, y), fill='#2e847b')
d.rectangle((15, 7, 48, 14), fill=CYAN)
d.polygon([(23, 62), (34, 62), (34, 55), (44, 68), (34, 80), (34, 73), (23, 73)], fill=CYAN)
save(image, 'exit')

image, d = canvas((512, 256))
d.rectangle((0, 0, 511, 255), fill='#101d2b')
for x in range(0, 512, 64):
    d.line((x, 0, x, 256), fill='#172b39', width=2)
    for y in range(0, 256, 64):
        d.line((x, y, x + 64, y), fill='#172b39', width=2)
for x in [30, 200, 380]:
    d.rectangle((x, 48, x + 64, 255), fill='#1a2f3d')
    d.rounded_rectangle((x + 3, 42, x + 61, 200), radius=20, fill='#213a48')
    d.rectangle((x + 20, 24, x + 28, 55), fill='#294452')
    for y in [78, 160]:
        d.rectangle((x + 3, y, x + 61, y + 5), fill='#294452')
d.line([(0, 30), (128, 30), (128, 110), (300, 110), (300, 0)], fill='#294452', width=8)
for x in range(0, 512, 128):
    d.rectangle((x + 5, 12, x + 28, 16), fill='#537b75')
save(image, 'factory_backdrop')

print('Generated', len(list(OUT.glob('*.png'))), 'original sprites.')
