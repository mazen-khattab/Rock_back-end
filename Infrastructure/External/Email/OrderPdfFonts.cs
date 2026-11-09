using QuestPDF.Drawing;

namespace Infrastructure.External.Email
{
    internal static class OrderPdfFonts
    {
        internal const string ArabicFamily = "Noto Sans Arabic";
        private static readonly Lazy<bool> Registered = new(RegisterFonts);

        internal static void EnsureRegistered() => _ = Registered.Value;

        private static bool RegisterFonts()
        {
            // Lazy initialization registers fonts once, including when background jobs run concurrently.
            var assembly = typeof(OrderPdfFonts).Assembly;
            FontManager.RegisterFontFromEmbeddedResource(assembly, "Rock.OrderPdf.Fonts.NotoSansArabic-Regular.ttf");
            FontManager.RegisterFontFromEmbeddedResource(assembly, "Rock.OrderPdf.Fonts.NotoSansArabic-Bold.ttf");
            return true;
        }
    }
}
