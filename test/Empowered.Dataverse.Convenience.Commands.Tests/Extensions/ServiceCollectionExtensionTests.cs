using Empowered.Dataverse.Connection.Client.Settings;
using Empowered.Dataverse.Connection.Store.Contracts;
using Empowered.Dataverse.Convenience.Commands.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Empowered.Dataverse.Convenience.Commands.Tests.Extensions;

public class ServiceCollectionExtensionTests
{
    [Fact(Skip = "requires interactive usage")]
    public void ShouldResolveConvenienceCommandFromServiceCollection()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(
                    $"{DataverseClientOptions.Section}:{nameof(DataverseClientOptions.Name)}", "test"),
                new KeyValuePair<string, string?>(
                    $"{DataverseClientOptions.Section}:{nameof(DataverseClientOptions.EnvironmentUrl)}",
                    "https://xxx.crm16.dynamics.com"),
                new KeyValuePair<string, string?>(
                    $"{DataverseClientOptions.Section}:{nameof(DataverseClientOptions.Type)}",
                    ConnectionType.Interactive.ToString())
            });
        var configuration = configurationBuilder.Build();
        var serviceCollection = new ServiceCollection()
            .AddSingleton<IConfiguration>( _ => configuration)
            .AddConvenienceCommand();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var convenienceCommand = serviceProvider.GetRequiredService<ConvenienceCommand>();

        convenienceCommand.Should().NotBeNull();
    }
}
