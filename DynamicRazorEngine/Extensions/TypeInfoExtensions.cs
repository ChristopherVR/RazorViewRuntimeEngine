using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DynamicRazorEngine.Extensions;

internal static class TypeInfoExtensions
{
    /// <summary>
    /// Gets the constructor or method paramater value. The server's <see cref="IServiceProvider"/> will be used to resolve any dependency injection services.
    /// <br/> If a <see cref="HttpContext"/> is available, this will be used to resolve any request data being posted alongside the generated method.
    /// </summary>
    /// <param name="propInfo">The <see cref="ParameterInfo"/> information for the given method or constructor.</param>
    /// <param name="assembly">The <see cref="Assembly"/> that will be used to determine if the <see cref="Type"/> exists within it.</param>
    /// <param name="token">Cancellation token that can be passed as the parameter value.</param>
    /// <returns></returns>
#pragma warning disable IDE1006 // Naming Styles
    internal static T CreateInstance<T>(this TypeInfo type, Assembly assembly, IServiceProvider serviceProvider) where T : class
#pragma warning restore IDE1006 // Naming Styles
    {
        var instance = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type);

        if (instance is not null)
        {
            return (T)instance;
        }

        var paramters = type
            .GetConstructors()
            .First()
            .GetParameters()
            .Select((y) => GetParamaterValue(serviceProvider, y, assembly))
            .ToArray();

        instance = type.GetConstructors().First().Invoke(paramters);

        return (T)instance;
    }

    private static object? GetParamaterValue(IServiceProvider serviceProvider, ParameterInfo propInfo, Assembly assembly)
    {
        if (propInfo.ParameterType == typeof(ILogger<>)
            || propInfo.ParameterType.GetInterface(nameof(ILogger)) is not null)
        {
            using var loggerFactory = serviceProvider.GetService<ILoggerFactory>()!;

            return loggerFactory.CreateGenericLogger(propInfo.ParameterType);
        }

        if (TryGetReferencedTypeValue(serviceProvider, propInfo, assembly, out var var))
        {
            return var;
        }

        if (!propInfo.ParameterType.IsValueType)
        {
            try
            {
                var instance = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, propInfo.ParameterType);
                return instance;
            }
#pragma warning disable CA1031
            catch
            {
            }
#pragma warning restore CA1031
        }

        if (propInfo.HasDefaultValue)
        {
            return propInfo.DefaultValue;
        }

        if (propInfo.ParameterType.IsValueType)
        {
            return Activator.CreateInstance(propInfo.ParameterType);
        }

        // See if there is any current assembly loaded into the AppDomain that satisfies the type
        foreach (var ase in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (TryGetReferencedTypeValue(serviceProvider, propInfo, ase, out var value))
            {
                return value;
            }
        }

        return default;
    }

    /// <summary>
    /// Tries to get a refenced value for the <see cref="ParameterInfo"/> in the specified <see cref="Assembly"/>.
    /// </summary>
    /// <param name="propInfo"></param>
    /// <param name="assembly"></param>
    /// <param name="token"></param>
    /// <param name="value"></param>
    /// <returns>True if successful.</returns>
    private static bool TryGetReferencedTypeValue(IServiceProvider serviceProvider, ParameterInfo propInfo, Assembly assembly, out object? value)
    {
        // This is for cases where the dynamic code injects a service that is defined in the dynamic code.
        if (!propInfo.ParameterType.IsValueType)
        {
            try
            {
                Type? objectType = assembly.GetType(propInfo.ParameterType.FullName!, throwOnError: false, ignoreCase: true);
                if (objectType is not null)
                {
                    var constructors = objectType.GetConstructors();
                    // assume we will have only one constructor
                    var firstConstrutor = constructors.FirstOrDefault() ?? objectType.GetConstructor(Type.EmptyTypes);
                    var parameters = firstConstrutor?.GetParameters().Select(y => GetParamaterValue(serviceProvider, y, assembly)).ToArray();
                    value = firstConstrutor?.Invoke(parameters);
                    return true;
                }
            }
#pragma warning disable CA1031
            catch
            {
                value = null;
            }
#pragma warning restore CA1031
        }
        value = null;
        return false;
    }
}
