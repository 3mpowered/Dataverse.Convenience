using Microsoft.Xrm.Sdk.Metadata;

namespace Empowered.Dataverse.Convenience.Auditing.Model;

public record ChangedColumnAuditSetting(
    Guid MetadataId,
    string LogicalName,
    string DisplayName,
    AttributeTypeCode TypeCode,
    string EntityLogicalName,
    bool IsAuditEnabled,
    bool CanAuditBeChanged,
    bool WasAuditEnabledBefore,
    string? ErrorMessage = null
) : ColumnAuditSetting(MetadataId, LogicalName, DisplayName, TypeCode, EntityLogicalName, IsAuditEnabled,
    CanAuditBeChanged);
