<p align="center"> 
  <img src="https://github.com/devicons/devicon/blob/master/icons/dotnetcore/dotnetcore-original.svg" alt="NET Logo" width="80px" height="80px">
</p>
<h1 align="center"> Dynamic Razor Page Report Engine </h1>
<h3 align="center"> This is a demonstration on how to compile .NET code in runtime alongside Razor Views. This can be useful in cases where you need to generate a report on a live environment without needing to recompile the project.</h3>  
</br>
<!-- TABLE OF CONTENTS -->
<h2 id="table-of-contents"> :book: Table of Contents</h2>
<details open="open">
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#about-the-project"> ➤ About The Project</a></li>
    <li><a href="#prerequisites"> ➤ Prerequisites</a></li>
    <li><a href="#setup"> ➤ Setup</a></li>
    <li><a href="#examples"> ➤ Examples</a></li>
    <li><a href="#config"> ➤ Configuration</a></li>
    <li><a href="#issues"> ➤ Known Issues</a></li>
  </ol>
</details>

![-----------------------------------------------------](https://github.com/ChristopherVR/ChristopherVR/blob/main/rainbow.png)

<!-- ABOUT THE PROJECT -->
<h2 id="about-the-project"> :pencil: About The Project</h2>
<p align="justify"> 
  This project aims to provide a proof of concept on how you can incorporate runtime compilation for Razor Views with its own set of Partials, Controllers, and ViewModels.
</p>

![-----------------------------------------------------](https://github.com/ChristopherVR/ChristopherVR/blob/main/rainbow.png)

<!-- PREREQUISITES -->
<h2 id="prerequisites"> :fork_and_knife: Prerequisites</h2>

[![Made with-dot-net](https://img.shields.io/badge/-Made%20with%20.NET-purple)](https://dotnet.microsoft.com/en-us/) <br>
[![build status][buildstatus-image]][buildstatus-url]

[buildstatus-image]: https://github.com/ChristopherVR/DynamicExecutor/blob/main/.github/workflows/badge.svg
[buildstatus-url]: https://github.com/ChristopherVR/DynamicExecutor/actions

<!--This project is written mainly in C# and JavaScript programming languages. <br>-->
The following open source packages are used in this project:
* <a href="https://github.com/dotnet/aspnetcore"> .NET 7</a> 
 
![-----------------------------------------------------](https://github.com/ChristopherVR/ChristopherVR/blob/main/rainbow.png)


<h2 id="setup"> :computer: Setup</h2>

<p align="justify"> 
A: Add razor runtime compilation to your project

```
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
```

B: Add the Dynamic Reporting Services & Reporting endpoints
```
builder.Services.AddDynamicReportingServices();
var app = builder.Build();
app.UseDynamicReporting(); 
```
C: See the Examples section on how this is used.

</p>

![-----------------------------------------------------](https://github.com/ChristopherVR/ChristopherVR/blob/main/rainbow.png)


<!-- ROADMAP -->
<h2 id="examples"> :dart: Examples</h2>

<p align="justify"> 

Clone this repository and run the Example project.
Open your browser and navigate to this path: `https://localhost:44376/reports/1`

</p>

![-----------------------------------------------------](https://github.com/ChristopherVR/ChristopherVR/blob/main/rainbow.png)


<!-- ROADMAP -->
<h2 id="config"> :dart: Configuration</h2>

<p align="justify"> 
The `UseDynamicReporting` method accepts an overload to include a set of ReportConfig defaults. This is also used by the CompliationService to determine if the Report should be loaded from an existing generated assembly.
  
The ReportingConfig class allows customisation for the following:

* **BasePath**: Indicates the base/root path where the reports are stored. Default is `wwwroot\Repors`.
* **DefaultRuntimeCache**: Indicates the default cache duration before expiring an existing generated assembly for a given report. Default is `6 Hours`.
* **RoutePattern**: Indicates the route pattern used to handle dynamic reports' logic. Example `/dynamic/reports/{reportId:int}`. Default is `/reports/{reportId:int}/{action?}/{controller?}`
* **HttpMethodsm**: Indicates the HttpMethods that the dynamic report middleware will allow. Example `['GET', 'POST']`. Default is `['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'DELETE']`

Register the `ReportingConfig` class using the Options pattern to configure this.
</p>

![-----------------------------------------------------](https://github.com/ChristopherVR/ChristopherVR/blob/main/rainbow.png)

!-- ROADMAP -->
<h2 id="issues"> ⁉️: Known Issues</h2>

<p align="justify"> 
The engine doesn't support strongly typed models in dynamic Razor Pages at the moment. As a workaround use the `dynamic` keyword for models in Razor Pages.
</p>

![-----------------------------------------------------](https://github.com/ChristopherVR/ChristopherVR/blob/main/rainbow.png)

