namespace Empowered.Dataverse.Convenience.Auditing.Model;

public record TableAuditSetting<TColumnAuditSetting>(
    Guid MetadataId,
    string LogicalName,
    string DisplayName,
    EntitySolutionBehaviour Behaviour,
    bool IsAuditEnabled,
    bool CanAuditBeChanged,
    IReadOnlyCollection<TColumnAuditSetting> ColumnAuditSettings
) where TColumnAuditSetting : ColumnAuditSetting;

public record TableAuditSetting(
    Guid MetadataId,
    string LogicalName,
    string DisplayName,
    EntitySolutionBehaviour Behaviour,
    bool IsAuditEnabled,
    bool CanAuditBeChanged,
    IReadOnlyCollection<ColumnAuditSetting> ColumnAuditSettings
) : TableAuditSetting<ColumnAuditSetting>(MetadataId,
    LogicalName, DisplayName, Behaviour, IsAuditEnabled, CanAuditBeChanged, ColumnAuditSettings);
