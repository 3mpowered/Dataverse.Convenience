using Empowered.Dataverse.Connection.Client.Extensions;
using Empowered.Dataverse.Convenience.Auditing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Xrm.Sdk;

namespace Empowered.Dataverse.Convenience.Auditing.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditing(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddScoped(typeof(IAuditingService), typeof(AuditingService));
        return serviceCollection
            .AddDataverseClient<IOrganizationService>()
            .AddScoped<IDataverseService, DataverseService>()
            .AddLogging();
    }
}
