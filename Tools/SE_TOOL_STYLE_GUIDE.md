# SE Tool Style Guide
> Dark industrial aesthetic for Space Engineers toolkit apps.
> All tools built to this spec will look and feel like a single suite.

---

## Colour Palette

| Token    | Hex       | Role                                              |
|----------|-----------|---------------------------------------------------|
| `BG`     | `#0d1117` | Window background, log area, deep recesses        |
| `PANEL`  | `#161b22` | Card/panel backgrounds, input fields, list boxes  |
| `CYAN`   | `#00d4ff` | Primary accent — headings, active borders, arrows |
| `BLUE`   | `#1f6feb` | Selection highlight, hyperlinks                   |
| `TEXT`   | `#e6edf3` | Body text, labels, input values                   |
| `MUTED`  | `#8b949e` | Secondary text, hints, separators, log metadata   |
| `GREEN`  | `#3fb950` | Success log messages                              |
| `ORANGE` | `#d29922` | Warning log messages                              |
| `RED`    | `#f85149` | Error log messages                                |
| `HOVER`  | `#21262d` | Button/interactive hover state                    |
| `BORDER` | `#30363d` | Borders, dividers, inactive highlights            |

```python
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
```

---

## Typography

**All text uses Courier New (monospace).** This is intentional — it enables column-aligned dropdown formatting and gives the HUD/terminal aesthetic.

| Token        | Spec                          | Usage                             |
|--------------|-------------------------------|-----------------------------------|
| `FONT_TITLE` | `("Courier New", 16, "bold")` | App title in header               |
| `FONT_LABEL` | `("Courier New", 10)`         | Labels, combobox entries, buttons |
| `FONT_BODY`  | `("Courier New", 10)`         | General body text                 |
| `FONT_SMALL` | `("Courier New", 8)`          | Minor annotations                 |
| `FONT_MONO`  | `"Courier New"`               | Bare family name when size varies |

Log text / treeview / listbox: `("Courier New", 9)`
Hero CTA button: `("Courier New", 13, "bold")`

---

## ttk Theme Base

Always start from the **clam** theme — never the default. This gives full control over colours.

```python
style.theme_use("clam")
```

---

## ttk Style Definitions

### Frames

```python
style.configure("TFrame",       background=BG)
style.configure("Panel.TFrame", background=PANEL)
```

### Labels

```python
style.configure("TLabel",         background=BG,    foreground=TEXT,  font=FONT_LABEL)
style.configure("Panel.TLabel",   background=PANEL, foreground=TEXT,  font=FONT_LABEL)
style.configure("Muted.TLabel",   background=BG,    foreground=MUTED, font=("Courier New", 9))
style.configure("Title.TLabel",   background=BG,    foreground=CYAN,  font=FONT_TITLE)
style.configure("Section.TLabel", background=BG,    foreground=CYAN,  font=("Courier New", 10, "bold"))
```

### Buttons — Standard (`SE.TButton`)

Used for all action buttons (SELECT, BROWSE, CLEAR, CLOSE, etc.)

```python
style.configure("SE.TButton",
    background=PANEL, foreground=CYAN,
    bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
    relief="flat", font=FONT_LABEL, padding=(10, 4))
style.map("SE.TButton",
    background=[("active", HOVER), ("disabled", BG)],
    foreground=[("disabled", BORDER)])
```

### Buttons — Back (`Back.TButton`)

Navigation back button. Muted at rest, cyan on hover.

```python
style.configure("Back.TButton",
    background=PANEL, foreground=MUTED,
    bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
    relief="flat", font=("Courier New", 10), padding=(10, 4))
style.map("Back.TButton",
    background=[("active", HOVER)],
    foreground=[("active", CYAN)])
```

### Buttons — Info (`Info.TButton`)

Small secondary button (e.g. the ⓘ reference button). Muted at rest, cyan on hover.

```python
style.configure("Info.TButton",
    background=PANEL, foreground=MUTED,
    bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
    relief="flat", font=("Courier New", 9), padding=(4, 2))
style.map("Info.TButton",
    background=[("active", HOVER)],
    foreground=[("active", CYAN)])
```

### Hero / CTA Button

The primary action button is a plain `tk.Button` (not ttk) for full colour control. Defined in `se_theme.hero_button()`.

```python
btn = tk.Button(
    parent,
    text="  ▶  CONVERT  ▶  ",
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
```

**Disabled state** (before content is available):
```python
btn.config(state="disabled", highlightbackground=BORDER, fg=MUTED)
```
Re-enable after content is ready:
```python
btn.config(state="normal", highlightbackground=CYAN, fg=CYAN)
```

### Combobox (`SE.TCombobox`)

```python
style.configure("SE.TCombobox",
    fieldbackground=PANEL, background=PANEL,
    foreground=TEXT, selectforeground=CYAN, selectbackground=PANEL,
    bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
    arrowcolor=CYAN, font=FONT_LABEL)
style.map("SE.TCombobox",
    fieldbackground=[("readonly", PANEL)],
    selectbackground=[("readonly", PANEL)],
    selectforeground=[("readonly", CYAN)])
```

Dropdown listbox colours must also be set via `option_add` on the root window (tkinter limitation):

```python
root.option_add("*TCombobox*Listbox*Background",       PANEL)
root.option_add("*TCombobox*Listbox*Foreground",       TEXT)
root.option_add("*TCombobox*Listbox*SelectBackground", BLUE)
root.option_add("*TCombobox*Listbox*Font",             ("Courier New", 10))
```

Always pass `font=FONT_LABEL` directly on the `ttk.Combobox` widget — style font alone doesn't always apply to the entry portion.

### Checkbutton (`SE.TCheckbutton`)

```python
style.configure("SE.TCheckbutton",
    background=BG, foreground=TEXT, font=FONT_LABEL,
    indicatorcolor=PANEL, indicatordiameter=14)
style.map("SE.TCheckbutton",
    indicatorcolor=[("selected", CYAN), ("!selected", PANEL)],
    background=[("active", BG)],
    foreground=[("active", TEXT), ("disabled", BORDER)])
```

### Progress Bar (`SE.Horizontal.TProgressbar`)

```python
style.configure("SE.Horizontal.TProgressbar",
    troughcolor=PANEL, background=CYAN,
    bordercolor=BORDER, darkcolor=CYAN, lightcolor=CYAN,
    thickness=18)
```

### Scrollbar (`SE.Vertical.TScrollbar`)

```python
style.configure("SE.Vertical.TScrollbar",
    background=PANEL, troughcolor=BG,
    bordercolor=BORDER, arrowcolor=CYAN,
    darkcolor=PANEL, lightcolor=PANEL)
style.map("SE.Vertical.TScrollbar", background=[("active", HOVER)])
```

### Treeview (`SE.Treeview`)

```python
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
```

Treeview row striping:
```python
tree.tag_configure("even", background=PANEL)
tree.tag_configure("odd",  background=BG)
```

---

## Plain `tk` Widgets

Some widgets can't be fully styled via ttk. Use these specs directly:

### Listbox

```python
tk.Listbox(
    parent,
    bg=PANEL, fg=TEXT,
    selectbackground=BLUE, selectforeground=TEXT,
    highlightthickness=1, highlightcolor=BORDER,
    highlightbackground=BORDER,
    relief="flat", bd=0,
    font=("Courier New", 9),
    activestyle="none",
)
```

### Text (log / raw output)

```python
tk.Text(
    parent,
    bg=BG, fg=TEXT,
    font=("Courier New", 9),
    relief="flat", bd=0,
    state="disabled",
    wrap="word", highlightthickness=0,
    selectbackground=BLUE,
)
```

Log colour tags:
```python
text.tag_configure("info",    foreground=TEXT)
text.tag_configure("success", foreground=GREEN)
text.tag_configure("warn",    foreground=ORANGE)
text.tag_configure("error",   foreground=RED)
text.tag_configure("cyan",    foreground=CYAN)
text.tag_configure("muted",   foreground=MUTED)
text.tag_configure("sep",     foreground=MUTED, justify="center")
```

### Entry (editable)

```python
tk.Entry(
    parent,
    bg=PANEL, fg=TEXT,
    insertbackground=CYAN,
    disabledbackground=BG, disabledforeground=MUTED,
    font=("Courier New", 9),
    relief="flat", bd=1,
    highlightthickness=1,
    highlightbackground=BORDER, highlightcolor=CYAN,
)
```
Add `ipady=3` on `.pack()` / `.grid()` for comfortable vertical padding.

### Entry (read-only path display)

```python
tk.Entry(
    parent,
    state="readonly",
    readonlybackground=PANEL,
    fg=MUTED, font=("Courier New", 9),
    relief="flat", bd=1,
    highlightthickness=1,
    highlightbackground=BORDER, highlightcolor=CYAN,
)
```

### Colour Swatch (clickable `tk.Frame`)

Used for colour picker triggers (e.g. background colour selection).

```python
swatch = tk.Frame(
    parent, width=22, height=22,
    bg="#000000", cursor="hand2",
    highlightthickness=1, highlightbackground=BORDER,
)
swatch.pack_propagate(False)
swatch.pack(side="left", padx=(0, 8))
swatch.bind("<Button-1>", lambda _e: _on_pick_color())
```

To open the system colour picker:
```python
from tkinter import colorchooser
result = colorchooser.askcolor(color=current_hex, title="...", parent=root)
if result and result[0]:
    r, g, b = (int(x) for x in result[0])
    swatch.config(bg="#{:02x}{:02x}{:02x}".format(r, g, b))
```

When a related option (e.g. Transparency) makes the swatch irrelevant, disable it:
```python
swatch.config(cursor="", highlightbackground=BG)
swatch.bind("<Button-1>", lambda _e: None)
hex_label.config(foreground=BORDER)
```

### Hyperlink Label

```python
link = tk.Label(
    parent, text="Link text",
    bg=BG, fg=BLUE,
    font=("Courier New", 9, "underline"),
    cursor="hand2",
)
link.bind("<Button-1>", lambda _e: webbrowser.open(url))
link.bind("<Enter>",    lambda _e: link.config(fg=CYAN))
link.bind("<Leave>",    lambda _e: link.config(fg=BLUE))
```

---

## Layout Rules

### Window

- Base theme: `clam`
- Window BG: `BG`
- Non-resizable by default: `self.resizable(False, False)`
- Standard window size: `700×840` for all main screens
- Outer padding: `padx=16` on all main sections

### Horizontal Separators

```python
T.separator(parent, pady=(8, 8))
# or directly:
tk.Frame(parent, bg=BORDER, height=1).pack(fill="x", padx=16, pady=(8, 8))
```

Separator spacing between major sections: `pady=(10, 8)` or `pady=(8, 8)`.

### Section Headers

```python
ttk.Label(self, text="▣  SECTION NAME", style="Section.TLabel").pack(anchor="w", padx=16)
```

- Prefix every section header with `▣  ` (filled square + 2 spaces)
- All caps
- Colour: CYAN via `Section.TLabel` style

### Standard Screen Header — `T.build_header()`

Every screen gets a consistent header via `se_theme.build_header()`. Do not build headers manually.

```python
T.build_header(
    parent,
    title="SCREEN TITLE",
    subtitle="One-line description of this screen.",
    back_cb=lambda: app.show_screen("home"),  # omit on home screen
    note="Optional third muted line (e.g. Supported formats)",
)
```

- **`back_cb`**: if provided, a `◀  BACK` button (`Back.TButton`) appears top-right
- **`note`**: optional third muted line (use for supplementary info like supported formats)
- All screens use identical icon size (54px hex canvas), padding, and text layout

### Settings Grid (two-column label/control layout)

```python
sf = ttk.Frame(self, style="TFrame")
sf.pack(fill="x", padx=16, pady=(6, 0))
sf.columnconfigure(0, minsize=115)   # fixed label column
sf.columnconfigure(1, weight=1)       # controls expand

ttk.Label(sf, text="Label:", style="TLabel").grid(
    row=0, column=0, sticky="w", pady=(0, 6))
control.grid(row=0, column=1, sticky="w", pady=(0, 6))
```

- **Labels always use `sticky="w"`** (left-aligned in their cell)
- Row spacing: `pady=(0, 6)` on all cells
- Controls that need to be hidden/shown: use `.grid_remove()` and `.grid()` (not pack/pack_forget)

### Hiding/showing a settings row

```python
# Hide
label_widget.grid_remove()
ctrl_frame.grid_remove()

# Restore
label_widget.grid()
ctrl_frame.grid()
```

### Hero button pairs (equal width)

When two CTA buttons sit side-by-side, use a grid frame with equal column weights:

```python
row = ttk.Frame(parent, style="TFrame")
row.pack(fill="x")
row.columnconfigure(0, weight=1)
row.columnconfigure(1, weight=1)

btn_a = T.hero_button(row, "  ⓘ  ACTION A  ", cb_a)
btn_a.grid(row=0, column=0, sticky="ew", padx=(0, 4))

btn_b = T.hero_button(row, "  ⧉  ACTION B  ", cb_b)
btn_b.grid(row=0, column=1, sticky="ew", padx=(4, 0))
```

---

## Dropdown Column Alignment

When a combobox needs two-column formatting (name left, value right), use monospace padding. This works because the entire UI uses Courier New.

```python
def fmt_option(name: str, value: str) -> str:
    return f"  {name:<20}  {value:>4}  "
```

- 2 spaces left margin, 2 spaces right margin
- Name field: `<20` (left-justified, 20 chars wide — adjust to longest name)
- Value field: `>4` (right-justified, 4 chars wide — adjust for your data)
- Set `width` on the combobox to the total string length

Always pass `font=FONT_LABEL` directly on the widget in addition to the style.

### Shared preset formatting helpers (`se_lcd_convert`)

The Screen Target preset names are formatted and shared across both tools via `se_lcd_convert`:

```python
from se_lcd_convert import PRESET_DISPLAY_NAMES, PRESET_DISPLAY_MAP, fmt_preset
```

- `PRESET_DISPLAY_NAMES` — list of formatted strings for the combobox `values`
- `PRESET_DISPLAY_MAP` — `{formatted: raw_name}` for reverse lookup
- `fmt_preset(name)` — formats a single name

---

## Multi-Screen Launcher Architecture

The suite uses a single `tk.Tk` window. `SEToolsApp.show_screen(name)` destroys the current screen frame and instantiates the new one.

```python
class SEToolsApp(tk.Tk):
    def show_screen(self, name: str) -> None:
        if self._current_screen:
            self._current_screen.destroy()
        ScreenClass = _load_screen(name)   # lazy import
        screen = ScreenClass(self._container, self)
        screen.pack(fill="both", expand=True)
        self._current_screen = screen
```

Each screen is a `ttk.Frame` subclass that receives `(parent, app)`:

```python
class MyScreen(ttk.Frame):
    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app
        self._build()
```

Navigate back: `self._app.show_screen("home")`

All 4 screens share the same window size (`700×840`). Styles are configured once at app startup in `se_theme.configure_styles()`.

---

## Nav Cards (Home Screen)

Nav cards are plain `tk.Frame` panels with hover highlighting and full-card click binding.

Key rules:
- Use a **fixed-size icon container** (`pack_propagate(False)` + `place(relx=0.5, rely=0.5)`) to keep icon column width identical across all cards regardless of the character rendered
- Use plain Unicode geometric characters for icons (e.g. `◈`, `▣`, `▶`) — **never emoji** (they render at different sizes on Windows and misalign the grid)
- On hover: set `highlightbackground=CYAN` on the outer frame and `bg=HOVER` on all child widgets
- On leave: restore `highlightbackground=BORDER` and `bg=PANEL`

```python
icon_box = tk.Frame(inner, bg=PANEL, width=48, height=48)
icon_box.pack_propagate(False)
icon_box.pack(side="left", padx=(0, 16))

icon_lbl = tk.Label(icon_box, text="▣", bg=PANEL, fg=CYAN,
                    font=("Courier New", 22, "bold"))
icon_lbl.place(relx=0.5, rely=0.5, anchor="center")
```

---

## LCD Screen Reference Window

A shared resizable `Toplevel` treeview listing all SE LCD block dimensions. Defined in `se_theme.LCDReferenceWindow`. Used by both Image to DDS and Image to LCD screens.

```python
# Open (with raise-if-already-open guard):
if self._ref_window and self._ref_window.winfo_exists():
    self._ref_window.lift()
else:
    self._ref_window = T.LCDReferenceWindow(self.winfo_toplevel())
```

The window lazy-imports `SCREEN_REFERENCE_DATA` from `se_lcd_convert` so `se_theme` has no hard dependency on tool modules.

---

## Themed Dialogs

Never use native `tkinter.messagebox` — it can't be styled to match the dark theme.

Use `T.themed_showinfo(parent, title, message, width=460)` for info dialogs and `T.themed_askokcancel(parent, title, message)` for confirmation dialogs.

Key rules for custom dialogs:
- `bg=BG` on the Toplevel
- `dlg.transient(parent)` + `dlg.grab_set()` for modality
- Header uses `Section.TLabel` with `▣  TITLE` prefix
- 1px `BORDER` divider lines above and below the message
- Buttons use `SE.TButton`, right-aligned, OK on the far right
- `<Return>` → OK, `<Escape>` → Cancel
- Centre on parent after `update_idletasks()`

---

## Log / Status Conventions

| Tag       | Colour  | When to use                              |
|-----------|---------|------------------------------------------|
| `"info"`  | TEXT    | Normal progress messages                 |
| `"success"` | GREEN | File converted, operation completed      |
| `"warn"`  | ORANGE  | Non-fatal issue, fallback used           |
| `"error"` | RED     | Failed conversion, missing dependency    |
| `"cyan"`  | CYAN    | Section labels, filenames, key values    |
| `"muted"` | MUTED   | Metadata, paths, secondary detail        |
| `"sep"`   | MUTED   | Separator lines (centered, `━` × 38)     |

Log separator:
```python
append_log(widget, "━" * 38, "sep")
```

---

## Screen Footer Pattern

```python
T.separator(self, pady=(18, 0))

footer = ttk.Frame(self, style="TFrame")
footer.pack(fill="x", padx=18, pady=(4, 10))

# Credits on the left
credits = ttk.Frame(footer, style="TFrame")
credits.pack(side="left")
ttk.Label(credits, text="Made with ♥ by ...", style="Muted.TLabel").pack(anchor="w")

# Version on the right
ttk.Label(footer, text="v1.0  ·  App Name", style="Muted.TLabel").pack(side="right", anchor="s")
```

---

## Window Icon

Tools use a programmatically drawn hexagon icon (no external file needed). Built via `T.build_icon_photoimage(root)`. Falls back silently if Pillow is unavailable.

```python
self._icon = T.build_icon_photoimage(self)
if self._icon:
    try:
        self.iconphoto(True, self._icon)
    except Exception:
        pass
```
