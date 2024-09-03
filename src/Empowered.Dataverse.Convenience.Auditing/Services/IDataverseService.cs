using Empowered.Dataverse.Convenience.Auditing.Model;
using Empowered.Dataverse.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Empowered.Dataverse.Convenience.Auditing.Services;

internal interface IDataverseService
{
    void UpdateAuditingForOrganization(Guid organizationId, bool isAuditEnabled);
    void UpdateUserAccessAuditing(Guid organizationId, bool isAuditEnabled);
    void Publish(IReadOnlyCollection<string> entities);
    Organization GetOrganization();
    Solution? GetSolution(string solutionName);

    Result<ColumnAuditSetting> UpdateAttributeAuditMetadata(
        ColumnAuditSetting column, string solutionName, bool isAuditEnabled);
    (EntityMetadata EntityMetadata, SolutionComponent SolutionComponent) GetEntityMetadata(
        SolutionComponent component);
    List<SolutionComponent> GetEntityComponents(Solution solution);
    Result<TableAuditSetting> UpdateEntityAuditingMetadata(TableAuditSetting table, bool isEnabled);
    List<AttributeMetadata> GetAttributeComponents(EntityReference entityComponentReference,
        string entityLogicalName);
}
