using Microsoft.Xrm.Sdk.Metadata;

namespace Empowered.Dataverse.Convenience.Auditing.Tests.Extensions;

public static class MetadataExtensions
{
    public static void SetSealedPropertyValue(
        this AttributeMetadata attributeMetadata,
        string sPropertyName,
        object value)
    {
        attributeMetadata.GetType().GetProperty(sPropertyName)?.SetValue(attributeMetadata, value,  null);
    }

}
