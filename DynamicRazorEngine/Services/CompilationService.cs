using DynamicRazorEngine.Extensions;
using DynamicRazorEngine.Factories;
using DynamicRazorEngine.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Options;
using System.Dynamic;
using System.Reflection;

namespace DynamicRazorEngine.Services;

internal sealed class CompilationService
{
    private readonly ApplicationPartManager _partManager;
    private readonly CSharpCompilationOptions _compilationOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ReportingConfig _reportingConfig;

    public CompilationService(ApplicationPartManager partManager, IHttpContextAccessor httpContextAccessor, IOptions<ReportingConfig>? reportingConfigOptions)
    {
        _partManager = partManager ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithAllowUnsafe(true)
            .WithNullableContextOptions(NullableContextOptions.Enable)
            .WithOptimizationLevel(OptimizationLevel.Release);
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        _reportingConfig = reportingConfigOptions?.Value ?? DefaultReportConfiguration.Default();
    }

    /// <summary>
    /// Compiles a report's Source code into a <see cref="Assembly"/> object.
    /// </summary>
    /// <param name="report"></param>
    /// <param name="config"></param>
    /// <returns>Returns a <see cref="CompilationResult"/></returns>
    public async Task<CompilationResult> CompileAsync(Report report)
    {
        var path = _reportingConfig.BasePath + report.RelativePath;

        var assemblyPath = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories).FirstOrDefault();
        var cachedAssembly = LoadCachedAssembly(assemblyPath, report.CacheDuration ?? _reportingConfig.DefaultRuntimeCache, report.MainController);

        if (cachedAssembly is not null)
        {
            return cachedAssembly;
        }

        var syntaxTrees = new List<SyntaxTree>();

        foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
        {
            string contents = await File.ReadAllTextAsync(file).ConfigureAwait(false);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(contents));
        }

        var references = new List<MetadataReference>(new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.1.0.0").Location),
            MetadataReference.CreateFromFile(typeof(ExpandoObject).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.GetCallingAssembly().Location),
            MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location),
            MetadataReference.CreateFromFile(typeof(HttpPostAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ActionResult).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IFormCollection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ControllerBase).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(HttpContext).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Controller).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
        });

        if (Assembly.GetEntryAssembly() is not null)
        {
            references.Add(MetadataReference.CreateFromFile(Assembly.GetEntryAssembly()!.Location));
        }

        foreach (var library in GetLibraries())
        {
            var metadataReference = MetadataReference.CreateFromFile(library);
            references.Add(metadataReference);
        }

        foreach(var domain in AppDomain.CurrentDomain.GetAssemblies().Where(y => !string.IsNullOrWhiteSpace(y.Location)))
        {
            var metadataReference = MetadataReference.CreateFromFile(domain.Location);
            references.Add(metadataReference);
        }

        var compilation = CSharpCompilation.Create($"Dynamic_Controller_Assembly_{report.Id}")
            .WithOptions(_compilationOptions)
            .AddSyntaxTrees(syntaxTrees)
            .AddReferences(references);

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        
        if (!result.Success)
        {
            var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
            
            return new()
            {
                Errors = failures.Select(x => x.GetMessage(_httpContextAccessor.HttpContext?.GetFormatProvider())),
                Assembly = null!,
            };
        }
        
        ms.Seek(0, SeekOrigin.Begin);

        byte[] assemblyBytes = ms.ToArray();
        var assembly = Assembly.Load(assemblyBytes);
        Directory.CreateDirectory($"{path}\\Assembly");
        await File.WriteAllBytesAsync($"{path}\\Assembly\\CompiledReport.dll", assemblyBytes).ConfigureAwait(false);
        var type = GetControllerTypeInfo(assembly, report.MainController);
        
        return new() 
        { 
            Success = true,
            MainControllerType = type?.GetTypeInfo(),
            Assembly = assembly,
        };
    }

    private IEnumerable<string> GetLibraries() 
        => _partManager.ApplicationParts
            .OfType<AssemblyPart>()
            .Where(x => !string.IsNullOrWhiteSpace(x.Assembly.Location))
            .Select(x => x.Assembly.Location)
            .Distinct();

    private static CompilationResult? LoadCachedAssembly(string? assemblyPath, TimeSpan cacheDuration, string? controller)
    {
        if (!File.Exists(assemblyPath))
        {
            return null;
        }

        if (CheckIfFileIsOutdated(assemblyPath, cacheDuration))
        {
            try {  File.Delete(assemblyPath); }
#pragma warning disable CA1031 
            catch { }
#pragma warning restore CA1031
            return null;
        }

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);

            return new()
            {
                Assembly = assembly,
                Success = true,
                MainControllerType = GetControllerTypeInfo(assembly, controller),
            };
        }
#pragma warning disable CA1031
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }

    private static bool CheckIfFileIsOutdated(string assemblyPath, TimeSpan cacheDuration)
        => File.GetCreationTimeUtc(assemblyPath).Add(cacheDuration) < DateTime.UtcNow;

    private static System.Reflection.TypeInfo? GetControllerTypeInfo(Assembly assembly, string? name)
        => assembly.GetExportedTypes().FirstOrDefault(y => y.IsAssignableTo(typeof(ControllerBase)) && (name is null || y.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)))?.GetTypeInfo();

}

public class CompilationResult
{
    public bool Success { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    public System.Reflection.TypeInfo? MainControllerType { get; set; }
    public required Assembly Assembly { get; set; }
}
