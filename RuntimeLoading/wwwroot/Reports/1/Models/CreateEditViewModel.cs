using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// Namespace can be anything. Preferably unique to prevent issues with other reports containing the same model & namespace.
namespace RuntimeLoading.wwwroot.Reports.ReportNameHere.Models;

// Model can be called anything. This whole class is just to demonstrate that the controller will work like a normal MVC project.
public class CreateEditViewModel
{
    public string HalloWorld { get; init; }
    public long ReportId { get; set; }
    public bool Created { get; set; }
    public bool Deleted { get; set; }
    public bool Updated { get; set; }

    public CreateEditViewModel(int id) => ReportId = id;
}
