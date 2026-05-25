using ClinicalIntake.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClinicalIntake.Api.Tests;

public sealed class AiSummaryProviderRegistrationTests
{
    [Fact]
    public void AddAiSummaryProvider_DefaultsToDeterministicMockProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddAiSummaryProvider(configuration);

        using var provider = services.BuildServiceProvider();
        var summaryService = provider.GetRequiredService<IAiSummaryService>();

        var mockService = Assert.IsType<MockAiSummaryService>(summaryService);
        Assert.Equal(AiSummaryProviderNames.Mock, mockService.ProviderName);
    }

    [Fact]
    public void AddAiSummaryProvider_RejectsUnregisteredExternalProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiSummary:Provider"] = "OpenAI",
                ["AiSummary:ExternalProvidersEnabled"] = "true"
            })
            .Build();

        services.AddAiSummaryProvider(configuration);

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IAiSummaryService>());

        Assert.Contains("no adapter is registered", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
