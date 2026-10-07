"""Generates the small tiling/gradient textures the UITK theme needs.

USS has no linear-gradient(), so vertical gradients are baked into 1-D-ish textures and
stretched with -unity-background-scale-mode; the stripe overlay is a tileable diagonal
pattern used with background-repeat. Re-run this script to regenerate; the PNGs live in
Assets/_Project/Art/Textures/UI/. Requires Pillow.
"""
import os
from PIL import Image

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Art", "Textures", "UI")


def hex_rgb(value):
    value = value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4))


def lerp(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def vertical_gradient(path, stops, width, height):
    """stops: list of (position 0..1, '#rrggbb')."""
    img = Image.new("RGBA", (width, height))
    for y in range(height):
        t = y / (height - 1)
        for i in range(len(stops) - 1):
            p0, c0 = stops[i]
            p1, c1 = stops[i + 1]
            if p0 <= t <= p1:
                rgb = lerp(hex_rgb(c0), hex_rgb(c1), (t - p0) / (p1 - p0) if p1 > p0 else 0)
                break
        for x in range(width):
            img.putpixel((x, y), rgb + (255,))
    img.save(os.path.join(OUT, path))


def stripes(path, size=64, period=32, band=14, alpha=10):
    """Diagonal '/' stripes, tileable when size is a multiple of period. 4x supersampled."""
    ss = 4
    big = Image.new("L", (size * ss, size * ss), 0)
    for y in range(size * ss):
        for x in range(size * ss):
            if ((x + y) / ss) % period < band:
                big.putpixel((x, y), alpha)
    mask = big.resize((size, size), Image.LANCZOS)
    img = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    img.putalpha(mask)
    img.save(os.path.join(OUT, path))


if __name__ == "__main__":
    vertical_gradient("Tex_UiBgGradient.png", [(0, "#5B86A6"), (0.35, "#3D6280"), (0.7, "#243A4F"), (1, "#1B2937")], 4, 512)
    vertical_gradient("Tex_UiGradAmber.png", [(0, "#FFC76A"), (1, "#F0A93B")], 4, 64)
    stripes("Tex_UiBgStripes.png")
    print("ok")
