#!/usr/bin/env python3
"""
screen_setup.py — Setup & Requirements screen for SE Tools.

Walks the user through installing Python, Pillow, and the optional
DirectXTex texconv encoder, with clickable links for every download.
"""

import webbrowser
import tkinter as tk
from tkinter import ttk

import se_theme as T


# External download URLs
_URL_PYTHON     = "https://www.python.org/downloads/"
_URL_DIRECTXTEX = "https://github.com/microsoft/DirectXTex/releases/latest"
_URL_IMAGEMAGICK= "https://imagemagick.org/script/download.php"


class SetupScreen(ttk.Frame):

    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app
        self._build()

    # -----------------------------------------------------------------------

    def _build(self):
        T.build_header(
            self,
            title="SETUP  &  REQUIREMENTS",
            subtitle="Everything you need to get both tools running.",
            back_cb=lambda: self._app.show_screen("home"),
        )
        T.separator(self, pady=(10, 0))

        # Scrollable content area
        canvas  = tk.Canvas(self, bg=T.BG, bd=0, highlightthickness=0)
        scrollbar = ttk.Scrollbar(self, orient="vertical",
                                  command=canvas.yview,
                                  style="SE.Vertical.TScrollbar")
        canvas.configure(yscrollcommand=scrollbar.set)

        scrollbar.pack(side="right", fill="y")
        canvas.pack(side="left", fill="both", expand=True)

        content = ttk.Frame(canvas, style="TFrame")
        win_id  = canvas.create_window((0, 0), window=content, anchor="nw")

        def _on_resize(e):
            canvas.itemconfig(win_id, width=e.width)

        def _on_frame_configure(_e):
            canvas.configure(scrollregion=canvas.bbox("all"))

        canvas.bind("<Configure>", _on_resize)
        content.bind("<Configure>", _on_frame_configure)

        # Mouse-wheel scrolling
        def _on_wheel(e):
            canvas.yview_scroll(int(-1 * (e.delta / 120)), "units")

        canvas.bind_all("<MouseWheel>", _on_wheel)

        pad = dict(padx=24)

        # ── Step 1 — Python ─────────────────────────────────────────────────
        self._section(content, "STEP 1  ·  INSTALL PYTHON 3.8+", pad)
        self._body(content,
                   "Both tools require Python 3.8 or newer.\n"
                   "Download the installer for your platform from python.org.\n"
                   "During installation, check  \"Add Python to PATH\".", pad)
        self._link_btn(content, "  ⬇  Download Python", _URL_PYTHON, pad)

        self._rule(content)

        # ── Step 2 — Pillow ──────────────────────────────────────────────────
        self._section(content, "STEP 2  ·  INSTALL PILLOW", pad)
        self._body(content,
                   "Pillow is the image processing library used by both tools.\n"
                   "After installing Python, open a terminal and run:", pad)
        self._code(content, "pip install Pillow", pad)

        self._rule(content)

        # ── Step 3 — texconv (Image Converter only) ─────────────────────────
        self._section(content, "STEP 3  ·  TEXCONV  (IMAGE CONVERTER ONLY)", pad)
        self._body(content,
                   "texconv.exe is the DirectXTex encoder used to produce\n"
                   "high-quality BC7_UNORM DDS files for SE modding.\n\n"
                   "Download the latest release from Microsoft's DirectXTex\n"
                   "repository.  Place  texconv.exe  in either:\n"
                   "  • the same folder as  se_lcd_gui.py, or\n"
                   "  • anywhere on your system PATH.", pad)
        self._link_btn(content, "  ⬇  Download DirectXTex (texconv.exe)",
                       _URL_DIRECTXTEX, pad)
        self._body(content,
                   "If texconv is not found, the tool falls back to\n"
                   "Pillow's built-in DDS encoder automatically.", pad)

        self._rule(content)

        # ── Optional — ImageMagick ────────────────────────────────────────────
        self._section(content, "OPTIONAL  ·  IMAGEMAGICK", pad)
        self._body(content,
                   "ImageMagick provides additional format support for the\n"
                   "Image Converter (RAW, TIFF, HEIC, etc.).\n"
                   "It is not required for normal use.", pad)
        self._link_btn(content, "  ⬇  Download ImageMagick", _URL_IMAGEMAGICK, pad)

        ttk.Frame(content, style="TFrame", height=20).pack()

    # -----------------------------------------------------------------------
    # Content helpers
    # -----------------------------------------------------------------------

    def _section(self, parent, text: str, pad: dict) -> None:
        ttk.Label(parent, text=f"▣  {text}",
                  style="Section.TLabel").pack(anchor="w", pady=(14, 2), **pad)

    def _body(self, parent, text: str, pad: dict) -> None:
        tk.Label(parent, text=text,
                 bg=T.BG, fg=T.TEXT,
                 font=("Courier New", 9),
                 justify="left", anchor="w").pack(anchor="w", pady=(2, 0), **pad)

    def _code(self, parent, text: str, pad: dict) -> None:
        frame = tk.Frame(parent, bg=T.PANEL,
                         highlightthickness=1,
                         highlightbackground=T.BORDER)
        frame.pack(fill="x", pady=(4, 2), **pad)
        tk.Label(frame, text=text,
                 bg=T.PANEL, fg=T.CYAN,
                 font=("Courier New", 10, "bold"),
                 anchor="w", padx=12, pady=6).pack(anchor="w")

    def _link_btn(self, parent, text: str, url: str, pad: dict) -> None:
        btn = tk.Label(parent, text=text,
                       bg=T.BG, fg=T.BLUE,
                       font=("Courier New", 9, "underline"),
                       cursor="hand2", anchor="w")
        btn.pack(anchor="w", pady=(4, 2), **pad)
        btn.bind("<Button-1>", lambda _e: webbrowser.open(url))
        btn.bind("<Enter>",    lambda _e: btn.config(fg=T.CYAN))
        btn.bind("<Leave>",    lambda _e: btn.config(fg=T.BLUE))

    def _rule(self, parent) -> None:
        tk.Frame(parent, bg=T.BORDER, height=1).pack(
            fill="x", padx=24, pady=(14, 0))
