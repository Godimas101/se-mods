@echo off
:: ============================================================
:: SE Image Converter — build script
:: Produces:  dist\SE Image Converter.exe
:: ============================================================

echo.
echo  SE Image Converter - Build
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
pip install --quiet --upgrade Pillow pyinstaller

echo.
echo  Building executable...
echo.

python -m PyInstaller SE_Image_Converter.spec --clean --noconfirm

if errorlevel 1 (
    echo.
    echo  BUILD FAILED. Check the output above for errors.
    pause & exit /b 1
)

echo.
echo  ============================================================
echo   Build complete:  dist\SE Image Converter.exe
echo  ============================================================
echo.
echo  NOTE: texconv.exe (optional, for best DDS quality) is NOT
echo  bundled. Users can place it in the same folder as the exe
echo  or anywhere on their system PATH.
echo.
pause
