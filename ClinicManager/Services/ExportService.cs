using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class ExportService
{
    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task ExportPatientsToExcelAsync(List<Patient> patients, string filePath)
    {
        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Patients");

            ws.Cell(1, 1).Value = "ID";
            ws.Cell(1, 2).Value = "Name";
            ws.Cell(1, 3).Value = "Phone";
            ws.Cell(1, 4).Value = "Date of Birth";
            ws.Cell(1, 5).Value = "Gender";
            ws.Cell(1, 6).Value = "Email";
            ws.Cell(1, 7).Value = "Address";
            ws.Cell(1, 8).Value = "Notes";

            var headerRange = ws.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            headerRange.Style.Font.FontColor = XLColor.White;

            for (int i = 0; i < patients.Count; i++)
            {
                var p = patients[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = p.Id;
                ws.Cell(row, 2).Value = p.FullName;
                ws.Cell(row, 3).Value = p.Phone;
                ws.Cell(row, 4).Value = p.DateOfBirth?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row, 5).Value = p.Gender;
                ws.Cell(row, 6).Value = p.Email;
                ws.Cell(row, 7).Value = p.Address;
                ws.Cell(row, 8).Value = p.Notes;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        });
    }

    public async Task ExportPatientsToPdfAsync(List<Patient> patients, string filePath, string clinicName)
    {
        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(clinicName).FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text("Patient List").FontSize(14).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(40);  // ID
                            cols.RelativeColumn(3);   // Name
                            cols.RelativeColumn(2);   // Phone
                            cols.RelativeColumn(2);   // DOB
                            cols.RelativeColumn(1);   // Gender
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                  .Text("ID").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                  .Text("Name").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                  .Text("Phone").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                  .Text("Date of Birth").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                  .Text("Gender").FontColor(Colors.White).Bold();
                        });

                        foreach (var p in patients)
                        {
                            var bg = patients.IndexOf(p) % 2 == 0
                                ? Colors.White : Colors.Grey.Lighten4;

                            table.Cell().Background(bg).Padding(5).Text(p.Id.ToString());
                            table.Cell().Background(bg).Padding(5).Text(p.FullName);
                            table.Cell().Background(bg).Padding(5).Text(p.Phone);
                            table.Cell().Background(bg).Padding(5).Text(p.DateOfBirth?.ToString("yyyy-MM-dd") ?? "");
                            table.Cell().Background(bg).Padding(5).Text(p.Gender);
                        }
                    });

                    page.Footer().AlignCenter()
                        .Text(t =>
                        {
                            t.Span("Page ");
                            t.CurrentPageNumber();
                            t.Span(" of ");
                            t.TotalPages();
                        });
                });
            }).GeneratePdf(filePath);
        });
    }

    public async Task ExportPaymentsToExcelAsync(List<Payment> payments, string filePath)
    {
        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Payments");

            ws.Cell(1, 1).Value = "Invoice";
            ws.Cell(1, 2).Value = "Patient";
            ws.Cell(1, 3).Value = "Amount";
            ws.Cell(1, 4).Value = "Date";
            ws.Cell(1, 5).Value = "Method";
            ws.Cell(1, 6).Value = "Status";
            ws.Cell(1, 7).Value = "Description";

            var headerRange = ws.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
            headerRange.Style.Font.FontColor = XLColor.White;

            for (int i = 0; i < payments.Count; i++)
            {
                var p = payments[i];
                var row = i + 2;
                ws.Cell(row, 1).Value = p.InvoiceNumber;
                ws.Cell(row, 2).Value = p.Patient?.FullName ?? "";
                ws.Cell(row, 3).Value = p.Amount;
                ws.Cell(row, 4).Value = p.Date.ToString("yyyy-MM-dd");
                ws.Cell(row, 5).Value = p.Method.ToString();
                ws.Cell(row, 6).Value = p.Status.ToString();
                ws.Cell(row, 7).Value = p.Description;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        });
    }
}
