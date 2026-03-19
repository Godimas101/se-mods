#!/usr/bin/env python3
"""
se_lcd_convert.py — Space Engineers LCD Image Converter
Converts common image formats to SE-compatible DDS textures.

Output format: BC7_UNORM_SRGB (best quality, used by Keen + all major community
mods).  Requires texconv.exe (DirectXTex) on PATH.  Falls back to DXT5 via
wand/ImageMagick, then a pure-Python DXT5 encoder as a last resort.

Images are composited onto a black background before encoding. Alpha is set to 1
throughout — SE TexturePath alpha is inverse emissivity (1 ≈ fully self-lit,
255 = fully unlit). Alpha=1 is Keen's own recommendation.

Usage:
    python se_lcd_convert.py input.png
    python se_lcd_convert.py input.png --screen "Wide LCD Panel  ·  2:1"
    python se_lcd_convert.py ./folder/ --screen custom --size 1024
    python se_lcd_convert.py input.png --no-mipmaps

Install texconv for best quality:
    https://github.com/microsoft/DirectXTex/releases
"""

import argparse
import math
import shutil
import struct
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

# ---------------------------------------------------------------------------
# Supported input extensions
# ---------------------------------------------------------------------------
SUPPORTED_EXTS = {".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".webp", ".dds"}

DEFAULT_MAX_SIZE = 1024


# ===========================================================================
# Screen Presets
# ===========================================================================

@dataclass
class ScreenPreset:
    """
    Defines the output DDS dimensions and the visible compose region.

    The user's image is scaled (aspect-preserving) to fit within
    surface_w × surface_h, then centered on a black canvas of dds_w × dds_h.
    For presets where dds == surface the image fills the entire texture.
    font_size is the SE LCD font size required to render text-art at pixel
    accuracy (used by the Image to LCD tool).
    """
    name:      str
    dds_w:     int   # output DDS width  (power of two)
    dds_h:     int   # output DDS height (power of two)
    surface_w: int   # compose region width  (≤ dds_w)
    surface_h: int   # compose region height (≤ dds_h)
    font_size: float = 0.1  # in-game font size for text-art LCD output


# Sentinel: caller wants the old "custom max_size / preserve_aspect" path.
CUSTOM_PRESET = None

SCREEN_PRESETS: list[ScreenPreset] = [
    ScreenPreset("LCD Panel  ·  1:1",              1024, 1024, 1024, 1024, 0.1),
    ScreenPreset("Wide LCD Panel  ·  2:1",         1024,  512, 1024,  512, 0.1),
    ScreenPreset("Text Panel / Curved  ·  ~5:3",   1024, 1024, 1024,  614, 0.1),
    ScreenPreset("Widescreen  ·  16:9",            1024, 1024, 1024,  576, 0.1),
    ScreenPreset("Corner LCD Strip  ·  ~6:1",      1024, 1024, 1024,  171, 0.4),
]

PRESET_NAMES: list[str] = [p.name for p in SCREEN_PRESETS] + ["Custom"]

# Quick lookup by name
_PRESET_BY_NAME: dict[str, ScreenPreset] = {p.name: p for p in SCREEN_PRESETS}


def get_preset(name: str) -> ScreenPreset | None:
    """Return a ScreenPreset by name, or None for 'Custom'."""
    return _PRESET_BY_NAME.get(name, CUSTOM_PRESET)


# ---------------------------------------------------------------------------
# Shared display formatting  (used by both GUI screens)
# ---------------------------------------------------------------------------

def fmt_preset(name: str) -> str:
    """Format a preset name for display in a fixed-width dropdown."""
    if "  ·  " in name:
        left, ratio = name.split("  ·  ", 1)
        return f"  {left:<20}  {ratio:>4}  "
    return f"  {name}"


PRESET_DISPLAY_NAMES: list[str]       = [fmt_preset(n) for n in PRESET_NAMES]
PRESET_DISPLAY_MAP:   dict[str, str]  = {fmt_preset(n): n for n in PRESET_NAMES}


# ---------------------------------------------------------------------------
# Full block reference data  (for the in-app info window)
# Columns: (Block Name, Screen Name, Tex W, Tex H, Surf W, Surf H)
# Pixel values sourced from Whiplash141/Whips-Image-Converter where available;
# new blocks derived from SE SBC TextureResolution × surface aspect ratios.
# ---------------------------------------------------------------------------
SCREEN_REFERENCE_DATA: list[tuple[str, str, int, int, int, int]] = [
    # ── Classic LCD Panels ────────────────────────────────────────────────
    ("LCD Panel  (Small / Large)",             "Screen",                512,  512,  512,  512),
    ("Wide LCD Panel  (Small / Large)",        "Screen",               1024,  512, 1024,  512),
    ("Text Panel  (Small / Large)",            "Screen",                512,  512,  512,  307),
    ("Corner LCD  ·  Large  v1 / v2 / Flat",  "Screen",                512,  512,  512,   86),
    ("Corner LCD  ·  Small  v1 / v2 / Flat",  "Screen",                512,  512,  512,  144),
    # ── Decorative / DLC ─────────────────────────────────────────────────
    ("Transparent LCD  (Small / Large)",       "Screen",                512,  512,  512,  512),
    ("Holo LCD  (Small / Large)",              "Screen",                512,  512,  512,  512),
    ("Full Block LCD  (Small / Large)",        "Screen",                512,  512,  512,  512),
    ("Curved LCD Panel  (Small / Large)",      "Screen",                512,  512,  512,  307),
    ("Diagonal LCD Panel  (Small / Large)",    "Screen",                512,  512,  512,  512),
    # ── Cockpits ─────────────────────────────────────────────────────────
    ("Fighter Cockpit",                        "Top Center Screen",     256,  256,  256,  154),
    ("Fighter Cockpit",                        "Top Left Screen",       128,  128,  128,   85),
    ("Fighter Cockpit",                        "Top Right Screen",      128,  128,  128,   85),
    ("Fighter Cockpit",                        "Bottom Center Screen",  256,  256,  205,  256),
    ("Large Cockpit",                          "Top Center Screen",     256,  256,  256,  177),
    ("Large Cockpit",                          "Top Left Screen",       256,  256,  256,  192),
    ("Large Cockpit",                          "Top Right Screen",      256,  256,  256,  192),
    ("Large Cockpit",                          "Bottom Left Screen",    256,  256,  256,  199),
    ("Large Cockpit",                          "Bottom Right Screen",   256,  256,  256,  199),
    ("Small Cockpit",                          "Top Center Screen",     256,  256,  256,  256),
    ("Small Cockpit",                          "Top Left / Right",      256,  256,  256,  192),
    ("Large Industrial Cockpit",               "Large Display",         256,  256,  256,  154),
    ("Large Industrial Cockpit",               "Top Left Screen",       256,  256,  256,  179),
    ("Large Industrial Cockpit",               "Top Center Screen",     256,  256,  256,  179),
    ("Large Industrial Cockpit",               "Top Right Screen",      256,  256,  256,  154),
    ("Small Industrial Cockpit",               "Top Left Screen",       256,  256,  256,  183),
    ("Small Industrial Cockpit",               "Top Center Screen",     256,  256,  256,  171),
    ("Small Industrial Cockpit",               "Top Right Screen",      256,  256,  256,  183),
    ("Standing Cockpit  (Small / Large)",      "Small Screen",          512,  512,  512,  171),
    ("Standing Cockpit  (Small / Large)",      "Large Screen",          512,  512,  512,  205),
    ("Console Block",                          "Projection Area",       512,  512,  512,  512),
    ("Console Block",                          "Large Display",         256,  256,  256,  175),
    ("Large Flight Seat",                      "Large Display",         512,  128,  512,  114),
    ("Control Station",                        "Large Display",         512,  512,  512,  307),
    ("Large Programmable Block",               "Main Display",          512,  512,  512,  320),
    ("Small Programmable Block",               "Main Display",          256,  256,  256,  256),
    ("Turret Control Block  (Large)",          "Main Display",          512,  512,  512,  365),
    # ── Economy ──────────────────────────────────────────────────────────
    ("Store / ATM / Contract Block",           "Main Display",          512,  512,  512,  320),
    ("Vending Machine",                        "Main Display",         1024, 1024, 1024,  576),
    # ── Entertainment / Decorative DLC ───────────────────────────────────
    ("Entertainment Corner",                   "Main Display",         1024, 1024, 1024,  576),
    ("Jukebox",                                "Main Display",         1024, 1024, 1024,  576),
    ("Food Dispenser",                         "Main Display",         1024, 1024, 1024,  576),
    ("Lab Equipment",                          "Main Display",          512,  512,  512,  512),
    ("Medical Station",                        "Main Display",          512,  512,  512,  307),
    ("Medical Station",                        "Secondary Display",     512,  512,  512,  307),
    ("Inset Button Panel",                     "Panel 1 / 2 / 3",      256,  256,  256,  192),
    # ── Contact Pack ─────────────────────────────────────────────────────
    ("Captain's Desk",                         "Main Display",          512,  512,  512,  354),
    ("Modular Bridge Cockpit",                 "Left / Right Screen",   256,  256,  256,  154),
    # ── Apex / Core Systems ───────────────────────────────────────────────
    ("Survival Kit Reskin",                    "Main Display",          512,  512,  512,  307),
    ("Suspended Control Seat",                 "Top Center Screen",     512,  512,  512,  219),
]


# ===========================================================================
# Utility: power-of-two helpers
# ===========================================================================

def next_pow2(n: int) -> int:
    """Return the smallest power of two >= n."""
    if n <= 1:
        return 1
    return 1 << (n - 1).bit_length()


def _nearest_pow2(n: int) -> int:
    """Return the nearest power of two to n (rounds down when equidistant)."""
    if n <= 1:
        return 1
    higher = 1 << (n - 1).bit_length()
    lower  = higher >> 1
    if lower == 0:
        return higher
    return higher if (higher - n) < (n - lower) else lower


def mip_count(w: int, h: int) -> int:
    """Return the number of mipmap levels for a texture of size w × h."""
    return int(math.log2(max(w, h))) + 1


# ===========================================================================
# Image preparation
# ===========================================================================

def _flatten_to_black(img, bg_color=(0, 0, 0)) -> "PIL.Image.Image":
    """
    Composite img onto bg_color and return RGBA with alpha=255.

    NOTE: alpha=255 here (fully opaque) is intentional — Pillow applies
    premultiplied-alpha math during LANCZOS resize, so resizing with alpha=1
    (≈0.4% opacity) would quantize every channel to 0 or 255.  The final
    alpha=1 (SE inverse-emissivity value) is applied after all resize/composite
    operations, just before encoding.

    SE TexturePath alpha is INVERSE emissivity (confirmed by SE wiki + shader):
        alpha=0   → emissive = 1.0  (fully self-lit)
        alpha=1   → emissive ≈ 0.996  (Keen's recommended value for all pixels)
        alpha=255 → emissive = 0.0  (fully unlit — causes the "dull" appearance)
    """
    from PIL import Image

    if img.mode == "P":
        img = img.convert("RGBA")

    if img.mode in ("RGBA", "LA"):
        background = Image.new("RGB", img.size, bg_color)
        alpha = img.split()[-1]
        background.paste(img.convert("RGB"), mask=alpha)
        rgb = background
    else:
        rgb = img.convert("RGB")

    r, g, b = rgb.split()
    a = Image.new("L", rgb.size, 255)
    return Image.merge("RGBA", (r, g, b, a))


def _load_image(img_path: Path) -> "PIL.Image.Image":
    """Open an image, handling animated GIFs (first frame) and DDS files."""
    from PIL import Image

    img = Image.open(str(img_path))

    # Animated GIF — take first frame
    if hasattr(img, "is_animated") and img.is_animated:
        img.seek(0)
        img = img.copy()

    return img


def _load_and_compose(img_path: Path, preset: ScreenPreset,
                      bg_color=(0, 0, 0)) -> "PIL.Image.Image":
    """
    Load the source image, flatten alpha, scale to fit the visible compose
    region, and center it on a bg_color DDS canvas of the preset dimensions.
    Returns an RGB PIL image ready for encoding.
    """
    from PIL import Image

    img = _flatten_to_black(_load_image(img_path), bg_color)

    # Scale image to fit within surface region (letterbox / pillarbox)
    scale = min(preset.surface_w / img.width, preset.surface_h / img.height)
    new_w = max(1, round(img.width  * scale))
    new_h = max(1, round(img.height * scale))
    if (new_w, new_h) != img.size:
        img = img.resize((new_w, new_h), Image.LANCZOS)

    canvas = Image.new("RGBA", (preset.dds_w, preset.dds_h), (*bg_color, 255))
    x = (preset.dds_w - new_w) // 2
    y = (preset.dds_h - new_h) // 2
    canvas.paste(img, (x, y))

    # Apply SE inverse-emissivity alpha=1 now that all resizing is done.
    r, g, b, _ = canvas.split()
    return Image.merge("RGBA", (r, g, b, Image.new("L", canvas.size, 1)))


def _load_and_compose_custom(img_path: Path, max_width: int, max_height: int,
                              preserve_aspect: bool,
                              bg_color=(0, 0, 0)) -> "PIL.Image.Image":
    """
    Load and resize to user-specified dimensions.
    Each dimension is rounded to the nearest power of two, then capped at
    max_width / max_height independently.
    If preserve_aspect=True, scale uniformly so the image fits within the
    max_width × max_height box before rounding.
    Returns an RGBA PIL image.
    """
    from PIL import Image

    img = _flatten_to_black(_load_image(img_path), bg_color)
    orig_w, orig_h = img.size

    # Canvas size: round user dimensions to nearest power-of-two.
    target_w = _nearest_pow2(max_width)
    target_h = _nearest_pow2(max_height)

    if preserve_aspect:
        # Scale image to fit within canvas, preserving aspect ratio.
        scale    = min(target_w / orig_w, target_h / orig_h)
        scaled_w = max(1, round(orig_w * scale))
        scaled_h = max(1, round(orig_h * scale))
        resized  = img.resize((scaled_w, scaled_h), Image.LANCZOS)
        # Paste centred onto a canvas of the full target size.
        canvas = Image.new("RGBA", (target_w, target_h), (*bg_color, 255))
        x = (target_w - scaled_w) // 2
        y = (target_h - scaled_h) // 2
        canvas.paste(resized, (x, y))
        img = canvas
    else:
        # Stretch to fill the full canvas.
        img = img.resize((target_w, target_h), Image.LANCZOS)

    # Apply SE inverse-emissivity alpha=1 now that all resizing is done.
    r, g, b, _ = img.split()
    return Image.merge("RGBA", (r, g, b, Image.new("L", img.size, 1)))


# ===========================================================================
# Encoding backends
# Each receives a pre-composed RGB PIL image and writes the DDS output file.
# ===========================================================================

def _encode_with_texconv(texconv_path: str, img: "PIL.Image.Image",
                         out_path: Path, gen_mipmaps: bool) -> None:
    """BC7_UNORM_SRGB via texconv.exe — best quality."""
    with tempfile.TemporaryDirectory() as tmp:
        tmp_path = Path(tmp)
        tmp_png  = tmp_path / (out_path.stem + ".png")
        img.save(str(tmp_png))

        mip_arg = "0" if gen_mipmaps else "1"
        cmd = [
            texconv_path,
            "-f", "BC7_UNORM_SRGB",
            "-if", "CUBIC",
            "-bc", "x",
            "-sepalpha",
            "-y",
            "-m", mip_arg,
            "-o", str(tmp_path),
            str(tmp_png),
        ]
        result = subprocess.run(cmd, capture_output=True, text=True)
        if result.returncode != 0:
            raise RuntimeError(
                f"texconv failed (exit {result.returncode}):\n"
                + (result.stderr or result.stdout).strip()
            )

        tmp_dds = tmp_path / (out_path.stem + ".dds")
        if not tmp_dds.exists():
            raise RuntimeError(
                f"texconv did not produce expected output: {tmp_dds.name}"
            )
        shutil.move(str(tmp_dds), str(out_path))


def _encode_with_wand(img: "PIL.Image.Image",
                      out_path: Path, gen_mipmaps: bool) -> None:
    """DXT5 via wand/ImageMagick — good quality fallback."""
    from wand.image import Image as WandImage

    with tempfile.TemporaryDirectory() as tmp:
        tmp_png = Path(tmp) / (out_path.stem + ".png")
        img.save(str(tmp_png))

        with WandImage(filename=str(tmp_png)) as wimg:
            wimg.compression = "dxt5"
            w, h = img.size
            wimg.options["dds:mipmaps"] = (
                str(mip_count(w, h) - 1) if gen_mipmaps else "0"
            )
            wimg.save(filename=str(out_path))


# ---------------------------------------------------------------------------
# Pure-Python DXT5 encoder (last-resort fallback)
# ---------------------------------------------------------------------------

def _compress_dxt5_block(pixels: list) -> bytes:
    """Compress a 4×4 RGBA pixel block to 16 bytes of DXT5."""
    # Alpha block (BC4)
    alphas = [p[3] for p in pixels]
    a0, a1 = max(alphas), min(alphas)
    if a0 == a1:
        alpha_block = struct.pack("BB", a0, a1) + b"\x00\x00\x00\x00\x00\x00"
    else:
        if a0 > a1:
            palette = [
                a0, a1,
                (6*a0 + 1*a1)//7, (5*a0 + 2*a1)//7,
                (4*a0 + 3*a1)//7, (3*a0 + 4*a1)//7,
                (2*a0 + 5*a1)//7, (1*a0 + 6*a1)//7,
            ]
        else:
            palette = [
                a0, a1,
                (4*a0+1*a1)//5, (3*a0+2*a1)//5,
                (2*a0+3*a1)//5, (1*a0+4*a1)//5,
                0, 255,
            ]
        indices = [min(range(8), key=lambda i: abs(palette[i] - a)) for a in alphas]
        bits = 0
        for i in range(16):
            bits |= (indices[i] & 0x7) << (i * 3)
        alpha_block = struct.pack("BB", a0, a1) + bits.to_bytes(6, "little")

    # Color block (DXT1)
    rs = [p[0] for p in pixels]
    gs = [p[1] for p in pixels]
    bs = [p[2] for p in pixels]

    def to_rgb565(r, g, b):
        return ((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3)

    # Pick endpoints as the actual brightest/darkest pixels (by luminance).
    # Using per-channel max/min produces synthetic colors that don't exist in
    # the block, causing severe banding at lower mip levels.
    lums = [0.299*r + 0.587*g + 0.114*b for r, g, b in zip(rs, gs, bs)]
    max_i = lums.index(max(lums))
    min_i = lums.index(min(lums))
    c0 = to_rgb565(rs[max_i], gs[max_i], bs[max_i])
    c1 = to_rgb565(rs[min_i], gs[min_i], bs[min_i])
    if c0 < c1:
        c0, c1 = c1, c0

    def from_rgb565(v):
        return (((v>>11)&0x1F)*255//31, ((v>>5)&0x3F)*255//63, (v&0x1F)*255//31)

    pal = [from_rgb565(c0), from_rgb565(c1), None, None]
    if c0 > c1:
        pal[2] = tuple((2*pal[0][i]+pal[1][i])//3 for i in range(3))
        pal[3] = tuple((pal[0][i]+2*pal[1][i])//3 for i in range(3))
    else:
        pal[2] = tuple((pal[0][i]+pal[1][i])//2 for i in range(3))
        pal[3] = (0, 0, 0)

    color_indices = 0
    for i in range(16):
        r, g, b = rs[i], gs[i], bs[i]
        best = min(range(4), key=lambda j: (
            (r-pal[j][0])**2 + (g-pal[j][1])**2 + (b-pal[j][2])**2
        ))
        color_indices |= (best << (i * 2))

    return alpha_block + struct.pack("<HHI", c0, c1, color_indices)


def _compress_dxt5(img_rgba, width: int, height: int) -> bytes:
    data = bytes(img_rgba)
    out  = bytearray()
    for by in range(0, height, 4):
        for bx in range(0, width, 4):
            pixels = []
            for py in range(4):
                for px in range(4):
                    x = min(bx+px, width-1)
                    y = min(by+py, height-1)
                    i = (y*width + x) * 4
                    pixels.append((data[i], data[i+1], data[i+2], data[i+3]))
            out += _compress_dxt5_block(pixels)
    return bytes(out)


def _build_dds_header(width: int, height: int, mip_levels: int) -> bytes:
    """Build a DDS file header for DXT5 (BC3_UNORM)."""
    DDPF_FOURCC       = 0x4
    DDSD_CAPS         = 0x1
    DDSD_HEIGHT       = 0x2
    DDSD_WIDTH        = 0x4
    DDSD_PIXELFORMAT  = 0x1000
    DDSD_LINEARSIZE   = 0x80000
    DDSD_MIPMAPCOUNT  = 0x20000
    DDSCAPS_TEXTURE   = 0x1000
    DDSCAPS_MIPMAP    = 0x400000
    DDSCAPS_COMPLEX   = 0x8

    pf = struct.pack("<II4sIIIII",
        32, DDPF_FOURCC, b"DXT5", 0, 0, 0, 0, 0)

    flags = DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_LINEARSIZE
    if mip_levels > 1:
        flags |= DDSD_MIPMAPCOUNT

    linear_size = max(1, (width+3)//4) * max(1, (height+3)//4) * 16
    caps = DDSCAPS_TEXTURE
    if mip_levels > 1:
        caps |= DDSCAPS_MIPMAP | DDSCAPS_COMPLEX

    header = struct.pack("<7I44s32s5I",
        124, flags, height, width, linear_size, 0, mip_levels,
        b"\x00"*44, pf, caps, 0, 0, 0, 0)

    return b"DDS " + header


def _encode_with_pillow(img: "PIL.Image.Image",
                        out_path: Path, gen_mipmaps: bool) -> None:
    """DXT5 pure-Python fallback encoder."""
    from PIL import Image

    w, h     = img.size
    img_rgba = img.convert("RGBA")

    levels: list = []
    if gen_mipmaps:
        mw, mh = w, h
        while True:
            if (mw, mh) == (w, h):
                levels.append(img_rgba)
            else:
                # Resize as RGB to avoid premultiplied-alpha quantization (alpha=1
                # would multiply every channel by ~0 during LANCZOS resampling).
                mip_rgb = img_rgba.convert("RGB").resize((mw, mh), Image.LANCZOS)
                mr, mg, mb = mip_rgb.split()
                levels.append(Image.merge("RGBA", (mr, mg, mb,
                                                   Image.new("L", mip_rgb.size, 1))))
            if mw == 1 and mh == 1:
                break
            mw = max(1, mw // 2)
            mh = max(1, mh // 2)
    else:
        levels.append(img_rgba)

    with open(str(out_path), "wb") as f:
        f.write(_build_dds_header(w, h, len(levels)))
        for lv in levels:
            lw, lh = lv.size
            pw = max(4, (lw+3) & ~3)
            ph = max(4, (lh+3) & ~3)
            if pw != lw or ph != lh:
                pad = Image.new("RGBA", (pw, ph), (0, 0, 0, 1))
                pad.paste(lv, (0, 0))
                lv = pad
            f.write(_compress_dxt5(lv.tobytes(), pw, ph))


# ===========================================================================
# Main conversion entry point
# ===========================================================================

def convert_image(img_path: Path, out_dir: Path,
                  preset: ScreenPreset | None,
                  gen_mipmaps: bool,
                  use_texconv: str | None,
                  use_wand: bool,
                  prefix: str = "",
                  suffix: str = "",
                  custom_max_size: int = DEFAULT_MAX_SIZE,
                  custom_max_height: int | None = None,
                  custom_preserve_aspect: bool = False,
                  bg_color: tuple = (0, 0, 0)) -> None:
    """
    Convert a single image to a SE LCD DDS texture.

    preset=None  →  custom path (custom_max_size / custom_max_height / custom_preserve_aspect)
    preset=...   →  compose image into the preset's visible surface region,
                    output at preset DDS dimensions.

    custom_max_height defaults to custom_max_size when not specified (square cap).
    Encoder priority: texconv (BC7) → wand (DXT5) → built-in DXT5.
    """
    out_path = out_dir / (prefix + img_path.stem + suffix + ".dds")

    # --- Load and compose ---
    if preset is CUSTOM_PRESET:
        max_h = custom_max_height if custom_max_height is not None else custom_max_size
        img = _load_and_compose_custom(img_path, custom_max_size, max_h,
                                       custom_preserve_aspect, bg_color)
    else:
        img = _load_and_compose(img_path, preset, bg_color)

    dds_w, dds_h = img.size
    mips = mip_count(dds_w, dds_h) if gen_mipmaps else 1
    enc_fmt = "BC7_UNORM_SRGB" if use_texconv else "DXT5"

    print(f"  {img_path.name} → {out_path.name}  "
          f"{dds_w}×{dds_h}  {enc_fmt}  "
          f"{mips} mip{'s' if mips != 1 else ''}")

    # --- Encode ---
    if use_texconv:
        try:
            _encode_with_texconv(use_texconv, img, out_path, gen_mipmaps)
            return
        except Exception as e:
            print(f"    [texconv failed: {e}] — falling back to wand/DXT5")

    if use_wand:
        try:
            _encode_with_wand(img, out_path, gen_mipmaps)
            return
        except Exception as e:
            print(f"    [wand failed: {e}] — falling back to built-in encoder")

    _encode_with_pillow(img, out_path, gen_mipmaps)


# ===========================================================================
# Backend detection
# ===========================================================================

def _detect_texconv() -> str | None:
    """Return the path to texconv.exe if found on PATH, else None."""
    return shutil.which("texconv")


def _detect_wand() -> bool:
    """Return True if wand (ImageMagick) is importable."""
    try:
        import wand.image  # noqa: F401
        return True
    except ImportError:
        return False


def _check_pillow() -> bool:
    try:
        import PIL  # noqa: F401
        return True
    except ImportError:
        return False


# ===========================================================================
# CLI
# ===========================================================================

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Convert images to Space Engineers LCD DDS textures.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=f"""
Screen presets:
  {chr(10).join('  ' + n for n in PRESET_NAMES)}

Examples:
  python se_lcd_convert.py image.png
  python se_lcd_convert.py image.png --screen "Wide LCD Panel  ·  2:1"
  python se_lcd_convert.py ./folder/ --screen custom --size 2048
  python se_lcd_convert.py image.png --no-mipmaps
        """,
    )
    parser.add_argument("input", help="Image file or folder to convert")
    parser.add_argument("--outdir", "-o", default=None,
        help="Output directory (default: same as input, or <input>_dds/ for folders)")
    parser.add_argument("--screen", "-sc",
        default=SCREEN_PRESETS[0].name,
        help=f"Screen target preset name (default: \"{SCREEN_PRESETS[0].name}\"). "
             "Use 'custom' with --size for manual control.")
    parser.add_argument("--size", "-s", type=int, default=DEFAULT_MAX_SIZE,
        help=f"Max dimension for 'custom' screen target (default: {DEFAULT_MAX_SIZE}). "
             "Must be a power of two.")
    parser.add_argument("--preserve-aspect", action="store_true",
        help="Preserve aspect ratio in 'custom' mode (letterbox to pow2 dimensions).")
    parser.add_argument("--no-mipmaps", action="store_true",
        help="Skip mipmap generation (not recommended for SE).")
    parser.add_argument("--no-texconv", action="store_true",
        help="Skip texconv.exe even if found on PATH.")
    parser.add_argument("--no-wand", action="store_true",
        help="Skip wand/ImageMagick even if available.")

    args = parser.parse_args()

    # Resolve screen preset
    if args.screen.lower() == "custom":
        preset = CUSTOM_PRESET
    else:
        preset = get_preset(args.screen)
        if preset is None and args.screen.lower() != "custom":
            parser.error(
                f"Unknown screen preset: '{args.screen}'\n"
                f"Available: {', '.join(PRESET_NAMES)}"
            )

    if preset is CUSTOM_PRESET:
        if args.size <= 0 or (args.size & (args.size - 1)) != 0:
            parser.error(f"--size must be a power of two. Got: {args.size}")

    gen_mipmaps = not args.no_mipmaps

    if not _check_pillow():
        print("ERROR: Pillow is required.  pip install Pillow")
        sys.exit(1)

    use_texconv = None if args.no_texconv else _detect_texconv()
    use_wand    = False if args.no_wand    else _detect_wand()

    if use_texconv:
        enc_label = f"texconv ({use_texconv})  →  BC7_UNORM_SRGB"
        fmt_label = "DDS / BC7_UNORM_SRGB (DXGI 99)"
    elif use_wand:
        enc_label = "wand (ImageMagick)  →  DXT5"
        fmt_label = "DDS / DXT5 (BC3_UNORM)"
    else:
        enc_label = "built-in pure-Python  →  DXT5"
        fmt_label = "DDS / DXT5 (BC3_UNORM)"

    print("=" * 60)
    print("Space Engineers LCD Image Converter")
    print("=" * 60)
    print(f"  Encoder : {enc_label}")
    print(f"  Format  : {fmt_label}")
    if preset is CUSTOM_PRESET:
        print(f"  Target  : Custom  (max {args.size}px, "
              f"{'preserve aspect' if args.preserve_aspect else 'stretch to pow2'})")
    else:
        print(f"  Target  : {preset.name}  "
              f"({preset.dds_w}×{preset.dds_h} DDS, "
              f"{preset.surface_w}×{preset.surface_h} visible)")
    print(f"  Mipmaps : {'yes' if gen_mipmaps else 'no'}")
    if not use_texconv:
        print("  TIP: Install texconv.exe for BC7 output (best quality).")
        print("       https://github.com/microsoft/DirectXTex/releases")
    print()

    input_path = Path(args.input)

    if input_path.is_file():
        if input_path.suffix.lower() not in SUPPORTED_EXTS:
            print(f"ERROR: Unsupported format '{input_path.suffix}'. "
                  f"Supported: {', '.join(sorted(SUPPORTED_EXTS))}")
            sys.exit(1)

        out_dir = Path(args.outdir) if args.outdir else input_path.parent
        out_dir.mkdir(parents=True, exist_ok=True)

        print(f"Converting: {input_path}")
        try:
            convert_image(input_path, out_dir, preset, gen_mipmaps,
                          use_texconv, use_wand,
                          custom_max_size=args.size,
                          custom_preserve_aspect=args.preserve_aspect)
            print("\nDone.")
        except Exception as e:
            print(f"\nERROR: {e}")
            sys.exit(1)

    elif input_path.is_dir():
        files = [
            f for f in sorted(input_path.iterdir())
            if f.is_file() and f.suffix.lower() in SUPPORTED_EXTS
        ]
        if not files:
            print(f"No supported image files found in: {input_path}")
            sys.exit(0)

        if args.outdir:
            out_dir = Path(args.outdir)
        else:
            out_dir = input_path.parent / (input_path.name + "_dds")
        out_dir.mkdir(parents=True, exist_ok=True)

        print(f"Input  : {input_path}")
        print(f"Output : {out_dir}")
        print(f"Files  : {len(files)}")
        print()

        ok = failed = 0
        for img_path in files:
            try:
                convert_image(img_path, out_dir, preset, gen_mipmaps,
                              use_texconv, use_wand,
                              custom_max_size=args.size,
                              custom_preserve_aspect=args.preserve_aspect)
                ok += 1
            except Exception as e:
                print(f"  [FAILED] {img_path.name}: {e}")
                failed += 1

        print()
        print(f"Done: {ok} converted, {failed} failed.")
        if ok:
            print(f"Output: {out_dir}")

    else:
        print(f"ERROR: '{input_path}' is not a file or directory.")
        sys.exit(1)


if __name__ == "__main__":
    main()
