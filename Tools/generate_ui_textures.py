"""Generates the small tiling/gradient textures the UITK theme needs.

USS has no linear-gradient(), so vertical gradients are baked into 1-D-ish textures and
stretched with -unity-background-scale-mode; the stripe overlay is a tileable diagonal
pattern used with background-repeat. Re-run this script to regenerate; the PNGs live in
Assets/_Project/Art/Textures/UI/. Requires Pillow.
"""
import os
from PIL import Image, ImageDraw

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


def hazard_tape(path, size=108, amber="#F0A93B", dark="#1B2330"):
    """Tileable '\' hazard stripes (amber / dark halves), the S03 warning tape. 4x supersampled.
    Stripes run down-right like the mockup's repeating-linear-gradient(45deg); horizontal period == size so the tile repeats seamlessly."""
    ss = 4
    a, d = hex_rgb(amber), hex_rgb(dark)
    big = Image.new("RGBA", (size * ss, size * ss))
    for y in range(size * ss):
        for x in range(size * ss):
            big.putpixel((x, y), (a if ((x - y) / ss) % size < size / 2 else d) + (255,))
    big.resize((size, size), Image.LANCZOS).save(os.path.join(OUT, path))


def edge_vignette(path, color, strength=0.9, width=512, height=288, inner=0.45, outer=1.15):
    """Full-frame vignette: transparent in the middle, `color` toward the edges and corners (stand-in for the mockup's
    inset box-shadow). Stretched over the screen with stretch-to-fill, so the shape follows the aspect ratio."""
    rgb = hex_rgb(color)
    img = Image.new("RGBA", (width, height))
    for y in range(height):
        ny = (y / (height - 1)) * 2 - 1
        for x in range(width):
            nx = (x / (width - 1)) * 2 - 1
            r = (nx * nx + ny * ny) ** 0.5
            t = min(max((r - inner) / (outer - inner), 0.0), 1.0)
            t = t * t * (3 - 2 * t)  # smoothstep
            img.putpixel((x, y), rgb + (round(255 * strength * t),))
    img.save(os.path.join(OUT, path))


def cracks(path, width=1200, height=540, seed=11):
    """Screen-crack overlay for the damage state: white jagged lines that taper and fade along their length.
    Layout follows the S03 h mockup (five cracks at fixed positions/angles), drawn at half the 2400x1080 reference,
    4x supersampled. Deterministic (fixed seed) so regenerating gives the same file."""
    import math
    import random
    from PIL import ImageDraw
    rng = random.Random(seed)
    ss = 2
    mask = Image.new("L", (width * ss, height * ss), 0)
    draw = ImageDraw.Draw(mask)
    # (left, top, length, angle in degrees clockwise) in 2400x1080 reference px, from the mockup
    specs = [(240, 120, 640, 24), (420, 300, 420, -38), (1500, 180, 560, -16), (1720, 420, 380, 42), (1000, 760, 480, 12)]
    k = 0.5 * ss

    def crack(x, y, angle, length, base_width, alpha0, depth):
        steps = max(4, int(length / 34))
        for i in range(steps):
            t = i / steps
            seg = length / steps
            a = angle + rng.uniform(-16, 16)
            nx = x + math.cos(math.radians(a)) * seg
            ny = y + math.sin(math.radians(a)) * seg
            w = max(1, round(base_width * (1 - t * 0.7)))
            alpha = round(alpha0 * (1 - t * 0.85))
            draw.line([(x * k, y * k), (nx * k, ny * k)], fill=alpha, width=w * ss)
            if depth < 2 and rng.random() < 0.28:
                crack(nx, ny, angle + rng.choice((-1, 1)) * rng.uniform(25, 55), length * 0.35, base_width * 0.6, alpha, depth + 1)
            x, y, angle = nx, ny, a

    for left, top, length, angle in specs:
        cx = left + length / 2
        cy = top + 3
        sx = cx - math.cos(math.radians(angle)) * length / 2
        sy = cy - math.sin(math.radians(angle)) * length / 2
        crack(sx, sy, angle, length, 7, 235, 0)
    mask = mask.resize((width, height), Image.LANCZOS)
    img = Image.new("RGBA", (width, height), (255, 255, 255, 0))
    img.putalpha(mask)
    img.save(os.path.join(OUT, path))


def arrow_up(path, size=64, pad=8):
    """White up-pointing triangle with soft edges; tinted in USS (-unity-background-image-tint-color).
    Replaces the text glyph (U+25B2) so it renders on every platform without a font fallback."""
    ss = 8
    big = Image.new("L", (size * ss, size * ss), 0)
    d = ImageDraw.Draw(big)
    left, right = pad * ss, (size - pad) * ss
    top, bottom = (pad + 2) * ss, (size - pad - 2) * ss
    d.polygon([(left, bottom), (right, bottom), ((left + right) // 2, top)], fill=255)
    mask = big.resize((size, size), Image.LANCZOS)
    img = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    img.putalpha(mask)
    img.save(os.path.join(OUT, path))


def dashed_ring(path, size=256, thickness=12, dashes=14, fill=0.58):
    """White dashed ring (USS has no dashed borders), tinted in USS. The empty / error states of the leaderboard
    use it where the mockup has a round dashed border."""
    import math
    ss = 4
    big = Image.new("L", (size * ss, size * ss), 0)
    d = ImageDraw.Draw(big)
    outer = size * ss / 2
    inset = thickness * ss / 2 + 2 * ss
    box = [inset, inset, size * ss - inset, size * ss - inset]
    step = 360.0 / dashes
    for i in range(dashes):
        start = i * step
        d.arc(box, start, start + step * fill, fill=255, width=thickness * ss)
    mask = big.resize((size, size), Image.LANCZOS)
    img = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    img.putalpha(mask)
    img.save(os.path.join(OUT, path))


if __name__ == "__main__":
    vertical_gradient("Tex_UiBgGradient.png", [(0, "#5B86A6"), (0.35, "#3D6280"), (0.7, "#243A4F"), (1, "#1B2937")], 4, 512)
    vertical_gradient("Tex_UiGradAmber.png", [(0, "#FFC76A"), (1, "#F0A93B")], 4, 64)
    stripes("Tex_UiBgStripes.png")
    hazard_tape("Tex_UiHazardTape.png")
    edge_vignette("Tex_UiVignetteRed.png", "#E0452F", 0.9)
    edge_vignette("Tex_UiVignetteDark.png", "#101823", 0.75)
    cracks("Tex_UiCracks.png")
    arrow_up("Tex_UiArrowUp.png")
    dashed_ring("Tex_UiDashedRing.png")
    print("ok")
