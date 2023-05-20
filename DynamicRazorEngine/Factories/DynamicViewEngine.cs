using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Encodings.Web;

namespace DynamicRazorEngine.Factories;

internal sealed class DynamicViewEngine : RazorViewEngine
{
    private readonly RazorViewEngineOptions _options;

    public DynamicViewEngine(
        IRazorPageFactoryProvider pageFactory,
        IRazorPageActivator pageActivator,
        HtmlEncoder htmlEncoder,
        IOptions<RazorViewEngineOptions> optionsAccessor,
        ILoggerFactory loggerFactory,
        DiagnosticListener diagnosticListener) : base(
        pageFactory,
        pageActivator,
        htmlEncoder,
        optionsAccessor,
        loggerFactory,
        diagnosticListener)
    {
        _options = optionsAccessor.Value;
    }

    //  ~/Areas/{2}/Views/{1}/{0}.cshtml",
    public void SetCustomLocation(string path) => _options.ViewLocationFormats.Add(path);
}
