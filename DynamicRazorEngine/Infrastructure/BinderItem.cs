using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DynamicRazorEngine.Infrastructure;

internal readonly record struct BinderItem(IModelBinder ModelBinder, ModelMetadata ModelMetadata);
