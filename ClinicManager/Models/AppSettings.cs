namespace ClinicManager.Models;

public class AppSettings
{
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "Light";
    public string ClinicName { get; set; } = "My Clinic";
    public string ClinicAddress { get; set; } = string.Empty;
    public string ClinicPhone { get; set; } = string.Empty;
    public string DefaultDoctor { get; set; } = string.Empty;
    public int DefaultAppointmentDuration { get; set; } = 30;
    public bool AutoBackup { get; set; } = true;
    public string BackupPath { get; set; } = string.Empty;
    public int BackupIntervalHours { get; set; } = 24;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
}
