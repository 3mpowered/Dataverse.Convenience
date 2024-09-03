using Microsoft.Xrm.Sdk.Metadata;

namespace Empowered.Dataverse.Convenience.Auditing.Model;

public record ColumnAuditSetting(
    Guid MetadataId,
    string LogicalName,
    string DisplayName,
    AttributeTypeCode TypeCode,
    string EntityLogicalName,
    bool IsAuditEnabled,
    bool CanAuditBeChanged
);
