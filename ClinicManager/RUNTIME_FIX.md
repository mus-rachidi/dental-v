# Runtime Compatibility Fix - SkiaSharp DllNotFoundException

## 1. Library Causing the Error

**SkiaSharp** (via LiveChartsCore.SkiaSharpView.WPF) throws:
- `TypeInitializationException` → `InitializationException` → "Your runtime is currently not supported" → `DllNotFoundException`

The crash occurs **after successful login** when the main window loads the Dashboard, which uses SkiaSharp for charts.

## 2. Dependencies with Native DLLs

| Package | Native DLL | Purpose |
|---------|------------|---------|
| **SkiaSharp** | libSkiaSharp.dll | 2D graphics (charts, rendering) |
| **Microsoft.Data.Sqlite** | e_sqlite3.dll | SQLite database |
| **QuestPDF** | (uses SkiaSharp) | PDF generation |
| **LiveChartsCore.SkiaSharpView.WPF** | (uses SkiaSharp) | Chart controls |

## 3. Root Cause

- **Any CPU** platform can run as 32-bit or 64-bit
- SkiaSharp native DLLs are in `runtimes/win-x64/native/` and `runtimes/win-x86/native/`
- If the app runs as 32-bit but only 64-bit DLLs exist (or vice versa), loading fails

## 4. Fixes Applied

### `.csproj` changes:

```xml
<!-- Force x64 so correct native DLLs load -->
<PlatformTarget>x64</PlatformTarget>
```

```xml
<!-- Explicit Windows native assets for SkiaSharp -->
<PackageReference Include="SkiaSharp.NativeAssets.Win32" Version="3.116.1" />
```

## 5. Required Runtime

- **.NET 8.0 Runtime** for Windows x64
- Install from: https://dotnet.microsoft.com/download/dotnet/8.0
- Or: `winget install Microsoft.DotNet.Runtime.8`

## 6. Clean Build Steps

1. Stop any running ClinicManager
2. Clean and rebuild:
   ```powershell
   cd c:\Users\musta\test\ClinicManager
   dotnet clean
   dotnet restore
   dotnet build
   dotnet run
   ```

## 7. If Still Failing

- Verify output in `bin\Debug\net8.0-windows\` contains:
  - `runtimes\win-x64\native\libSkiaSharp.dll` (or in root)
  - `runtimes\win-x64\native\e_sqlite3.dll`
- For 32-bit Windows, use: `<PlatformTarget>x86</PlatformTarget>`
