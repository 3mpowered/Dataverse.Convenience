using Empowered.Dataverse.Connection.Client.Extensions;
using Empowered.Dataverse.Convenience.Auditing.Extensions;
using Empowered.Dataverse.Convenience.Commands.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Xrm.Sdk;

namespace Empowered.Dataverse.Convenience.Commands.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConvenienceCommand(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddDataverseClient<IOrganizationService>();
        serviceCollection.AddAuditing();
        serviceCollection.TryAddScoped<IExportService, ExportService>();
        serviceCollection.TryAddScoped<ConvenienceCommand>();

        return serviceCollection;
    }
}
