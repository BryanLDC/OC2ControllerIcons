# Generates the mod's generic (layout-agnostic) button icons.
# Usage: python tools/make_icons.py  -> writes PNGs to res/
# 100% original art (geometric shapes); nothing is derived from game assets.
# Requires: Pillow (pip install pillow)
import os
from PIL import Image, ImageDraw

SS = 4  # supersampling
OUT = os.path.join(os.path.dirname(__file__), "..", "res")

DARK = (52, 54, 58, 255)        # button body (dark grey, matching the game's icons)
DARK_EDGE = (30, 31, 34, 255)
WHITE = (245, 245, 245, 255)
BORDER = (255, 255, 255, 255)


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def finish(img, name):
    w, h = img.size
    img = img.resize((w // SS, h // SS), Image.LANCZOS)
    img.save(os.path.join(OUT, name + ".png"))


def circle(d, cx, cy, r, fill=None, outline=None, width=0):
    d.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS],
              fill=fill, outline=outline, width=int(width * SS))


def rrect(d, x0, y0, x1, y1, rad, fill=None, outline=None, width=0):
    d.rounded_rectangle([x0 * SS, y0 * SS, x1 * SS, y1 * SS], radius=rad * SS,
                        fill=fill, outline=outline, width=int(width * SS))


def plate_circle(d, size, bordered):
    c = size / 2
    if bordered:
        circle(d, c, c, c - 1, fill=BORDER)
        circle(d, c, c, c - 8, fill=DARK)
    else:
        circle(d, c, c, c - 1, fill=DARK_EDGE)
        circle(d, c, c, c - 4, fill=DARK)


def face(target, bordered):
    # target: 'S','W','E','N' (south/west/east/north)
    size = 128
    img = canvas(size, size)
    d = ImageDraw.Draw(img)
    plate_circle(d, size, bordered)
    c = size / 2
    off = 27 if bordered else 29
    r = 11.5 if bordered else 12.5
    pos = {"N": (c, c - off), "S": (c, c + off), "W": (c - off, c), "E": (c + off, c)}
    for k, (x, y) in pos.items():
        if k == target:
            circle(d, x, y, r, fill=WHITE)
        else:
            circle(d, x, y, r, outline=WHITE, width=3.6)
    return img


def plate_rect(d, w, h, bordered):
    if bordered:
        rrect(d, 1, 1, w - 1, h - 1, 26, fill=BORDER)
        rrect(d, 8, 8, w - 8, h - 8, 20, fill=DARK)
    else:
        rrect(d, 1, 1, w - 1, h - 1, 24, fill=DARK_EDGE)
        rrect(d, 4, 4, w - 4, h - 4, 21, fill=DARK)


def shoulders(target, bordered):
    # target: 'LB','RB','LT','RT' -> top view: triggers above, bumpers below
    w, h = 160, 112
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    plate_rect(d, w, h, bordered)
    sw = 3.6
    shapes = {
        "LT": ("rr", 26, 22, 66, 56, 11),
        "RT": ("rr", w - 66, 22, w - 26, 56, 11),
        "LB": ("rr", 20, 66, 72, 88, 11),
        "RB": ("rr", w - 72, 66, w - 20, 88, 11),
    }
    for k, (_, x0, y0, x1, y1, rad) in shapes.items():
        if k == target:
            rrect(d, x0, y0, x1, y1, rad, fill=WHITE)
        else:
            rrect(d, x0, y0, x1, y1, rad, outline=WHITE, width=sw)
    return img


def sticks(target, bordered):
    # target: 'L','R' -> two sticks, the pressed one filled
    w, h = 160, 112
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    plate_rect(d, w, h, bordered)
    cy = h / 2
    for k, cx in (("L", 50), ("R", w - 50)):
        if k == target:
            circle(d, cx, cy, 25, fill=WHITE)
            circle(d, cx, cy, 13, fill=DARK)
            circle(d, cx, cy, 9, fill=WHITE)
        else:
            circle(d, cx, cy, 25, outline=WHITE, width=3.6)
            circle(d, cx, cy, 11, outline=WHITE, width=3.0)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for bordered in (True, False):
        suf = "_b" if bordered else "_nb"
        for t in "SWEN":
            finish(face(t, bordered), "face_" + t + suf)
        for t in ("LB", "RB", "LT", "RT"):
            finish(shoulders(t, bordered), "sh_" + t + suf)
        for t in "LR":
            finish(sticks(t, bordered), "stick_" + t + suf)
    print("ok")


if __name__ == "__main__":
    main()
