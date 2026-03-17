@echo off
:: ============================================================
:: SE Audio Converter — build script
:: Produces:  dist\SE Audio Converter.exe
:: ============================================================

echo.
echo  SE Audio Converter - Build
echo  ==========================
echo.

:: Check Python is available
python --version >nul 2>&1
if errorlevel 1 (
    echo  ERROR: Python not found. Install Python 3.8+ and add it to PATH.
    pause & exit /b 1
)

:: Install / upgrade build dependencies
echo  Installing dependencies...
pip install --quiet --upgrade numpy sounddevice pyinstaller

echo.
echo  Building executable...
echo.

python -m PyInstaller SE_Audio_Converter.spec --clean --noconfirm

if errorlevel 1 (
    echo.
    echo  BUILD FAILED. Check the output above for errors.
    pause & exit /b 1
)

echo.
echo  ============================================================
echo   Build complete:  dist\SE Audio Converter.exe
echo  ============================================================
echo.
echo  NOTE: The following external tools are NOT bundled.
echo  Place them next to SE Audio Converter.exe or add to PATH:
echo.
echo    ffmpeg.exe       https://ffmpeg.org/download.html
echo    xWMAEncode.exe   https://archive.org/details/dxsdk_2010
echo.
pause
