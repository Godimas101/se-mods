# -*- mode: python ; coding: utf-8 -*-
#
# PyInstaller spec for SE Image Converter
#
# Build with:
#   pyinstaller SE_Image_Converter.spec
#
# Output: dist/SE Image Converter.exe

from PyInstaller.utils.hooks import collect_all

# Collect all Pillow components (image format plugins, libs, etc.)
pil_datas, pil_binaries, pil_hiddenimports = collect_all("PIL")

a = Analysis(
    ["se_launcher.py"],
    pathex=[],
    binaries=pil_binaries,
    datas=pil_datas + [("icon.ico", ".")],
    hiddenimports=pil_hiddenimports + [
        # Screen modules are loaded lazily by string name in se_launcher.py
        # so PyInstaller won't detect them via static analysis.
        "screen_home",
        "screen_setup",
        "screen_image_converter",
        "screen_text_converter",
        # Tool logic modules
        "se_theme",
        "se_lcd_convert",
        "se_text_convert",
        # tkinter — usually auto-detected, listed explicitly for safety
        "tkinter",
        "tkinter.ttk",
        "tkinter.filedialog",
        "tkinter.colorchooser",
    ],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
)

pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name="SE Image Converter",
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    icon="icon.ico",
    console=False,      # no console window — GUI only
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
