# Clinic Manager – Morocco Dental Implementation Plan

## Executive Summary

This document outlines the upgrade path to make Clinic Manager a **professional, Morocco-ready dental management system**. The plan preserves the existing WPF architecture while adding Morocco-specific features (CNSS, MAD, TVA), enhanced security, and modern UX.

---

## 1. Patient Module (Pro-Level)

### 1.1 Data Model Extensions

| Field | Type | Purpose |
|-------|------|---------|
| CIN | string(20) | Moroccan national ID (Carte d'Identité Nationale) |
| EmergencyContact | string(200) | Emergency contact name/phone |
| RegistrationDate | DateTime | When patient registered |
| Allergies | string(1000) | Known allergies |
| Medications | string(1000) | Current medications |
| ChronicDiseases | string(1000) | Diabetes, hypertension, etc. |
| PregnancyStatus | string(50) | N/A, Pregnant, Not applicable |
| CNSSNumber | string(50) | CNSS affiliation number |
| CNSSCoverageType | string(50) | Assuré, Ayant droit, etc. |
| CNSSRegistrationDate | DateTime? | CNSS registration date |
| CNSSValidityDate | DateTime? | Coverage validity end |

### 1.2 Integration Steps

1. Run database migration (new columns added via `AddColumnIfMissing`)
2. Update `PatientService` to handle new fields
3. Extend `PatientsPage.xaml` with tabs: General | Medical History | CNSS | Documents
4. Add `PatientDocument` model for file storage (path, type, date)
5. Add `AuditLog` for patient data changes

### 1.3 FDI Tooth Chart

- Existing `ToothShapeControl` uses FDI notation (1–32)
- Ensure tooth numbers follow Universal/FDI standard
- Add consent form storage linked to patient

---

## 2. Appointments Module

### 2.1 Enhancements

- **Conflict detection**: Before save, query `Appointments` for same `DoctorName` + `Date` + overlapping `Time`/`DurationMinutes`
- **Calendar views**: Add `CalendarViewMode` (Day/Week/Month) with WPF `Calendar` or custom grid
- **Reminders**: Use `System.Windows.Threading.DispatcherTimer` or `Hardcodet.NotifyIcon` for in-app notifications; optional: integrate Twilio/AfricasTalking for SMS
- **Reschedule/Cancel logging**: Add `AppointmentHistory` table (OldDate, NewDate, Action, User, Timestamp)

### 2.2 Integration

- Add `AppointmentService.CheckConflictAsync(doctor, date, time, duration, excludeId)`
- Add `ReminderService` with configurable interval
- Add `AppointmentHistory` model and migration

---

## 3. Billing Module (Morocco-Compliant)

### 3.1 Payment Model Extensions

| Field | Type | Purpose |
|-------|------|---------|
| TreatmentCost | decimal | Total treatment cost |
| CNSSCoveredAmount | decimal | Amount reimbursed by CNSS |
| PatientAmount | decimal | Patient pays (TreatmentCost - CNSSCoveredAmount - Discount) |
| DiscountAmount | decimal | Discount applied |
| VATRate | decimal | TVA rate (e.g. 20%) |
| VATAmount | decimal | TVA amount |
| Currency | string(3) | "MAD" |
| CNSSClaimStatus | enum | NotSubmitted, Submitted, Approved, Rejected |
| CNSSReceiptNumber | string(50) | CNSS receipt reference |

### 3.2 Invoice Generation

- Use existing `QuestPDF` for PDF invoices
- Add MAD formatting: `{0:N2} MAD`
- Add TVA calculation: `VATAmount = TreatmentCost * VATRate / 100`
- CNSS report: Export payments with `CNSSClaimStatus`, `CNSSCoveredAmount` for monthly submission

### 3.3 Integration

- Extend `Payment` model and migration
- Add `PaymentService.GetCNSSReportAsync(from, to)`
- Update `BillingPage` form with CNSS fields
- Add `MoroccoFormattingHelper` for MAD and DD/MM/YYYY

---

## 4. Dashboard & Analytics

### 4.1 KPI Tiles (Existing + New)

- Total Patients ✓
- New Patients This Month
- Today's Appointments ✓
- Upcoming Appointments ✓
- Completed Treatments (count)
- Revenue (Today, Month) ✓
- Pending Bills (sum of Pending payments)
- Alerts (e.g. CNSS claims to submit)

### 4.2 Charts (Free Options)

- **LiveCharts2** (MIT): `dotnet add package LiveChartsCore.SkiaSharpView.WPF`
- **OxyPlot** (MIT): `dotnet add package OxyPlot.Wpf`
- Use for: Monthly Revenue, Appointments by Doctor, Treatment Types

### 4.3 Integration

- Add `DashboardViewModel` properties: `NewPatientsThisMonth`, `PendingBills`, `CompletedTreatments`
- Add chart bindings in `DashboardPage.xaml`
- Add click handlers to navigate from tiles to modules

---

## 5. UI/UX Enhancements

### 5.1 Current State

- ✓ Dark/Light mode
- ✓ French + English
- ✓ Top bar language/theme
- ✓ Modern theme (LightTheme.xaml, DarkTheme.xaml)

### 5.2 Additions

- **Morocco date format**: Use `CultureInfo("fr-MA")` or custom `dd/MM/yyyy`
- **MAD currency**: `StringFormat='{}{0:N2} MAD'`
- **Validation**: Inline errors via `Validation.ErrorTemplate`
- **Tooltips**: Add `ToolTip` to form labels
- **Arabic (optional)**: Add `Strings.ar.resx`, RTL layout for future

---

## 6. User Accounts & Security

### 6.1 Models

- `User`: Id, Username, PasswordHash, Role, IsActive, CreatedAt
- `Role`: Admin, Dentist, Assistant, Reception
- `AuditLog`: Id, UserId, Action, EntityType, EntityId, OldValue, NewValue, Timestamp

### 6.2 Security

- **Password hashing**: Use `BCrypt.Net-Next` or `Argon2` via `Microsoft.AspNetCore.Cryptography.KeyDerivation`
- **Session**: Store current user in `App.Current.Properties` or static `CurrentUser`
- **RBAC**: Check role before allowing actions (e.g. only Admin can delete patients)
- **Lockout**: Track failed logins in `User` or separate table; lock after 5 failures
- **Encryption**: Use `ProtectedData.Protect` for sensitive fields (optional)

### 6.3 Integration

- Add `LoginWindow` before `MainWindow`
- Add `UserService`, `AuditService`
- Wrap sensitive operations with audit logging

---

## 7. Licensing System

### 7.1 Current

- Machine ID via WMI
- License file with key + name
- SHA-256 based validation

### 7.2 Upgrade to RSA

- Generate RSA key pair (2048-bit)
- **Private key**: Kept only in License Generator (secure)
- **Public key**: Embedded in app
- **License file**: JSON `{ MachineId, LicensedTo, Expiry, Signature }`
- **Signature**: Sign `MachineId|LicensedTo|Expiry` with private key
- **Verification**: Verify signature with public key; check MachineId and Expiry

### 7.3 License Generator Tool

- Console or WPF app
- Input: Machine ID, Name, Expiry
- Output: `.license` file
- Uses private key from config/embedded

---

## 8. Backup & Disaster Recovery

### 8.1 Current

- Manual backup via Settings
- `DatabaseBackupService` copies `clinic.db`
- Restore from file picker

### 8.2 Enhancements

- **Scheduled backup**: Use `System.Threading.Timer` or `DispatcherTimer`; run `CreateBackupAsync` every N hours when `AutoBackup` is true
- **Versioning**: Keep last 30 backups; delete oldest when limit exceeded
- **Notifications**: Use `NotificationHelper` or tray icon for success/failure
- **File backup**: Include `PatientFiles` folder (if added) in backup zip

### 8.3 Integration

- In `App.OnStartup`, start backup timer if `AutoBackup` is true
- Add `BackupService.ScheduleBackup()` and `CancelSchedule()`

---

## 9. Technical Notes

### 9.1 WinUI 3 vs WPF

- **Recommendation**: Stay with WPF for this project. WinUI 3 migration would require significant rewrite.
- WPF with modern themes (as implemented) provides a professional look.
- WinUI 3 can be considered for a future v2.

### 9.2 Syncfusion / Telerik

- Commercial licenses required. For cost-free charts, use **LiveCharts2** or **OxyPlot**.

### 9.3 Database

- SQLite is suitable for single-clinic desktop. For multi-clinic or cloud, consider SQL Server / PostgreSQL migration later.

---

## 10. Morocco-Specific Checklist

| Feature | Status |
|---------|--------|
| CNSS patient fields | To implement |
| CNSS billing (covered amount, claim status) | To implement |
| MAD currency display | To implement |
| TVA calculation | To implement |
| Date format DD/MM/YYYY | To implement |
| CNSS reports export | To implement |
| CIN (national ID) field | To implement |
| Optional: SMS reminders | Future |
| Optional: Arabic language | Future |

---

## Integration Order

1. **Phase 1**: Data models + migrations (Patient, Payment extensions, AuditLog)
2. **Phase 2**: Morocco formatting (MAD, dates)
3. **Phase 3**: Patient form + CNSS section
4. **Phase 4**: Billing form + CNSS + invoice
5. **Phase 5**: Dashboard KPIs + charts
6. **Phase 6**: Scheduled backup
7. **Phase 7**: User accounts + audit (optional)
8. **Phase 8**: RSA licensing (optional)

---

## Audit Recommendations

- **Security**: Add input validation, parameterized queries (EF Core does this), avoid storing passwords in plain text
- **UI/UX**: Add loading indicators, disable buttons during save, confirm before delete
- **Morocco**: Validate CIN format if needed; ensure CNSS number format per official docs
- **Performance**: Index frequently queried columns; consider pagination for large lists
