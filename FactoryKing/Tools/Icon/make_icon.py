"""Generates the original Factory King app icon (1024x1024, RGB, no transparency) with Pillow.
Usage: python3 make_icon.py <output.png>"""
import math, sys
from PIL import Image, ImageDraw, ImageFilter

S = 1024
SS = 2  # supersampling for smooth edges
W = S * SS

def lerp(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))

img = Image.new("RGB", (W, W))
px = img.load()
top, bottom = (255, 168, 54), (214, 78, 32)
for y in range(W):
    c = lerp(top, bottom, y / W)
    for x in range(W):
        px[x, y] = c
d = ImageDraw.Draw(img)

# soft radial glow behind the emblem
glow = Image.new("L", (W, W), 0)
gd = ImageDraw.Draw(glow)
gd.ellipse([W*0.12, W*0.10, W*0.88, W*0.86], fill=120)
glow = glow.filter(ImageFilter.GaussianBlur(W * 0.08))
img = Image.composite(Image.new("RGB", (W, W), (255, 226, 150)), img, glow)
d = ImageDraw.Draw(img)

def gear(cx, cy, r_out, r_in, teeth, color, hole_color):
    pts = []
    for i in range(teeth * 4):
        a = i / (teeth * 4) * 2 * math.pi
        r = r_out if (i % 4) in (1, 2) else r_in
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    d.polygon(pts, fill=color)
    d.ellipse([cx - r_in*0.45, cy - r_in*0.45, cx + r_in*0.45, cy + r_in*0.45], fill=hole_color)

dark = (46, 52, 66)
# factory silhouette
base_y = W * 0.80
d.rectangle([W*0.16, W*0.52, W*0.84, base_y], fill=dark)
for i in range(3):  # saw-tooth roof
    x0 = W*0.16 + i * W*0.2266
    d.polygon([(x0, W*0.52), (x0 + W*0.2266, W*0.52), (x0 + W*0.2266, W*0.40)], fill=dark)
d.rectangle([W*0.72, W*0.33, W*0.79, W*0.52], fill=dark)  # chimney
d.rectangle([W*0.705, W*0.31, W*0.805, W*0.345], fill=dark)
for i in range(4):  # lit windows
    x = W*0.22 + i * W*0.15
    d.rounded_rectangle([x, W*0.60, x + W*0.09, W*0.70], radius=W*0.012, fill=(255, 214, 102))
# gear
gear(W*0.34, W*0.43, W*0.13, W*0.105, 10, (240, 244, 250), dark)
# crown on top
cx, cy, cw, ch = W*0.52, W*0.12, W*0.36, W*0.15
crown = [(cx - cw/2, cy + ch), (cx - cw/2, cy), (cx - cw/4, cy + ch*0.45), (cx, cy - ch*0.25),
         (cx + cw/4, cy + ch*0.45), (cx + cw/2, cy), (cx + cw/2, cy + ch)]
d.polygon(crown, fill=(255, 211, 61), outline=(150, 92, 10), width=int(W*0.008))
for (x, y) in [(cx - cw/2, cy), (cx, cy - ch*0.25), (cx + cw/2, cy)]:
    r = W*0.022
    d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 236, 140), outline=(150, 92, 10), width=int(W*0.005))
d.rectangle([cx - cw/2, cy + ch*0.82, cx + cw/2, cy + ch], fill=(230, 170, 30))

img = img.resize((S, S), Image.LANCZOS)
img.save(sys.argv[1] if len(sys.argv) > 1 else "AppIcon.png", "PNG")
