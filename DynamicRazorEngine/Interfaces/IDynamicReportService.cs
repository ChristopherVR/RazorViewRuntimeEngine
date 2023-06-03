using DynamicRazorEngine.Factories;
using Microsoft.AspNetCore.Mvc;

namespace DynamicRazorEngine.Interfaces;

internal interface IDynamicReportService 
{
    internal Task<(IActionResult? Result, ActionContext? ActionContext)> ExecuteAsync(ControllerFactoryRequest request);
}
