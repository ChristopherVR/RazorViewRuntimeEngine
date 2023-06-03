using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RuntimeLoading.Services;
using System.Linq;
using RuntimeLoading.wwwroot.Reports.ReportNameHere.Models;
namespace RuntimeLoading.wwwroot.Reports.ReportNameHere;

public class ReportController : DynamicRazorEngine.Models.ReportControllerBase
{
    private const string CreateEditViewPath = "~/wwwroot/Reports/{0}/CreateEdit.cshtml";
    private readonly IExampleService _exampleService;

    public ReportController(IExampleService exampleService) 
        => _exampleService = exampleService ?? throw new ArgumentNullException(nameof(exampleService));

    [HttpGet]
    public ActionResult Index()
    {
        return View($"~/wwwroot/Reports/{ReportId}/Index.cshtml", ReportId);
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Create()
    {
        var vm = new CreateEditViewModel()
        {
            HalloWorld = await _exampleService.GetHelloWorldAsync()!,
        };

        return View(Url.Content(string.Format(CreateEditViewPath, ReportId)), vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Create(CreateEditViewModel vm)
    {
        vm.HalloWorld = await _exampleService.GetHelloWorldAsync()!;
        vm.Created = true;
        return View(Url.Content(string.Format(CreateEditViewPath, ReportId)), vm);
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Edit(int id)
    {
        var vm = new CreateEditViewModel(id);
        vm.HalloWorld = await _exampleService.GetHelloWorldAsync();
        return View(Url.Content(string.Format(CreateEditViewPath, ReportId)), vm);
    }

    [HttpPost]
    public IActionResult Search(CreateEditViewModel vm)
    {
        var users = Enumerable.Range(0, 100)
        .Select(y => new User
        {
            Id = y,
            Name = $"Unique name - {y}",
        })
        .Where(y => string.IsNullOrWhiteSpace(vm.Keyword) ? true : y.Name!.Contains(vm.Keyword!, StringComparison.OrdinalIgnoreCase))
        .ToList();
        vm.Users = users;
        return View(Url.Content(string.Format(CreateEditViewPath, ReportId)), vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Edit(CreateEditViewModel vm)
    {
        vm.HalloWorld = await _exampleService.GetHelloWorldAsync();
        vm.Updated = true;
        return View(Url.Content(string.Format(CreateEditViewPath, ReportId)), vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Delete(CreateEditViewModel vm, [FromServices] IExampleService exampleService)
    {
        vm.Deleted = true;
        vm.HalloWorld = await exampleService.GetHelloWorldAsync();
        return View(Url.Content(string.Format(CreateEditViewPath, ReportId)), vm);
    }
}
