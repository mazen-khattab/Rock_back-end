using System.Text;
using Application.DTOs;
using Infrastructure.External.Email;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Xunit;

namespace Tests
{
    // QuestPDF settings are process-wide; isolate tests that deliberately disable all font discovery.
    [CollectionDefinition("Order PDF generation", DisableParallelization = true)]
    public sealed class OrderPdfCollection { }

    [Collection("Order PDF generation")]
    public sealed class OrderPdfTests
    {
        public OrderPdfTests()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = false;
            QuestPDF.Settings.FontDiscoveryPath = null;
            QuestPDF.Settings.ThrowOnMissingTextGlyphs = true;
        }

        [Theory]
        [InlineData("جاكيت اسود  -  S  -  احمر")]
        [InlineData("جَاكِيت قُطْن / Cotton Jacket XL")]
        public void ArabicAndMixedLanguageOrdersRenderWithOnlyEmbeddedFonts(string name)
        {
            var order = SampleOrder();
            order.Items.First().Name = name;
            byte[] pdf = new OrderPdfDocument(order).GeneratePdf();
            AssertValidPdf(pdf);
            Assert.False(QuestPDF.Settings.UseSystemFonts);
            Assert.Contains(FontManager.GetRegisteredFonts(), font => font.FamilyName == "Noto Sans Arabic");
        }

        [Fact]
        public void EnglishOnlyEmptyOrderCanStillBeRendered()
        {
            var order = new EmailInfoDto { OrderNumber = "ORD-EMPTY", FName = "Alex", LName = "Smith" };
            AssertValidPdf(new OrderPdfDocument(order).GeneratePdf());
        }

        [Fact]
        public void LongArabicAddressesAndLargeOrdersPaginateWithoutClippingFailures()
        {
            var order = SampleOrder();
            order.Address = string.Join(" ", Enumerable.Repeat("٢٣ شارع التحرير، الدور الثالث، شقة ١٢", 16));
            order.Notes = string.Join(" ", Enumerable.Repeat("يرجى الاتصال قبل الوصول. Delivery after 6 PM.", 12));
            order.Items = Enumerable.Range(1, 65).Select(i => new CartDto
            {
                Name = $"منتج رقم {i} / Cotton jacket with a longer product name",
                Color = "أزرق داكن", Size = "XL", Quantity = 2, Price = 150.25m
            });
            order.TotalPrice = 65 * 2 * 150.25m;
            var document = new OrderPdfDocument(order);
            AssertValidPdf(document.GeneratePdf());
            var pages = document.GenerateImages(new ImageGenerationSettings { RasterDpi = 36 }).ToList();
            Assert.True(pages.Count > 1);
            Assert.All(pages, page => Assert.NotEmpty(page));
        }

        [Fact]
        public void FontAssetsAndRedistributionLicenseTravelWithTheApplication()
        {
            var resources = typeof(OrderPdfDocument).Assembly.GetManifestResourceNames();
            Assert.Contains("Rock.OrderPdf.Fonts.NotoSansArabic-Regular.ttf", resources);
            Assert.Contains("Rock.OrderPdf.Fonts.NotoSansArabic-Bold.ttf", resources);
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "External", "Email", "Fonts", "OFL.txt")));
        }

        [Fact]
        public void GenerateDarkOrderPreviewForVisualReview()
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "Artifacts");
            Directory.CreateDirectory(directory);
            var document = new OrderPdfDocument(SampleOrder());
            byte[] pdf = document.GeneratePdf();
            AssertValidPdf(pdf);
            File.WriteAllBytes(Path.Combine(directory, "order-summary-dark.pdf"), pdf);
            var pages = document.GenerateImages(new ImageGenerationSettings { RasterDpi = 120 }).ToList();
            for (int i = 0; i < pages.Count; i++)
                File.WriteAllBytes(Path.Combine(directory, $"order-summary-dark-page-{i + 1}.png"), pages[i]);
            Assert.Single(pages);
        }

        private static void AssertValidPdf(byte[] pdf)
        {
            Assert.True(pdf.Length > 1000);
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
            Assert.Contains("%%EOF", Encoding.ASCII.GetString(pdf[^Math.Min(pdf.Length, 1024)..]));
        }

        private static EmailInfoDto SampleOrder() => new()
        {
            OrderNumber = "ORD-20261106-1024",
            FName = "أحمد",
            LName = "محمد",
            Email = "ahmed@example.com",
            Phone = "+20 100 123 4567",
            Governorate = "القاهرة",
            City = "مدينة نصر",
            Address = "٢٣ شارع التحرير، الدور الثالث، شقة ١٢",
            Notes = "برجاء الاتصال قبل التوصيل. Keep the package dry.",
            Items = new List<CartDto>
            {
                new() { Name = "جاكيت اسود", Size = "S", Color = "احمر", Price = 1250.50m, Quantity = 2 },
                new() { Name = "تي شيرت قطن / Cotton T-Shirt", Size = "L", Color = "كحلي", Price = 399m, Quantity = 1 }
            },
            TotalPrice = 2900m
        };
    }
}
