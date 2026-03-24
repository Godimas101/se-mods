#!/usr/bin/env python3
"""
screen_audio_converter.py — Audio Converter screen for SE Audio Converter.

Converts any supported audio format to WAV or XWM.
  • WAV output  — via ffmpeg (always available if ffmpeg is installed)
  • XWM output  — via ffmpeg → WAV → xWMAEncode.exe

Supported input: MP3, WAV, OGG, FLAC, AAC, M4A, WMA, AIFF, OPUS, XWM
"""

import queue
import shutil
import subprocess
import tempfile
import threading
try:
    import sounddevice as _sd
    _SD_OK = True
except ImportError:
    _sd = None
    _SD_OK = False
from pathlib import Path

import tkinter as tk
from tkinter import filedialog, ttk

import se_audio_theme as T

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

SUPPORTED_INPUT_EXTS = {
    ".mp3", ".wav", ".ogg", ".flac", ".aac",
    ".m4a", ".wma", ".aiff", ".aif", ".opus", ".xwm",
}

OUTPUT_FORMATS = ["WAV", "XWM"]


# ---------------------------------------------------------------------------
# Tool detection
# ---------------------------------------------------------------------------

def _find_tool(name: str) -> str | None:
    found = shutil.which(name)
    if found:
        return found
    for ext in ("", ".exe"):
        candidate = Path(__file__).parent / (name + ext)
        if candidate.exists():
            return str(candidate)
    return None


def check_ffmpeg() -> tuple[bool, str]:
    path = _find_tool("ffmpeg")
    return (True, path) if path else (False, "ffmpeg not found")


def check_xwmaencode() -> tuple[bool, str]:
    path = _find_tool("xWMAEncode")
    return (True, path) if path else (False, "xWMAEncode not found")


# ---------------------------------------------------------------------------
# Conversion logic
# ---------------------------------------------------------------------------

def convert_to_wav(input_path: Path, output_path: Path, ffmpeg: str) -> None:
    """Convert any audio file to 16-bit PCM WAV at 44100 Hz via ffmpeg."""
    result = subprocess.run(
        [ffmpeg, "-y", "-i", str(input_path),
         "-ar", "44100", "-sample_fmt", "s16",
         str(output_path)],
        capture_output=True, text=True,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.splitlines()[-1] if result.stderr else "ffmpeg failed")


def convert_to_xwm(input_path: Path, output_path: Path,
                   ffmpeg: str, xwmaencode: str) -> None:
    """Convert any audio file to XWM: input → WAV (temp) → XWM."""
    with tempfile.TemporaryDirectory() as tmp:
        wav_path = Path(tmp) / (input_path.stem + "_tmp.wav")
        convert_to_wav(input_path, wav_path, ffmpeg)
        result = subprocess.run(
            [xwmaencode, str(wav_path), str(output_path)],
            capture_output=True, text=True,
        )
        if result.returncode != 0:
            raise RuntimeError(
                result.stderr.splitlines()[-1] if result.stderr
                else result.stdout.splitlines()[-1] if result.stdout
                else "xWMAEncode failed"
            )


# ---------------------------------------------------------------------------
# Screen
# ---------------------------------------------------------------------------

class ConverterScreen(ttk.Frame):

    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app

        self._files: list[Path]       = []
        self._out_folder: Path | None = None
        self._converting              = False
        self._previewing              = False
        self._q: queue.Queue          = queue.Queue()

        self._ffmpeg_ok,    self._ffmpeg_path    = check_ffmpeg()
        self._xwmaenc_ok,   self._xwmaenc_path   = check_xwmaencode()

        self._build_ui()
        self._log_startup_info()

    # -----------------------------------------------------------------------
    # UI
    # -----------------------------------------------------------------------

    def _build_ui(self):
        pad = dict(padx=16, pady=0)
        ext_list = ", ".join(sorted(e.lstrip(".").upper()
                                    for e in SUPPORTED_INPUT_EXTS))

        T.build_header(
            self,
            title="AUDIO CONVERTER",
            subtitle="Convert any audio format to WAV or XWM.",
            back_cb=lambda: self._app.show_screen("home"),
            note=f"Supported input: {ext_list}",
        )
        T.separator(self, pady=(8, 8))

        # ── Input Files ──────────────────────────────────────────────────────
        ttk.Label(self, text="\u25a3  INPUT FILES",
                  style="Section.TLabel").pack(anchor="w", **pad)

        list_frame = ttk.Frame(self, style="Panel.TFrame")
        list_frame.pack(fill="x", padx=16, pady=(4, 0))

        lb_container = tk.Frame(list_frame, bg=T.PANEL)
        lb_container.pack(side="left", fill="both", expand=True, padx=(6, 0), pady=6)

        self._listbox = tk.Listbox(
            lb_container,
            bg=T.PANEL, fg=T.TEXT,
            selectbackground=T.BLUE, selectforeground=T.TEXT,
            highlightthickness=1, highlightcolor=T.BORDER,
            highlightbackground=T.BORDER,
            relief="flat", bd=0,
            font=("Courier New", 9),
            height=6, activestyle="none",
        )
        self._lb_scrollbar = ttk.Scrollbar(lb_container, orient="vertical",
                                           command=self._listbox.yview,
                                           style="SE.Vertical.TScrollbar")
        self._listbox.config(yscrollcommand=self._lb_scrollbar.set)
        self._listbox.pack(side="left", fill="both", expand=True)

        self._list_placeholder = tk.Label(
            lb_container, text="No files selected",
            bg=T.PANEL, fg=T.MUTED, font=("Courier New", 9))
        self._list_placeholder.place(relx=0.5, rely=0.5, anchor="center")

        btn_col = ttk.Frame(list_frame, style="Panel.TFrame")
        btn_col.pack(side="right", fill="y", padx=8, pady=6)
        self._se_btn(btn_col, "SELECT", self._on_select, 8).pack(pady=(0, 6))
        self._se_btn(btn_col, "CLEAR",  self._on_clear,  8).pack()
        self._preview_btn = self._se_btn(btn_col, "\u25b6 PLAY", self._on_preview, 8)
        self._preview_btn.pack(pady=(6, 0))

        self._file_count_var = tk.StringVar(value="")
        ttk.Label(self, textvariable=self._file_count_var,
                  style="Muted.TLabel").pack(anchor="e", padx=18, pady=(2, 0))

        T.separator(self, pady=(8, 8))

        # ── Output Settings ──────────────────────────────────────────────────
        ttk.Label(self, text="\u25a3  OUTPUT SETTINGS",
                  style="Section.TLabel").pack(anchor="w", **pad)

        settings = ttk.Frame(self, style="TFrame")
        settings.pack(fill="x", padx=16, pady=(6, 0))
        settings.columnconfigure(0, minsize=120)
        settings.columnconfigure(1, weight=1)

        # Output format
        ttk.Label(settings, text="Output Format:", style="TLabel").grid(
            row=0, column=0, sticky="w", pady=(0, 6))
        self._format_var = tk.StringVar(value="WAV")
        self._format_combo = ttk.Combobox(
            settings, textvariable=self._format_var,
            values=OUTPUT_FORMATS,
            state="readonly", width=8, style="SE.TCombobox",
        )
        self._format_combo.grid(row=0, column=1, sticky="w", pady=(0, 6))
        self._format_combo.bind("<<ComboboxSelected>>", self._on_format_change)

        # XWM note (shown when XWM selected)
        self._xwm_note_var = tk.StringVar(value="")
        self._xwm_note_lbl = ttk.Label(settings, textvariable=self._xwm_note_var,
                                        style="Muted.TLabel")

        # File rename
        ttk.Label(settings, text="File Rename:", style="TLabel").grid(
            row=2, column=0, sticky="w", pady=(0, 6))
        rename_ctrl = ttk.Frame(settings, style="TFrame")
        rename_ctrl.grid(row=2, column=1, sticky="w", pady=(0, 6))

        self._affix_mode_var = tk.StringVar(value="None")
        affix_combo = ttk.Combobox(
            rename_ctrl, textvariable=self._affix_mode_var,
            values=["None", "Add Prefix", "Add Suffix"],
            state="readonly", width=12, style="SE.TCombobox",
        )
        affix_combo.pack(side="left", padx=(0, 8))
        affix_combo.bind("<<ComboboxSelected>>", self._on_affix_change)

        self._affix_text_var = tk.StringVar(value="")
        self._affix_entry = tk.Entry(
            rename_ctrl,
            textvariable=self._affix_text_var,
            bg=T.PANEL, fg=T.TEXT, insertbackground=T.CYAN,
            disabledbackground=T.BG, disabledforeground=T.MUTED,
            font=("Courier New", 9), relief="flat", bd=1,
            highlightthickness=1, highlightbackground=T.BORDER,
            highlightcolor=T.CYAN, width=16, state="disabled",
        )
        self._affix_entry.pack(side="left", ipady=3)

        # Output folder
        ttk.Label(settings, text="Output Folder:", style="TLabel").grid(
            row=3, column=0, sticky="w", padx=(0, 8))
        out_ctrl = ttk.Frame(settings, style="TFrame")
        out_ctrl.grid(row=3, column=1, sticky="ew")

        self._outfolder_var = tk.StringVar(value="same as source file")
        tk.Entry(
            out_ctrl, textvariable=self._outfolder_var,
            state="readonly", readonlybackground=T.PANEL,
            fg=T.MUTED, font=("Courier New", 9),
            relief="flat", bd=1, highlightthickness=1,
            highlightbackground=T.BORDER, highlightcolor=T.CYAN,
            width=33,
        ).pack(side="left", padx=(0, 8), ipady=3)
        self._se_btn(out_ctrl, "BROWSE...", self._on_browse, 10).pack(side="left", padx=(0, 6))
        self._se_btn(out_ctrl, "RESET", self._on_reset_out, 7).pack(side="left")

        T.separator(self, pady=(14, 10))

        # ── Convert button ────────────────────────────────────────────────────
        btn_frame = ttk.Frame(self, style="TFrame")
        btn_frame.pack(pady=(0, 10))
        self._btn_convert = T.hero_button(btn_frame,
                                          "  \u25b6  CONVERT  \u25b6  ",
                                          self._on_convert)
        self._btn_convert.pack()

        T.separator(self, pady=(10, 8))

        # ── Progress ──────────────────────────────────────────────────────────
        prog_frame = ttk.Frame(self, style="TFrame")
        prog_frame.pack(fill="x", padx=16, pady=(0, 4))
        self._progress_var = tk.DoubleVar(value=0.0)
        ttk.Progressbar(prog_frame, variable=self._progress_var,
                        maximum=100.0, mode="determinate",
                        style="SE.Horizontal.TProgressbar",
                        length=520).pack(side="left", fill="x", expand=True, pady=2)
        self._pct_var = tk.StringVar(value="  0%")
        ttk.Label(prog_frame, textvariable=self._pct_var,
                  style="Muted.TLabel", width=5).pack(side="left", padx=(8, 0))

        self._status_var = tk.StringVar(value="")
        ttk.Label(self, textvariable=self._status_var,
                  style="Muted.TLabel").pack(anchor="w", padx=18, pady=(0, 6))

        # ── Log ───────────────────────────────────────────────────────────────
        log_frame = ttk.Frame(self, style="Panel.TFrame")
        log_frame.pack(fill="both", expand=True, padx=16, pady=(0, 14))

        log_hdr = ttk.Frame(log_frame, style="Panel.TFrame")
        log_hdr.pack(fill="x", padx=6, pady=(4, 2))
        ttk.Label(log_hdr, text="\u25a3  CONVERSION LOG",
                  style="Section.TLabel", background=T.PANEL).pack(side="left")
        tk.Button(log_hdr, text="CLEAR LOG", command=self._clear_log,
                  bg=T.PANEL, fg=T.MUTED,
                  activebackground=T.HOVER, activeforeground=T.TEXT,
                  font=("Courier New", 8), relief="flat", bd=0,
                  cursor="hand2").pack(side="right")

        self._log_text = T.log_text_widget(log_frame)
        log_sb = ttk.Scrollbar(log_frame, orient="vertical",
                               command=self._log_text.yview,
                               style="SE.Vertical.TScrollbar")
        self._log_text.config(yscrollcommand=log_sb.set)
        log_sb.pack(side="right", fill="y")
        self._log_text.pack(fill="both", expand=True, padx=6, pady=(0, 6))

    # -----------------------------------------------------------------------
    # Helpers
    # -----------------------------------------------------------------------

    # -----------------------------------------------------------------------
    # Preview playback
    # -----------------------------------------------------------------------

    def _on_preview(self):
        if self._previewing:
            try:
                if _SD_OK:
                    _sd.stop()
            except Exception:
                pass
            return
        if not _SD_OK:
            self._log("sounddevice not installed — preview unavailable.", "warn")
            return
        idx = self._listbox.curselection()
        if not idx:
            self._log("Select a file in the list to preview.", "warn")
            return
        path = self._files[idx[0]]
        self._previewing = True
        self._preview_btn.config(text="\u25a0 STOP")
        threading.Thread(target=self._preview_thread, args=(path,), daemon=True).start()

    def _preview_thread(self, path: Path):
        import wave, numpy as np
        tmp_path = None
        try:
            if path.suffix.lower() != ".wav":
                if not self._ffmpeg_ok:
                    self._q.put(("log", "ffmpeg required to preview non-WAV files.", "warn"))
                    return
                tmp = tempfile.NamedTemporaryFile(suffix=".wav", delete=False)
                tmp.close()
                tmp_path = tmp.name
                r = subprocess.run(
                    [self._ffmpeg_path, "-y", "-i", str(path), tmp_path],
                    capture_output=True,
                )
                if r.returncode != 0:
                    self._q.put(("log", "Preview: could not decode file.", "error"))
                    return
                wav_path = tmp_path
            else:
                wav_path = str(path)

            with wave.open(wav_path, "rb") as wf:
                sr       = wf.getframerate()
                channels = wf.getnchannels()
                sampwidth = wf.getsampwidth()
                frames   = wf.readframes(wf.getnframes())

            dtype = {1: np.int8, 2: np.int16, 4: np.int32}.get(sampwidth, np.int16)
            data  = np.frombuffer(frames, dtype=dtype)
            if channels > 1:
                data = data.reshape(-1, channels)

            _sd.play(data, sr)
            _sd.wait()          # blocks daemon thread only — UI stays responsive
        except Exception as exc:
            self._q.put(("log", f"Preview error: {exc}", "error"))
        finally:
            self._previewing = False
            self.after(0, lambda: self._preview_btn.config(text="\u25b6 PLAY"))
            if tmp_path:
                try:
                    Path(tmp_path).unlink(missing_ok=True)
                except Exception:
                    pass

    def _se_btn(self, parent, text, cmd, width=None):
        kw = {"width": width} if width else {}
        return ttk.Button(parent, text=text, command=cmd,
                          style="SE.TButton", **kw)

    def _log(self, msg, tag="info"):
        T.append_log(self._log_text, msg, tag)

    def _log_sep(self):
        self._log("\u2501" * 38, "sep")

    def _clear_log(self):
        self._log_text.config(state="normal")
        self._log_text.delete("1.0", "end")
        self._log_text.config(state="disabled")

    def _log_startup_info(self):
        self._log("  Audio Converter", "cyan")
        self._log_sep()
        if self._ffmpeg_ok:
            self._log(f"ffmpeg found: {self._ffmpeg_path}", "success")
        else:
            self._log("ffmpeg not found \u2014 see Setup screen.", "error")
        if self._xwmaenc_ok:
            self._log(f"xWMAEncode found: {self._xwmaenc_path}", "success")
        else:
            self._log("xWMAEncode not found \u2014 XWM output unavailable (see Setup).", "warn")
        self._log_sep()
        self._on_format_change()

    # -----------------------------------------------------------------------
    # Event handlers
    # -----------------------------------------------------------------------

    def _on_select(self):
        exts = " ".join(f"*{e}" for e in sorted(SUPPORTED_INPUT_EXTS))
        paths = filedialog.askopenfilenames(
            title="Select audio files",
            filetypes=[
                ("Audio files", exts),
                ("WAV / XWM", "*.wav *.xwm"),
                ("MP3 / OGG / FLAC", "*.mp3 *.ogg *.flac"),
                ("All files", "*.*"),
            ],
        )
        if paths:
            for p in paths:
                path = Path(p)
                if path not in self._files:
                    self._files.append(path)
            self._refresh_list()

    def _on_clear(self):
        self._files.clear()
        self._refresh_list()

    def _refresh_list(self):
        self._listbox.delete(0, "end")
        if self._files:
            for f in self._files:
                self._listbox.insert("end", f.name)
            self._lb_scrollbar.pack(side="right", fill="y")
            self._list_placeholder.place_forget()
            n = len(self._files)
            self._file_count_var.set(f"{n} file{'s' if n != 1 else ''} selected")
        else:
            self._lb_scrollbar.pack_forget()
            self._list_placeholder.place(relx=0.5, rely=0.5, anchor="center")
            self._file_count_var.set("")

    def _on_format_change(self, _e=None):
        fmt = self._format_var.get()
        if fmt == "XWM":
            if self._xwmaenc_ok:
                self._xwm_note_var.set(
                    f"xWMAEncode found \u2713  \u2014  files will be encoded via WAV intermediate")
                self._xwm_note_lbl.configure(foreground=T.GREEN)
            else:
                self._xwm_note_var.set(
                    "xWMAEncode not found \u2014 XWM output unavailable. See Setup screen.")
                self._xwm_note_lbl.configure(foreground=T.RED)
            self._xwm_note_lbl.grid(row=1, column=1, sticky="w", pady=(0, 6))
        else:
            self._xwm_note_lbl.grid_remove()

    def _on_affix_change(self, _e=None):
        mode = self._affix_mode_var.get()
        if mode == "None":
            self._affix_entry.config(state="disabled")
        elif mode == "Add Prefix":
            self._affix_entry.config(state="normal")
            self._affix_text_var.set("converted_")
        else:
            self._affix_entry.config(state="normal")
            self._affix_text_var.set("_converted")

    def _on_browse(self):
        folder = filedialog.askdirectory(title="Select output folder")
        if folder:
            self._out_folder = Path(folder)
            self._outfolder_var.set(str(self._out_folder))

    def _on_reset_out(self):
        self._out_folder = None
        self._outfolder_var.set("same as source file")

    def _on_convert(self):
        if self._converting:
            return
        if not self._files:
            T.themed_showinfo(self.winfo_toplevel(), "No Files",
                              "Select at least one audio file to convert.")
            return
        if not self._ffmpeg_ok:
            T.themed_showinfo(self.winfo_toplevel(), "ffmpeg Missing",
                              "ffmpeg is required. See the Setup screen.")
            return
        fmt = self._format_var.get()
        if fmt == "XWM" and not self._xwmaenc_ok:
            T.themed_showinfo(self.winfo_toplevel(), "xWMAEncode Missing",
                              "xWMAEncode.exe is required for XWM output.\n"
                              "See the Setup screen for download instructions.")
            return

        affix_mode = self._affix_mode_var.get()
        affix_text = self._affix_text_var.get() if affix_mode != "None" else ""
        prefix     = affix_text if affix_mode == "Add Prefix" else ""
        suffix     = affix_text if affix_mode == "Add Suffix" else ""
        out_ext    = "." + fmt.lower()
        out_dir    = self._out_folder

        # Overwrite check
        existing = []
        for f in self._files:
            od  = out_dir or f.parent
            out = od / (prefix + f.stem + suffix + out_ext)
            if out.exists():
                existing.append(out.name)
        if existing:
            names = "\n".join(f"  \u2022 {n}" for n in existing[:5])
            if len(existing) > 5:
                names += f"\n  \u2026 and {len(existing)-5} more"
            if not T.themed_askokcancel(self.winfo_toplevel(), "Overwrite Files",
                                        f"These output files already exist:\n\n{names}\n\nOverwrite?"):
                return

        self._start(fmt, prefix, suffix, out_dir)

    def _start(self, fmt, prefix, suffix, out_dir):
        self._converting = True
        self._btn_convert.config(state="disabled")
        self._progress_var.set(0)
        self._pct_var.set("  0%")
        self._status_var.set("")
        threading.Thread(
            target=self._worker,
            args=(fmt, prefix, suffix, out_dir),
            daemon=True,
        ).start()
        self._poll()

    def _worker(self, fmt, prefix, suffix, out_dir):
        files = list(self._files)
        total = len(files)
        for i, f in enumerate(files, 1):
            od  = out_dir or f.parent
            ext = "." + fmt.lower()
            out = od / (prefix + f.stem + suffix + ext)
            self._q.put(("status", f"[{i}/{total}] {f.name}"))
            self._q.put(("log", f"Converting: {f.name}", "cyan"))
            try:
                if fmt == "WAV":
                    convert_to_wav(f, out, self._ffmpeg_path)
                else:
                    convert_to_xwm(f, out, self._ffmpeg_path, self._xwmaenc_path)
                self._q.put(("log", f"  \u2713 Saved: {out.name}", "success"))
            except Exception as exc:
                self._q.put(("log", f"  \u2717 Error: {exc}", "error"))
            self._q.put(("progress", int(i / total * 100)))
        self._q.put(("done", total))

    def _poll(self):
        try:
            while True:
                msg  = self._q.get_nowait()
                kind = msg[0]
                if kind == "log":
                    self._log(msg[1], msg[2])
                elif kind == "status":
                    self._status_var.set(msg[1])
                elif kind == "progress":
                    self._progress_var.set(msg[1])
                    self._pct_var.set(f"{msg[1]:3d}%")
                elif kind == "done":
                    self._progress_var.set(100)
                    self._pct_var.set("100%")
                    self._status_var.set(f"Done \u2014 {msg[1]} file(s) converted.")
                    self._log_sep()
                    self._log(f"Done \u2014 {msg[1]} file(s) converted.", "success")
                    self._log_sep()
                    self._converting = False
                    self._btn_convert.config(state="normal")
                    return
        except queue.Empty:
            pass
        self.after(50, self._poll)
