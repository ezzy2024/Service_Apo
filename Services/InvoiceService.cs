using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Services
{
    public class InvoiceService
    {
        public byte[] GenerateInvoice(Pharmacist pharmacist, Pharmacy pharmacy, JobPost jobPost)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                    page.Header().Element(compose => 
                    {
                        compose.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("RECHNUNG").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                                col.Item().Text($"Rechnungsnummer: RE-{jobPost.Id}-{DateTime.UtcNow:yyyyMMdd}");
                                col.Item().Text($"Datum: {DateTime.UtcNow:dd.MM.yyyy}");
                            });
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Item().Text("Leistungserbringer (Vertretung):").SemiBold();
                        col.Item().Text(pharmacist.FullName);
                        col.Item().Text(pharmacist.Email);
                        
                        col.Item().PaddingTop(10).Text("Rechnungsempfänger (Apotheke):").SemiBold();
                        col.Item().Text(pharmacy.PharmacyName);
                        col.Item().Text(pharmacy.Address);

                        col.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Beschreibung").SemiBold();
                                header.Cell().AlignRight().Text("Menge").SemiBold();
                                header.Cell().AlignRight().Text("Gesamt (€)").SemiBold();
                            });

                            table.Cell().PaddingTop(5).Text($"Apotheker-Vertretung ({jobPost.StartDate})");
                            table.Cell().PaddingTop(5).AlignRight().Text("1 Einsatz");
                            table.Cell().PaddingTop(5).AlignRight().Text(jobPost.Salary.ToString("F2"));
                        });

                        col.Item().PaddingTop(20).AlignRight().Text($"Endbetrag: {jobPost.Salary:F2} €").FontSize(14).SemiBold();
                        
                        col.Item().PaddingTop(30).Text("Gemäß § 19 UStG wird keine Umsatzsteuer berechnet. (Hinweis: Anpassen, falls USt-pflichtig)").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Seite ");
                        x.CurrentPageNumber();
                        x.Span(" von ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
    }
}