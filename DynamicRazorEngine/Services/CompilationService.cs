using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Dynamic;
using System.Reflection;

namespace DynamicRazorEngine.Services;

// TODO: This needs to be swapped with the DynamicModule nuget package.
internal sealed class CompilationServices
{
    private readonly ApplicationPartManager _partManager;
    private readonly CSharpCompilationOptions _compilationOptions;

    public CompilationServices(ApplicationPartManager partManager)
    {
        _partManager = partManager;
        _compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
    }

    public CompilationResult Compile(string fileName, string content)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(content);
        var references = new List<MetadataReference>(new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            // Default netstandard assembly is required.
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
            //MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.1.0.0").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
            MetadataReference.CreateFromFile(Assembly.Load("Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
            MetadataReference.CreateFromFile(Assembly.Load("Microsoft.Extensions.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Linq, Version=7.0.0.0").Location),
            MetadataReference.CreateFromFile(typeof(ExpandoObject).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("Microsoft.AspNetCore.Mvc.Abstractions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60").Location),
               MetadataReference.CreateFromFile(Assembly.Load("System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
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
        var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(fileName))
            .WithOptions(_compilationOptions)
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(references);

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        
        if (!result.Success)
        {
            var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
            
            return new()
            {
                Errors = failures.Select(x => x.GetMessage()),
                Assembly = null!,
            };
        }
        
        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var type = assembly.GetExportedTypes().First(y => y.BaseType == typeof(Controller));
        
        return new() 
        { 
            Success = true,
            CompiledType = type!.GetTypeInfo(),
            Assembly = assembly,
        };
    }

    private IEnumerable<string> GetLibraries()
    {
        return _partManager.ApplicationParts
            .OfType<AssemblyPart>()
            .Where(x => !string.IsNullOrWhiteSpace(x.Assembly.Location))
            .Select(x => x.Assembly.Location)
            .Distinct();
    }
}

public class CompilationResult
{
    public bool Success { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    public System.Reflection.TypeInfo? CompiledType { get; set; }
    public required Assembly Assembly { get; set; }
}
