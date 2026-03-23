# Sign-In: Frontend, Backend, and Database

## Overview

Sign-in flows: **LoginWindow (UI)** → **LoginViewModel** → **AuthService** → **SQLite DB (Users)**. After success, **App.xaml.cs** sets the session and opens the main window.

---

## 1. Frontend

### 1.1 LoginWindow (View – XAML + code-behind)

| What it does | Details |
|--------------|--------|
| **UI** | Username `TextBox`, Password `PasswordBox`, Sign In `Button`, error message `Border`, "Signing in..." text, ScrollViewer. |
| **Data** | `DataContext = LoginViewModel`; Username is two-way bound; Password is synced in `PasswordBox_PasswordChanged` and in `SignInButton_Click` (WPF cannot bind `PasswordBox.Password`). |
| **Click** | `SignInButton_Click` reads `PasswordBox.Password` and `UsernameBox.Text`, sets `_viewModel.Password`/`Username`, calls `_viewModel.StartLogin()`. |
| **Enter key** | `PasswordBox_KeyDown` triggers `StartLogin()` when Enter is pressed. |

**Possible frontend issues**

- **Password not in VM**  
  If `PasswordChanged` doesn’t run (e.g. control recreated), password can be empty. Mitigation: `SignInButton_Click` always sets `_viewModel.Password = PasswordBox.Password` before `StartLogin()`.

- **Nothing visible after click**  
  Error or "Signing in..." might be off-screen. Mitigation: ScrollViewer added so user can scroll to see feedback.

- **Wrong or null DataContext**  
  Click handler used to depend on `DataContext`; now it uses a stored `_viewModel` reference, so this is addressed.

- **Exceptions in click**  
  Unhandled exception would break the click. Mitigation: `SignInButton_Click` is wrapped in try/catch and sets `ErrorMessage` on exception.

---

## 1.2 LoginViewModel

| What it does | Details |
|--------------|--------|
| **State** | `Username`, `Password`, `ErrorMessage`, `IsLoading`; all raise property change for binding. |
| **Entry point** | `StartLogin()` checks `!_isLoading`, then starts `LoginAsync()` (fire-and-forget). |
| **Validation** | `LoginAsync()` returns early with "Please enter username and password." if either is null/whitespace. |
| **Loading** | Sets `IsLoading = true` on UI thread, then `await _authService.LoginAsync(Username, Password)`. |
| **Result** | On UI thread: success → `OnLoginSuccess?.Invoke(user, mustChangePassword)`; failure → `ErrorMessage = result.Message`. Always clears loading in `finally`. |

**Possible ViewModel issues**

- **OnLoginSuccess null**  
  If the app doesn’t set `OnLoginSuccess` (e.g. after creating `LoginViewModel`), success does nothing. Mitigation: if `OnLoginSuccess == null`, ViewModel sets "Application configuration error. Please restart."

- **UI thread**  
  After `await`, continuation can be on a background thread. Mitigation: all UI updates (error, loading, success callback) go through `RunOnUiThread()`.

- **Empty username/password**  
  Handled in `LoginAsync()` with a clear message; no backend call.

---

## 2. Backend (AuthService)

| What it does | Details |
|--------------|--------|
| **Input** | `LoginAsync(string username, string password)`. |
| **Lookup** | New `ClinicDbContext`, `db.Users.AsNoTracking()`, find user by `Username` (case-insensitive). |
| **Checks** | User exists; `Status != Inactive`; not locked (`LockoutEnd` in future). |
| **Password** | `BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)`. |
| **On failure** | Increment `FailedLoginAttempts`; if ≥ 5, set `Status = Locked` and `LockoutEnd`; save with a new context. |
| **On success** | Reset `FailedLoginAttempts`, `LockoutEnd`, set `Status = Active`, `LastLogin = UtcNow`; save; return `AuthResult` with `User` and `MustChangePassword`. |

**Possible backend issues**

- **Multiple DbContexts**  
  Read uses one context; failed-attempt update and success update each use another. That’s intentional and correct for this design.

- **SqliteException**  
  Caught and returned as "Database error. Try again or restart the app."

- **BCrypt exception**  
  Caught and returned as "Password is incorrect."

- **Generic Exception**  
  Caught and message passed through to the user (could be refined for production).

---

## 3. Database

| What it does | Details |
|--------------|--------|
| **Storage** | SQLite file: `%LocalAppData%\ClinicManager\clinic.db`. |
| **Table** | `Users` (via EF Core `DbSet<User>`). |
| **Relevant fields** | `Username`, `PasswordHash` (BCrypt), `Role`, `Status`, `FailedLoginAttempts`, `LockoutEnd`, `LastLogin`, `MustChangePassword`. |
| **Creation** | `ClinicDbContext` ensures DB and schema exist (`EnsureCreated()` in App startup). |
| **Default admin** | `AuthService.EnsureAdminExistsAsync()` ensures an admin user exists (username `admin`, password `Admin@123`, `MustChangePassword = true`). |

**Possible database issues**

- **DB file missing or locked**  
  First run creates the file; if another process locks it, login can fail with a database error.

- **Schema / migrations**  
  Using `EnsureCreated()` only; no migrations. Schema changes later may require manual handling or migration strategy.

- **Case-sensitive username**  
  Backend uses `.ToLower()` for comparison; DB column is not forced to a collation, but lookup is case-insensitive in code.

---

## 4. App startup (glue)

- **App.xaml.cs** builds services, runs license check, calls `EnsureAdminExistsAsync()`, creates `LoginViewModel` and `LoginWindow`, sets `loginVm.OnLoginSuccess` to:
  - Optionally show **ChangePasswordDialog** if `mustChangePassword`.
  - Set **SessionService** current user and logout callback.
  - Log login, create **MainWindow**, show it, close login window.
- **Issue if sign-in “does nothing”**  
  Usually either: `OnLoginSuccess` was not set (fixed by checking for null and showing config error), or an exception was thrown before/after the async call (fixed by try/catch in click and in `LoginAsync` and by marshalling to UI thread).

---

## 5. Quick checklist (what was fixed / what to watch)

| Layer    | Issue | Status |
|----------|--------|--------|
| Frontend | Click did nothing (DataContext) | Fixed: use `_viewModel` in handler |
| Frontend | Couldn’t see error / "Signing in..." | Fixed: ScrollViewer + scroll error/button row into view on change |
| Frontend | Exception in click | Fixed: try/catch in `SignInButton_Click` |
| Frontend | PropertyChanged leak on close | Fixed: unsubscribe in Window.Closed |
| ViewModel | OnLoginSuccess null | Fixed: show config error message |
| ViewModel | UI updates off thread | Fixed: `RunOnUiThread` for all UI changes |
| ViewModel | Loading not visible immediately | Fixed: set `IsLoading` on UI thread before await |
| ViewModel | Crash during app shutdown in RunOnUiThread | Fixed: catch InvalidOperationException, ObjectDisposedException |
| Backend  | BCrypt.Verify on null/empty PasswordHash | Fixed: validate before verify; return clear message |
| DB       | — | No known sign-in bug |

For "run the app" failures: often the app is already running and the build can’t overwrite the .exe/.dll. Close the running app (or stop the process) and run again.
