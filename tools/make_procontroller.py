# Generates the Nintendo Switch Pro Controller silhouette for the player-select cards,
# in the same mask format the game uses (616x374):
#   cyan (0,255,255) = background, white = outline, magenta (255,0,255) = fill (player colour).
# Output: res/pad_nx_whole.png, pad_nx_left.png, pad_nx_right.png  (original art)
# Requires: Pillow (pip install pillow)
import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

W, H, SS = 616, 374, 4
OUT = os.path.join(os.path.dirname(__file__), "..", "res")
BORDER = 22


def shape_mask():
    m = Image.new("L", (W * SS, H * SS), 0)
    d = ImageDraw.Draw(m)
    s = lambda v: [int(x * SS) for x in v]
    # upper body: wide, almost flat top edge with rounded shoulders
    d.rounded_rectangle(s([62, 28, 554, 220]), radius=100 * SS, fill=255)
    # grips: long ellipses tilted outwards
    for cx, ang in ((142, -14), (474, 14)):
        g = Image.new("L", (W * SS, H * SS), 0)
        gd = ImageDraw.Draw(g)
        gd.ellipse(s([cx - 84, 80, cx + 84, 360]), fill=255)
        g = g.rotate(ang, center=(cx * SS, 140 * SS), resample=Image.BICUBIC)
        m = ImageChops.lighter(m, g)
    # lower centre gap (arch between the grips)
    d = ImageDraw.Draw(m)
    d.ellipse(s([212, 212, 404, 340]), fill=0)
    d.rectangle(s([255, 276, 361, 374]), fill=0)
    m = m.filter(ImageFilter.GaussianBlur(9 * SS))
    m = m.point(lambda v: 255 if v > 127 else 0)
    return m.resize((W, H), Image.LANCZOS)


def erode(mask, px):
    m = mask
    for _ in range(px // 4):
        m = m.filter(ImageFilter.MinFilter(9))
    return m


def compose(lines, fill):
    # lines = outline (white), fill = fill (magenta); everything else = cyan.
    # R = outline or fill, G = 255 except inside the fill, B = 255.
    r = ImageChops.lighter(lines, fill)
    g = ImageChops.invert(ImageChops.subtract(fill, lines))
    b = Image.new("L", (W, H), 255)
    return Image.merge("RGBA", (r, g, b, Image.new("L", (W, H), 255)))


def main():
    os.makedirs(OUT, exist_ok=True)
    shape = shape_mask()
    fill = erode(shape, BORDER)
    ring = ImageChops.subtract(shape, fill)
    compose(ring, fill).save(os.path.join(OUT, "pad_nx_whole.png"))

    # halves (controller shared by two players): divider line + fill on one side
    half = BORDER // 2
    for side in ("left", "right"):
        f = fill.copy()
        d = ImageDraw.Draw(f)
        if side == "left":
            d.rectangle([W // 2, 0, W, H], fill=0)
        else:
            d.rectangle([0, 0, W // 2, H], fill=0)
        divider = Image.new("L", (W, H), 0)
        ImageDraw.Draw(divider).rectangle([W // 2 - half, 0, W // 2 + half, H], fill=255)
        divider = ImageChops.multiply(divider, shape)
        lines = ImageChops.lighter(ring, divider)
        compose(lines, f).save(os.path.join(OUT, "pad_nx_" + side + ".png"))
    print("ok")


if __name__ == "__main__":
    main()
