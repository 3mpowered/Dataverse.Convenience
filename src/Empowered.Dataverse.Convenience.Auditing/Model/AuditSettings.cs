namespace Empowered.Dataverse.Convenience.Auditing.Model;

public record AuditSettings<TTableAuditSetting>(
    Guid OrganizationId,
    bool IsAuditEnabled,
    int AuditRetentionPeriod,
    bool IsUserAccessAuditEnabled,
    int UserAccessRetentionPeriod,
    IReadOnlyCollection<TTableAuditSetting> TableAuditSettings
);

public record AuditSettings(
    Guid OrganizationId,
    bool IsAuditEnabled,
    int AuditRetentionPeriod,
    bool IsUserAccessAuditEnabled,
    int UserAccessRetentionPeriod,
    IReadOnlyCollection<TableAuditSetting> TableAuditSettings
) : AuditSettings<TableAuditSetting>(OrganizationId, IsAuditEnabled, AuditRetentionPeriod, IsUserAccessAuditEnabled,
    UserAccessRetentionPeriod, TableAuditSettings);
