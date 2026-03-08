using System;
using System.Globalization;

namespace ClinicManager.Helpers;

/// <summary>
/// Morocco-specific formatting: MAD currency, DD/MM/YYYY dates, TVA.
/// </summary>
public static class MoroccoFormatting
{
    public static readonly CultureInfo MoroccoCulture = new("fr-MA");

    /// <summary>Format amount as MAD (e.g. "1 234,56 MAD").</summary>
    public static string FormatMAD(decimal amount) =>
        amount.ToString("N2", MoroccoCulture) + " MAD";

    /// <summary>Format date as DD/MM/YYYY (Morocco standard).</summary>
    public static string FormatDate(DateTime date) =>
        date.ToString("dd/MM/yyyy", MoroccoCulture);

    /// <summary>Format date with time.</summary>
    public static string FormatDateTime(DateTime date) =>
        date.ToString("dd/MM/yyyy HH:mm", MoroccoCulture);

    /// <summary>Moroccan TVA rate (20% default).</summary>
    public const decimal DefaultVATRate = 20m;

    /// <summary>Calculate TVA amount from gross.</summary>
    public static decimal CalculateVAT(decimal amount, decimal ratePercent = DefaultVATRate) =>
        Math.Round(amount * ratePercent / 100, 2);

    /// <summary>Calculate patient amount: TreatmentCost - CNSSCovered - Discount.</summary>
    public static decimal CalculatePatientAmount(decimal treatmentCost, decimal cnssCovered, decimal discount) =>
        Math.Max(0, treatmentCost - cnssCovered - discount);
}
