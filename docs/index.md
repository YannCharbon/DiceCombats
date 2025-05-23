---
layout: default
title: DiceCombats
---

<p align="center">
   <img src="https://raw.githubusercontent.com/YannCharbon/DiceCombats/main/DiceCombats/wwwroot/dicecombats_logo.svg" alt="DiceCombats" width="150" height="auto" />
</p>

# DiceCombats

DiceCombats is an open-source, system-agnostic tabletop RPG combat management tool designed to make game mastering seamless, modular, and enjoyable. It is aimed at frustrated GMs who have struggled to find the right tool for managing combats effectively due to organizational challenges, missing fields, or tags to track specific information. The keyword here is **customization**.

---

## Key Features

- **Flexible Creature Creation**: Customize creatures with attributes, abilities, and information to suit any RPG system. Save them for reuse in future campaigns.
- **Modular Combat Management**: Efficiently track initiative, hit points, status effects, and any custom-defined information in real-time.
- **System-Agnostic Design**: Adaptable to any RPG system, giving you maximum flexibility for your campaigns.
- **Customizable Setup**: Import and export data to share with other GMs. Configure settings to match your unique game-mastering style.
- **For D&D Players**: Compatibility with [aidedd.org](https://www.aidedd.org/en/) to easily import any existing creature.
- **Multi-language Support**: Currently available languages are **English** and **French**
- **Experimental Android Support**: Application is also available on Android.

<div style="display:flex;">
<div style="width:45%;padding:8px;">
<img src="https://raw.githubusercontent.com/YannCharbon/DiceCombats/main/DiceCombats/wwwroot/img/app_examples/DiceCombats_example_1.png" />
</div>
<div style="width:45%;padding:8px;">
<img src="https://raw.githubusercontent.com/YannCharbon/DiceCombats/main/DiceCombats/wwwroot/img/app_examples/DiceCombats_example_2.png" />
</div>
</div>

---

## Installation

You can find the latest pre-compiled DiceCombats app here: [![Latest Release](https://img.shields.io/github/v/release/YannCharbon/DiceCombats?style=for-the-badge)](https://github.com/YannCharbon/DiceCombats/releases/latest).

- **Windows Users**: Simply extract the ZIP archive to any location on your PC and run `DiceCombats.exe`. For quick access, you can create a shortcut on your Desktop or pin it to the Start menu.
- **Android Users**: Download the APK and follow the installation process. **Note that the Android app is experimental *(rendering might have some issues)***

## Manual Build

If you prefer to build manually from the source code, follow these steps:

### Prerequisites
- Ensure you have the .NET SDK installed. [Download .NET SDK](https://dotnet.microsoft.com/download)
- For mobile platforms, ensure you have a compatible environment for building MAUI applications.
- You can also open the solution in Visual Studio and build it from there.

### Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/YannCharbon/DiceCombats.git
   ```
2. Navigate to the project directory:
   ```bash
   cd DiceCombats
   ```
3. Build:
   ```bash
   dotnet build DiceCombats.sln -f net8.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=None -p:IncludeAllContentForSelfExtract=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true
   ```
4. Locate binary:
   The produced binaries can be found at `DiceCombats/bin/Release/`.

---

## Development Guide

- **Issues and feature requests**: Report issues and suggest features here [![GitHub Issues](https://img.shields.io/github/issues/YannCharbon/DiceCombats?style=for-the-badge)](https://github.com/YannCharbon/DiceCombats/issues).
- **Contribution**: Contributions are welcome! Ensure all pull requests are well-documented and follow best coding practices. [![GitHub Pull Requests](https://img.shields.io/github/issues-pr/YannCharbon/DiceCombats?style=for-the-badge)](https://github.com/YannCharbon/DiceCombats/pulls)
- **License**: This project is licensed under GPL-3.0.

---

## Support the Project

As this is a free and open-source project developed in my free time, updates and bug fixes may take time. If you enjoy using DiceCombats and wish to support its development, please consider donating via PayPal

[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=4X9ZURL5T4E6N)


---

Thank you for choosing DiceCombats! Happy gaming!
