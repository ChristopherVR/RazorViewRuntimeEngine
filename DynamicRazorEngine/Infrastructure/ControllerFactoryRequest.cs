using Microsoft.AspNetCore.Mvc;

namespace DynamicRazorEngine.Factories;

internal sealed record ControllerFactoryRequest(long ReportId, string Action, string? Controller, ControllerContext? ControllerContext);
