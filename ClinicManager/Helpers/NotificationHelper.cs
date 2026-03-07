using System;
using System.Windows;

namespace ClinicManager.Helpers;

/// <summary>
/// Centralized user-friendly notifications. All messages use clear, professional wording.
/// </summary>
public static class NotificationHelper
{
    public static void Info(string message, string title = "Information") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public static void Success(string message, string title = "Success") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public static void Warning(string message, string title = "Warning") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void Error(string message, string title = "Error") =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public static bool Confirm(string message, string title = "Confirm") =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    // Validation messages
    public static void SelectPatientRequired() =>
        Warning(Localization.Strings.SelectPatientRequired, Localization.Strings.Validation);

    public static void PatientNameRequired() =>
        Warning(Localization.Strings.PatientNameRequired, Localization.Strings.Validation);

    public static void ItemNotFound(string itemName = "Item") =>
        Warning($"{itemName} not found. It may have been deleted or does not exist.", Localization.Strings.NotFound);

    public static void SaveError(string operation, Exception ex) =>
        Error($"{operation} failed.\n\nDetails: {ex.Message}", Localization.Strings.Error);

    public static void DeleteError(string itemName, Exception ex) =>
        Error($"Could not delete {itemName}.\n\nDetails: {ex.Message}", Localization.Strings.Error);
}
