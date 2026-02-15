# 🎭📹 MoodScan — Emotion Tracking While Watching Video

**MoodScan** is a desktop app that captures a viewer’s **facial expressions** while they watch a video
and logs predicted emotions over time.  
The goal is to verify whether the viewer’s emotions match what the video was *intended* to evoke
(e.g., a joke at 3–5 seconds → the viewer should smile/laugh).

This can be useful for **content evaluation** and for measuring the emotional impact of
**advertising campaigns**.

### Research System
This project is part of a two-app research system:
- ✅ **MoodScan** — collects emotion data while a user watches a video
- 🔎 **MoodScanAnalyzer** — analyzes whether the expected emotions were met (per scene and overall)

➡️ MoodScanAnalyzer repository: **https://github.com/gosiula/MoodScanAnalyzer**

---

## ✨ Key Features
- 🎥 Plays a selected video inside the app
- 😀 Detects viewer emotions from the camera (facial expressions)
- 🧾 Logs emotions with timestamps to a CSV (for later analysis)
- 🎬 Records the screen to help verify user reactions
- ⏸️ Handles pause/resume and keeps timestamps consistent

---

## 🧠 Supported Emotions (Fixed Set)
MoodScan supports exactly **7 emotion labels**:

- 😀 `happy`
- 😢 `sad`
- 😡 `angry`
- 😮 `surprised`
- 😕 `confused`
- 😐 `neutral`
- 😨 `scared`

Only these values should be used in `VideoLabels.csv`.

---

## 🗂️ Required Project Setup

### 1) Add `ffmpeg.exe` (not included in the repo)
To enable screen/video processing features, you must add **FFmpeg** locally.

Download FFmpeg for Windows (Windows builds):  
https://www.ffmpeg.org/download.html#build-windows

Place the executable here: MoodScan/Resources/ffmpeg.exe

Make sure that in Visual Studio the ffmpeg.exe file has **Build Action = Content** and **Copy to Output Directory = Copy if newer** so it gets copied to the app’s output folder.

### 2) Create the `Video` folder NEXT TO the project folder
Your folder layout must look like this:

```text
Desktop/
├─ MoodScan/           <-- your cloned repo (the project)
└─ Video/              <-- separate folder next to the project
   ├─ VideoLabels.csv
   └─ Video/
      └─ video.mp4
```

So the video file path will be: Desktop/Video/Video/wideo.mp4

And the CSV file path will be: Desktop/Video/VideoLabels.csv

## 🧾 VideoLabels.csv Format

Create this file: Desktop/Video/VideoLabels.csv

Use **semicolon** (`;`) as the separator.

Required headers: VideoFilePath;PredictedEmotions;Length

Example row: Video/video.mp4;happy(31.00-40.00), surprised(31.00-40.00), happy(50.00-75.00);179,44

### Field Explanation

#### `VideoFilePath`
Relative path from the outer `Video/` folder, e.g.:
- `Video/video.mp4`

#### `PredictedEmotions`
A comma-separated list of expectations in the form: emotion(start-end)

Example:
- `happy(31.00-40.00)`

✅ If **two or more emotions share the same time range**, it means **any one of them is acceptable** for that segment  
(e.g., `happy(31.00-40.00), surprised(31.00-40.00)` → showing *either* emotion satisfies the assumption).

#### `Length`
Video duration in **seconds**, e.g. `179,44`.

---

## ▶️ How to Run

### 1️⃣ Requirements
- 🪟 Windows
- 🧠 Visual Studio (recommended)
- ⚙️ .NET (the version used by your solution)

### 2️⃣ Setup
1. Clone the repository
2. Add `ffmpeg.exe` to:
   - `MoodScan/Resources/ffmpeg.exe`
3. Create the required folder structure:
   - `Desktop/Video/VideoLabels.csv`
   - `Desktop/Video/Video/<your_video>.mp4`
4. Open the solution in Visual Studio
5. Run the project

---

## 🔎 Analysis App
MoodScan **collects the emotion logs**.  
To verify how many viewers reacted “as expected” per scene or across the entire video, use:

➡️ **MoodScanAnalyzer**: **https://github.com/gosiula/MoodScanAnalyzer**

---

## 📄 License
Copyright (c) 2026 Małgorzata Skowron. All rights reserved.

Permission is granted to use, copy, modify, and distribute this software
for noncommercial purposes only, provided that this notice is included in all copies.

Commercial use is not permitted without prior written permission from the author.
For commercial licensing, please contact the author.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED.
IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY.
