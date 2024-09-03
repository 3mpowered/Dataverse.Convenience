using Empowered.Dataverse.Convenience.Auditing.Extensions;
using Empowered.Dataverse.Convenience.Auditing.Model;
using Empowered.Dataverse.Model;
using FluentAssertions;
using Microsoft.Xrm.Sdk.Metadata;

namespace Empowered.Dataverse.Convenience.Auditing.Tests.Extensions;

public class EnumExtensionsTests
{
    [Theory]
    [InlineData(solutioncomponent_rootcomponentbehavior.IncludeAsShellOnly, EntitySolutionBehaviour.SelectedAttributes)]
    [InlineData(solutioncomponent_rootcomponentbehavior.Donotincludesubcomponents,
        EntitySolutionBehaviour.SelectedAttributes)]
    [InlineData(solutioncomponent_rootcomponentbehavior.IncludeSubcomponents, EntitySolutionBehaviour.AllAttributes)]
    public void ShouldMapRootComponentBehaviourToSolutionBehaviour(
        solutioncomponent_rootcomponentbehavior? rootComponentBehaviour,
        EntitySolutionBehaviour expectedSolutionBehaviour)
    {
        rootComponentBehaviour.ToEntitySolutionBehaviour().Should().Be(expectedSolutionBehaviour);
    }

    [Theory]
    [InlineData(EntitySolutionBehaviour.AllAttributes, "All Attributes")]
    [InlineData(EntitySolutionBehaviour.SelectedAttributes, "Selected Attributes")]
    public void ShouldFormatEntitySolutionBehaviour(EntitySolutionBehaviour behaviour, string expectedFormat)
    {
        behaviour.Format().Should().Be(expectedFormat);
    }

    [Theory]
    [InlineData(AttributeTypeCode.Boolean, "Yes/no")]
    [InlineData(AttributeTypeCode.Customer, "Customer")]
    [InlineData(AttributeTypeCode.Decimal, "Decimal")]
    [InlineData(AttributeTypeCode.Double, "Float")]
    [InlineData(AttributeTypeCode.Integer, "Whole number")]
    [InlineData(AttributeTypeCode.Lookup, "Lookup")]
    [InlineData(AttributeTypeCode.Memo, "Text area")]
    [InlineData(AttributeTypeCode.Money, "Currency")]
    [InlineData(AttributeTypeCode.Owner, "Owner")]
    [InlineData(AttributeTypeCode.Picklist, "Choice")]
    [InlineData(AttributeTypeCode.State, "Status")]
    [InlineData(AttributeTypeCode.Status, "Status reason")]
    [InlineData(AttributeTypeCode.String, "Text")]
    [InlineData(AttributeTypeCode.Uniqueidentifier, "Unique identifier")]
    [InlineData(AttributeTypeCode.Virtual, "Virtual")]
    [InlineData(AttributeTypeCode.BigInt, "Whole number (Big)")]
    [InlineData(AttributeTypeCode.CalendarRules, "Calendar rules")]
    [InlineData(AttributeTypeCode.DateTime, "Date and time")]
    [InlineData(AttributeTypeCode.EntityName, "Entity name")]
    [InlineData(AttributeTypeCode.ManagedProperty, "Managed property")]
    [InlineData(AttributeTypeCode.PartyList, "Activity party list")]
    public void ShouldFormatAttributeTypeCode(AttributeTypeCode attributeTypeCode, string expectedFormat)
    {
        attributeTypeCode.Format().Should().Be(expectedFormat);
    }
}
