# Login Authentication Analysis & Fixes

## 1. Where Login Authentication Logic Is Implemented

| File | Purpose |
|------|---------|
| `Services/AuthService.cs` | Main login logic: `LoginAsync()`, password verification, user lookup |
| `Services/PasswordHelper.cs` | Password hashing (PBKDF2) and verification |
| `ViewModels/LoginViewModel.cs` | UI binding, calls `AuthService.LoginAsync()` |
| `Views/LoginWindow.xaml.cs` | Sign-in button handler, passes credentials to ViewModel |
| `App.xaml.cs` | Startup: calls `EnsureAdminExistsAsync()` before showing login |

## 2. Default Admin User & Password

**Yes, the application expects and creates a default admin:**

- **Username:** `admin`
- **Password:** `Admin@123`
- **Location:** `AuthService.cs` – constants `DefaultUsername` and `DefaultPassword`
- **Creation:** `EnsureAdminExistsAsync()` in `App.xaml.cs` (line ~60)

## 3. Database Seeding

**Yes, the database is seeded with a default admin:**

- **When:** At startup in `App.xaml.cs`, after license check
- **Method:** `AuthService.EnsureAdminExistsAsync()`
- **Logic:**
  - If any admin exists → reset that admin’s password to `Admin@123`
  - If no admin exists → create new user `admin` with password `Admin@123`

## 4. Why TypeInitializationException Occurred

**Cause:** Static initialization in `AuthService`:

```csharp
// OLD - caused TypeInitializationException when AuthService was first loaded
private static readonly Regex PasswordStrengthRegex = new(...);
```

The static `Regex` was initialized when the type was first used. On some platforms this can throw `TypeInitializationException`.

**Fix:** Lazy initialization so the `Regex` is created only when needed:

```csharp
private static Regex? _passwordStrengthRegex;
private static Regex PasswordStrengthRegex =>
    _passwordStrengthRegex ??= new Regex(...);
```

## 5. InnerException and Stack Trace Display

**Changes made:**

- **AuthService.cs:** `GetExceptionDetails()` builds a string with message, inner exceptions, and stack trace
- **LoginViewModel.cs:** `GetFullExceptionDisplay()` shows full exception chain and stack trace in the UI
- **App.xaml.cs:** `GetFullExceptionMessage()` already used for startup errors

## 6. Creating Default Admin User

**Default admin is created in two places:**

1. **Startup:** `App.xaml.cs` → `EnsureAdminExistsAsync()`
2. **On login:** If the DB has no users and the user enters `admin` / `Admin@123`, `LoginAsync()` creates the admin and retries login

**Manual creation (if needed):** Run the app once; `EnsureAdminExistsAsync()` will create the admin. If startup fails, try logging in with `admin` / `Admin@123`; the login flow will create the admin.

## 7. Handling Login When Database Has No Users

**Changes made:**

1. **Empty DB + default credentials:** If no users exist and the user enters `admin` / `Admin@123`, `LoginAsync()` calls `EnsureAdminExistsAsync()` and then retries login.
2. **Startup failure:** If `EnsureAdminExistsAsync()` throws at startup, the error is logged and the app still shows the login screen. The user can then try `admin` / `Admin@123`, which will trigger admin creation.
3. **Clear error messages:** Exceptions now include InnerException and stack trace for easier debugging.

## Files Modified

1. **Services/AuthService.cs**
   - Lazy `Regex` initialization
   - `DefaultUsername` and `DefaultPassword` constants
   - `GetExceptionDetails()` for full exception info
   - `TypeInitializationException` handling
   - Admin creation when DB is empty and default credentials are used

2. **ViewModels/LoginViewModel.cs**
   - `GetFullExceptionDisplay()` for full exception display
   - Improved exception handling in login flow

3. **App.xaml.cs**
   - Try/catch around `EnsureAdminExistsAsync()` so startup continues even if it fails
   - Error logging to file

## Default Credentials

- **Username:** `admin`
- **Password:** `Admin@123`
