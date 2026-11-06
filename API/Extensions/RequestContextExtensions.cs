using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace API.Extensions
{
    public static class RequestContextExtensions
    {
        public static string GetBaseUrl(this HttpRequest request) =>
            $"{request.Scheme}://{request.Host}{request.PathBase}";

        public static string GetLanguageCode(this HttpRequest request) =>
            (request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture
                ?? CultureInfo.CurrentCulture).TwoLetterISOLanguageName;
    }
}
