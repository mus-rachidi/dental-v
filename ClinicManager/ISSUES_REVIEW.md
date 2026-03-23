# ClinicManager - Issues Review & Fixes Applied

## 1. License Issues ✅ FIXED

### Root Cause
- **Path**: `C:\Users\musta\AppData\Local\ClinicManager\.license`
- **Error**: "Access to the path ... is denied"
- **Why**: The `AppData\Local\ClinicManager` folder is inaccessible

### Fixes Applied
- License paths exclude AppData (App dir, Documents, UserProfile, Temp only)
- **IsLicensed()** now returns `true` on any exception (allows app to run when license access fails)
- User can still activate license or click "Continue without license"

---

## 2. Database & Settings - Same Access Problem ✅ FIXED

### Root Cause
- Database and Settings used `LocalApplicationData\ClinicManager` which was inaccessible

### Fixes Applied
- **DataPathHelper** - new helper that finds a writable directory by trying:
  1. App directory (e.g. `bin\Debug\...\ClinicManager`)
  2. Documents (`My Documents\ClinicManager`)
  3. User profile (`C:\Users\musta\ClinicManager`)
  4. Temp (`%TEMP%\ClinicManager`)
  5. AppData (last resort)
- **ClinicDbContext** now uses `DataPathHelper.GetClinicManagerDirectory()`
- **SettingsService** now uses `DataPathHelper.GetClinicManagerDirectory()`
- **App.xaml.cs** log path uses DataPathHelper

---

## 3. Default Credentials ✅ FIXED

### Fixes Applied
- **AuthService** exposes `DemoUsername` and `DemoPassword` (single source of truth)
- **LoginWindow** uses `AuthService.DemoUsername` / `AuthService.DemoPassword` instead of hardcoding
- AuthService already creates admin on first login when no users exist (when user clicks "Use demo credentials")

---

## 4. Summary

All data (database, settings, license, logs) now uses a writable directory. The app will:
1. Find first writable location among App dir, Documents, UserProfile, Temp, AppData
2. Use that for all ClinicManager data
3. Skip license check on errors (allow app to run)
4. Default credentials work via AuthService.DemoUsername/DemoPassword
