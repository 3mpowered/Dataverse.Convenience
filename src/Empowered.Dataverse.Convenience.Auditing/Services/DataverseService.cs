using Empowered.Dataverse.Convenience.Auditing.Model;
using Empowered.Dataverse.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Empowered.Dataverse.Convenience.Auditing.Services;

internal class DataverseService(IOrganizationService organizationService, ILogger<DataverseService> logger) : IDataverseService
{
    public void UpdateAuditingForOrganization(Guid organizationId, bool isAuditEnabled)
    {
        organizationService.Update(new Organization
        {
            Id = organizationId,
            IsAuditEnabled = isAuditEnabled,
        });
        logger.LogInformation("Enabled auditing on organization {OrganizationId}", organizationId);
    }

    public void UpdateUserAccessAuditing(Guid organizationId, bool isAuditEnabled)
    {
        organizationService.Update(new Organization
        {
            Id = organizationId,
            IsUserAccessAuditEnabled = isAuditEnabled
        });
        logger.LogInformation("Enabled user access auditing on organization {OrganizationId}", organizationId);
    }

    public void Publish(IReadOnlyCollection<string> entities)
    {
        var request = new PublishXmlRequest
        {
            ParameterXml = $@"
                        <importexportxml>
                            <entities>
                                {string.Join(Environment.NewLine, entities.Select(entity => $"<entity>{entity}</entity>"))}
                            </entities>
                        </importexportxml>"
        };
        organizationService.Execute(request);
        logger.LogInformation("Published the following {RecordCount} entities: {Entities}",
            entities.Count,
            string.Join(", ", entities)
        );
    }

    public Organization GetOrganization()
    {
        var query = new QueryExpression(Organization.EntityLogicalName)
        {
            NoLock = true,
            TopCount = 1,
            ColumnSet = new ColumnSet(
                Organization.Fields.Id,
                Organization.Fields.IsAuditEnabled,
                Organization.Fields.AuditRetentionPeriodV2,
                Organization.Fields.IsUserAccessAuditEnabled,
                Organization.Fields.UserAccessAuditingInterval
            ),
        };
        var organization = organizationService.RetrieveMultiple(query).Entities.First().ToEntity<Organization>();
        logger.LogDebug(
            "Retrieved organization {OrganizationId} with is audit enabled equals {IsAuditEnabled}, retention period {AuditRetentionPeriod}, is user access audit enabled equals {IsUserAccessAuditEnabled} and user access auditing interval {UserAccessAuditInterval}",
            organization.Id, organization.IsAuditEnabled, organization.AuditRetentionPeriodV2,
            organization.IsUserAccessAuditEnabled, organization.UserAccessAuditingInterval);

        return organization;
    }

    public Solution? GetSolution(string solutionName)
    {
        var query = new QueryExpression(Solution.EntityLogicalName)
        {
            NoLock = true,
            TopCount = 1,
            ColumnSet = new ColumnSet(
                Solution.Fields.Id,
                Solution.Fields.FriendlyName,
                Solution.Fields.Version,
                Solution.Fields.IsManaged
            ),
            Criteria = new FilterExpression(LogicalOperator.And)
            {
                Conditions =
                {
                    new ConditionExpression
                    {
                        AttributeName = Solution.Fields.ParentSolutionId,
                        Operator = ConditionOperator.Null
                    },
                    new ConditionExpression
                    {
                        AttributeName = Solution.Fields.UniqueName,
                        Operator = ConditionOperator.Equal,
                        Values =
                        {
                            solutionName
                        }
                    }
                }
            },
        };
        var solution = organizationService.RetrieveMultiple(query).Entities.Select(x => x.ToEntity<Solution>())
            .FirstOrDefault();
        logger.LogDebug(
            "Retrieved solution with id {SolutionId}, display name {SolutionDisplayName} and version {SolutionVersion} by unique name {SolutionUniqueName}",
            solution?.Id, solution?.FriendlyName, solution?.Version, solutionName);
        return solution;
    }

    public Result<ColumnAuditSetting> UpdateAttributeAuditMetadata(
        ColumnAuditSetting column, string solutionName, bool isAuditEnabled)
    {
        var attributeMetadata = GetAttributeMetadata(column);
        try
        {
            attributeMetadata.IsAuditEnabled = new BooleanManagedProperty(isAuditEnabled);
            var request = new UpdateAttributeRequest
            {
                EntityName = column.EntityLogicalName,
                SolutionUniqueName = solutionName,
                MergeLabels = true,
                Attribute = attributeMetadata
            };
            organizationService.Execute(request);
            return Result<ColumnAuditSetting>.Ok(column);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Update of attribute metadata for attribute {AttributeLogicalName} for entity {EntityLogicalName} failed with error: {ErrorMessage}",
                column.LogicalName, column.EntityLogicalName, exception.Message);
            return Result<ColumnAuditSetting>.Fail(column, exception);
        }
    }

    private AttributeMetadata GetAttributeMetadata(ColumnAuditSetting column)
    {
        var request = new RetrieveAttributeRequest
        {
            MetadataId = column.MetadataId,
            RetrieveAsIfPublished = true,
            EntityLogicalName = column.EntityLogicalName,
        };
        var response = (RetrieveAttributeResponse)organizationService.Execute(request);
        var attributeMetadata = response.AttributeMetadata;
        return attributeMetadata;
    }

    public (EntityMetadata EntityMetadata, SolutionComponent SolutionComponent) GetEntityMetadata(
        SolutionComponent component)
    {
        var metadataResponse = (RetrieveEntityResponse)organizationService.Execute(new RetrieveEntityRequest
        {
            MetadataId = component.ObjectId.Value,
            EntityFilters = EntityFilters.Attributes,
            RetrieveAsIfPublished = true
        });
        return (metadataResponse.EntityMetadata, component);
    }

    public List<SolutionComponent> GetEntityComponents(Solution solution)
    {
        var query = new QueryExpression(SolutionComponent.EntityLogicalName)
        {
            NoLock = true,
            ColumnSet = new ColumnSet(
                SolutionComponent.Fields.Id,
                SolutionComponent.Fields.ComponentType,
                SolutionComponent.Fields.ObjectId,
                SolutionComponent.Fields.RootComponentBehavior
            ),
            Criteria = new FilterExpression(LogicalOperator.And)
            {
                Conditions =
                {
                    new ConditionExpression
                    {
                        AttributeName = SolutionComponent.Fields.SolutionId,
                        Operator = ConditionOperator.Equal,
                        Values =
                        {
                            solution.Id
                        }
                    },
                    new ConditionExpression
                    {
                        AttributeName = SolutionComponent.Fields.ComponentType,
                        Operator = ConditionOperator.Equal,
                        Values =
                        {
                            (int)componenttype.Entity
                        }
                    }
                }
            }
        };
        var entityComponents = organizationService.RetrieveMultiple(query).Entities
            .Select(x => x.ToEntity<SolutionComponent>()).ToList();
        logger.LogInformation(
            "Retrieved {RecordCount} solution components of type entity for solution with id {SolutionId}",
            entityComponents.Count, solution.Id);
        return entityComponents;
    }

    public Result<TableAuditSetting> UpdateEntityAuditingMetadata(TableAuditSetting table, bool isEnabled)
    {
        var entityMetadata = GetEntityMetadata(table);
        try
        {
            entityMetadata.IsAuditEnabled = new BooleanManagedProperty(isEnabled);
            var request = new UpdateEntityRequest
            {
                Entity = entityMetadata
            };
            organizationService.Execute(request);
            return Result<TableAuditSetting>.Ok(table);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Enabling of entity {EntityLogicalName} failed with error: {Message}",
                table.LogicalName, exception.Message);
            return Result<TableAuditSetting>.Fail(table, exception);
        }
    }

    private EntityMetadata GetEntityMetadata(TableAuditSetting table)
    {
        var request = new RetrieveEntityRequest
        {
            MetadataId = table.MetadataId,
            RetrieveAsIfPublished = true,
            EntityFilters = EntityFilters.Entity,
        };
        var response = (RetrieveEntityResponse)organizationService.Execute(request);
        var entityMetadata = response.EntityMetadata;
        return entityMetadata;
    }

    public List<AttributeMetadata> GetAttributeComponents(EntityReference entityComponentReference,
        string entityLogicalName)
    {
        var query = new QueryExpression(SolutionComponent.EntityLogicalName)
        {
            NoLock = true,
            ColumnSet = new ColumnSet(
                SolutionComponent.Fields.Id,
                SolutionComponent.Fields.ObjectId
            ),
            Criteria = new FilterExpression(LogicalOperator.And)
            {
                Conditions =
                {
                    new ConditionExpression
                    {
                        AttributeName = SolutionComponent.Fields.ComponentType,
                        Operator = ConditionOperator.Equal,
                        Values =
                        {
                            (int)componenttype.Attribute
                        }
                    },
                    new ConditionExpression
                    {
                        AttributeName = SolutionComponent.Fields.RootSolutionComponentId,
                        Operator = ConditionOperator.Equal,
                        Values =
                        {
                            entityComponentReference.Id
                        }
                    }
                }
            }
        };

        var components = organizationService
            .RetrieveMultiple(query)
            .Entities
            .Select(entity => entity.ToEntity<SolutionComponent>())
            .ToList();
        logger.LogDebug(
            "Retrieved {RecordCount} attribute solution components for parent solution component {ParentComponentId}",
            components.Count, entityComponentReference.Id);

        var attributes = components
            .Where(component => component.ObjectId.HasValue)
            .Select(component => (RetrieveAttributeResponse)organizationService.Execute(new RetrieveAttributeRequest
                {
                    MetadataId = component.ObjectId.Value,
                    EntityLogicalName = entityLogicalName,
                    RetrieveAsIfPublished = true,
                }
            ))
            .Select(response => response.AttributeMetadata)
            .ToList();
        logger.LogDebug("Retrieved {MetadataCount} attribute metadata for entity {EntityLogicalName}",
            attributes.Count, entityLogicalName);
        return attributes;
    }
}
