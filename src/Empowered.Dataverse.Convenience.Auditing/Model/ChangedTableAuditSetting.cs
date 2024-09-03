namespace Empowered.Dataverse.Convenience.Auditing.Model;

public record ChangedTableAuditSetting(
    Guid MetadataId,
    string LogicalName,
    string DisplayName,
    EntitySolutionBehaviour Behaviour,
    bool IsAuditEnabled,
    bool WasAuditEnabledBefore,
    bool CanAuditBeChanged,
    IReadOnlyCollection<ChangedColumnAuditSetting> ColumnAuditSettings,
    string? ErrorMessage = null
) : TableAuditSetting<ChangedColumnAuditSetting>(MetadataId, LogicalName, DisplayName, Behaviour, IsAuditEnabled,
    CanAuditBeChanged, ColumnAuditSettings);
