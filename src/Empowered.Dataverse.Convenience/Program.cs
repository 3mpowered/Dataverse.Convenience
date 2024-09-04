using Empowered.CommandLine.Extensions;
using Empowered.CommandLine.Extensions.Dataverse;
using Empowered.Dataverse.Connection.Client.Extensions;
using Empowered.Dataverse.Connection.Store.Extensions;
using Empowered.Dataverse.Convenience.Commands;
using Empowered.Dataverse.Convenience.Commands.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;

namespace Empowered.Dataverse.Convenience;

public static class Program
{
    public static int Main(string[] args)
    {
        var appRunner = new EmpoweredAppRunner<ConvenienceCommand>("3mpwrd-convenience", ConfigureApp);
        appRunner.UseDataverseConnectionTest<IOrganizationService>();
        return appRunner.Run(args);
    }
    private static void ConfigureApp(IServiceCollection serviceCollection, IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .AddDataverseConnectionSource();
        serviceCollection
            .AddConvenienceCommand()
            .AddConnectionStore();
    }
}
