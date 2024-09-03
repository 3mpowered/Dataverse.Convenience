namespace Empowered.Dataverse.Convenience.Auditing.Model;

public record ChangedAuditSettings(
    Guid OrganizationId,
    bool IsAuditEnabled,
    bool WasAuditEnabledBefore,
    int AuditRetentionPeriod,
    bool IsUserAccessAuditEnabled,
    bool WasUserAccessAuditEnabledBefore,
    int UserAccessRetentionPeriod,
    IReadOnlyCollection<ChangedTableAuditSetting> TableAuditSettings
) : AuditSettings<ChangedTableAuditSetting>(OrganizationId, IsAuditEnabled, AuditRetentionPeriod,
    IsUserAccessAuditEnabled, UserAccessRetentionPeriod, TableAuditSettings);
