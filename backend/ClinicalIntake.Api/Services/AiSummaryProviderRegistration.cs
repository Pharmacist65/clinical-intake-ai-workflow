using Microsoft.Extensions.Options;

namespace ClinicalIntake.Api.Services;

public static class AiSummaryProviderRegistration
{
    public static IServiceCollection AddAiSummaryProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiSummaryProviderOptions>(
            configuration.GetSection(AiSummaryProviderOptions.SectionName));

        services.AddScoped<MockAiSummaryService>();
        services.AddScoped<IAiSummaryService>(ResolveAiSummaryService);

        return services;
    }

    private static IAiSummaryService ResolveAiSummaryService(IServiceProvider serviceProvider)
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<AiSummaryProviderOptions>>()
            .Value;
        var providerName = NormalizeProviderName(options.Provider);

        if (providerName.Equals(AiSummaryProviderNames.Mock, StringComparison.OrdinalIgnoreCase))
        {
            return serviceProvider.GetRequiredService<MockAiSummaryService>();
        }

        if (options.ExternalProvidersEnabled)
        {
            throw new InvalidOperationException(
                $"AI summary provider '{providerName}' is enabled, but no adapter is registered for this provider.");
        }

        throw new InvalidOperationException(
            $"AI summary provider '{providerName}' is not available. Only the deterministic mock provider is registered; external providers remain disabled unless an adapter is explicitly added.");
    }

    private static string NormalizeProviderName(string? providerName) =>
        string.IsNullOrWhiteSpace(providerName)
            ? AiSummaryProviderNames.Mock
            : providerName.Trim();
}
