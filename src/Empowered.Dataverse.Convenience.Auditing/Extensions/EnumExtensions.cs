using Empowered.Dataverse.Convenience.Auditing.Model;
using Empowered.Dataverse.Model;
using Microsoft.Xrm.Sdk.Metadata;

namespace Empowered.Dataverse.Convenience.Auditing.Extensions;

public static class EnumExtensions
{
    public static EntitySolutionBehaviour ToEntitySolutionBehaviour(
        this solutioncomponent_rootcomponentbehavior? behavior) => behavior switch
    {
        solutioncomponent_rootcomponentbehavior.Donotincludesubcomponents => EntitySolutionBehaviour.SelectedAttributes,
        solutioncomponent_rootcomponentbehavior.IncludeAsShellOnly => EntitySolutionBehaviour.SelectedAttributes,
        solutioncomponent_rootcomponentbehavior.IncludeSubcomponents => EntitySolutionBehaviour.AllAttributes,
        null => throw new ArgumentNullException(nameof(behavior), "Root component behaviour can't be null!"),
        _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior,
            $"Unknown behaviour {behavior} can't be mapped!")
    };

    public static string Format(this EntitySolutionBehaviour behaviour) => behaviour switch
    {
        EntitySolutionBehaviour.AllAttributes => "All Attributes",
        EntitySolutionBehaviour.SelectedAttributes => "Selected Attributes",
        _ => string.Empty
    };

    public static string Format(this AttributeTypeCode attributeTypeCode) => attributeTypeCode switch
    {
        AttributeTypeCode.Boolean => "Yes/no",
        AttributeTypeCode.Customer => "Customer",
        AttributeTypeCode.Decimal => "Decimal",
        AttributeTypeCode.Double => "Float",
        AttributeTypeCode.Integer => "Whole number",
        AttributeTypeCode.Lookup => "Lookup",
        AttributeTypeCode.Memo => "Text area",
        AttributeTypeCode.Money => "Currency",
        AttributeTypeCode.Owner => "Owner",
        AttributeTypeCode.Picklist => "Choice",
        AttributeTypeCode.State => "Status",
        AttributeTypeCode.Status => "Status reason",
        AttributeTypeCode.String => "Text",
        AttributeTypeCode.Uniqueidentifier => "Unique identifier",
        AttributeTypeCode.Virtual => "Virtual",
        AttributeTypeCode.BigInt => "Whole number (Big)",
        AttributeTypeCode.CalendarRules => "Calendar rules",
        AttributeTypeCode.DateTime => "Date and time",
        AttributeTypeCode.EntityName => "Entity name",
        AttributeTypeCode.ManagedProperty => "Managed property",
        AttributeTypeCode.PartyList => "Activity party list",
        _ => string.Empty
    };
}
