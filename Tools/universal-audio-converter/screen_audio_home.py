#!/usr/bin/env python3
"""
screen_audio_home.py — SE Audio Converter title / home screen.

Four nav-card buttons route the user to:
  • Setup & Requirements
  • Audio Converter  (any format → WAV or XWM)
  • Audio Editor     (WAV waveform editor)
  • SBC Generator    (AudioDefinition + SoundBlock SBC files)
"""

import tkinter as tk
from tkinter import ttk
import webbrowser

import se_audio_theme as T


class HomeScreen(ttk.Frame):

    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app
        self._build()

    # -----------------------------------------------------------------------

    def _build(self):
        # ── Warning box (packed first so it anchors to the bottom) ───────────
        warn_outer = tk.Frame(
            self, bg=T.ORANGE,
            highlightthickness=1, highlightbackground=T.ORANGE,
        )
        warn_outer.pack(side="bottom", fill="x", padx=24, pady=(0, 10))

        warn_inner = tk.Frame(warn_outer, bg=T.PANEL)
        warn_inner.pack(fill="x", padx=1, pady=1)

        warn_text = tk.Frame(warn_inner, bg=T.PANEL)
        warn_text.pack(fill="x", padx=14, pady=8)

        tk.Label(
            warn_text,
            text="IMPORTANT:  Space Engineers sound files must be MONO.",
            bg=T.PANEL, fg=T.ORANGE,
            font=("Courier New", 9, "bold"),
            anchor="w",
        ).pack(anchor="w")

        tk.Label(
            warn_text,
            text="Use the Audio Editor (Stereo \u2192 Mono) before converting. Stereo files will play left-channel only in-game.",
            bg=T.PANEL, fg=T.MUTED,
            font=("Courier New", 9),
            anchor="w",
        ).pack(anchor="w", pady=(2, 0))

        # ── Header ──────────────────────────────────────────────────────────
        T.build_header(
            self,
            title="UNIVERSAL AUDIO CONVERTER",
            subtitle="Space Engineers custom sound and music mod utilities.",
        )
        T.separator(self, pady=(10, 14))

        # ── Nav cards ────────────────────────────────────────────────────────
        nav = ttk.Frame(self, style="TFrame")
        nav.pack(fill="x", expand=False, padx=24)

        self._nav_card(
            nav,
            icon="\u25c8",
            title="SETUP  &  REQUIREMENTS",
            desc=("ffmpeg and xWMAEncode setup instructions.\n"
                  "Download links and where to place the tools."),
            command=lambda: self._app.show_screen("setup"),
        ).pack(fill="x", pady=(0, 10))

        self._nav_card(
            nav,
            icon="\u266a",
            title="AUDIO CONVERTER",
            desc=("Convert any audio format to WAV or XWM.\n"
                  "MP3, OGG, FLAC, AAC, M4A and more \u2014 requires ffmpeg."),
            command=lambda: self._app.show_screen("converter"),
        ).pack(fill="x", pady=(0, 10))

        self._nav_card(
            nav,
            icon="\u2702",
            title="AUDIO EDITOR",
            desc=("Open a WAV file and edit it visually.\n"
                  "Trim, fade, normalize, adjust channels, change speed."),
            command=lambda: self._app.show_screen("editor"),
        ).pack(fill="x", pady=(0, 10))

        self._nav_card(
            nav,
            icon="\u25a3",
            title="SBC GENERATOR  \u00b7  FOR MODDERS",
            desc=("Generate AudioDefinition and SoundBlock SBC files\n"
                  "for Space Engineers audio mods."),
            command=lambda: self._app.show_screen("sbc"),
        ).pack(fill="x")

        # ── Footer ───────────────────────────────────────────────────────────
        T.separator(self, pady=(14, 0))

        footer = tk.Frame(self, bg=T.BG)
        footer.pack(anchor="center", pady=(6, 0))

        tk.Label(footer, text="Made with \u2665 by ",
                 bg=T.BG, fg=T.MUTED,
                 font=("Courier New", 8)).pack(side="left")

        godimas = tk.Label(footer, text="Godimas",
                           bg=T.BG, fg=T.MUTED,
                           font=("Courier New", 8, "underline"),
                           cursor="hand2")
        godimas.pack(side="left")
        godimas.bind("<Button-1>", lambda _e: webbrowser.open(
            "https://steamcommunity.com/id/godimas/myworkshopfiles"))
        godimas.bind("<Enter>", lambda _e: godimas.config(fg=T.CYAN))
        godimas.bind("<Leave>", lambda _e: godimas.config(fg=T.MUTED))

        tk.Label(footer, text=" and ",
                 bg=T.BG, fg=T.MUTED,
                 font=("Courier New", 8)).pack(side="left")

        claude = tk.Label(footer, text="Claude",
                          bg=T.BG, fg=T.MUTED,
                          font=("Courier New", 8, "underline"),
                          cursor="hand2")
        claude.pack(side="left")
        claude.bind("<Button-1>", lambda _e: webbrowser.open("https://claude.ai"))
        claude.bind("<Enter>", lambda _e: claude.config(fg=T.CYAN))
        claude.bind("<Leave>", lambda _e: claude.config(fg=T.MUTED))

        ttk.Label(self, text="v1.0  \u00b7  SE Audio Converter",
                  style="Muted.TLabel").pack(anchor="center", pady=(4, 10))

    # -----------------------------------------------------------------------

    def _nav_card(self, parent, icon: str, title: str,
                  desc: str, command) -> tk.Frame:
        outer = tk.Frame(
            parent, bg=T.PANEL, cursor="hand2",
            highlightthickness=1,
            highlightbackground=T.BORDER,
            highlightcolor=T.CYAN,
        )
        inner = tk.Frame(outer, bg=T.PANEL)
        inner.pack(fill="both", expand=True, padx=16, pady=12)

        icon_box = tk.Frame(inner, bg=T.PANEL, width=48, height=48)
        icon_box.pack_propagate(False)
        icon_box.pack(side="left", padx=(0, 16))

        icon_lbl = tk.Label(icon_box, text=icon,
                            bg=T.PANEL, fg=T.CYAN,
                            font=("Courier New", 22, "bold"))
        icon_lbl.place(relx=0.5, rely=0.5, anchor="center")

        text_frame = tk.Frame(inner, bg=T.PANEL)
        text_frame.pack(side="left", fill="both", expand=True)

        title_lbl = tk.Label(text_frame, text=title,
                             bg=T.PANEL, fg=T.CYAN,
                             font=("Courier New", 11, "bold"),
                             anchor="w")
        title_lbl.pack(anchor="w")

        desc_lbl = tk.Label(text_frame, text=desc,
                            bg=T.PANEL, fg=T.MUTED,
                            font=("Courier New", 9),
                            anchor="w", justify="left")
        desc_lbl.pack(anchor="w", pady=(2, 0))

        all_w = [outer, inner, icon_box, text_frame, icon_lbl, title_lbl, desc_lbl]

        def _enter(_e):
            outer.config(highlightbackground=T.CYAN)
            for w in all_w:
                try:
                    w.config(bg=T.HOVER)
                except tk.TclError:
                    pass

        def _leave(_e):
            outer.config(highlightbackground=T.BORDER)
            for w in all_w:
                try:
                    w.config(bg=T.PANEL)
                except tk.TclError:
                    pass

        for w in all_w:
            w.bind("<Enter>",    _enter)
            w.bind("<Leave>",    _leave)
            w.bind("<Button-1>", lambda _e: command())

        return outer
