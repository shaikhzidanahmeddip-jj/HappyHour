# Happy Hour (Liar's Bar QoL Mod)

Happy Hour is a MelonLoader mod for Liar's Bar focused on quality-of-life fixes that reduce frustrating gameplay edge cases.

## What this mod does

### 1) Quick Disconnect
- Press `End` to safely leave a broken/stuck lobby without needing Alt+F4.
- It forces client leave flow (lobby leave + network client stop) and loads `SteamTest` scene.

### 2) Chat Input Safety (in-game)
- Prevents emote hotkeys from triggering while chat input is active.
- Includes state repair so blocked emote input does not incorrectly consume emote cooldown.

### 3) Chat Active Indicator
- Shows a top-screen overlay when chat is actively capturing keyboard input.
- Helps prevent confusion where key presses are going to chat instead of gameplay.

## Requirements
- Liar's Bar (Steam)
- MelonLoader `0.7+`

## Installation
1. Install MelonLoader into Liar's Bar.
2. Download `HappyHour.dll` from releases (or build it yourself).
3. Place `HappyHour.dll` in:
	 - `...\Liar's Bar\Mods\`
4. Launch the game.

## Disclaimer
- This is an unofficial community mod and is not affiliated with the game developers.
