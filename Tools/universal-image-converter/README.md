# 🖼️ Universal Image Converter
### Space Engineers image tools for modders and players

> Your pixels, Space Engineers-ready — no modding degree required.

---

## ⬇ Download

### **[➤ Download SE Image Converter.exe](https://raw.githubusercontent.com/Godimas101/mods/main/space-engineers-mods/Tools/universal-image-converter/SE%20Image%20Converter.exe)**

**No install required. No Python required. Just download and run.**

**Latest stable release: v1.3**

> Optionally, place [`texconv.exe`](https://github.com/microsoft/DirectXTex/releases/latest) in the same folder as the exe for best-quality DDS output (Image to DDS tool only). Not required — the tool works without it.

---

## What's inside

Two tools in one launcher:

**Image to DDS** — for modders. Converts any image to a DDS texture ready to drop into a Space Engineers LCD mod.

**Image to LCD** — for players. Converts any image to a text string you paste directly into an in-game LCD panel. No mods, no files, nothing to install.

<img src="https://i.imgur.com/HE6HNsv.png" alt="Alt text" width="500">

---

## Image to DDS — Quick Start

1. Select your image(s)
2. Choose your **Screen Target** — the tool outputs the correct DDS size and adds letterbox bars automatically
3. Click **Convert**

Drop the output `.dds` into your mod's `Textures/Models/` folder.

### Screen targets

| Preset | Covers |
|--------|--------|
| LCD Panel · 1:1 | LCD Panel, Transparent LCD, Holo LCD, Full Block LCD |
| Wide LCD Panel · 2:1 | Wide LCD Panel |
| Text Panel / Curved · ~5:3 | Text Panel, Curved LCD, most cockpit screens |
| Widescreen · 16:9 | Vending Machine, Jukebox, Food Dispenser, Entertainment Corner |
| Corner LCD Strip · ~6:1 | Corner LCD panels |
| Custom | Full manual control |

Click **ⓘ** next to the dropdown for a full reference table of every SE block.

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

---

## Image to LCD — Quick Start

1. Select your image
2. Choose your **Screen Target**
3. Pick a dithering mode (Floyd-Steinberg is best for photos)
4. Click **Convert to Text**
5. Click **Copy to Clipboard**
6. In-game, open your LCD's control panel and set:

| Setting | Value |
|---------|-------|
| Content | Text and Images |
| Font | Monospaced |
| Font Size | as shown in the converter (usually 0.1) |
| Text Padding | 0 |

7. Click **Edit Text**, paste, done.

---

## Running from source

If you'd prefer to run the Python source directly:

```bash
pip install Pillow
python se_launcher.py
```

Requires Python 3.8+.

---

## Credits

Made with ♥ by **Godimas** and **Claude**

Image to LCD encoding reverse engineered from [Whiplash's Image Converter](https://github.com/Whiplash141/Whips-Image-Converter) by Whiplash141.
Values sourced from SE game tools and game files (2026).

---

## 🧡 Support

This tool is free and always will be. If it saves you time on your next mod, consider supporting on Patreon — it helps keep the tools and mods coming.

[![Support on Patreon](https://raw.githubusercontent.com/Godimas101/personal-projects/main/patreon/patreon-medium.png)](https://patreon.com/Godimas101)

---

*May cause an uncontrollable urge to texture every LCD on your base.* 🖥️
