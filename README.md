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

## How it works (technical)
- Built as a `MelonMod` loaded by MelonLoader.
- Uses game state (`Manager.Instance.Chatting`) as source-of-truth for chat-active behavior.
- Uses Harmony patching to intercept emote execution paths and prevent unintended triggers while chatting.

## Requirements
- Liar's Bar (Steam)
- MelonLoader `0.7+`

## Installation (players)
1. Install MelonLoader into Liar's Bar.
2. Download `HappyHour.dll` from releases (or build it yourself).
3. Place `HappyHour.dll` in:
	 - `...\Liar's Bar\Mods\`
4. Launch the game.

## Build from source (developers)

### 1) Clone/open
- Open this project folder in your IDE:
	- `HappyHour.sln` or `HappyHour.csproj`

### 2) Verify references
- The project references game-managed DLLs via absolute `HintPath` entries in `HappyHour.csproj`.
- If your game is installed in a different location, update those paths.

### 3) Build
- Example:
	- `dotnet build HappyHour.csproj -c Debug`

### 4) Output
- DLL output:
	- `bin\Debug\netstandard2.1\HappyHour.dll`
- Post-build also copies DLL into the Liar's Bar `Mods` folder (based on your configured path in project file).

## Configuration
- Current features are code-based with no external config file yet.

## Troubleshooting
- Mod not loading:
	- Confirm MelonLoader is installed correctly and the DLL is inside `Mods`.
- Build errors about missing assemblies:
	- Recheck `HintPath` values in `HappyHour.csproj`.
- Push/auth issues with GitHub:
	- Clear stale Git credentials and re-auth with the correct GitHub account.

## Disclaimer
- This is an unofficial community mod and is not affiliated with the game developers.
