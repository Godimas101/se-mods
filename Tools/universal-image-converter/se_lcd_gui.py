#!/usr/bin/env python3
"""
se_lcd_gui.py — Space Engineers LCD Image Converter (GUI)
A dark industrial-themed tkinter GUI wrapping se_lcd_convert.convert_image().

Run with:
    python se_lcd_gui.py
"""

import base64
import io
import math
import queue
import sys
import threading
from pathlib import Path

import tkinter as tk
from tkinter import filedialog, ttk

# ---------------------------------------------------------------------------
# Import from the CLI module — no conversion logic is duplicated here.
# ---------------------------------------------------------------------------
try:
    from se_lcd_convert import (
        convert_image,
        SUPPORTED_EXTS,
        DEFAULT_MAX_SIZE,
        SCREEN_PRESETS,
        PRESET_NAMES,
        SCREEN_REFERENCE_DATA,
        get_preset,
        mip_count,
        _detect_texconv,
        _detect_wand,
        _check_pillow,
    )
except ImportError as e:
    import tkinter.messagebox as mb
    root = tk.Tk()
    root.withdraw()
    mb.showerror(
        "Import Error",
        f"Could not import se_lcd_convert.py.\n"
        f"Make sure it lives in the same directory as se_lcd_gui.py.\n\n{e}",
    )
    sys.exit(1)


# ===========================================================================
# SE Colour Palette
# ===========================================================================
BG      = "#0d1117"
PANEL   = "#161b22"
CYAN    = "#00d4ff"
BLUE    = "#1f6feb"
TEXT    = "#e6edf3"
MUTED   = "#8b949e"
GREEN   = "#3fb950"
ORANGE  = "#d29922"
RED     = "#f85149"
HOVER   = "#21262d"
BORDER  = "#30363d"

FONT_MONO  = "Courier New"
FONT_BODY  = ("Courier New", 10)
FONT_LABEL = ("Courier New", 10)
FONT_TITLE = ("Courier New", 16, "bold")
FONT_SMALL = ("Courier New", 8)

SIZE_OPTIONS = ["256", "512", "1024", "2048"]
DEFAULT_SIZE = "1024"


# ===========================================================================
# Preset display formatting
# Monospace font lets us column-align: name left-justified, ratio right-justified
# e.g. "Wide LCD Panel        2:1"
# ===========================================================================

def _fmt_preset(name: str) -> str:
    """Return a fixed-width display string for a preset name."""
    if "  ·  " in name:
        left, ratio = name.split("  ·  ", 1)
        return f"  {left:<20}  {ratio:>4}  "
    return f"  {name}"  # "Custom" — no ratio column

# Display names shown in the combobox
_PRESET_DISPLAY_NAMES = [_fmt_preset(n) for n in PRESET_NAMES]
# Map display name → original preset name for get_preset() lookup
_PRESET_DISPLAY_MAP   = {_fmt_preset(n): n for n in PRESET_NAMES}


# ===========================================================================
# Helpers
# ===========================================================================

def _aspect_label(surf_w: int, surf_h: int) -> str:
    """Return a simplified aspect ratio string, e.g. '16:9'."""
    g = math.gcd(surf_w, surf_h)
    return f"{surf_w//g}:{surf_h//g}"


# ===========================================================================
# Embedded window icon
# ===========================================================================

def _build_icon_photoimage(root_widget):
    try:
        from PIL import Image as PILImage, ImageDraw, ImageFont
        size = 64
        img  = PILImage.new("RGBA", (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        cx, cy, r = size//2, size//2, 28
        import math as _math
        pts = [(cx + r*_math.cos(_math.radians(a)),
                cy + r*_math.sin(_math.radians(a))) for a in range(0, 360, 60)]
        draw.polygon(pts, fill="#0d1117")
        for offset in range(3):
            ro = r - offset
            pts_o = [(cx + ro*_math.cos(_math.radians(a)),
                      cy + ro*_math.sin(_math.radians(a))) for a in range(0, 360, 60)]
            draw.polygon(pts_o, outline="#00d4ff")
        draw.text((cx-12, cy-9), "SE", fill="#00d4ff", font=ImageFont.load_default())
        buf = io.BytesIO()
        img.save(buf, format="PNG")
        buf.seek(0)
        return tk.PhotoImage(data=base64.b64encode(buf.read()).decode())
    except Exception:
        return None


def _draw_hex_canvas(parent):
    import math as _math
    c = tk.Canvas(parent, width=54, height=54, bg=BG, bd=0, highlightthickness=0)
    cx, cy, r = 27, 27, 24
    pts = []
    for a in range(0, 360, 60):
        pts += [cx + r*_math.cos(_math.radians(a-30)),
                cy + r*_math.sin(_math.radians(a-30))]
    c.create_polygon(pts, fill=PANEL, outline=CYAN, width=2)
    ri, pts2 = 16, []
    for a in range(0, 360, 60):
        pts2 += [cx + ri*_math.cos(_math.radians(a-30)),
                 cy + ri*_math.sin(_math.radians(a-30))]
    c.create_polygon(pts2, fill=BG, outline=CYAN, width=1)
    c.create_text(cx, cy, text="SE", fill=CYAN, font=("Courier New", 12, "bold"))
    return c


# ===========================================================================
# ttk Style
# ===========================================================================

def _configure_styles(style: ttk.Style):
    style.theme_use("clam")

    style.configure("TFrame",       background=BG)
    style.configure("Panel.TFrame", background=PANEL)

    style.configure("TLabel",        background=BG,    foreground=TEXT,  font=FONT_LABEL)
    style.configure("Panel.TLabel",  background=PANEL, foreground=TEXT,  font=FONT_LABEL)
    style.configure("Muted.TLabel",  background=BG,    foreground=MUTED, font=("Courier New", 9))
    style.configure("Title.TLabel",  background=BG,    foreground=CYAN,  font=FONT_TITLE)
    style.configure("Section.TLabel",background=BG,    foreground=CYAN,  font=("Courier New", 10, "bold"))

    style.configure("SE.TButton",
        background=PANEL, foreground=CYAN,
        bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
        relief="flat", font=FONT_LABEL, padding=(10, 4))
    style.map("SE.TButton",
        background=[("active", HOVER), ("disabled", BG)],
        foreground=[("disabled", BORDER)])

    style.configure("Info.TButton",
        background=PANEL, foreground=MUTED,
        bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
        relief="flat", font=("Courier New", 9), padding=(4, 2))
    style.map("Info.TButton",
        background=[("active", HOVER)],
        foreground=[("active", CYAN)])

    style.configure("SE.Horizontal.TProgressbar",
        troughcolor=PANEL, background=CYAN,
        bordercolor=BORDER, darkcolor=CYAN, lightcolor=CYAN,
        thickness=18)

    style.configure("SE.TCheckbutton",
        background=BG, foreground=TEXT, font=FONT_LABEL,
        indicatorcolor=PANEL, indicatordiameter=14)
    style.map("SE.TCheckbutton",
        indicatorcolor=[("selected", CYAN), ("!selected", PANEL)],
        background=[("active", BG)],
        foreground=[("active", TEXT), ("disabled", BORDER)])

    style.configure("SE.TCombobox",
        fieldbackground=PANEL, background=PANEL,
        foreground=TEXT, selectforeground=CYAN, selectbackground=PANEL,
        bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
        arrowcolor=CYAN, font=FONT_LABEL)
    style.map("SE.TCombobox",
        fieldbackground=[("readonly", PANEL)],
        selectbackground=[("readonly", PANEL)],
        selectforeground=[("readonly", CYAN)])

    style.configure("SE.Vertical.TScrollbar",
        background=PANEL, troughcolor=BG,
        bordercolor=BORDER, arrowcolor=CYAN,
        darkcolor=PANEL, lightcolor=PANEL)
    style.map("SE.Vertical.TScrollbar", background=[("active", HOVER)])

    # Treeview for the reference window
    style.configure("SE.Treeview",
        background=BG, foreground=TEXT, fieldbackground=BG,
        bordercolor=BORDER, font=("Courier New", 9), rowheight=22)
    style.map("SE.Treeview",
        background=[("selected", BLUE)],
        foreground=[("selected", TEXT)])
    style.configure("SE.Treeview.Heading",
        background=PANEL, foreground=CYAN,
        font=("Courier New", 9, "bold"),
        bordercolor=BORDER, relief="flat")
    style.map("SE.Treeview.Heading",
        background=[("active", HOVER)])


# ===========================================================================
# Themed dialog helper
# ===========================================================================

def _themed_askokcancel(parent, title: str, message: str) -> bool:
    """
    Dark-themed modal OK / Cancel dialog matching the app's colour scheme.
    Returns True if OK was clicked, False otherwise.
    """
    result = tk.BooleanVar(value=False)

    dlg = tk.Toplevel(parent)
    dlg.title(title)
    dlg.configure(bg=BG)
    dlg.resizable(False, False)
    dlg.transient(parent)
    dlg.grab_set()

    # ── Header ──────────────────────────────────────────────────────────────
    hdr = ttk.Frame(dlg, style="TFrame")
    hdr.pack(fill="x", padx=16, pady=(14, 0))
    ttk.Label(hdr, text=f"▣  {title.upper()}",
              style="Section.TLabel").pack(anchor="w")
    tk.Frame(dlg, bg=BORDER, height=1).pack(fill="x", padx=16, pady=(8, 0))

    # ── Message ─────────────────────────────────────────────────────────────
    tk.Label(
        dlg, text=message,
        bg=BG, fg=TEXT,
        font=("Courier New", 9),
        justify="left", wraplength=420, anchor="w",
        padx=16, pady=12,
    ).pack(fill="x")

    tk.Frame(dlg, bg=BORDER, height=1).pack(fill="x", padx=16, pady=(0, 0))

    # ── Buttons ──────────────────────────────────────────────────────────────
    btn_row = ttk.Frame(dlg, style="TFrame")
    btn_row.pack(fill="x", padx=16, pady=(10, 14))

    def _ok():
        result.set(True)
        dlg.destroy()

    def _cancel():
        result.set(False)
        dlg.destroy()

    dlg.bind("<Return>", lambda _: _ok())
    dlg.bind("<Escape>", lambda _: _cancel())

    ttk.Button(btn_row, text="CANCEL", command=_cancel,
               style="SE.TButton", width=8).pack(side="right", padx=(6, 0))
    ttk.Button(btn_row, text="OK", command=_ok,
               style="SE.TButton", width=8).pack(side="right")

    # ── Centre on parent ─────────────────────────────────────────────────────
    parent.update_idletasks()
    dlg.update_idletasks()
    x = parent.winfo_x() + (parent.winfo_width()  - dlg.winfo_reqwidth())  // 2
    y = parent.winfo_y() + (parent.winfo_height() - dlg.winfo_reqheight()) // 2
    dlg.geometry(f"+{x}+{y}")

    dlg.wait_window()
    return result.get()


# ===========================================================================
# LCD Screen Reference Window
# ===========================================================================

class ScreenReferenceWindow(tk.Toplevel):
    """Floating reference window showing every SE block's screen dimensions."""

    def __init__(self, parent):
        super().__init__(parent)
        self.title("LCD Screen Reference")
        self.configure(bg=BG)
        self.resizable(True, True)
        self.geometry("780x540")
        self.minsize(600, 400)

        # Keep on top of parent but not system-modal
        self.transient(parent)

        self._build()

    def _build(self):
        # Header
        hdr = ttk.Frame(self, style="TFrame")
        hdr.pack(fill="x", padx=14, pady=(12, 4))
        ttk.Label(hdr, text="▣  LCD SCREEN REFERENCE",
                  style="Section.TLabel").pack(side="left")
        ttk.Label(hdr,
                  text="Texture size = DDS output  ·  Visible area = what the player sees on screen",
                  style="Muted.TLabel").pack(side="left", padx=(16, 0))

        sep = tk.Frame(self, bg=BORDER, height=1)
        sep.pack(fill="x", padx=14, pady=(4, 8))

        # Treeview + scrollbar
        tree_frame = ttk.Frame(self, style="TFrame")
        tree_frame.pack(fill="both", expand=True, padx=14, pady=(0, 0))

        cols = ("block", "screen", "texture", "visible", "aspect")
        self._tree = ttk.Treeview(tree_frame, columns=cols, show="headings",
                                  style="SE.Treeview", selectmode="browse")

        self._tree.heading("block",   text="Block")
        self._tree.heading("screen",  text="Screen")
        self._tree.heading("texture", text="Texture Size")
        self._tree.heading("visible", text="Visible Area")
        self._tree.heading("aspect",  text="Aspect")

        self._tree.column("block",   width=260, minwidth=160, stretch=True)
        self._tree.column("screen",  width=190, minwidth=120, stretch=True)
        self._tree.column("texture", width=100, minwidth=80,  stretch=False, anchor="center")
        self._tree.column("visible", width=100, minwidth=80,  stretch=False, anchor="center")
        self._tree.column("aspect",  width=80,  minwidth=60,  stretch=False, anchor="center")

        vsb = ttk.Scrollbar(tree_frame, orient="vertical",
                            command=self._tree.yview,
                            style="SE.Vertical.TScrollbar")
        self._tree.configure(yscrollcommand=vsb.set)

        self._tree.pack(side="left", fill="both", expand=True)
        vsb.pack(side="right", fill="y")

        # Populate
        self._tree.tag_configure("odd",  background=BG)
        self._tree.tag_configure("even", background=PANEL)

        for i, (block, screen, tw, th, sw, sh) in enumerate(SCREEN_REFERENCE_DATA):
            tag  = "even" if i % 2 == 0 else "odd"
            self._tree.insert("", "end", values=(
                block,
                screen,
                f"{tw}×{th}",
                f"{sw}×{sh}",
                _aspect_label(sw, sh),
            ), tags=(tag,))

        # Footer
        foot = ttk.Frame(self, style="TFrame")
        foot.pack(fill="x", padx=14, pady=(8, 12))
        ttk.Label(foot,
                  text="Values sourced from Whiplash141/Whips-Image-Converter + SE game files audit (2026).",
                  style="Muted.TLabel").pack(side="left")
        close_btn = ttk.Button(foot, text="CLOSE",
                               command=self.destroy,
                               style="SE.TButton")
        close_btn.pack(side="right")


# ===========================================================================
# Main Application Window
# ===========================================================================

class SEConverterApp(tk.Tk):

    def __init__(self):
        super().__init__()

        self.title("Universal Image Converter")
        self.configure(bg=BG)
        self.resizable(False, False)
        self.geometry("680x760")

        self._icon_img = _build_icon_photoimage(self)
        if self._icon_img:
            try:
                self.iconphoto(True, self._icon_img)
            except Exception:
                pass

        self._style = ttk.Style(self)
        _configure_styles(self._style)

        # Internal state
        self._files: list[Path]  = []
        self._out_folder: Path | None = None
        self._converting  = False
        self._q: queue.Queue = queue.Queue()
        self._ref_window: ScreenReferenceWindow | None = None

        # Detect backends
        self._has_pillow  = _check_pillow()
        self._use_texconv = _detect_texconv() if self._has_pillow else None
        self._use_wand    = _detect_wand()    if self._has_pillow else False

        self._build_ui()
        self._log_startup_info()

    # -----------------------------------------------------------------------
    # UI construction
    # -----------------------------------------------------------------------

    def _build_ui(self):
        pad = dict(padx=16, pady=0)

        # ── Header ──────────────────────────────────────────────────────────
        hdr = ttk.Frame(self, style="TFrame")
        hdr.pack(fill="x", padx=16, pady=(14, 0))

        hex_canvas = _draw_hex_canvas(hdr)
        hex_canvas.pack(side="left", padx=(0, 12))

        hdr_text = ttk.Frame(hdr, style="TFrame")
        hdr_text.pack(side="left", fill="both")

        ttk.Label(hdr_text, text="UNIVERSAL IMAGE CONVERTER",
                  style="Title.TLabel").pack(anchor="w")
        ttk.Label(hdr_text,
                  text="Convert images to DDS format for Space Engineers LCD mods.",
                  style="Muted.TLabel").pack(anchor="w")
        ttk.Label(hdr_text,
                  text=f"Supported: {', '.join(sorted(e.lstrip('.').upper() for e in SUPPORTED_EXTS))}",
                  style="Muted.TLabel").pack(anchor="w")

        self._separator(pady=(10, 8))

        # ── Input Images ────────────────────────────────────────────────────
        ttk.Label(self, text="▣  INPUT IMAGES",
                  style="Section.TLabel").pack(anchor="w", **pad)

        list_frame = ttk.Frame(self, style="Panel.TFrame")
        list_frame.pack(fill="x", padx=16, pady=(4, 0))

        lb_container = tk.Frame(list_frame, bg=PANEL)
        lb_container.pack(side="left", fill="both", expand=True, padx=(6, 0), pady=6)

        self._listbox = tk.Listbox(
            lb_container,
            bg=PANEL, fg=TEXT,
            selectbackground=BLUE, selectforeground=TEXT,
            highlightthickness=1, highlightcolor=BORDER,
            highlightbackground=BORDER,
            relief="flat", bd=0,
            font=("Courier New", 9),
            height=6, activestyle="none",
        )
        self._lb_scrollbar = ttk.Scrollbar(lb_container, orient="vertical",
                                           command=self._listbox.yview,
                                           style="SE.Vertical.TScrollbar")
        self._listbox.config(yscrollcommand=self._lb_scrollbar.set)
        self._listbox.pack(side="left", fill="both", expand=True)
        # scrollbar packed conditionally in _refresh_listbox

        self._list_placeholder = tk.Label(
            lb_container, text="No images selected",
            bg=PANEL, fg=MUTED, font=("Courier New", 9))
        self._list_placeholder.place(relx=0.5, rely=0.5, anchor="center")

        btn_frame = ttk.Frame(list_frame, style="Panel.TFrame")
        btn_frame.pack(side="right", fill="y", padx=8, pady=6)
        self._btn_select = self._se_button(btn_frame, "SELECT", self._on_select, width=8)
        self._btn_select.pack(pady=(0, 6))
        self._btn_clear  = self._se_button(btn_frame, "CLEAR",  self._on_clear,  width=8)
        self._btn_clear.pack()

        self._file_count_var = tk.StringVar(value="")
        ttk.Label(self, textvariable=self._file_count_var,
                  style="Muted.TLabel").pack(anchor="e", padx=18, pady=(2, 0))

        self._separator(pady=(8, 8))

        # ── Output Settings ──────────────────────────────────────────────────
        ttk.Label(self, text="▣  OUTPUT SETTINGS",
                  style="Section.TLabel").pack(anchor="w", **pad)

        self.option_add("*TCombobox*Listbox*Background",       PANEL)
        self.option_add("*TCombobox*Listbox*Foreground",       TEXT)
        self.option_add("*TCombobox*Listbox*SelectBackground", BLUE)
        self.option_add("*TCombobox*Listbox*Font",             ("Courier New", 10))

        # Grid layout: column 0 = fixed-width labels, column 1 = controls
        settings_frame = ttk.Frame(self, style="TFrame")
        settings_frame.pack(fill="x", padx=16, pady=(6, 0))
        settings_frame.columnconfigure(0, minsize=115)
        settings_frame.columnconfigure(1, weight=1)

        # Row 0: File Rename
        ttk.Label(settings_frame, text="File Rename:", style="TLabel").grid(
            row=0, column=0, sticky="w", pady=(0, 6))
        rename_ctrl = ttk.Frame(settings_frame, style="TFrame")
        rename_ctrl.grid(row=0, column=1, sticky="w", pady=(0, 6))

        self._affix_mode_var = tk.StringVar(value="None")
        affix_combo = ttk.Combobox(
            rename_ctrl, textvariable=self._affix_mode_var,
            values=["None", "Add Prefix", "Add Suffix"],
            state="readonly", width=12, style="SE.TCombobox",
        )
        affix_combo.pack(side="left", padx=(0, 8))
        affix_combo.bind("<<ComboboxSelected>>", self._on_affix_change)

        self._affix_text_var = tk.StringVar(value="converted_")
        self._affix_entry = tk.Entry(
            rename_ctrl,
            textvariable=self._affix_text_var,
            bg=PANEL, fg=TEXT,
            insertbackground=CYAN,
            disabledbackground=BG, disabledforeground=MUTED,
            font=("Courier New", 9),
            relief="flat", bd=1,
            highlightthickness=1,
            highlightbackground=BORDER, highlightcolor=CYAN,
            width=16, state="disabled",
        )
        self._affix_entry.pack(side="left", ipady=3)

        # Row 1: Screen Target
        ttk.Label(settings_frame, text="Screen Target:", style="TLabel").grid(
            row=1, column=0, sticky="e", padx=(0, 8), pady=(0, 6))
        screen_ctrl = ttk.Frame(settings_frame, style="TFrame")
        screen_ctrl.grid(row=1, column=1, sticky="w", pady=(0, 6))

        self._screen_var = tk.StringVar(value=_PRESET_DISPLAY_NAMES[0])
        screen_combo = ttk.Combobox(
            screen_ctrl, textvariable=self._screen_var,
            values=_PRESET_DISPLAY_NAMES, state="readonly",
            width=30, style="SE.TCombobox", font=FONT_LABEL,
        )
        screen_combo.pack(side="left", padx=(0, 6))
        screen_combo.bind("<<ComboboxSelected>>", self._on_screen_change)

        self._btn_info = ttk.Button(screen_ctrl, text="ⓘ",
                                    command=self._on_open_reference,
                                    style="Info.TButton")
        self._btn_info.pack(side="left")

        # Row 2: Custom controls (hidden unless "Custom" selected)
        self._custom_row = ttk.Frame(settings_frame, style="TFrame")
        # Not gridded yet — shown/hidden dynamically via _on_screen_change

        ttk.Label(self._custom_row, text="Max Size:", style="TLabel").pack(side="left")
        self._size_var = tk.StringVar(value=DEFAULT_SIZE)
        size_combo = ttk.Combobox(
            self._custom_row, textvariable=self._size_var,
            values=SIZE_OPTIONS, state="readonly",
            width=7, style="SE.TCombobox",
        )
        size_combo.pack(side="left", padx=(6, 4))
        ttk.Label(self._custom_row, text="px", style="Muted.TLabel").pack(side="left", padx=(0, 20))

        self._aspect_var = tk.BooleanVar(value=False)
        ttk.Checkbutton(
            self._custom_row, text="Preserve Aspect Ratio",
            variable=self._aspect_var,
            style="SE.TCheckbutton",
        ).pack(side="left")

        # Row 3: Output Folder
        ttk.Label(settings_frame, text="Output Folder:", style="TLabel").grid(
            row=3, column=0, sticky="e", padx=(0, 8))
        outfolder_ctrl = ttk.Frame(settings_frame, style="TFrame")
        outfolder_ctrl.grid(row=3, column=1, sticky="ew")

        self._outfolder_var = tk.StringVar(value="same as source file")
        outfolder_entry = tk.Entry(
            outfolder_ctrl,
            textvariable=self._outfolder_var,
            state="readonly",
            readonlybackground=PANEL,
            fg=MUTED, font=("Courier New", 9),
            relief="flat", bd=1,
            highlightthickness=1,
            highlightbackground=BORDER, highlightcolor=CYAN,
            width=33,
        )
        outfolder_entry.pack(side="left", padx=(0, 8), ipady=3)

        self._btn_browse    = self._se_button(outfolder_ctrl, "BROWSE...", self._on_browse, width=10)
        self._btn_browse.pack(side="left", padx=(0, 6))
        self._btn_clear_out = self._se_button(outfolder_ctrl, "RESET", self._on_reset_outfolder, width=7)
        self._btn_clear_out.pack(side="left")

        self._separator(pady=(14, 10))

        # ── Convert Button ───────────────────────────────────────────────────
        convert_frame = ttk.Frame(self, style="TFrame")
        convert_frame.pack(pady=(0, 10))

        self._btn_convert = tk.Button(
            convert_frame,
            text="  ▶  CONVERT  ▶  ",
            command=self._on_convert,
            bg=PANEL, fg=CYAN,
            activebackground=HOVER, activeforeground=CYAN,
            font=("Courier New", 13, "bold"),
            relief="flat", bd=0,
            padx=24, pady=8,
            cursor="hand2",
            highlightthickness=2,
            highlightbackground=CYAN, highlightcolor=CYAN,
        )
        self._btn_convert.pack()
        self._btn_convert.bind("<Enter>", lambda e: self._btn_convert.config(bg=HOVER))
        self._btn_convert.bind("<Leave>", lambda e: self._btn_convert.config(bg=PANEL))

        self._separator(pady=(10, 8))

        # ── Progress ─────────────────────────────────────────────────────────
        progress_frame = ttk.Frame(self, style="TFrame")
        progress_frame.pack(fill="x", padx=16, pady=(0, 4))

        self._progress_var = tk.DoubleVar(value=0.0)
        self._progressbar  = ttk.Progressbar(
            progress_frame, variable=self._progress_var,
            maximum=100.0, mode="determinate",
            style="SE.Horizontal.TProgressbar", length=520,
        )
        self._progressbar.pack(side="left", fill="x", expand=True, pady=2)

        self._pct_var = tk.StringVar(value="  0%")
        ttk.Label(progress_frame, textvariable=self._pct_var,
                  style="Muted.TLabel", width=5).pack(side="left", padx=(8, 0))

        self._status_var = tk.StringVar(value="")
        ttk.Label(self, textvariable=self._status_var,
                  style="Muted.TLabel").pack(anchor="w", padx=18, pady=(0, 6))

        # ── Log Area ─────────────────────────────────────────────────────────
        log_frame = ttk.Frame(self, style="Panel.TFrame")
        log_frame.pack(fill="both", expand=True, padx=16, pady=(0, 14))

        log_header = ttk.Frame(log_frame, style="Panel.TFrame")
        log_header.pack(fill="x", padx=6, pady=(4, 2))
        ttk.Label(log_header, text="▣  CONVERSION LOG",
                  style="Section.TLabel", background=PANEL).pack(side="left")
        tk.Button(
            log_header, text="CLEAR LOG",
            command=self._clear_log,
            bg=PANEL, fg=MUTED,
            activebackground=HOVER, activeforeground=TEXT,
            font=("Courier New", 8), relief="flat", bd=0, cursor="hand2",
        ).pack(side="right")

        self._log_text = tk.Text(
            log_frame,
            bg=BG, fg=TEXT,
            font=("Courier New", 9),
            relief="flat", bd=0,
            state="disabled", height=10,
            wrap="word", highlightthickness=0,
            selectbackground=BLUE,
        )
        log_sb_v = ttk.Scrollbar(log_frame, orient="vertical",
                                 command=self._log_text.yview,
                                 style="SE.Vertical.TScrollbar")
        self._log_text.config(yscrollcommand=log_sb_v.set)
        log_sb_v.pack(side="right", fill="y")
        self._log_text.pack(fill="both", expand=True, padx=6, pady=(0, 6))

        self._log_text.tag_configure("info",    foreground=TEXT)
        self._log_text.tag_configure("success", foreground=GREEN)
        self._log_text.tag_configure("warn",    foreground=ORANGE)
        self._log_text.tag_configure("error",   foreground=RED)
        self._log_text.tag_configure("cyan",    foreground=CYAN)
        self._log_text.tag_configure("muted",   foreground=MUTED)
        self._log_text.tag_configure("sep",     foreground=MUTED, justify="center")

    # -----------------------------------------------------------------------
    # Widget helpers
    # -----------------------------------------------------------------------

    def _separator(self, pady=(8, 8)):
        tk.Frame(self, bg=BORDER, height=1).pack(fill="x", padx=16, pady=pady)

    def _se_button(self, parent, text, command, width=None):
        kw = {"width": width} if width else {}
        return ttk.Button(parent, text=text, command=command,
                          style="SE.TButton", **kw)

    # -----------------------------------------------------------------------
    # Log helpers
    # -----------------------------------------------------------------------

    def _log(self, msg: str, tag: str = "info"):
        self._log_text.config(state="normal")
        self._log_text.insert("end", msg + "\n", tag)
        self._log_text.see("end")
        self._log_text.config(state="disabled")

    def _log_sep(self):
        self._log("━" * 38, "sep")

    def _clear_log(self):
        self._log_text.config(state="normal")
        self._log_text.delete("1.0", "end")
        self._log_text.config(state="disabled")

    def _log_startup_info(self):
        self._log_sep()
        self._log("  Universal Image Converter", "cyan")
        self._log_sep()
        if not self._has_pillow:
            self._log("  [ERROR] Pillow is not installed.", "error")
            self._log("          pip install Pillow", "warn")
        else:
            if self._use_texconv:
                self._log("  Encoder : texconv.exe  →  BC7_UNORM", "success")
                self._log("  Format  : DDS / BC7_UNORM (DXGI 98)", "info")
            elif self._use_wand:
                self._log("  Encoder : wand (ImageMagick)  →  DXT5", "info")
                self._log("  Format  : DDS / DXT5 (BC3_UNORM)", "info")
                self._log("  Tip     : Install texconv.exe for BC7 output (best quality).", "muted")
                self._log("            github.com/microsoft/DirectXTex/releases", "muted")
            else:
                self._log("  Encoder : built-in pure-Python  →  DXT5", "warn")
                self._log("  Format  : DDS / DXT5 (BC3_UNORM)", "info")
                self._log("  Tip     : Install texconv.exe for BC7 output (best quality).", "muted")
                self._log("            github.com/microsoft/DirectXTex/releases", "muted")
        self._log_sep()

    # -----------------------------------------------------------------------
    # Button / widget callbacks
    # -----------------------------------------------------------------------

    def _on_select(self):
        ext_str = " ".join(f"*{e}" for e in sorted(SUPPORTED_EXTS))
        paths = filedialog.askopenfilenames(
            title="Select Images",
            filetypes=[
                ("Supported images", ext_str),
                ("PNG files", "*.png"),
                ("JPEG files", "*.jpg *.jpeg"),
                ("BMP files", "*.bmp"),
                ("TGA files", "*.tga"),
                ("GIF files", "*.gif"),
                ("DDS files", "*.dds"),
                ("All files", "*.*"),
            ],
        )
        if not paths:
            return
        existing = set(self._files)
        added = 0
        for p in [Path(x) for x in paths]:
            if p not in existing:
                self._files.append(p)
                existing.add(p)
                added += 1
        self._refresh_listbox()
        if added:
            self._log(f"  Added {added} file(s). Total: {len(self._files)}", "muted")

    def _on_clear(self):
        self._files.clear()
        self._refresh_listbox()
        self._log("  Selection cleared.", "muted")

    def _on_browse(self):
        folder = filedialog.askdirectory(title="Select Output Folder")
        if folder:
            self._out_folder = Path(folder)
            self._outfolder_var.set(str(self._out_folder))

    def _on_reset_outfolder(self):
        self._out_folder = None
        self._outfolder_var.set("same as source file")

    def _on_affix_change(self, _event=None):
        mode = self._affix_mode_var.get()
        self._affix_entry.config(state="normal" if mode != "None" else "disabled")
        # Suggest a sensible default when switching modes
        current = self._affix_text_var.get()
        if mode == "Add Prefix" and current == "_converted":
            self._affix_text_var.set("converted_")
        elif mode == "Add Suffix" and current == "converted_":
            self._affix_text_var.set("_converted")

    def _on_screen_change(self, _event=None):
        """Show/hide the custom controls row based on selected preset."""
        name = _PRESET_DISPLAY_MAP.get(self._screen_var.get(), self._screen_var.get())
        if name == "Custom":
            self._custom_row.grid(row=2, column=0, columnspan=2,
                                  sticky="ew", pady=(0, 6))
        else:
            self._custom_row.grid_remove()

    def _on_open_reference(self):
        """Open (or bring to front) the screen reference window."""
        if self._ref_window is not None and self._ref_window.winfo_exists():
            self._ref_window.lift()
            self._ref_window.focus_set()
        else:
            self._ref_window = ScreenReferenceWindow(self)

    def _on_convert(self):
        if self._converting:
            return
        if not self._has_pillow:
            self._log("  [ERROR] Pillow is required. Run: pip install Pillow", "error")
            return
        if not self._files:
            self._log("  [ERROR] No images selected.", "error")
            return

        screen_display = self._screen_var.get()
        screen_name    = _PRESET_DISPLAY_MAP.get(screen_display, screen_display)
        gen_mipmaps  = True
        affix_mode   = self._affix_mode_var.get()
        affix_text   = self._affix_text_var.get() if affix_mode != "None" else ""
        prefix       = affix_text if affix_mode == "Add Prefix" else ""
        suffix       = affix_text if affix_mode == "Add Suffix"  else ""

        if screen_name == "Custom":
            preset       = None
            custom_size  = int(self._size_var.get())
            custom_asp   = self._aspect_var.get()
            if custom_size <= 0 or (custom_size & (custom_size - 1)) != 0:
                self._log(f"  [ERROR] Invalid max size: {custom_size}", "error")
                return
        else:
            preset       = get_preset(screen_name)
            custom_size  = DEFAULT_MAX_SIZE
            custom_asp   = False

        if not self._use_texconv:
            proceed = _themed_askokcancel(
                self,
                "Low Quality Warning",
                "texconv.exe was not found on your PATH.\n\n"
                "Without it, images are encoded using the built-in "
                "pure-Python DXT5 encoder, which produces noticeably "
                "lower quality — colours may be posterized and subtle "
                "gradients may be lost.\n\n"
                "For best results, install texconv.exe (BC7_UNORM):\n\n"
                "  1. Download from:\n"
                "     github.com/microsoft/DirectXTex/releases\n\n"
                "  2. Place it on your system PATH\n"
                "     (e.g. C:\\Windows\\System32\\)\n\n"
                "  3. Relaunch the converter.\n\n"
                "Press OK to convert anyway, or Cancel to abort.",
            )
            if not proceed:
                return

        # Overwrite check — resolve all output paths before starting the thread
        would_overwrite = []
        for img_path in self._files:
            dest_dir = self._out_folder if self._out_folder is not None else img_path.parent
            out_path = dest_dir / (prefix + img_path.stem + suffix + ".dds")
            if out_path.exists():
                would_overwrite.append(out_path.name)
        if would_overwrite:
            names = "\n".join(f"  • {n}" for n in would_overwrite[:10])
            if len(would_overwrite) > 10:
                names += f"\n  ... and {len(would_overwrite) - 10} more"
            proceed = _themed_askokcancel(
                self,
                "Overwrite existing files?",
                f"The following file(s) already exist and will be overwritten:\n\n"
                f"{names}\n\n"
                f"Proceed?",
            )
            if not proceed:
                return

        self._start_conversion(preset, gen_mipmaps, prefix, suffix,
                               custom_size, custom_asp)

    # -----------------------------------------------------------------------
    # Listbox management
    # -----------------------------------------------------------------------

    def _refresh_listbox(self):
        self._listbox.delete(0, "end")
        if self._files:
            self._list_placeholder.place_forget()
            for p in self._files:
                self._listbox.insert("end", f"  {p.name}")
            self._file_count_var.set(f"{len(self._files)} file(s) selected")
        else:
            self._list_placeholder.place(relx=0.5, rely=0.5, anchor="center")
            self._file_count_var.set("")

        if len(self._files) > 6:
            self._lb_scrollbar.pack(side="right", fill="y")
        else:
            self._lb_scrollbar.pack_forget()

    # -----------------------------------------------------------------------
    # Conversion — runs in a background thread
    # -----------------------------------------------------------------------

    def _start_conversion(self, preset, gen_mipmaps: bool, prefix: str, suffix: str,
                          custom_size: int, custom_asp: bool):
        self._converting = True
        self._set_controls_state("disabled")
        self._progress_var.set(0.0)
        self._pct_var.set("  0%")
        self._status_var.set("")

        files_snapshot = list(self._files)
        out_folder     = self._out_folder

        if self._use_texconv:
            enc_label = "texconv/BC7"
        elif self._use_wand:
            enc_label = "wand/DXT5"
        else:
            enc_label = "built-in/DXT5"

        if preset is None:
            target_label = f"Custom {custom_size}px"
        else:
            target_label = preset.name

        if prefix:
            rename_str = f'Prefix : "{prefix}"'
        elif suffix:
            rename_str = f'Suffix : "{suffix}"'
        else:
            rename_str = "Rename : none"

        self._log("", "info")
        self._log_sep()
        self._log(f"  Starting conversion of {len(files_snapshot)} file(s)...", "cyan")
        self._log(f"  Target   : {target_label}", "muted")
        self._log(f"  Mipmaps  : {'yes' if gen_mipmaps else 'no'}  |  "
                  f"Encoder : {enc_label}  |  {rename_str}", "muted")
        self._log_sep()

        t = threading.Thread(
            target=self._worker,
            args=(files_snapshot, out_folder, preset, gen_mipmaps, prefix, suffix,
                  custom_size, custom_asp,
                  self._use_texconv, self._use_wand),
            daemon=True,
        )
        t.start()
        self.after(50, self._poll_queue)

    def _worker(self, files, out_folder, preset, gen_mipmaps, prefix, suffix,
                custom_size, custom_asp, use_texconv, use_wand):
        """Runs in background thread. Sends messages to main thread via _q."""
        import io as _io
        total  = len(files)
        ok = failed = 0

        captured  = _io.StringIO()
        old_stdout = sys.stdout
        sys.stdout = captured

        try:
            for idx, img_path in enumerate(files, 1):
                dest_dir = out_folder if out_folder is not None else img_path.parent
                dest_dir.mkdir(parents=True, exist_ok=True)

                pct = ((idx - 1) / total) * 100
                self._q.put(("progress", pct,
                              f"Converting [{idx}/{total}]: {img_path.name}"))

                try:
                    captured.truncate(0)
                    captured.seek(0)

                    convert_image(
                        img_path, dest_dir, preset, gen_mipmaps,
                        use_texconv, use_wand,
                        prefix=prefix,
                        suffix=suffix,
                        custom_max_size=custom_size,
                        custom_preserve_aspect=custom_asp,
                    )

                    printed = captured.getvalue().strip()
                    captured.truncate(0); captured.seek(0)

                    out_name = dest_dir / (prefix + img_path.stem + suffix + ".dds")
                    self._q.put(("log_success",
                                 f"  [{idx}/{total}] {img_path.name} → {out_name.name}"))
                    for line in (printed.splitlines() if printed else []):
                        self._q.put(("log_info", f"         {line.strip()}"))
                    ok += 1

                except Exception as exc:
                    printed = captured.getvalue().strip()
                    captured.truncate(0); captured.seek(0)
                    self._q.put(("log_error",
                                 f"  [{idx}/{total}] FAILED: {img_path.name} — {exc}"))
                    for line in (printed.splitlines() if printed else []):
                        self._q.put(("log_warn", f"         {line.strip()}"))
                    failed += 1

        finally:
            sys.stdout = old_stdout

        self._q.put(("done", ok, failed, total))

    def _poll_queue(self):
        try:
            while True:
                msg  = self._q.get_nowait()
                kind = msg[0]

                if kind == "progress":
                    _, pct, status = msg
                    self._progress_var.set(pct)
                    self._pct_var.set(f"{int(pct):3d}%")
                    self._status_var.set(status)

                elif kind == "log_success":
                    self._log(msg[1], "success")
                elif kind == "log_info":
                    self._log(msg[1], "info")
                elif kind == "log_warn":
                    self._log(msg[1], "warn")
                elif kind == "log_error":
                    self._log(msg[1], "error")

                elif kind == "done":
                    _, ok, failed, total = msg
                    self._on_conversion_done(ok, failed, total)
                    return

        except queue.Empty:
            pass

        if self._converting:
            self.after(50, self._poll_queue)

    def _on_conversion_done(self, ok: int, failed: int, total: int):
        self._converting = False
        self._progress_var.set(100.0)
        self._pct_var.set("100%")

        self._log_sep()
        if failed == 0:
            self._log(f"  Done — {ok}/{total} file(s) converted successfully.", "success")
        else:
            self._log(f"  Done — {ok} converted, {failed} failed.", "warn")
        self._log_sep()

        self._status_var.set(
            f"Complete — {ok} file(s) converted." if failed == 0
            else f"Complete — {ok} ok, {failed} failed."
        )
        self._set_controls_state("normal")

    # -----------------------------------------------------------------------
    # UI lock / unlock during conversion
    # -----------------------------------------------------------------------

    def _set_controls_state(self, state: str):
        ttk_state = "disabled" if state == "disabled" else "!disabled"

        for btn in (self._btn_select, self._btn_clear,
                    self._btn_browse, self._btn_clear_out, self._btn_info):
            btn.state([ttk_state])

        if state == "disabled":
            self._btn_convert.config(
                state="disabled", fg=MUTED,
                highlightbackground=BORDER, highlightcolor=BORDER)
            self._affix_entry.config(state="disabled")
        else:
            self._btn_convert.config(
                state="normal", fg=CYAN,
                highlightbackground=CYAN, highlightcolor=CYAN)
            if self._affix_mode_var.get() != "None":
                self._affix_entry.config(state="normal")


# ===========================================================================
# Entry point
# ===========================================================================

def main():
    app = SEConverterApp()
    app.mainloop()


if __name__ == "__main__":
    main()
