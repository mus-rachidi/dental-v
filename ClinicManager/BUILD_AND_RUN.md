# How to Build and Run ClinicManager (with Login)

## ✅ Verification: Your code HAS the login & auth features

The source code includes:
- Login window (shows first)
- AuthService, SessionService, User management
- App.xaml.cs starts with LoginWindow

---

## Step-by-step: See the Login screen

### 1. Open Terminal in the project folder
```
cd "/Users/mustapharachidi/Desktop/untitled folder 2/ClinicManager"
```

### 2. Clean and rebuild (important!)
```bash
dotnet clean
dotnet build -c Release
```

### 3. Run the correct exe
**Path to the exe:**
```
ClinicManager/bin/Release/net8.0-windows/win-x64/ClinicManager.exe
```

Or run from terminal:
```bash
dotnet run -c Release
```

---

## Startup order (what you'll see)

1. **License dialog** (if not activated) – activate or skip
2. **Login window** – username/password (default: admin / Admin@123)
3. **Main window** – after successful login

---

## If you still don't see the Login

- **Check you're running the right exe** – Don't use an exe from a different folder or old shortcut
- **License first** – If you see a license dialog, complete it; Login comes next
- **Error on startup?** – A message box with an error means something failed before Login

---

## Quick test from terminal
```bash
cd "/Users/mustapharachidi/Desktop/untitled folder 2/ClinicManager"
dotnet run -c Release
```
