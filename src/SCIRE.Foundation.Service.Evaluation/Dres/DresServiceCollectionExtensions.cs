using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SCIRE.Foundation.Service.Evaluation.Mapping;

namespace SCIRE.Foundation.Service.Evaluation.Dres;

/// <summary>Optional dependency-injection registration for one stateful DRES client. No web host is required.</summary>
public static class DresServiceCollectionExtensions
{
    /// <summary>Registers one shared session as IEvaluationService and DresEvaluationService. The container owns its lifetime.</summary>
    public static IServiceCollection AddDresEvaluation(this IServiceCollection services, DresOptions options, Action<EvaluationScopeMappings>? configureMappings = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        var mappings = new EvaluationScopeMappings();
        configureMappings?.Invoke(mappings);
        services.AddSingleton(provider => DresEvaluationService.Create(options, mappings, provider.GetService<ILogger<DresEvaluationService>>()));
        services.AddSingleton<IEvaluationService>(provider => provider.GetRequiredService<DresEvaluationService>());
        return services;
    }
}
