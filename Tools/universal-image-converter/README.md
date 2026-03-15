# Universal Image Converter
### Space Engineers image tools for modders and players

Two tools in one launcher:

- **Image to DDS** — converts images to DDS textures for LCD image mods
- **Image to LCD** — converts images to pasteable text strings for in-game LCD panels (no modding required)

---

## Requirements

- **Python 3.8+** — [python.org/downloads](https://www.python.org/downloads/) — tick *Add Python to PATH* during install
- **Pillow** — `pip install Pillow`

---

## Quick Start

```bash
python se_launcher.py
```

The home screen routes you to whichever tool you need.

---

## Image to DDS — For Modders

Converts any image to a BC7_UNORM DDS texture ready for a Space Engineers LCD mod.

### How it works

1. Select your source image(s)
2. Choose your **Screen Target** — the tool automatically outputs the correct DDS dimensions and adds letterbox bars to match the visible area of that LCD block
3. Click **Convert**

The output DDS file drops into your mod's `Textures/Models/` folder.

### Screen targets

| Preset | Covers |
|--------|--------|
| LCD Panel · 1:1 | LCD Panel, Transparent LCD, Holo LCD, Full Block LCD |
| Wide LCD Panel · 2:1 | Wide LCD Panel |
| Text Panel / Curved · ~5:3 | Text Panel, Curved LCD, most cockpit screens |
| Widescreen · 16:9 | Vending Machine, Jukebox, Food Dispenser, Entertainment Corner |
| Corner LCD Strip · ~6:1 | Corner LCD panels |
| Custom | Full manual control — choose max size and aspect handling |

Click **ⓘ** next to the dropdown for a full reference table of every SE block.

### Encoder priority

The tool auto-detects available encoders and uses the best one:

| Priority | Encoder | Format | Notes |
|----------|---------|--------|-------|
| 1st | `texconv.exe` | BC7_UNORM_SRGB | Best quality — matches Keen's own textures |
| 2nd | wand / ImageMagick | DXT5 | Good fallback |
| 3rd | Pillow built-in | DXT5 | Always available |

**To get BC7 output:** download `texconv.exe` from [Microsoft's DirectXTex releases](https://github.com/microsoft/DirectXTex/releases/latest) and place it anywhere on your system PATH.

### Mod folder structure

```
MyMod/
  Data/
    LCDTextures.sbc
  Textures/
    Models/
      MyImage.dds
    Sprites/
      MyImage.dds
```

### LCDTextures.sbc example

```xml
<?xml version="1.0"?>
<Definitions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
             xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <LCDTextures>
    <LCDTextureDefinition>
      <Id>
        <TypeId>LCDTextureDefinition</TypeId>
        <SubtypeId>MyUniqueImageName</SubtypeId>
      </Id>
      <LocalizationId>MyUniqueImageName</LocalizationId>
      <TexturePath>Textures\Models\MyImage.dds</TexturePath>
      <SpritePath>Textures\Sprites\MyImage.dds</SpritePath>
      <Selectable>true</Selectable>
    </LCDTextureDefinition>
  </LCDTextures>
</Definitions>
```

`SubtypeId` must be unique across all loaded mods.

---

## Image to LCD — For Players

Converts any image to a string you paste directly into an in-game LCD panel. No mods, no files, no tools beyond this one.

### How it works

1. Select your image
2. Choose your **Screen Target** — the tool scales the image to match the character resolution of that LCD block
3. Pick a dithering mode (Floyd-Steinberg gives the best results for photos)
4. Click **Convert to Text**
5. Click **Copy to Clipboard**
6. Paste into your LCD panel in-game

### In-game setup (required)

Once you've pasted your string, set the LCD to:

| Setting | Value |
|---------|-------|
| Content | Text and Images |
| Font | Monospaced |
| Font Size | as shown in the converter (usually 0.1) |
| Text Padding | 0 |

### Dithering modes

| Mode | Best for |
|------|----------|
| None | Logos, flat art, pixel art |
| Floyd-Steinberg | Photos, gradients — best overall quality |
| Sierra 3 / Sierra 2 | Softer gradients, slightly less noise than Floyd-Steinberg |
| Ju-Ji-Ni / Stucci | Experimental — try them for specific images |
| Sierra Lite | Fastest dithered mode |

### Options

- **Preserve aspect ratio** — letterboxes/pillarboxes the image to fit the target resolution
- **Transparency** — transparent pixels in the source image become blank SE glyphs instead of a solid fill colour (the LCD's own background shows through)
- **Background** — sets the fill colour for letterbox bars and transparent pixels (only active when Transparency is off)

---

## Supported input formats

PNG, JPG/JPEG, BMP, GIF, TIF/TIFF, WEBP

---

## CLI usage (Image to DDS only)

```bash
# Convert a single file (default: LCD Panel 1:1)
python se_lcd_convert.py image.png

# Specific screen target
python se_lcd_convert.py image.png --screen "Wide LCD Panel  ·  2:1"

# Batch convert a folder
python se_lcd_convert.py ./my_images/ --outdir ./MyMod/Textures/Models/

# Custom size
python se_lcd_convert.py image.png --screen custom --size 512
```

Run `python se_lcd_convert.py --help` for all flags.

---

## Credits

Made with ♥ by **Godimas** and **Claude**

Image to LCD encoding reverse engineered from [Whiplash's Image Converter](https://github.com/Whiplash141/Whips-Image-Converter) by Whiplash141.
Values sourced from SE game tools and game files (2026).
