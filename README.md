# DeepL Translate — Flow Launcher plugin

[![Publish Release](https://github.com/Nreel/Flow.Launcher.Plugin.DeepLTranslate/actions/workflows/Publish%20Release.yml/badge.svg)](https://github.com/Nreel/Flow.Launcher.Plugin.DeepLTranslate/actions/workflows/Publish%20Release.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Flow Launcher](https://img.shields.io/badge/Flow%20Launcher-2.0.0%2B-2f6fed.svg)](https://www.flowlauncher.com/)

A [Flow Launcher](https://www.flowlauncher.com/) plugin that translates text using the
[DeepL API](https://www.deepl.com/pro-api) (Free or Pro), with automatic source-language
detection and per-query language overrides.

## Features

- Translate via the DeepL API (Free or Pro plan, selectable in settings).
- Automatic source-language detection (DeepL's built-in detection).
- Configurable default source and target languages.
- Per-query overrides: `tr es hola` → Spanish, `tr en es hello` → English → Spanish.
- API key is encrypted at rest with Windows DPAPI and masked in the settings UI.

## Requirements

- Flow Launcher **v2.0.0 or newer** (.NET 9).
- A DeepL API key ([Free](https://www.deepl.com/pro#developer) or Pro).

## Installation

### From the Flow Launcher Plugin Store

Once the plugin is listed, run `pm install DeepL Translate` in Flow Launcher, or open
**Settings → Plugins → Store** and install it there.

### From a GitHub release

1. Download `Flow.Launcher.Plugin.DeepLTranslate.zip` from the
   [latest release](https://github.com/Nreel/Flow.Launcher.Plugin.DeepLTranslate/releases/latest).
2. Unpack it into its own folder — the folder must be named after the plugin:

   ```
   %APPDATA%\FlowLauncher\Plugins\Flow.Launcher.Plugin.DeepLTranslate\
   ```

3. Restart Flow Launcher (or use **Settings → Plugins → … → Reload**).

### Build from source

```powershell
dotnet build -c Release
```

Then copy the contents of `bin\Release\` into a new plugin folder:

```
%APPDATA%\FlowLauncher\Plugins\Flow.Launcher.Plugin.DeepLTranslate\
```

The folder must contain:

- `Flow.Launcher.Plugin.DeepLTranslate.dll`
- `Flow.Launcher.Plugin.dll` (SDK, copied automatically)
- `plugin.json`
- `Images\deepl.png`

Restart Flow Launcher (or use `Settings → Plugin → ... → Reload`).

## Configuration

Open Flow Launcher **Settings → Plugins → DeepL Translate**.

1. **DeepL API key** — paste your key; it is encrypted with Windows DPAPI and shown as
   masked/placeholder after entry. Use **Clear key** to remove it.
2. **DeepL API plan** — Free (`api-free.deepl.com`) or Pro (`api.deepl.com`).
3. **Default source language** — `Auto-detect` (recommended) or a fixed language.
4. **Default target language** — e.g. English (US).

## Usage

| Input                 | Meaning                                          |
| --------------------- | ------------------------------------------------ |
| `tr привет`           | Auto-detect → default target (e.g. English)      |
| `tr hello`            | Auto-detect → default target                     |
| `tr es hola`          | Translate **to Spanish** (source auto-detected)  |
| `tr en es hello`      | Translate **from English to Spanish**            |

Press **Enter** on a result to copy the translation to the clipboard.
The subtitle shows the detected source → target languages.

### Language override syntax

- Two leading language codes: `<from> <to> <text>` — override both source and target.
- One leading code: `<to> <text>` — override the target only; source stays auto-detected.
- No code: `<text>` — use the defaults from settings.

The trigger phrase is Flow Launcher's action keyword. Defaults are `tr`, `transl`, and
`translate`. Change it via **Settings → Plugins → DeepL Translate** (edit keyword).

## Language codes

Codes are case-insensitive. A few examples: `en` English, `es` Spanish, `ru` Russian,
`de` German, `fr` French, `it` Italian, `pt` Portuguese, `ja` Japanese, `ko` Korean,
`zh` Chinese (simplified), `uk` Ukrainian, `tr` Turkish, `pl` Polish, `ar` Arabic.

Regional variants: `en-gb`, `pt-br`, `pt-pt`, `zh-hant`. The full list is shown in the
plugin settings and in `Languages.cs`.

## Security note

The API key is encrypted with the Windows Data Protection API (DPAPI), scoped to the
current Windows user, and stored in the plugin's `Settings.json`. It is tied to your
Windows account/profile — copying the settings file to another machine or user will not
expose the key (it simply won't decrypt).

## Building and releasing

```powershell
dotnet build -c Release
```

The release artifact is the whole `bin\Release` folder zipped as
`Flow.Launcher.Plugin.DeepLTranslate.zip` (`plugin.json`, the plugin DLL, the copied
`Flow.Launcher.Plugin.dll` SDK, and `Images\deepl.png`).

Releases are automated by
[`.github/workflows/Publish Release.yml`](.github/workflows/Publish%20Release.yml), which
runs on every push to `main` that touches C#/XAML/`plugin.json`:

1. builds in Release with the .NET 9 SDK,
2. reads `Version` from `plugin.json` (the single source of truth),
3. zips `bin\Release` into `Flow.Launcher.Plugin.DeepLTranslate.zip`,
4. creates or updates the GitHub Release tagged `v<version>` with **only** that zip attached.

> **To cut a release:** bump `Version` in `plugin.json`, commit, and push to `main`.
> The Flow Launcher plugin store updater picks up the new tag automatically and refreshes
> the store listing every few hours.

## License

[MIT](LICENSE) © 2026 Nreel

## Project layout

| File                          | Purpose                                        |
| ----------------------------- | ---------------------------------------------- |
| `Main.cs`                     | Plugin entry point (`IAsyncPlugin`, settings)  |
| `TranslationRequestParser.cs` | Query → source/target/text parsing             |
| `DeepLClient.cs`              | DeepL HTTP client                              |
| `Languages.cs`                | Language catalog + code normalization          |
| `CredentialProtector.cs`      | DPAPI encrypt/decrypt of the API key           |
| `Settings.cs`                 | Persisted settings model                       |
| `SettingsView.xaml(.cs)`      | Settings panel UI                              |
| `plugin.json`                 | Flow Launcher plugin manifest                  |
