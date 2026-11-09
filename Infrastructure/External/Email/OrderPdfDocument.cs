using System.Globalization;
using Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.External.Email
{
    public sealed class OrderPdfDocument : IDocument
    {
        private const string Background = "#0C1217";
        private const string Panel = "#152028";
        private const string AlternatePanel = "#111B22";
        private const string Border = "#2A3943";
        private const string Text = "#F2F6F8";
        private const string Muted = "#A6B6C0";
        private const string Accent = "#B8EEA6";

        private readonly EmailInfoDto _order;
        private readonly IReadOnlyList<CartDto> _items;

        public OrderPdfDocument(EmailInfoDto order)
        {
            ArgumentNullException.ThrowIfNull(order);
            OrderPdfFonts.EnsureRegistered();
            _order = order;
            // QuestPDF measures content more than once; snapshot an incoming enumerable only once.
            _items = order.Items?.ToList() ?? new List<CartDto>();
        }

        public DocumentMetadata GetMetadata() => new()
        {
            Title = $"Keef wear - Order {_order.OrderNumber}",
            Author = "Keef wear",
            Subject = "Customer order summary"
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.PageColor(Background);
                // Keep missing-glyph checks enabled: the embedded Arabic family supplies the fallback.
                page.DefaultTextStyle(style => style.FontFamily("Lato", OrderPdfFonts.ArabicFamily)
                    .FontSize(10).FontColor(Text));
                page.Header().Element(ComposeHeader);
                page.Content().PaddingTop(22).PaddingBottom(18).Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(14);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(brand =>
                    {
                        brand.Item().Text("KEEF WEAR").FontSize(32).ExtraBold().FontColor(Accent);
                        brand.Item().PaddingTop(2).Text("STORE / CUSTOMER ORDER").FontSize(8).FontColor(Muted);
                    });
                    row.ConstantItem(220).AlignRight().Column(reference =>
                    {
                        reference.Item().AlignRight().Text("ORDER SUMMARY").FontSize(9).FontColor(Muted);
                        reference.Item().PaddingTop(6).AlignRight().Text(Display(_order.OrderNumber))
                            .FontSize(15).Bold().DirectionAuto();
                    });
                });
                column.Item().LineHorizontal(1).LineColor(Accent);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(20);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Order details").FontSize(21).SemiBold();
                    row.AutoItem().AlignBottom().Text($"{_items.Count} LINE ITEMS").FontSize(8).FontColor(Muted);
                });

                column.Item().Row(row =>
                {
                    row.Spacing(14);
                    row.RelativeItem().Element(Card).Column(customer =>
                    {
                        customer.Spacing(10);
                        customer.Item().Text("CUSTOMER").FontSize(9).Bold().FontColor(Accent);
                        Field(customer.Item(), "Name", $"{_order.FName} {_order.LName}".Trim());
                        Field(customer.Item(), "Email", _order.Email);
                        Field(customer.Item(), "Phone", _order.Phone);
                    });
                    row.RelativeItem().Element(Card).Column(delivery =>
                    {
                        delivery.Spacing(10);
                        delivery.Item().Text("DELIVERY").FontSize(9).Bold().FontColor(Accent);
                        Field(delivery.Item(), "Governorate", _order.Governorate);
                        Field(delivery.Item(), "City", _order.City);
                        Field(delivery.Item(), "Address", _order.Address);
                    });
                });

                if (!string.IsNullOrWhiteSpace(_order.Notes))
                {
                    column.Item().Element(Card).Column(notes =>
                    {
                        notes.Spacing(8);
                        notes.Item().Text("DELIVERY NOTES").FontSize(9).Bold().FontColor(Accent);
                        Value(notes.Item(), _order.Notes);
                    });
                }

                column.Item().Text("Order items").FontSize(16).SemiBold();
                column.Item().Element(ComposeItems);

                column.Item().EnsureSpace(90).AlignRight().Width(240)
                    .Background(Accent).CornerRadius(8).Padding(18).Column(total =>
                    {
                        total.Spacing(5);
                        total.Item().Text("ORDER TOTAL").FontSize(9).Bold().FontColor(Background);
                        // The recorded checkout total remains authoritative, including any discounts.
                        total.Item().Text(Amount(_order.TotalPrice)).FontSize(26).Bold().FontColor(Background);
                    });
            });
        }

        private void ComposeItems(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(82);
                    columns.ConstantColumn(90);
                });

                // Repeat column headings on every page of a large order.
                table.Header(header =>
                {
                    header.Cell().Element(TableHeading).Text("ITEM");
                    header.Cell().Element(TableHeading).AlignCenter().Text("QTY");
                    header.Cell().Element(TableHeading).AlignRight().Text("UNIT PRICE");
                    header.Cell().Element(TableHeading).AlignRight().Text("LINE TOTAL");
                });

                for (int index = 0; index < _items.Count; index++)
                {
                    var item = _items[index];
                    string rowColor = index % 2 == 0 ? Panel : AlternatePanel;
                    table.Cell().Element(cell => TableCell(cell, rowColor)).Column(description =>
                    {
                        Value(description.Item(), item.Name, 11, bold: true);
                        description.Item().PaddingTop(5).Text(text =>
                        {
                            text.DefaultTextStyle(style => style.FontSize(9).FontColor(Muted));
                            text.Span("Size: ");
                            text.Span(Display(item.Size)).DirectionAuto();
                            text.Span("   /   Color: ");
                            text.Span(Display(item.Color)).DirectionAuto();
                        });
                    });
                    table.Cell().Element(cell => TableCell(cell, rowColor)).AlignCenter()
                        .Text(item.Quantity.ToString(CultureInfo.InvariantCulture));
                    table.Cell().Element(cell => TableCell(cell, rowColor)).AlignRight().Text(Amount(item.Price));
                    table.Cell().Element(cell => TableCell(cell, rowColor)).AlignRight()
                        .Text(Amount(item.Price * item.Quantity)).Bold();
                }

                if (_items.Count == 0)
                    table.Cell().ColumnSpan(4).Element(cell => TableCell(cell, Panel)).Text("No items were provided.").FontColor(Muted);
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.BorderTop(1).BorderColor(Border).PaddingTop(12).Row(row =>
            {
                row.RelativeItem().Text("Thank you for shopping with KEEF WEAR.").FontSize(9).FontColor(Muted);
                row.AutoItem().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(9).FontColor(Muted));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }

        private static IContainer Card(IContainer container) =>
            container.Background(Panel).CornerRadius(8).Padding(16);

        private static IContainer TableHeading(IContainer container) =>
            container.Background(Border).PaddingVertical(10).PaddingHorizontal(10)
                .DefaultTextStyle(style => style.FontSize(8).Bold().FontColor(Muted));

        private static IContainer TableCell(IContainer container, string color) =>
            container.Background(color).BorderBottom(1).BorderColor(Border).Padding(10);

        private static void Field(IContainer container, string label, string? value)
        {
            container.Column(column =>
            {
                column.Spacing(3);
                column.Item().Text(label.ToUpperInvariant()).FontSize(8).FontColor(Muted);
                Value(column.Item(), value);
            });
        }

        private static void Value(IContainer container, string? value, float size = 10, bool bold = false)
        {
            string content = Display(value);
            // Shape Arabic naturally and align Arabic values to the right without reversing strings.
            bool arabic = content.Any(c => c is >= '\u0600' and <= '\u06FF'
                or >= '\u0750' and <= '\u077F' or >= '\u08A0' and <= '\u08FF');
            var text = (arabic ? container.ContentFromRightToLeft() : container.ContentFromLeftToRight())
                .Text(content).FontSize(size).DirectionAuto();
            if (bold)
                text.Bold();
        }

        private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
        private static string Amount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
    }
}
