using BioLab.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BioLIS.Services
{
    public class PdfReportService
    {
        public PdfReportService()
        {
            // Licencia Community para QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateResultsPdf(Order order, List<TestResult> results)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(header => ComposeHeader(header, order));
                    page.Content().Element(content => ComposeContent(content, order, results));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, Order order)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("BioLIS Laboratory").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text("Resultados de Análisis Clínicos").FontSize(14).FontColor(Colors.Grey.Medium);
                    });

                    row.ConstantItem(150).AlignRight().Column(col =>
                    {
                        col.Item().Text($"Orden: {order.OrderNumber}").Bold();
                        col.Item().Text($"Fecha: {order.OrderDate:dd/MM/yyyy HH:mm}");
                    });
                });

                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });
        }

        private void ComposeContent(IContainer container, Order order, List<TestResult> results)
        {
            container.PaddingVertical(10).Column(column =>
            {
                // Información del paciente y doctor
                column.Item().PaddingBottom(15).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Paciente:").SemiBold();
                        col.Item().Text($"{order.Patient.FirstName} {order.Patient.LastName}");
                        var age = DateTime.Now.Year - order.Patient.BirthDate.Year;
                        col.Item().Text($"Edad: {age} años | Sexo: {order.Patient.Gender}");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Médico Solicitante:").SemiBold();
                        col.Item().Text(order.Doctor.FullName);
                        col.Item().Text($"Licencia: {order.Doctor.LicenseNumber ?? "S/N"}");
                    });

                    // 3ra columna: código de barras pequeño
                    row.ConstantItem(145).AlignRight().Column(col =>
                    {
                        col.Item().AlignRight().Text("Código").FontSize(7).FontColor(Colors.Grey.Medium);
                        col.Item()
                            .PaddingTop(2)
                            .AlignRight()
                            .Width(4.6f, Unit.Centimetre)
                            .Height(1.2f, Unit.Centimetre)
                            .BarcodeCode128(order.OrderNumber);
                    });
                });

                // Tabla de resultados
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Examen
                        columns.RelativeColumn(2); // Resultado
                        columns.RelativeColumn(1); // Unidades
                        columns.RelativeColumn(2); // Rango
                        columns.RelativeColumn(2); // Estado
                        columns.RelativeColumn(3); // Observaciones
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Examen").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Resultado").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Unidades").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Rango Ref.").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Estado").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Observaciones").SemiBold();
                    });

                    // Items
                    foreach (var result in results)
                    {
                        var resultColor = Colors.Black;
                        var estado = BioLab.Models.AlertLevels.GetDisplayName(result.AlertLevel);
                        var referenceRange = ResolveReferenceRange(result, order.Patient);
                        var referenceRangeText = referenceRange != null
                            ? $"{referenceRange.MinVal:0.##}-{referenceRange.MaxVal:0.##}"
                            : "Sin rango";
                        
                        if (result.AlertLevel == BioLab.Models.AlertLevels.Anormal)
                            resultColor = Colors.Orange.Medium;
                        else if (result.AlertLevel == BioLab.Models.AlertLevels.Critico)
                            resultColor = Colors.Red.Medium;

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5)
                             .Text(result.LabTest.TestName);
                             
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5)
                             .Text(result.ResultValue.HasValue ? result.ResultValue.Value.ToString("0.##") : "Pendiente")
                             .FontColor(resultColor).SemiBold();

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5)
                             .Text(result.LabTest.Units ?? "");

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5)
                             .Text(referenceRangeText)
                             .FontColor(referenceRange == null ? Colors.Grey.Medium : Colors.Black);

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5)
                             .Text(result.ResultValue.HasValue ? estado : "-")
                             .FontColor(resultColor);

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5)
                             .Text(result.Notes ?? "-");
                    }
                });

                // Resumen rápido
                var total = results.Count;
                var completed = results.Count(x => x.ResultValue.HasValue);
                var abnormal = results.Count(x => x.AlertLevel == AlertLevels.Anormal || x.AlertLevel == AlertLevels.Critico);

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Total exámenes: {total}").SemiBold();
                    row.RelativeItem().Text($"Completados: {completed}").SemiBold();
                    row.RelativeItem().Text($"Anormales/Críticos: {abnormal}").SemiBold().FontColor(abnormal > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                });

                var enteredByNames = results
                    .Where(x => x.EnteredByUser != null)
                    .Select(x => x.EnteredByUser!.Username)
                    .Distinct()
                    .ToList();

                var modifiedByNames = results
                    .Where(x => x.ModifiedByUser != null)
                    .Select(x => x.ModifiedByUser!.Username)
                    .Distinct()
                    .ToList();

                column.Item().PaddingTop(8).Column(auditColumn =>
                {
                    auditColumn.Item().Text($"Ingresado por: {(enteredByNames.Any() ? string.Join(", ", enteredByNames) : "N/D")}").FontSize(9);
                    auditColumn.Item().Text($"Modificado por: {(modifiedByNames.Any() ? string.Join(", ", modifiedByNames) : "N/D")}").FontSize(9);
                    if (order.Status == "Aprobada")
                    {
                        var approverName = order.ApprovedByUser?.DisplayName ?? "N/D";
                        auditColumn.Item().Text($"Aprobada por: {approverName}").FontSize(9).SemiBold();
                    }
                });

            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                column.Item().PaddingTop(5).Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
                column.Item().Text("Este reporte es generado de forma automática y los resultados son confidenciales.").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }

        private static ReferenceRange? ResolveReferenceRange(TestResult result, Patient patient)
        {
            var age = DateTime.Today.Year - patient.BirthDate.Year;
            if (patient.BirthDate.Date > DateTime.Today.AddYears(-age))
                age--;

            return result.LabTest.ReferenceRanges?
                .Where(rr => (rr.Gender == patient.Gender || rr.Gender == "A")
                             && age >= rr.MinAgeYear
                             && age <= rr.MaxAgeYear)
                .OrderBy(rr => rr.Gender == patient.Gender ? 0 : 1)
                .ThenBy(rr => rr.MaxAgeYear - rr.MinAgeYear)
                .FirstOrDefault();
        }

    }
}