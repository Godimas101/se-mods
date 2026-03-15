#!/usr/bin/env python3
"""
se_text_convert.py — Core logic for the SE LCD Text Converter.

Converts an image to a Space Engineers LCD text string using the
\uE100–\uE1FF character range of SE's monospace font.  Each character
encodes one pixel of 9-bit colour (8 levels × 8 × 8 = 512 colours).

The output string is pasted directly into any SE LCD's content field.
In-game settings required:
  Content type : Text and Images
  Font         : Monospaced
  Font size    : as specified per block (shown in the tool)
  Text padding : 0
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

# SE monospace font: one character = 2.88 surface-pixels at font-size 0.1
PIXELS_TO_CHARS: float = 1.0 / 2.88

# 9-bit palette: 8 evenly-spaced levels per channel (0, 36, 73 … 255)
BIT_SPACING: float = 255.0 / 7.0

# Alpha threshold below which a pixel is treated as transparent
ALPHA_THRESHOLD: int = 36

# Transparency spacer characters (SE-specific fixed-width blank glyphs)
_SPACER_1   = "\ue075\ue072\ue070"
_SPACER_2   = "\ue076\ue073\ue071"
_SPACER_4   = "\ue076\ue076\ue074\ue072"
_SPACER_8   = "\ue078\ue075\ue073"
_SPACER_178 = "\ue078" * 25 + "\ue077\ue075\ue074\ue073\ue071"

_TRANS_PLACEHOLDER = "#"   # temporary stand-in during string build


# ---------------------------------------------------------------------------
# Block / surface data
# ---------------------------------------------------------------------------

@dataclass(frozen=True)
class SETextSurface:
    block_name:   str
    surface_name: str
    texture_w:    float
    texture_h:    float
    surface_w:    float
    surface_h:    float
    font_size:    float = 0.1

    @property
    def char_w(self) -> int:
        scale = 512.0 / min(self.texture_w, self.texture_h)
        return round(self.surface_w * PIXELS_TO_CHARS * scale)

    @property
    def char_h(self) -> int:
        scale = 512.0 / min(self.texture_w, self.texture_h)
        return round(self.surface_h * PIXELS_TO_CHARS * scale)


_S = 512.0   # shorthand for the standard 512-pixel texture dimension

# All supported block / surface combinations (sourced from Whip's converter
# and verified against SE game files).
TEXT_SURFACES: list[SETextSurface] = [
    SETextSurface("LCD Panel",               "Screen Area",         _S,   _S,   _S,     _S,     0.1),
    SETextSurface("Wide LCD Panel",          "Screen Area",         1024, _S,   1024,   _S,     0.1),
    SETextSurface("Text Panel / Curved",     "Screen Area",         _S,   _S,   _S,     307.2,  0.1),
    SETextSurface("Large Corner LCD",        "Screen Area",         _S,   _S,   _S,     86.0,   0.4),
    SETextSurface("Small Corner LCD",        "Screen Area",         _S,   _S,   _S,     144.0,  0.2),
    SETextSurface("Lg. Programmable Block",  "Large Display",       _S,   _S,   _S,     320.0,  0.1),
    SETextSurface("Lg. Programmable Block",  "Keyboard",            _S,   _S,   _S,     204.8,  0.1),
    SETextSurface("Sm. Programmable Block",  "Large Display",       256,  256,  256,    256,    0.1),
    SETextSurface("Console Block",           "Projection Area",     _S,   _S,   _S,     _S,     0.1),
    SETextSurface("Console Block",           "Large Display",       256,  256,  256,    175.0,  0.1),
    SETextSurface("Fighter Cockpit",         "Top Center Screen",   256,  256,  256,    153.6,  0.1),
    SETextSurface("Fighter Cockpit",         "Top Left Screen",     128,  128,  128,    85.33,  0.1),
    SETextSurface("Fighter Cockpit",         "Top Right Screen",    128,  128,  128,    85.33,  0.1),
    SETextSurface("Large Cockpit",           "Top Center Screen",   256,  256,  256,    177.23, 0.1),
    SETextSurface("Large Cockpit",           "Top Left Screen",     256,  256,  256,    192.0,  0.1),
    SETextSurface("Large Cockpit",           "Top Right Screen",    256,  256,  256,    192.0,  0.1),
    SETextSurface("Small Cockpit",           "Top Center Screen",   256,  256,  256,    256,    0.1),
    SETextSurface("Small Cockpit",           "Top Left Screen",     256,  256,  256,    192.0,  0.1),
    SETextSurface("Small Cockpit",           "Top Right Screen",    256,  256,  256,    192.0,  0.1),
    SETextSurface("Lg. Industrial Cockpit",  "Large Display",       256,  256,  256,    153.6,  0.1),
    SETextSurface("Lg. Industrial Cockpit",  "Top Left Screen",     256,  256,  256,    179.2,  0.1),
    SETextSurface("Lg. Industrial Cockpit",  "Top Center Screen",   256,  256,  256,    179.2,  0.1),
    SETextSurface("Flight Seat",             "Large Display",       _S,   128,  _S,     113.78, 0.1),
    SETextSurface("Large Control Station",   "Large Display",       _S,   _S,   _S,     307.2,  0.1),
]

# Unique block names (insertion-order preserved) for the first dropdown
BLOCK_NAMES: list[str] = list(dict.fromkeys(s.block_name for s in TEXT_SURFACES))


def get_surfaces_for_block(block_name: str) -> list[SETextSurface]:
    return [s for s in TEXT_SURFACES if s.block_name == block_name]


# ---------------------------------------------------------------------------
# Colour quantisation helpers
# ---------------------------------------------------------------------------

def _clamp(v: float) -> int:
    return max(0, min(255, round(v)))


def _quantise_channel(v: float) -> int:
    """Return palette index 0-7 for a single channel value 0-255."""
    return round(max(0.0, min(255.0, v)) / BIT_SPACING)


# ---------------------------------------------------------------------------
# Dithering filter definitions
# Each entry: (weight, row_offset, col_offset)
# Divisor is computed as the sum of all weights.
# ---------------------------------------------------------------------------

DITHER_MODES: list[str] = [
    "None",
    "Floyd-Steinberg",
    "Ju-Ji-Ni",
    "Stucci",
    "Sierra 3",
    "Sierra 2",
    "Sierra Lite",
]

_DITHER_FILTERS: dict[str, list[tuple[int, int, int]]] = {
    "None":            [],
    "Floyd-Steinberg": [(7,0,1),(1,1,1),(5,1,0),(3,1,-1)],
    "Ju-Ji-Ni":        [(7,0,1),(5,0,2),(3,1,-2),(5,1,-1),(7,1,0),(5,1,1),(3,1,2),
                        (1,2,-2),(3,2,-1),(5,2,0),(3,2,1),(1,2,2)],
    "Stucci":          [(8,0,1),(4,0,2),(2,1,-2),(4,1,-1),(8,1,0),(4,1,1),(2,1,2),
                        (1,2,-2),(2,2,-1),(4,2,0),(2,2,1),(1,2,2)],
    "Sierra 3":        [(5,0,1),(3,0,2),(2,1,-2),(4,1,-1),(5,1,0),(4,1,1),(2,1,2),
                        (2,2,-1),(3,2,0),(2,2,1)],
    "Sierra 2":        [(4,0,1),(3,0,2),(1,1,-2),(2,1,-1),(3,1,0),(2,1,1),(1,1,2)],
    "Sierra Lite":     [(2,0,1),(1,1,-1),(1,1,0)],
}


def _spread_error(
    pix: list[list[list[float]]],
    row: int, col: int,
    er: float, eg: float, eb: float,
    filt: list[tuple[int, int, int]],
    divisor: int,
    height: int, width: int,
) -> None:
    for weight, dr, dc in filt:
        nr, nc = row + dr, col + dc
        if 0 <= nr < height and 0 <= nc < width:
            f = weight / divisor
            pix[nr][nc][0] += er * f
            pix[nr][nc][1] += eg * f
            pix[nr][nc][2] += eb * f


# ---------------------------------------------------------------------------
# Image framing (letterbox / pillarbox)
# ---------------------------------------------------------------------------

def _frame_image(src, target_w: int, target_h: int,
                 bg_color: tuple[int, int, int],
                 transparent: bool):
    """Fit *src* into *target_w × target_h* with letterbox/pillarbox."""
    from PIL import Image

    alpha = 0 if transparent else 255
    canvas = Image.new("RGBA", (target_w, target_h),
                       (bg_color[0], bg_color[1], bg_color[2], alpha))
    sw, sh = src.size
    scale  = min(target_w / sw, target_h / sh)
    new_w, new_h = round(sw * scale), round(sh * scale)
    scaled = src.resize((new_w, new_h), Image.LANCZOS)
    ox = (target_w - new_w) // 2
    oy = (target_h - new_h) // 2
    canvas.paste(scaled, (ox, oy), scaled)
    return canvas


# ---------------------------------------------------------------------------
# Core conversion
# ---------------------------------------------------------------------------

def convert_to_text(
    img_path:        "Path | str",
    surface:         SETextSurface,
    dither_mode:     str   = "Floyd-Steinberg",
    preserve_aspect: bool  = True,
    bg_color:        tuple = (0, 0, 0),
    transparency:    bool  = False,
    progress_cb      = None,   # optional callable(int 0-100)
) -> tuple[str, "Image"]:
    """
    Convert an image to an SE LCD text string.

    Returns (output_string, preview_pil_image).
      output_string   — paste directly into SE LCD content field
      preview_pil_image — quantised RGB preview at 2× scale (PIL Image)
    """
    from PIL import Image

    src = Image.open(Path(img_path)).convert("RGBA")

    tw, th = surface.char_w, surface.char_h

    if preserve_aspect:
        resized = _frame_image(src, tw, th, bg_color, transparency)
    else:
        resized = src.resize((tw, th), Image.LANCZOS)

    width, height = resized.size
    raw = resized.load()

    # Build float pixel buffer [row][col] = [r, g, b, a]
    pix: list[list[list[float]]] = [
        [[float(raw[c, r][0]), float(raw[c, r][1]),
          float(raw[c, r][2]), float(raw[c, r][3])]
         for c in range(width)]
        for r in range(height)
    ]

    filt    = _DITHER_FILTERS.get(dither_mode, [])
    divisor = sum(w for w, _, _ in filt) or 1
    do_dither = bool(filt)

    chars:   list[list[str]]             = []
    preview: list[list[tuple[int,int,int]]] = []
    total   = width * height
    done    = 0

    for row in range(height):
        row_chars: list[str]               = []
        row_rgb:   list[tuple[int,int,int]] = []

        for col in range(width):
            p = pix[row][col]
            r, g, b, a = p[0], p[1], p[2], p[3]

            if a < ALPHA_THRESHOLD:
                if transparency:
                    row_chars.append(_TRANS_PLACEHOLDER)
                    row_rgb.append(bg_color)
                    done += 1
                    if progress_cb and done % (width * 4) == 0:
                        progress_cb(int(done * 100 / total))
                    continue
                else:
                    r, g, b = float(bg_color[0]), float(bg_color[1]), float(bg_color[2])

            ri = _quantise_channel(r)
            gi = _quantise_channel(g)
            bi = _quantise_channel(b)

            qr = _clamp(ri * BIT_SPACING)
            qg = _clamp(gi * BIT_SPACING)
            qb = _clamp(bi * BIT_SPACING)

            row_chars.append(chr(0xe100 + (ri << 6) + (gi << 3) + bi))
            row_rgb.append((qr, qg, qb))

            if do_dither:
                _spread_error(pix, row, col,
                              r - qr, g - qg, b - qb,
                              filt, divisor, height, width)

            done += 1
            if progress_cb and done % (width * 4) == 0:
                progress_cb(int(done * 100 / total))

        chars.append(row_chars)
        preview.append(row_rgb)

    if progress_cb:
        progress_cb(100)

    # ── Build output string ──────────────────────────────────────────────────
    body = "\n".join("".join(row) for row in chars)

    if transparency:
        body = body.replace(_TRANS_PLACEHOLDER * 178, _SPACER_178)
        body = body.replace(_TRANS_PLACEHOLDER * 8,   _SPACER_8)
        body = body.replace(_TRANS_PLACEHOLDER * 4,   _SPACER_4)
        body = body.replace(_TRANS_PLACEHOLDER * 2,   _SPACER_2)
        body = body.replace(_TRANS_PLACEHOLDER,        _SPACER_1)
    else:
        body = body.replace(_TRANS_PLACEHOLDER, chr(0xe100))

    footer = (f"\nSE Tools  ·  {dither_mode}  ·  {width}\u00d7{height} px"
              f"  ·  font {surface.font_size}")
    output = body + footer

    # ── Build preview image ──────────────────────────────────────────────────
    from PIL import Image as _Image
    prev_img = _Image.new("RGB", (width, height))
    prev_pix = prev_img.load()
    for row_i, row_data in enumerate(preview):
        for col_i, rgb in enumerate(row_data):
            prev_pix[col_i, row_i] = rgb

    scale = max(1, min(4, 356 // max(width, 1)))
    if scale > 1:
        prev_img = prev_img.resize((width * scale, height * scale),
                                   _Image.NEAREST)

    return output, prev_img
