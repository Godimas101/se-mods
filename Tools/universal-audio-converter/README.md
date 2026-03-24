# Universal Audio Converter
### Space Engineers audio tools for modders

---

## ⬇ Download

Download `SE Audio Converter.exe` from the [releases page](../../releases).

> Place [`ffmpeg.exe`](https://ffmpeg.org/download.html) and (optionally) [`xWMAEncode.exe`](#xwmaencode) in the same folder as the exe. Neither is bundled — see [Setup & Requirements](#setup--requirements) below.

---

## What's inside

Four tools in one launcher:

| Tool | What it does |
|------|-------------|
| **Setup & Requirements** | Tool detection, download links, workflow overview |
| **Audio Converter** | Convert any audio format to WAV or XWM |
| **Audio Editor** | Visual WAV editor with waveform display |
| **SBC Generator** | Generate AudioDefinition + SoundBlock SBC files for SE mods |

<img src="https://i.imgur.com/sem8z0T.png" alt="Alt text" width="500">

---

## Recommended workflow

```
[MP3 / OGG / FLAC / etc.]
        │
        ▼
 ┌─────────────────┐
 │ Audio Converter │  →  WAV
 └─────────────────┘
        │
        ▼
 ┌──────────────┐
 │ Audio Editor │  →  trim, fade, normalize, channel mix  →  WAV
 └──────────────┘
        │
        ├──────────────────────────────────────────┐
        ▼                                          ▼
 ┌─────────────────┐                    ┌─────────────────┐
 │ Audio Converter │  →  XWM            │  SBC Generator  │  →  Audio.sbc
 │  (XWM output)   │                   │                  │      SoundBlock.sbc
 └─────────────────┘                   └─────────────────┘
        │
        ▼
  Drop XWM + SBC files into your mod folder. Done.
```

---

## Setup & Requirements

### ffmpeg
Required for the **Audio Converter** (all format conversions).

1. Download the Windows build from **[ffmpeg.org/download.html](https://ffmpeg.org/download.html)**
2. Extract the zip — find `ffmpeg.exe` inside the `bin/` folder
3. Place `ffmpeg.exe` next to `SE Audio Converter.exe` — or add it to your system PATH

### xWMAEncode
Required **only** if you want XWM output from the Audio Converter.

1. Download the **DirectX SDK (June 2010)** from **[archive.org/details/dxsdk_2010](https://archive.org/details/dxsdk_2010)**
2. Install it and navigate to `C:\Program Files (x86)\Microsoft DirectX SDK (June 2010)\Utilities\bin\x86\`
3. Copy `xWMAEncode.exe` next to `SE Audio Converter.exe` — or add it to PATH

> If xWMAEncode is not found, XWM output is automatically greyed out in the converter. All other features work without it.

### numpy + pygame
Required for the **Audio Editor**.

```bash
pip install numpy pygame
```

The Editor's open/save/waveform display works without playback support, but pygame enables the playhead cursor and Play / Play Selection / Stop buttons.

---

## Audio Converter — Quick Start

1. Click **SELECT** and pick your audio files
2. Choose **Output Format**: `WAV` or `XWM`
3. Optionally set a file rename prefix/suffix and output folder
4. Click **▶ CONVERT ▶**

### Supported input formats

| Format | Extension |
|--------|-----------|
| MP3    | `.mp3`    |
| WAV    | `.wav`    |
| OGG    | `.ogg`    |
| FLAC   | `.flac`   |
| AAC    | `.aac`    |
| M4A    | `.m4a`    |
| WMA    | `.wma`    |
| AIFF   | `.aiff` `.aif` |
| Opus   | `.opus`   |
| XWM    | `.xwm`    |

### XWM output pipeline

When XWM is selected the converter runs two steps automatically:
1. `ffmpeg` converts the source file to a 16-bit PCM WAV at 44100 Hz
2. `xWMAEncode` encodes that WAV to XWM

The intermediate WAV is written to a temp folder and deleted when done.

---

## Audio Editor — Quick Start

1. Click **OPEN WAV** and pick a WAV file
2. The waveform appears in the canvas — click and drag to select a region
3. Apply any edit from the toolbars
4. Click **SAVE** (overwrites) or **SAVE AS...** (new file)

### Selection handles

| Action | Result |
|--------|--------|
| Click + drag on empty area | Create a new selection |
| Drag left / right handle | Resize selection |
| Drag inside selection | Move selection |
| **SELECT ALL** button | Select the entire file |

### Edit operations

#### Clip & Volume
| Button | What it does |
|--------|-------------|
| **TRIM TO SELECTION** | Discard everything outside the selection — keeps only what's selected |
| **SILENCE** | Replace the selection with silence |
| **REVERSE** | Flip the selected region backwards |
| **NORMALIZE** | Scale the whole file so the loudest peak hits 0 dBFS |
| **DC OFFSET** | Subtract the mean — fixes a DC bias from some recordings |
| **GAIN + APPLY** | Multiply all samples by a constant (e.g. `0.5` = half volume, `2.0` = double) |

#### Fades
| Button | What it does |
|--------|-------------|
| **FADE IN** | Linear ramp from silence to full volume over the selection |
| **FADE OUT** | Linear ramp from full volume to silence over the selection |

> Tip: set a short selection at the very start of the file and apply Fade In to remove clicks at the beginning. Same at the end with Fade Out.

#### Speed
Pick a multiplier from the dropdown and click **APPLY**:

| Multiplier | Effect |
|-----------|--------|
| 0.50× | Half speed (double duration) |
| 0.75× | Three-quarter speed |
| 1.25× | Slightly faster |
| 1.50× | One and a half times faster |
| 2.00× | Double speed (half duration) |

> Note: speed changes use linear resampling. Quality is good for game audio — if you need studio-grade resampling, process in a DAW first and bring the WAV in.

#### Channels
| Button | What it does |
|--------|-------------|
| **MONO→STEREO** | Duplicate the single channel to left and right |
| **STEREO→MONO** | Average left and right channels into one |
| **SWAP L/R** | Swap left and right channels |
| **EXTRACT L** | Keep only the left channel as mono |
| **EXTRACT R** | Keep only the right channel as mono |

> **SE tip:** Space Engineers sound files — including Sound Block (Sb category) sounds — must be **mono**. Use **STEREO→MONO** before converting to XWM. Stereo files will play left-channel only in-game.

### Undo
Every edit is pushed to a 20-level undo stack. Click **UNDO** to step back one edit at a time.

### Playback
If pygame is installed, use the playback bar:
- **▶ PLAY** — plays the whole file from the beginning
- **PLAY SELECTION** — plays only the selected region
- **■ STOP** — stops playback

The orange playhead cursor tracks position on the waveform in real time.

---

## SBC Generator — Quick Start

1. Click **ADD FILES** and pick your `.wav` or `.xwm` audio files
2. Select a file in the list to edit its settings in the right panel
3. Set **SubtypeId**, **Category**, **Wave Type**, loop options, etc.
4. Click **▶▶ GENERATE SBC ▶▶**
5. Use the **Audio.sbc** and **SoundBlock.sbc** tabs to copy or save the output

### Key settings

| Setting | Notes |
|---------|-------|
| **SubtypeId** | Unique ID for this sound — used in SBC and referenced in-game |
| **Category** | `Sb` for Sound Block sounds, `Music` for music, others for game events |
| **Wave Type** | `D2` = no 3D positioning (Sound Blocks, music), `D3` = 3D positional (block-attached sounds) — both require mono audio |
| **Volume** | 1.0 = original volume |
| **Max Distance** | Relevant for D3/positional sounds — in metres |
| **Loop Type** | None / Simple loop / Start + Loop + End |
| **Stream Sound** | Enable for long music tracks to avoid memory spikes |
| **SB Category ID** | Groups sounds under a named category in Sound Blocks |

### Mod folder structure

```
MyMod/
  Data/
    Audio.sbc          ← from SBC Generator
    SoundBlock.sbc     ← from SBC Generator (Sb category sounds only)
  Audio/
    MySound.xwm        ← your converted audio file
```

### Audio.sbc example (generated output)

```xml
<?xml version="1.0"?>
<Definitions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Sounds>
    <Sound>
      <Id>
        <TypeId>MyObjectBuilder_AudioDefinition</TypeId>
        <SubtypeId>MyModSound</SubtypeId>
      </Id>
      <Category>Sb</Category>
      <MaxDistance>200</MaxDistance>
      <Volume>1.0</Volume>
      <Waves>
        <Wave Type="D2">
          <Loop>Audio\MySound.xwm</Loop>
        </Wave>
      </Waves>
      <StreamSound>false</StreamSound>
    </Sound>
  </Sounds>
</Definitions>
```

---

## Running from source

```bash
pip install numpy pygame
python se_audio_launcher.py
```

Requires Python 3.8+. ffmpeg and xWMAEncode are detected automatically if placed next to the script or on PATH.

## Building the exe

```bash
build.bat
```

Output: `dist\SE Audio Converter.exe`

---

## Credits

Made with ♥ by **Godimas** and **Claude**

Audio format information sourced from SE game files and the DirectX SDK documentation.
