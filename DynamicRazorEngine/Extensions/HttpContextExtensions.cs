using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace DynamicRazorEngine.Extensions;

internal static class HttpContextExtensions
{
    internal static IFormatProvider? GetFormatProvider(this HttpContext context)
    {
        var rqf = context.Features.Get<IRequestCultureFeature>();
        return rqf?.RequestCulture.Culture;
    }
}
