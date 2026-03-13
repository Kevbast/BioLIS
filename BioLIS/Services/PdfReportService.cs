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
                });

                // Tabla de resultados
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Examen
                        columns.RelativeColumn(2); // Resultado
                        columns.RelativeColumn(1); // Unidades
                        columns.RelativeColumn(2); // Estado
                        columns.RelativeColumn(3); // Observaciones
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Examen").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Resultado").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Unidades").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Estado").SemiBold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Observaciones").SemiBold();
                    });

                    // Items
                    foreach (var result in results)
                    {
                        var resultColor = Colors.Black;
                        var estado = BioLab.Models.AlertLevels.GetDisplayName(result.AlertLevel);
                        
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
                             .Text(result.ResultValue.HasValue ? estado : "-")
                             .FontColor(resultColor);

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(5)
                             .Text(result.Notes ?? "-");
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
    }
}