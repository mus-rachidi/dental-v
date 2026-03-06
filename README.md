# Clinic Manager - Professional Clinic Management System

A fully offline Windows desktop application for managing clinic operations, built with .NET 8, WPF, and SQLite.
ZYQSW82LGDURY0IOY8YZKAPEOGFXRDSL
---

## Features

- **Patient Management** - Full CRUD with search, export to PDF/Excel
- **Appointment Scheduling** - Date-filtered views, status tracking, doctor assignment
- **Billing & Payments** - Invoice generation, revenue tracking, multiple payment methods
- **Medical Records** - Diagnosis, prescriptions, vitals, patient history
- **Dashboard** - Real-time stats, today's appointments, quick search
- **Dark Mode** - Full light/dark theme support
- **Bilingual** - English and French with live language switching
- **Offline** - No internet required, all data stored locally
- **Secure** - Encrypted database, hardware-bound licensing, error logging
- **Backups** - Automatic and manual database backup/restore
- **Exports** - PDF patient reports, Excel data exports

---

## Tech Stack

| Component       | Technology                    |
|-----------------|-------------------------------|
| Framework       | .NET 8                        |
| Language        | C#                            |
| UI              | WPF with MVVM                 |
| Database        | SQLite (encrypted)            |
| PDF Export      | QuestPDF                      |
| Excel Export    | ClosedXML                     |
| DI              | Microsoft.Extensions.DI       |
| MVVM Toolkit    | CommunityToolkit.Mvvm         |
| Installer       | Inno Setup                    |

---

## Project Structure

```
ClinicManager/
├── Models/              # Data entities (Patient, Appointment, Payment, MedicalRecord)
├── Database/            # DbContext and backup service
├── Services/            # Business logic (CRUD, export, settings)
├── ViewModels/          # MVVM view models for each page
├── Views/
│   ├── Pages/           # Dashboard, Patients, Appointments, Billing, Records, Settings
│   ├── Dialogs/         # License activation dialog
│   └── MainWindow.xaml  # Shell with sidebar navigation
├── Helpers/             # Commands, converters, base classes
├── Licensing/           # Hardware fingerprint + license validation
├── Localization/        # English/French .resx files + translation engine
├── Resources/
│   └── Themes/          # Light and Dark theme XAML
└── Installer/           # Inno Setup script + license text

LicenseGenerator/        # Standalone console tool for generating license keys
```

---

## Prerequisites

- **Windows 10/11** (x64)
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (recommended) or VS Code with C# extension
- **Inno Setup 6** (for building installer) - [Download](https://jrsoftware.org/isinfo.php)

---

## Build Instructions

### 1. Clone / Copy the Project

Copy the entire project folder to your development machine.

### 2. Restore Dependencies

```bash
cd ClinicManager
dotnet restore
```

### 3. Build in Debug Mode

```bash
dotnet build
```

### 4. Run the Application

```bash
dotnet run --project ClinicManager
```

### 5. Build for Release (Self-Contained)

This creates a standalone executable that includes the .NET runtime:

```bash
dotnet publish ClinicManager/ClinicManager.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=false \
  -o ClinicManager/bin/Release/net8.0-windows/win-x64/publish
```

### 6. Build the License Generator

```bash
dotnet publish LicenseGenerator/LicenseGenerator.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o LicenseGenerator/bin/publish
```

---

## Creating the Installer

### Prerequisites
- Install [Inno Setup 6](https://jrsoftware.org/isinfo.php)
- Build the project in Release mode first (step 5 above)

### Steps

1. Open `ClinicManager/Installer/setup.iss` in Inno Setup Compiler
2. Press **Ctrl+F9** (or Build > Compile)
3. The installer will be created at `ClinicManager/Installer/Output/ClinicManager_Setup_1.0.0.exe`

### App Icon

Create an `app.ico` file (256x256 recommended) and place it at:
```
ClinicManager/Resources/Icons/app.ico
```

---

## License Key System

### How It Works

1. **First Launch**: App generates a unique Machine ID from CPU, disk, and motherboard serial numbers
2. **Activation**: User provides this Machine ID to you (the vendor)
3. **Key Generation**: You run the LicenseGenerator tool with their Machine ID
4. **Activation**: User enters the license key and it's validated against their hardware
5. **Storage**: License is encrypted and stored locally

### Generating a License Key

```bash
# Run the license generator
LicenseGenerator.exe

# Or pass the Machine ID as argument
LicenseGenerator.exe ABCD1234EFGH5678IJKL9012
```

The tool will output a 25-character key in format: `XXXXX-XXXXX-XXXXX-XXXXX-XXXXX`

### License Flow

```
Client Machine                          Vendor
     |                                     |
     |--- Shows Machine ID: ABC123... ---->|
     |                                     |
     |                    Runs LicenseGenerator
     |                    with Machine ID  |
     |                                     |
     |<--- Returns License Key: XX-XX... --|
     |                                     |
     | Enters key in activation dialog     |
     | Key validated against hardware      |
     | Encrypted license saved locally     |
```

---

## Packaging for USB Distribution

### Step-by-Step

1. **Build the installer** (see Creating the Installer above)

2. **Prepare USB contents:**
   ```
   USB Drive/
   ├── ClinicManager_Setup_1.0.0.exe    # The installer
   ├── README.txt                        # Quick start guide
   └── LICENSE.txt                       # License agreement
   ```

3. **Create a README.txt for the USB:**
   ```
   CLINIC MANAGER - Installation Guide
   ====================================
   1. Double-click ClinicManager_Setup_1.0.0.exe
   2. Follow the installation wizard
   3. Launch from Desktop shortcut
   4. Contact us with your Machine ID for license activation
   ```

4. **Copy to USB drive** and distribute

### Autorun (Optional)

Create `autorun.inf` on the USB:
```ini
[autorun]
open=ClinicManager_Setup_1.0.0.exe
icon=ClinicManager_Setup_1.0.0.exe,0
label=Clinic Manager Setup
```

---

## Security Best Practices

### Database Encryption
The SQLite database uses a password-protected connection string. For production, consider using [SQLCipher](https://www.zetetic.net/sqlcipher/) for full AES-256 encryption by replacing the `Microsoft.EntityFrameworkCore.Sqlite` package with `Microsoft.EntityFrameworkCore.SqlCipher`.

### Code Obfuscation
For release builds, use one of these .NET obfuscators:
- **[ConfuserEx](https://github.com/mkaring/ConfuserEx)** (free, open source)
- **[Dotfuscator](https://www.preemptive.com/products/dotfuscator)** (included in VS)
- **[Eazfuscator.NET](https://www.gapotchenko.com/eazfuscator.net)** (commercial)

### Anti-Debugging
Add to your release build:
```csharp
#if !DEBUG
if (System.Diagnostics.Debugger.IsAttached)
{
    MessageBox.Show("Debugging is not allowed.");
    Environment.Exit(1);
}
#endif
```

### License Security
- License file is AES-256 encrypted
- Hardware fingerprint uses CPU ID + Disk Serial + Motherboard ID
- License key is a HMAC-SHA256 hash that cannot be reverse-engineered
- The shared secret between LicenseGenerator and the app should be changed before distribution

### Recommended Additional Steps
1. **Change all encryption keys** in `LicenseManager.cs` before building for production
2. **Change the `LicenseSecret`** in both `LicenseManager.cs` and `LicenseGenerator/Program.cs`
3. **Sign your executable** with a code signing certificate
4. **Use .NET trimming** to reduce attack surface
5. **Enable ReadyToRun** compilation for better startup and harder reverse engineering

---

## Keyboard Shortcuts

| Shortcut | Action                     |
|----------|----------------------------|
| F5       | Refresh current view       |

---

## Data Storage Locations

| Item        | Path                                           |
|-------------|------------------------------------------------|
| Database    | `%LOCALAPPDATA%\ClinicManager\clinic.db`       |
| Settings    | `%LOCALAPPDATA%\ClinicManager\settings.json`   |
| License     | `%LOCALAPPDATA%\ClinicManager\.license`         |
| Backups     | `%LOCALAPPDATA%\ClinicManager\Backups\`         |
| Error Logs  | `%LOCALAPPDATA%\ClinicManager\Logs\`            |

---

## Localization

The app supports English and French. To add more languages:

1. Copy `Localization/Strings.resx` to `Strings.{culture}.resx` (e.g., `Strings.ar.resx`)
2. Translate all values in the new file
3. Add the new culture option to the Settings page combo box
4. Rebuild

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| App won't start | Check error logs in `%LOCALAPPDATA%\ClinicManager\Logs\` |
| License invalid | Ensure Machine ID matches; hardware changes invalidate licenses |
| Database errors | Try restoring from a backup in Settings |
| Blank screen | Delete `settings.json` and restart |

---

## License

Copyright 2026 ClinicSoft. All rights reserved. Proprietary software.
