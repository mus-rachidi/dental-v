using System;

namespace ClinicManager.Models;

[Flags]
public enum Permission
{
    None = 0,
    ViewDashboard = 1 << 0,
    ViewPatients = 1 << 1,
    EditPatients = 1 << 2,
    ViewAppointments = 1 << 3,
    EditAppointments = 1 << 4,
    ViewMedicalRecords = 1 << 5,
    EditMedicalRecords = 1 << 6,
    ViewBilling = 1 << 7,
    EditBilling = 1 << 8,
    ViewStaff = 1 << 9,
    ManageUsers = 1 << 10,
    ViewInventory = 1 << 11,
    EditInventory = 1 << 12,
    ViewReports = 1 << 13,
    ManageSettings = 1 << 14,
}

public static class RolePermissions
{
    public static Permission GetPermissions(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => Permission.ViewDashboard | Permission.ViewPatients | Permission.EditPatients
                | Permission.ViewAppointments | Permission.EditAppointments | Permission.ViewMedicalRecords
                | Permission.EditMedicalRecords | Permission.ViewBilling | Permission.EditBilling
                | Permission.ViewStaff | Permission.ManageUsers | Permission.ViewInventory | Permission.EditInventory
                | Permission.ViewReports | Permission.ManageSettings,
            UserRole.Dentist => Permission.ViewDashboard | Permission.ViewPatients | Permission.EditPatients
                | Permission.ViewAppointments | Permission.EditAppointments | Permission.ViewMedicalRecords
                | Permission.EditMedicalRecords | Permission.ViewBilling | Permission.ViewStaff
                | Permission.ViewInventory | Permission.ViewReports,
            UserRole.Assistant => Permission.ViewDashboard | Permission.ViewPatients | Permission.ViewAppointments
                | Permission.ViewMedicalRecords | Permission.EditMedicalRecords | Permission.ViewBilling
                | Permission.ViewStaff | Permission.ViewInventory | Permission.ViewReports,
            UserRole.Reception => Permission.ViewDashboard | Permission.ViewPatients | Permission.ViewAppointments
                | Permission.EditAppointments | Permission.ViewMedicalRecords | Permission.ViewBilling
                | Permission.EditBilling | Permission.ViewStaff | Permission.ViewInventory | Permission.ViewReports,
            _ => Permission.ViewDashboard
        };
    }
}
