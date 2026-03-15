#!/usr/bin/env python3
"""
se_launcher.py — SE Tools Suite entry point.

Run this file to launch the full SE Tools suite.
  python se_launcher.py

The launcher manages a single top-level window whose content is swapped
between screens via show_screen().  Each screen is a ttk.Frame subclass
that receives (parent, app) and fills the window.

Individual tools can still be run standalone:
  python universal-image-converter/se_lcd_gui.py
"""

import sys
import tkinter as tk
from tkinter import ttk
from pathlib import Path

import se_theme as T

# Lazy imports — screen modules are imported on first use to keep startup fast
_screen_modules: dict[str, object] = {}


def _load_screen(name: str):
    global _screen_modules
    if name not in _screen_modules:
        if name == "home":
            from screen_home import HomeScreen
            _screen_modules["home"] = HomeScreen
        elif name == "setup":
            from screen_setup import SetupScreen
            _screen_modules["setup"] = SetupScreen
        elif name == "image_converter":
            from screen_image_converter import ImageConverterScreen
            _screen_modules["image_converter"] = ImageConverterScreen
        elif name == "text_converter":
            from screen_text_converter import TextConverterScreen
            _screen_modules["text_converter"] = TextConverterScreen
    return _screen_modules[name]


# All screens share the same window size
_WINDOW_SIZE = "700x840"
_GEOMETRY: dict[str, str] = {
    "home":            _WINDOW_SIZE,
    "setup":           _WINDOW_SIZE,
    "image_converter": _WINDOW_SIZE,
    "text_converter":  _WINDOW_SIZE,
}


class SEToolsApp(tk.Tk):

    def __init__(self):
        super().__init__()
        self.title("Universal Image Converter")
        self.configure(bg=T.BG)
        self.resizable(False, False)

        # Configure styles once for the whole application lifetime
        self._style = ttk.Style(self)
        T.configure_styles(self._style)

        # Combobox listbox colours (must be set on the root window)
        self.option_add("*TCombobox*Listbox*Background",       T.PANEL)
        self.option_add("*TCombobox*Listbox*Foreground",       T.TEXT)
        self.option_add("*TCombobox*Listbox*SelectBackground", T.BLUE)
        self.option_add("*TCombobox*Listbox*Font",             ("Courier New", 10))

        # Window icon
        self._icon = T.build_icon_photoimage(self)
        if self._icon:
            try:
                self.iconphoto(True, self._icon)
            except Exception:
                pass

        # Container that fills the whole window; screens are packed into it
        self._container = ttk.Frame(self, style="TFrame")
        self._container.pack(fill="both", expand=True)

        self._current_screen = None

        self.show_screen("home")

    # -----------------------------------------------------------------------

    def show_screen(self, name: str) -> None:
        """Destroy the current screen frame and show a new one."""
        # Destroy previous screen
        if self._current_screen is not None:
            self._current_screen.destroy()
            self._current_screen = None

        # Resize window
        geom = _GEOMETRY.get(name, "700x700")
        self.geometry(geom)

        # Instantiate and pack the new screen
        ScreenClass = _load_screen(name)
        screen = ScreenClass(self._container, self)
        screen.pack(fill="both", expand=True)
        self._current_screen = screen


def main():
    app = SEToolsApp()
    app.mainloop()


if __name__ == "__main__":
    main()
