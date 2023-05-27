using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RuntimeLoading.Services;
using System.Linq;
using RuntimeLoading.wwwroot.Reports.ReportNameHere.Models;
// Namespace can be anything. Preferably unique to prevent issues with other reports containing the same controller & namespace.
namespace RuntimeLoading.wwwroot.Reports.ReportNameHere;

public class User { public int Id {get; set;} public string Name {get; set; }}

// Controller can be called anything. This whole class is just to demonstrate that the controller will work like a normal MVC project.
public class ReportController : Controller
{
    private const string CreateEditViewPath = "~/wwwroot/Reports/1/CreateEdit.cshtml";
    private readonly IExampleService _exampleService;

    public ReportController(IExampleService exampleService) 
        => _exampleService = exampleService ?? throw new ArgumentNullException(nameof(exampleService));

    [HttpGet]
    public ActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Create()
    {
        var vm = new CreateEditViewModel()
        {
            HalloWorld = await _exampleService.GetHelloWorldAsync(),
        };

        return View(Url.Content(CreateEditViewPath), new CreateEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Create(CreateEditViewModel vm)
    {
        vm.HalloWorld = await _exampleService.GetHelloWorldAsync();
        vm.Created = true; // Demonstrating changes.
        return View(Url.Content(CreateEditViewPath), vm);
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Edit(int id)
    {
        var vm = new CreateEditViewModel(id);
        vm.HalloWorld = await _exampleService.GetHelloWorldAsync();
        return View(Url.Content(CreateEditViewPath), vm);
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
        .Where(y => string.IsNullOrWhiteSpace(vm.Keyword) ? true : y.Name.Contains(vm.Keyword, StringComparison.OrdinalIgnoreCase))
        .ToList();
        vm.Users = users;
        return View(Url.Content(CreateEditViewPath), vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Edit(CreateEditViewModel vm)
    {
        vm.HalloWorld = await _exampleService.GetHelloWorldAsync();
        vm.Updated = true; // Demonstrating changes.
        return View(Url.Content(CreateEditViewPath), vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Delete(CreateEditViewModel vm)
    {
        vm.Deleted = true;
        vm.HalloWorld = await _exampleService.GetHelloWorldAsync();
        return View(Url.Content(CreateEditViewPath), vm);
    }
}
