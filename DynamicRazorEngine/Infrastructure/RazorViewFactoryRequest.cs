using Microsoft.AspNetCore.Mvc;

namespace DynamicRazorEngine.Factories;

internal sealed record RazorViewFactoryRequest(long ReportId, ControllerContext? ControllerContext);
