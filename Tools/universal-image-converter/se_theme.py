#!/usr/bin/env python3
"""
se_theme.py — Shared theme for the SE Tools suite.

All colour constants, font specs, ttk style configuration,
and reusable widget helpers.  Import this module in every screen.
"""

import base64
import io
import json
import math
import threading
import urllib.request
import webbrowser
import tkinter as tk
from tkinter import ttk


# ===========================================================================
# Colour Palette
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

# ===========================================================================
# Typography  (all Courier New — monospace keeps column alignment intact)
# ===========================================================================
FONT_MONO  = "Courier New"
FONT_BODY  = ("Courier New", 10)
FONT_LABEL = ("Courier New", 10)
FONT_TITLE = ("Courier New", 16, "bold")
FONT_SMALL = ("Courier New", 8)


# ===========================================================================
# ttk Style Configuration  — call once from the launcher at startup
# ===========================================================================

def configure_styles(style: ttk.Style) -> None:
    style.theme_use("clam")

    style.configure("TFrame",        background=BG)
    style.configure("Panel.TFrame",  background=PANEL)

    style.configure("TLabel",         background=BG,    foreground=TEXT,  font=FONT_LABEL)
    style.configure("Panel.TLabel",   background=PANEL, foreground=TEXT,  font=FONT_LABEL)
    style.configure("Muted.TLabel",   background=BG,    foreground=MUTED, font=("Courier New", 9))
    style.configure("Title.TLabel",   background=BG,    foreground=CYAN,  font=FONT_TITLE)
    style.configure("Section.TLabel", background=BG,    foreground=CYAN,  font=("Courier New", 10, "bold"))

    # Standard action button
    style.configure("SE.TButton",
        background=PANEL, foreground=CYAN,
        bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
        relief="flat", font=FONT_LABEL, padding=(10, 4))
    style.map("SE.TButton",
        background=[("active", HOVER), ("disabled", BG)],
        foreground=[("disabled", BORDER)])

    # Back navigation button — muted at rest, cyan on hover
    style.configure("Back.TButton",
        background=PANEL, foreground=MUTED,
        bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
        relief="flat", font=("Courier New", 10), padding=(10, 4))
    style.map("Back.TButton",
        background=[("active", HOVER)],
        foreground=[("active", CYAN)])

    # Small info / secondary button
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
# Hex Canvas Icon
# ===========================================================================

def draw_hex_canvas(parent, bg: str = BG, size: int = 54) -> tk.Canvas:
    c  = tk.Canvas(parent, width=size, height=size, bg=bg, bd=0, highlightthickness=0)
    cx = cy = size // 2
    r  = int(size * 24 / 54)
    ri = int(size * 16 / 54)

    def _hex_pts(radius):
        pts = []
        for a in range(0, 360, 60):
            pts += [cx + radius * math.cos(math.radians(a - 30)),
                    cy + radius * math.sin(math.radians(a - 30))]
        return pts

    c.create_polygon(_hex_pts(r),  fill=PANEL, outline=CYAN, width=2)
    c.create_polygon(_hex_pts(ri), fill=BG,    outline=CYAN, width=1)
    c.create_text(cx, cy, text="SE", fill=CYAN,
                  font=("Courier New", max(8, int(size * 12 / 54)), "bold"))
    return c


def build_icon_photoimage(root_widget) -> "tk.PhotoImage | None":
    try:
        from PIL import Image as PILImage, ImageFont
        from PIL import ImageDraw
        size = 64
        img  = PILImage.new("RGBA", (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        cx = cy = size // 2
        r  = 28
        pts = [(cx + r * math.cos(math.radians(a)),
                cy + r * math.sin(math.radians(a))) for a in range(0, 360, 60)]
        draw.polygon(pts, fill="#0d1117")
        for offset in range(3):
            ro   = r - offset
            pts2 = [(cx + ro * math.cos(math.radians(a)),
                     cy + ro * math.sin(math.radians(a))) for a in range(0, 360, 60)]
            draw.polygon(pts2, outline="#00d4ff")
        draw.text((cx - 12, cy - 9), "SE", fill="#00d4ff",
                  font=ImageFont.load_default())
        buf = io.BytesIO()
        img.save(buf, format="PNG")
        buf.seek(0)
        return tk.PhotoImage(data=base64.b64encode(buf.read()).decode())
    except Exception:
        return None


# ===========================================================================
# Standard Screen Header
# ===========================================================================

def build_header(parent, title: str, subtitle: str,
                 back_cb=None, note: str = "") -> ttk.Frame:
    """
    Attach a consistent header to *parent*.
    If back_cb is provided a ◀ BACK button appears on the right.
    If note is provided it appears as a third muted line below subtitle.
    Returns the outer header frame.
    """
    outer = ttk.Frame(parent, style="TFrame")
    outer.pack(fill="x", padx=16, pady=(14, 0))

    # Back button pinned to the right — packed first so it takes priority
    if back_cb:
        ttk.Button(outer, text="◀  BACK", command=back_cb,
                   style="Back.TButton", width=10).pack(side="right", anchor="n")

    icon_canvas = draw_hex_canvas(outer)
    icon_canvas.pack(side="left", padx=(0, 12))

    text_block = ttk.Frame(outer, style="TFrame")
    text_block.pack(side="left", fill="both", expand=True)
    ttk.Label(text_block, text=title,    style="Title.TLabel").pack(anchor="w")
    ttk.Label(text_block, text=subtitle, style="Muted.TLabel").pack(anchor="w")
    if note:
        ttk.Label(text_block, text=note, style="Muted.TLabel").pack(anchor="w")

    return outer


# ===========================================================================
# Layout Helpers
# ===========================================================================

def separator(parent, pady: tuple = (8, 8)) -> None:
    """Thin 1 px horizontal rule in BORDER colour."""
    tk.Frame(parent, bg=BORDER, height=1).pack(fill="x", padx=16, pady=pady)


def hero_button(parent, text: str, command) -> tk.Button:
    """The large cyan-bordered CONVERT-style action button."""
    btn = tk.Button(
        parent, text=text, command=command,
        bg=PANEL, fg=CYAN,
        activebackground=HOVER, activeforeground=CYAN,
        font=("Courier New", 13, "bold"),
        relief="flat", bd=0,
        padx=24, pady=8,
        cursor="hand2",
        highlightthickness=2,
        highlightbackground=CYAN, highlightcolor=CYAN,
    )
    btn.bind("<Enter>", lambda e: btn.config(bg=HOVER))
    btn.bind("<Leave>", lambda e: btn.config(bg=PANEL))
    return btn


def log_text_widget(parent) -> tk.Text:
    """Standard read-only log Text widget."""
    widget = tk.Text(
        parent,
        bg=BG, fg=TEXT,
        font=("Courier New", 9),
        relief="flat", bd=0,
        state="disabled", height=10,
        wrap="word", highlightthickness=0,
        selectbackground=BLUE,
    )
    widget.tag_configure("info",    foreground=TEXT)
    widget.tag_configure("success", foreground=GREEN)
    widget.tag_configure("warn",    foreground=ORANGE)
    widget.tag_configure("error",   foreground=RED)
    widget.tag_configure("cyan",    foreground=CYAN)
    widget.tag_configure("muted",   foreground=MUTED)
    widget.tag_configure("sep",     foreground=MUTED, justify="center")
    return widget


def append_log(widget: tk.Text, message: str, tag: str = "info") -> None:
    """Thread-safe log append (call from main thread via after())."""
    widget.config(state="normal")
    widget.insert("end", message + "\n", tag)
    widget.see("end")
    widget.config(state="disabled")


# ===========================================================================
# LCD Screen Reference Window  (shared by Image to DDS and Image to LCD)
# ===========================================================================

def _aspect_label(w: int, h: int) -> str:
    import math
    g = math.gcd(w, h)
    return f"{w//g}:{h//g}"


class LCDReferenceWindow(tk.Toplevel):
    """Dark-themed resizable treeview of all SE LCD block screen dimensions."""

    def __init__(self, parent):
        super().__init__(parent)
        self.title("LCD Screen Reference")
        self.configure(bg=BG)
        self.resizable(True, True)
        self.geometry("780x540")
        self.minsize(600, 400)
        self.transient(parent)
        self._build()

    def _build(self):
        # Lazy import keeps se_theme free of a hard dependency on se_lcd_convert
        try:
            from se_lcd_convert import SCREEN_REFERENCE_DATA
            rows = SCREEN_REFERENCE_DATA
        except ImportError:
            rows = []

        hdr = ttk.Frame(self, style="TFrame")
        hdr.pack(fill="x", padx=14, pady=(12, 4))
        ttk.Label(hdr, text="▣  LCD SCREEN REFERENCE",
                  style="Section.TLabel").pack(side="left")
        ttk.Label(hdr,
                  text="Texture size = DDS output  ·  Visible area = what the player sees",
                  style="Muted.TLabel").pack(side="left", padx=(16, 0))

        tk.Frame(self, bg=BORDER, height=1).pack(fill="x", padx=14, pady=(4, 8))

        tree_frame = ttk.Frame(self, style="TFrame")
        tree_frame.pack(fill="both", expand=True, padx=14)

        cols = ("block", "screen", "texture", "visible", "aspect")
        tree = ttk.Treeview(tree_frame, columns=cols, show="headings",
                            style="SE.Treeview", selectmode="browse")

        tree.heading("block",   text="Block")
        tree.heading("screen",  text="Screen")
        tree.heading("texture", text="Texture Size")
        tree.heading("visible", text="Visible Area")
        tree.heading("aspect",  text="Aspect")

        tree.column("block",   width=260, minwidth=160, stretch=True)
        tree.column("screen",  width=190, minwidth=120, stretch=True)
        tree.column("texture", width=100, minwidth=80,  stretch=False, anchor="center")
        tree.column("visible", width=100, minwidth=80,  stretch=False, anchor="center")
        tree.column("aspect",  width=80,  minwidth=60,  stretch=False, anchor="center")

        vsb = ttk.Scrollbar(tree_frame, orient="vertical",
                            command=tree.yview,
                            style="SE.Vertical.TScrollbar")
        tree.configure(yscrollcommand=vsb.set)
        tree.pack(side="left", fill="both", expand=True)
        vsb.pack(side="right", fill="y")

        tree.tag_configure("odd",  background=BG)
        tree.tag_configure("even", background=PANEL)

        for i, (block, screen, tw, th, sw, sh) in enumerate(rows):
            tag = "even" if i % 2 == 0 else "odd"
            tree.insert("", "end", values=(
                block, screen, f"{tw}×{th}", f"{sw}×{sh}",
                _aspect_label(sw, sh),
            ), tags=(tag,))

        foot = ttk.Frame(self, style="TFrame")
        foot.pack(fill="x", padx=14, pady=(8, 12))
        ttk.Label(foot,
                  text="Values sourced from SE game tools and game files (2026).",
                  style="Muted.TLabel").pack(side="left")
        ttk.Button(foot, text="CLOSE", command=self.destroy,
                   style="SE.TButton").pack(side="right")


# ===========================================================================
# Supporters Window
# ===========================================================================

_SUPPORTERS_URL = ("https://raw.githubusercontent.com/Godimas101/"
                   "personal-projects/main/patreon/supporters.json")
_PATREON_URL = "https://patreon.com/Godimas101"


class SupportersWindow(tk.Toplevel):
    """Dark-themed popup that fetches and displays Patreon supporters live."""

    def __init__(self, parent):
        super().__init__(parent)
        self.title("Supporters")
        self.configure(bg=BG)
        self.resizable(True, True)
        self.geometry("500x460")
        self.minsize(400, 300)
        self.transient(parent)
        self._build()
        threading.Thread(target=self._fetch, daemon=True).start()

    def _build(self):
        hdr = ttk.Frame(self, style="TFrame")
        hdr.pack(fill="x", padx=16, pady=(14, 0))
        ttk.Label(hdr, text="\u25a3  OUR SUPPORTERS",
                  style="Section.TLabel").pack(side="left")
        tk.Frame(self, bg=BORDER, height=1).pack(fill="x", padx=16, pady=(8, 0))

        tk.Label(self,
                 text="These humans help keep the mods and tools free for everyone.",
                 bg=BG, fg=MUTED, font=("Courier New", 9),
                 justify="left", anchor="w").pack(fill="x", padx=16, pady=(10, 0))

        # Scrollable content area
        container = tk.Frame(self, bg=BG)
        container.pack(fill="both", expand=True, padx=16, pady=(8, 0))

        canvas = tk.Canvas(container, bg=BG, bd=0, highlightthickness=0)
        vsb = ttk.Scrollbar(container, orient="vertical",
                            command=canvas.yview,
                            style="SE.Vertical.TScrollbar")
        canvas.configure(yscrollcommand=vsb.set)
        vsb.pack(side="right", fill="y")
        canvas.pack(side="left", fill="both", expand=True)

        self._scroll_frame = tk.Frame(canvas, bg=BG)
        self._canvas_window = canvas.create_window(
            (0, 0), window=self._scroll_frame, anchor="nw")

        def _on_frame_configure(_e):
            canvas.configure(scrollregion=canvas.bbox("all"))

        def _on_canvas_configure(e):
            canvas.itemconfig(self._canvas_window, width=e.width)

        self._scroll_frame.bind("<Configure>", _on_frame_configure)
        canvas.bind("<Configure>", _on_canvas_configure)

        self._status_lbl = tk.Label(
            self._scroll_frame,
            text="Loading supporters\u2026",
            bg=BG, fg=MUTED, font=("Courier New", 9),
            anchor="w")
        self._status_lbl.pack(anchor="w", pady=6)

        tk.Frame(self, bg=BORDER, height=1).pack(fill="x", padx=16, pady=(8, 0))

        btn_row = ttk.Frame(self, style="TFrame")
        btn_row.pack(fill="x", padx=16, pady=(10, 14))

        patreon_btn = tk.Button(
            btn_row, text="SUPPORT ON PATREON",
            bg=PANEL, fg=CYAN,
            activebackground=HOVER, activeforeground=CYAN,
            font=("Courier New", 9, "bold"),
            relief="flat", bd=0, padx=12, pady=4, cursor="hand2",
            highlightthickness=1,
            highlightbackground=CYAN, highlightcolor=CYAN,
            command=lambda: webbrowser.open(_PATREON_URL),
        )
        patreon_btn.pack(side="left")

        ttk.Button(btn_row, text="CLOSE", command=self.destroy,
                   style="SE.TButton").pack(side="right")

    def _fetch(self):
        try:
            req = urllib.request.Request(
                _SUPPORTERS_URL,
                headers={"Cache-Control": "no-cache",
                         "User-Agent": "SE-Image-Converter/1.0"})
            with urllib.request.urlopen(req, timeout=10) as resp:
                data = json.loads(resp.read().decode())
            self.after(0, lambda: self._populate(data))
        except Exception as exc:
            self.after(0, lambda: self._show_error(str(exc)))

    def _populate(self, data):
        if not self.winfo_exists():
            return
        self._status_lbl.destroy()

        tiers = data.get("tiers", [])
        if not tiers or not any(t.get("members") for t in tiers):
            tk.Label(self._scroll_frame,
                     text="No supporters yet \u2014 be the first!",
                     bg=BG, fg=MUTED, font=("Courier New", 9),
                     anchor="w").pack(anchor="w", pady=6)
            return

        for tier in tiers:
            members = tier.get("members", [])
            if not members:
                continue
            tk.Label(self._scroll_frame,
                     text=tier.get("tier", "Supporters"),
                     bg=BG, fg=CYAN,
                     font=("Courier New", 10, "bold"),
                     anchor="w").pack(anchor="w", pady=(10, 2))
            tk.Frame(self._scroll_frame, bg=BORDER, height=1).pack(
                fill="x", pady=(0, 6))
            for name in members:
                tk.Label(self._scroll_frame,
                         text=f"  \u2713  {name}",
                         bg=BG, fg=TEXT,
                         font=("Courier New", 9),
                         anchor="w").pack(anchor="w")

    def _show_error(self, msg):
        if not self.winfo_exists():
            return
        self._status_lbl.config(
            text=f"Could not load supporters: {msg}",
            fg=RED)


# ===========================================================================
# Themed Dialogs
# ===========================================================================

def _base_dialog(parent, title: str, message: str,
                 width: int = 440) -> tuple:
    """
    Build a dark themed Toplevel.  Returns (dlg, btn_row).
    Caller adds buttons to btn_row.
    """
    dlg = tk.Toplevel(parent)
    dlg.title(title)
    dlg.configure(bg=BG)
    dlg.resizable(False, False)
    dlg.transient(parent)
    dlg.grab_set()

    hdr = ttk.Frame(dlg, style="TFrame")
    hdr.pack(fill="x", padx=16, pady=(14, 0))
    ttk.Label(hdr, text=f"▣  {title.upper()}",
              style="Section.TLabel").pack(anchor="w")
    tk.Frame(dlg, bg=BORDER, height=1).pack(fill="x", padx=16, pady=(8, 0))

    tk.Label(dlg, text=message,
             bg=BG, fg=TEXT,
             font=("Courier New", 9),
             justify="left", wraplength=width, anchor="w",
             padx=16, pady=12).pack(fill="x")

    tk.Frame(dlg, bg=BORDER, height=1).pack(fill="x", padx=16)

    btn_row = ttk.Frame(dlg, style="TFrame")
    btn_row.pack(fill="x", padx=16, pady=(10, 14))

    parent.update_idletasks()
    dlg.update_idletasks()
    x = parent.winfo_x() + (parent.winfo_width()  - dlg.winfo_reqwidth())  // 2
    y = parent.winfo_y() + (parent.winfo_height() - dlg.winfo_reqheight()) // 2
    dlg.geometry(f"+{x}+{y}")

    return dlg, btn_row


def themed_askokcancel(parent, title: str, message: str) -> bool:
    """Dark-themed OK / Cancel dialog.  Returns True if OK clicked."""
    result = tk.BooleanVar(value=False)
    dlg, btn_row = _base_dialog(parent, title, message)

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
    ttk.Button(btn_row, text="OK",     command=_ok,
               style="SE.TButton", width=8).pack(side="right")

    dlg.wait_window()
    return result.get()


def themed_showinfo(parent, title: str, message: str,
                    width: int = 460) -> None:
    """Dark-themed single-button info dialog."""
    dlg, btn_row = _base_dialog(parent, title, message, width=width)
    dlg.bind("<Return>", lambda _: dlg.destroy())
    dlg.bind("<Escape>", lambda _: dlg.destroy())
    ttk.Button(btn_row, text="OK", command=dlg.destroy,
               style="SE.TButton", width=8).pack(side="right")
    dlg.wait_window()
