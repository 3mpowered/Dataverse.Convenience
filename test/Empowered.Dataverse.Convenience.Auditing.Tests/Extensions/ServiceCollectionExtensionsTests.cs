using Empowered.Dataverse.Connection.Client.Settings;
using Empowered.Dataverse.Connection.Store.Contracts;
using Empowered.Dataverse.Convenience.Auditing.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Empowered.Dataverse.Convenience.Auditing.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact(Skip = "Requires user interaction")]
    public void ShouldResolveAuditingService()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(
                    $"{DataverseClientOptions.Section}:{nameof(DataverseClientOptions.Name)}", "test"),
                new KeyValuePair<string, string?>(
                    $"{DataverseClientOptions.Section}:{nameof(DataverseClientOptions.EnvironmentUrl)}",
                    "https://empowered.crm16.dynamics.com"),
                new KeyValuePair<string, string?>(
                    $"{DataverseClientOptions.Section}:{nameof(DataverseClientOptions.Type)}",
                    ConnectionType.Interactive.ToString())
            });
        var configuration = configurationBuilder.Build();
        var serviceCollection = new ServiceCollection()
            .AddSingleton<IConfiguration>(_ => configuration)
            .AddAuditing();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var auditingService = serviceProvider.GetRequiredService<IAuditingService>();

        auditingService.Should().NotBeNull();
    }
}
