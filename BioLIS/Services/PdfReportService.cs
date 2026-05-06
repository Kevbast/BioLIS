using BioLIS.Models;
using BioLIS.Models.DTOs.Portal;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BioLIS.Services
{
    public class PdfReportService
    {
        public PdfReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateResultsPdf(PortalOrderDto order, List<PortalResultDto> results)
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

        private void ComposeHeader(IContainer container, PortalOrderDto order)
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

        private void ComposeContent(IContainer container, PortalOrderDto order, List<PortalResultDto> results)
        {
            container.PaddingVertical(10).Column(column =>
            {
                column.Item().PaddingBottom(15).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Paciente:").SemiBold();
                        col.Item().Text(order.PatientName);
                        col.Item().Text($"Edad: {order.PatientAge} años | Sexo: {order.PatientGender}");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Médico Solicitante:").SemiBold();
                        col.Item().Text(order.DoctorName);
                        col.Item().Text($"Licencia: {order.DoctorLicense ?? "S/N"}");
                    });

                    row.ConstantItem(145).AlignRight().Column(col =>
                    {
                        col.Item().AlignRight().Text("Código").FontSize(7).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(2).AlignRight().Width(4.6f, Unit.Centimetre).Height(1.2f, Unit.Centimetre).BarcodeCode128(order.OrderNumber);
                    });
                });

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Examen").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Resultado").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Unidades").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Rango Ref.").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Estado").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Observaciones").SemiBold();
                    });

                    foreach (var result in results)
                    {
                        var resultColor = Colors.Black;
                        var estado = result.AlertLevel ?? "-";

                        if (estado.Contains("Anormal", StringComparison.OrdinalIgnoreCase)) resultColor = Colors.Orange.Medium;
                        else if (estado.Contains("Critico", StringComparison.OrdinalIgnoreCase) || estado.Contains("Crítico", StringComparison.OrdinalIgnoreCase)) resultColor = Colors.Red.Medium;

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5).Text(result.TestName);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5).Text(result.ResultValue.HasValue ? result.ResultValue.Value.ToString("0.##") : "Pendiente").FontColor(resultColor).SemiBold();
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5).Text(result.Units ?? "");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5).Text(result.ReferenceRangeText).FontColor(result.ReferenceRangeText == "Sin rango" ? Colors.Grey.Medium : Colors.Black);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5).Text(result.ResultValue.HasValue ? estado : "-").FontColor(resultColor);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5).Text(result.Notes ?? "-");
                    }
                });

                var total = results.Count;
                var completed = results.Count(x => x.ResultValue.HasValue);
                var abnormal = results.Count(x => x.AlertLevel == "Anormal" || x.AlertLevel == "Crítico" || x.AlertLevel == "Critico");

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Total exámenes: {total}").SemiBold();
                    row.RelativeItem().Text($"Completados: {completed}").SemiBold();
                    row.RelativeItem().Text($"Anormales/Críticos: {abnormal}").SemiBold().FontColor(abnormal > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                });

                var enteredByNames = results.Where(x => !string.IsNullOrEmpty(x.EnteredByName)).Select(x => x.EnteredByName).Distinct().ToList();
                var modifiedByNames = results.Where(x => !string.IsNullOrEmpty(x.ModifiedByName)).Select(x => x.ModifiedByName).Distinct().ToList();
                var firstEntryDate = results.Where(x => x.EnteredDate.HasValue).Select(x => x.EnteredDate).OrderBy(d => d).FirstOrDefault();
                var lastModifiedDate = results.Where(x => x.ModifiedDate.HasValue).Select(x => x.ModifiedDate).OrderByDescending(d => d).FirstOrDefault();

                column.Item().PaddingTop(8).Column(auditColumn =>
                {
                    auditColumn.Item().Text($"Ingresado por: {(enteredByNames.Any() ? string.Join(", ", enteredByNames) : "N/D")}").FontSize(9);
                    auditColumn.Item().Text($"Fecha ingreso: {(firstEntryDate.HasValue ? firstEntryDate.Value.ToString("dd/MM/yyyy HH:mm") : "N/D")}").FontSize(9);

                    if (modifiedByNames.Any() || lastModifiedDate.HasValue)
                    {
                        auditColumn.Item().Text($"Modificado por: {string.Join(", ", modifiedByNames)}").FontSize(9);
                        auditColumn.Item().Text($"Última modificación: {(lastModifiedDate.HasValue ? lastModifiedDate.Value.ToString("dd/MM/yyyy HH:mm") : "N/D")}").FontSize(9);
                    }

                    if (order.Status == "Aprobada")
                    {
                        auditColumn.Item().Text($"Aprobada por: {order.ApproverName ?? "N/D"}").FontSize(9).SemiBold();
                    }
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                column.Item().PaddingTop(5).Text(x => { x.Span("Página "); x.CurrentPageNumber(); x.Span(" de "); x.TotalPages(); });
                column.Item().Text("Este reporte es generado de forma automática y los resultados son confidenciales.").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }
    }
}