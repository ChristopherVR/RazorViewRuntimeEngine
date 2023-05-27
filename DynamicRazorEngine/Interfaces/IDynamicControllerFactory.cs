using DynamicRazorEngine.Factories;
using Microsoft.AspNetCore.Mvc;

namespace DynamicRazorEngine.Interfaces;

internal interface IDynamicControllerFactory 
{
    Task<(IActionResult? Result, ActionContext? ActionContext)> ExecuteAsync(ControllerFactoryRequest request);
}
