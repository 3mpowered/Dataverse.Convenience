using Empowered.Dataverse.Convenience.Auditing.Extensions;
using Empowered.Dataverse.Convenience.Auditing.Model;
using Empowered.Dataverse.Convenience.Auditing.Services;
using Empowered.Dataverse.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk.Metadata;

namespace Empowered.Dataverse.Convenience.Auditing;

internal class AuditingService(IDataverseService dataverseService, ILogger<AuditingService> logger)
    : IAuditingService
{
    public AuditSettings Get(string solutionName, bool handleAllAttributes = false)
    {
        logger.LogDebug(
            "Get audit metadata for solution {SolutionName} with handle all attributes equals {HandleAllAttributesFlag}",
            solutionName, handleAllAttributes);

        var solution = dataverseService.GetSolution(solutionName);
        if (solution == null)
        {
            logger.LogError("Couldn't find a solution with unique name {SolutionName}", solutionName);
            throw new ArgumentOutOfRangeException(nameof(solutionName), solutionName,
                $"A solution with unique name {solutionName} does not exist");
        }

        var entities = dataverseService.GetEntityComponents(solution)
            .Where(component => component.ObjectId.HasValue)
            .Select(dataverseService.GetEntityMetadata)
            .ToList();
        logger.LogDebug(
            "Retrieved {MetadataCount} entity metadata for the entity solution components in solution {SolutionId}",
            entities.Count, solution.Id);

        var tables = entities
            .Select(entity =>
                TransformEntityToTableAuditSetting(entity.EntityMetadata, entity.SolutionComponent,
                    handleAllAttributes))
            .OrderBy(table => table.DisplayName)
            .ToList();
        logger.LogDebug("Transformed {TableCount} entities to tables", tables.Count);

        var organization = dataverseService.GetOrganization();
        var auditSettings = new AuditSettings(
            organization.Id,
            organization.IsAuditEnabled.GetValueOrDefault(),
            organization.AuditRetentionPeriodV2.GetValueOrDefault(),
            organization.IsUserAccessAuditEnabled.GetValueOrDefault(),
            organization.UserAccessAuditingInterval.GetValueOrDefault(),
            tables
        );

        return auditSettings;
    }

    public ChangedAuditSettings Enable(string solutionName, bool handleAllAttributes, bool ignoreUserAccess = true,
        bool publish = true)
    {
        var auditSettings = Get(solutionName, handleAllAttributes);

        if (!auditSettings.IsAuditEnabled)
        {
            dataverseService.UpdateAuditingForOrganization(auditSettings.OrganizationId, true);
        }

        if (!ignoreUserAccess && !auditSettings.IsUserAccessAuditEnabled)
        {
            dataverseService.UpdateUserAccessAuditing(auditSettings.OrganizationId, true);
        }

        var disabledTables = auditSettings
            .TableAuditSettings
            .Where(table => table is { IsAuditEnabled: false, CanAuditBeChanged: true })
            .ToList();
        logger.LogInformation("Enabling auditing for the following {RecordCount} entities: {DisabledEntities}",
            disabledTables.Count, string.Join(", ", disabledTables.Select(table => table.LogicalName)));

        var lockedTables = auditSettings
            .TableAuditSettings
            .Where(table => table is { IsAuditEnabled: false, CanAuditBeChanged: false })
            .ToList();
        logger.LogInformation("Auditing can't be enabled for the following {RecordCount} entities: {LockedEntities}",
            lockedTables.Count, string.Join(", ", lockedTables.Select(table => table.LogicalName)));

        var updatedTables = disabledTables
            .Select(table => dataverseService.UpdateEntityAuditingMetadata(table, true))
            .ToList();

        logger.LogInformation(
            "Updated {SuccessfulRecordCount}/{TotalRecordCount} records successfully with {FailedRecordCount} failed records",
            updatedTables.Where(x => !x.IsFailed).ToList().Count,
            updatedTables.Count,
            updatedTables.Where(x => x.IsFailed).ToList().Count);

        var disabledColumns = auditSettings.TableAuditSettings
            .SelectMany(table => table.ColumnAuditSettings)
            .Where(column => column is { IsAuditEnabled: false, CanAuditBeChanged: true })
            .ToList();
        logger.LogInformation("Enabling auditing for {RecordCount} columns", disabledColumns.Count);

        var lockedColumns = auditSettings.TableAuditSettings
            .SelectMany(table => table.ColumnAuditSettings)
            .Where(column => column is { IsAuditEnabled: false, CanAuditBeChanged: false })
            .ToList();
        logger.LogInformation("Auditing can't be enabled for the following {RecordCount} columns: {Columns}",
            lockedColumns.Count, string.Join(", ", lockedColumns.Select(x => x.LogicalName)));

        var updatedColumns = disabledColumns
            .Select(column => dataverseService.UpdateAttributeAuditMetadata(column, solutionName, true))
            .ToList();
        logger.LogInformation(
            "Updated {SuccessfulRecordCount}/{TotalRecordCount} attributes successfully with {FailedRecordCount} failed updates",
            updatedColumns.Where(x => x.IsSuccess).ToList().Count,
            updatedColumns.Count,
            updatedColumns.Where(x => x.IsFailed).ToList().Count
        );

        if (publish)
        {
            var entities = updatedTables
                .Where(table => table.IsSuccess)
                .Select(table => table.Value.LogicalName)
                .ToList();
            dataverseService.Publish(entities);
        }

        var unchangedColumns = auditSettings.TableAuditSettings
            .SelectMany(table => table.ColumnAuditSettings)
            .Where(column => column.IsAuditEnabled)
            .Select(column => new ChangedColumnAuditSetting(
                    column.MetadataId,
                    column.LogicalName,
                    column.DisplayName,
                    column.TypeCode,
                    column.EntityLogicalName,
                    column.IsAuditEnabled,
                    column.CanAuditBeChanged,
                    column.IsAuditEnabled
                )
            );
        var changedColumns = updatedColumns
            .Select(column => new ChangedColumnAuditSetting(
                    column.Value.MetadataId,
                    column.Value.LogicalName,
                    column.Value.DisplayName,
                    column.Value.TypeCode,
                    column.Value.EntityLogicalName,
                    column.IsSuccess,
                    column.Value.CanAuditBeChanged,
                    column.Value.IsAuditEnabled,
                    column.IsFailed
                        ? string.Join(Environment.NewLine, column.Error)
                        : null
                )
            );
        var changedLockedColumns = lockedColumns
            .Select(column => new ChangedColumnAuditSetting(
                    column.MetadataId,
                    column.LogicalName,
                    column.DisplayName,
                    column.TypeCode,
                    column.EntityLogicalName,
                    column.IsAuditEnabled,
                    column.CanAuditBeChanged,
                    column.IsAuditEnabled,
                    $"Auditing can not be enabled for column {column.LogicalName} of entity {column.EntityLogicalName} as customization is locked"
                )
            );
        var columns = changedColumns
            .Concat(unchangedColumns)
            .Concat(changedLockedColumns);

        var changedTableSettings = updatedTables
            .Select(table => new ChangedTableAuditSetting(
                table.Value.MetadataId,
                table.Value.LogicalName,
                table.Value.DisplayName,
                table.Value.Behaviour,
                table.IsSuccess,
                table.Value.IsAuditEnabled,
                table.Value.CanAuditBeChanged,
                columns
                    .Where(column => column.EntityLogicalName == table.Value.LogicalName)
                    .OrderBy(column => column.DisplayName)
                    .ToList(),
                table.IsFailed ? string.Join(Environment.NewLine, table.Error) : null
            ));
        var unchangedTableSettings = auditSettings.TableAuditSettings
            .Where(table => table.IsAuditEnabled)
            .Select(table => new ChangedTableAuditSetting(
                table.MetadataId,
                table.LogicalName,
                table.DisplayName,
                table.Behaviour,
                table.IsAuditEnabled,
                table.IsAuditEnabled,
                table.CanAuditBeChanged,
                columns
                    .Where(column => column.EntityLogicalName == table.LogicalName)
                    .OrderBy(column => column.DisplayName)
                    .ToList()
            ));
        var changedLockedTableSettings = lockedTables
            .Select(table => new ChangedTableAuditSetting(
                    table.MetadataId,
                    table.LogicalName,
                    table.DisplayName,
                    table.Behaviour,
                    table.IsAuditEnabled,
                    table.IsAuditEnabled,
                    table.CanAuditBeChanged,
                    columns
                        .Where(column => column.EntityLogicalName == table.LogicalName)
                        .OrderBy(column => column.DisplayName)
                        .ToList(),
                    $"Auditing can't be enabled for table {table.LogicalName} as customization is locked"
                )
            );
        var tables = unchangedTableSettings
            .Concat(changedTableSettings)
            .Concat(changedLockedTableSettings)
            .OrderBy(table => table.DisplayName)
            .ToList();

        var changedAuditSettings = new ChangedAuditSettings(
            auditSettings.OrganizationId,
            true,
            auditSettings.IsAuditEnabled,
            auditSettings.AuditRetentionPeriod,
            !ignoreUserAccess,
            auditSettings.IsUserAccessAuditEnabled,
            auditSettings.UserAccessRetentionPeriod,
            tables
        );

        return changedAuditSettings;
    }

    public ChangedAuditSettings Disable(string solutionName, bool handleAllAttributes, bool ignoreUserAccess = true,
        bool publish = true)
    {
        var auditSettings = Get(solutionName, handleAllAttributes);

        if (auditSettings.IsAuditEnabled)
        {
            dataverseService.UpdateAuditingForOrganization(auditSettings.OrganizationId, false);
        }

        if (!ignoreUserAccess && auditSettings.IsUserAccessAuditEnabled)
        {
            dataverseService.UpdateUserAccessAuditing(auditSettings.OrganizationId, false);
        }

        var enabledTables = auditSettings
            .TableAuditSettings
            .Where(table => table is { IsAuditEnabled: true, CanAuditBeChanged: true })
            .ToList();
        logger.LogInformation("Disabling auditing for the following {RecordCount} entities: {DisabledEntities}",
            enabledTables.Count, string.Join(", ", enabledTables.Select(table => table.LogicalName)));

        var lockedTables = auditSettings
            .TableAuditSettings
            .Where(table => table is { IsAuditEnabled: true, CanAuditBeChanged: false })
            .ToList();
        logger.LogInformation("Auditing can't be disabled for the following {RecordCount} entities: {LockedEntities}",
            lockedTables.Count, string.Join(", ", lockedTables.Select(table => table.LogicalName)));

        var updatedTables = enabledTables
            .Select(table => dataverseService.UpdateEntityAuditingMetadata(table, false))
            .ToList();

        logger.LogInformation(
            "Updated {SuccessfulRecordCount}/{TotalRecordCount} records successfully with {FailedRecordCount} failed records",
            updatedTables.Where(x => !x.IsFailed).ToList().Count,
            updatedTables.Count,
            updatedTables.Where(x => x.IsFailed).ToList().Count);

        var enabledColumns = auditSettings.TableAuditSettings
            .SelectMany(table => table.ColumnAuditSettings)
            .Where(column => column is { IsAuditEnabled: true, CanAuditBeChanged: true })
            .ToList();
        logger.LogInformation("Enabling auditing for {RecordCount} columns", enabledColumns.Count);

        var lockedColumns = auditSettings.TableAuditSettings
            .SelectMany(table => table.ColumnAuditSettings)
            .Where(column => column is { IsAuditEnabled: true, CanAuditBeChanged: false })
            .ToList();
        logger.LogInformation("Auditing can't be disabled for the following {RecordCount} columns: {Columns}",
            lockedColumns.Count, string.Join(", ", lockedColumns.Select(x => x.LogicalName)));

        var updatedColumns = enabledColumns
            .Select(column => dataverseService.UpdateAttributeAuditMetadata(column, solutionName, false))
            .ToList();
        logger.LogInformation(
            "Updated {SuccessfulRecordCount}/{TotalRecordCount} attributes successfully with {FailedRecordCount} failed updates",
            updatedColumns.Where(x => x.IsSuccess).ToList().Count,
            updatedColumns.Count,
            updatedColumns.Where(x => x.IsFailed).ToList().Count
        );

        if (publish)
        {
            var entities = updatedTables
                .Where(table => table.IsSuccess)
                .Select(table => table.Value.LogicalName)
                .ToList();
            dataverseService.Publish(entities);
        }

        var unchangedColumns = auditSettings.TableAuditSettings
            .SelectMany(table => table.ColumnAuditSettings)
            .Where(column => !column.IsAuditEnabled)
            .Select(column => new ChangedColumnAuditSetting(
                    column.MetadataId,
                    column.LogicalName,
                    column.DisplayName,
                    column.TypeCode,
                    column.EntityLogicalName,
                    column.IsAuditEnabled,
                    column.CanAuditBeChanged,
                    column.IsAuditEnabled
                )
            );
        var changedColumns = updatedColumns
            .Select(column => new ChangedColumnAuditSetting(
                    column.Value.MetadataId,
                    column.Value.LogicalName,
                    column.Value.DisplayName,
                    column.Value.TypeCode,
                    column.Value.EntityLogicalName,
                    !column.IsSuccess,
                    column.Value.CanAuditBeChanged,
                    column.Value.IsAuditEnabled,
                    column.IsFailed
                        ? string.Join(Environment.NewLine, column.Error)
                        : null
                )
            );
        var changedLockedColumns = lockedColumns
            .Select(column => new ChangedColumnAuditSetting(
                    column.MetadataId,
                    column.LogicalName,
                    column.DisplayName,
                    column.TypeCode,
                    column.EntityLogicalName,
                    column.IsAuditEnabled,
                    column.CanAuditBeChanged,
                    column.IsAuditEnabled,
                    $"Auditing can not be disabled for column {column.LogicalName} of entity {column.EntityLogicalName} as customization is locked"
                )
            );
        var columns = changedColumns
            .Concat(unchangedColumns)
            .Concat(changedLockedColumns);

        var changedTableSettings = updatedTables
            .Select(table => new ChangedTableAuditSetting(
                table.Value.MetadataId,
                table.Value.LogicalName,
                table.Value.DisplayName,
                table.Value.Behaviour,
                !table.IsSuccess,
                table.Value.IsAuditEnabled,
                table.Value.CanAuditBeChanged,
                columns
                    .Where(column => column.EntityLogicalName == table.Value.LogicalName)
                    .OrderBy(column => column.DisplayName)
                    .ToList(),
                table.IsFailed ? string.Join(Environment.NewLine, table.Error) : null
            ));
        var unchangedTableSettings = auditSettings.TableAuditSettings
            .Where(table => !table.IsAuditEnabled)
            .Select(table => new ChangedTableAuditSetting(
                table.MetadataId,
                table.LogicalName,
                table.DisplayName,
                table.Behaviour,
                table.IsAuditEnabled,
                table.IsAuditEnabled,
                table.CanAuditBeChanged,
                columns
                    .Where(column => column.EntityLogicalName == table.LogicalName)
                    .OrderBy(column => column.DisplayName)
                    .ToList()
            ));
        var changedLockedTableSettings = lockedTables
            .Select(table => new ChangedTableAuditSetting(
                    table.MetadataId,
                    table.LogicalName,
                    table.DisplayName,
                    table.Behaviour,
                    table.IsAuditEnabled,
                    table.IsAuditEnabled,
                    table.CanAuditBeChanged,
                    columns
                        .Where(column => column.EntityLogicalName == table.LogicalName)
                        .OrderBy(column => column.DisplayName)
                        .ToList(),
                    $"Auditing can't be disabled for table {table.LogicalName} as customization is locked"
                )
            );
        var tables = unchangedTableSettings
            .Concat(changedTableSettings)
            .Concat(changedLockedTableSettings)
            .OrderBy(table => table.DisplayName)
            .ToList();

        var changedAuditSettings = new ChangedAuditSettings(
            auditSettings.OrganizationId,
            false,
            auditSettings.IsAuditEnabled,
            auditSettings.AuditRetentionPeriod,
            ignoreUserAccess && auditSettings.IsUserAccessAuditEnabled,
            auditSettings.IsUserAccessAuditEnabled,
            auditSettings.UserAccessRetentionPeriod,
            tables
        );

        return changedAuditSettings;
    }

    private TableAuditSetting TransformEntityToTableAuditSetting(EntityMetadata entityMetadata,
        SolutionComponent solutionComponent,
        bool handleAllAttributes)
    {
        var metadataId = entityMetadata.MetadataId ?? Guid.Empty;
        var logicalName = entityMetadata.LogicalName;
        var displayName = entityMetadata.DisplayName?.UserLocalizedLabel?.Label;
        var entitySolutionBehaviour = solutionComponent.RootComponentBehavior.ToEntitySolutionBehaviour();
        var isAuditEnabled = entityMetadata.IsAuditEnabled.Value;
        var canAuditBeChanged = entityMetadata.IsAuditEnabled.CanBeChanged;

        var attributes = handleAllAttributes || entitySolutionBehaviour == EntitySolutionBehaviour.AllAttributes
            ? entityMetadata.Attributes.ToList()
            : dataverseService.GetAttributeComponents(solutionComponent.ToEntityReference(),
                entityMetadata.LogicalName);
        logger.LogTrace(
            "Transformed entity with id {MetadataId}, logical name {LogicalName}, display name {DisplayName}, solution behaviour {SolutionBehaviour}, is audit enabled equals {IsAuditEnabled} and can audit be changed equals {CanAuditBeChanged}",
            metadataId, logicalName, displayName, entitySolutionBehaviour, isAuditEnabled, canAuditBeChanged);

        var columnAuditSettings = attributes
            .Where(attribute =>
                attribute.AttributeType != AttributeTypeCode.Virtual && attribute.AttributeOf == null)
            .Select(TansformAttributeToColumnAuditSetting)
            .OrderBy(attribute => attribute.DisplayName)
            .ToList();

        return new TableAuditSetting(
            metadataId,
            logicalName,
            displayName ?? string.Empty,
            entitySolutionBehaviour,
            isAuditEnabled,
            canAuditBeChanged,
            columnAuditSettings
        );
    }

    private ColumnAuditSetting TansformAttributeToColumnAuditSetting(AttributeMetadata attributeMetadata)
    {
        var metadataId = attributeMetadata.MetadataId ?? Guid.Empty;
        var logicalName = attributeMetadata.LogicalName;
        var displayName = attributeMetadata.DisplayName.UserLocalizedLabel?.Label ?? string.Empty;
        var type = attributeMetadata.AttributeType ?? AttributeTypeCode.Virtual;
        var isAuditEnabled = attributeMetadata.IsAuditEnabled.Value;
        var canAuditBeChanged = attributeMetadata.IsAuditEnabled.CanBeChanged;
        var entityLogicalName = attributeMetadata.EntityLogicalName;

        logger.LogTrace(
            "Transform attribute metadata {MetadataId} with logical name {LogicalName}, display name {DisplayName}, type {AttributeType}, entity logical name {EntityLogicalName}, is audit enabled {IsAuditEnabled}, can audit be changed {CanAuditBeChanged} to column",
            metadataId, logicalName, displayName, type, entityLogicalName, isAuditEnabled, canAuditBeChanged);
        return new ColumnAuditSetting(
            metadataId,
            logicalName,
            displayName,
            type,
            entityLogicalName,
            isAuditEnabled,
            canAuditBeChanged
        );
    }
}
