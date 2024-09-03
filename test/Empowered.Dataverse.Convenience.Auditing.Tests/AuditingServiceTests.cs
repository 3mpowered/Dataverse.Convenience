using Empowered.Dataverse.Convenience.Auditing.Extensions;
using Empowered.Dataverse.Convenience.Auditing.Model;
using Empowered.Dataverse.Convenience.Auditing.Services;
using Empowered.Dataverse.Convenience.Auditing.Tests.Extensions;
using Empowered.Dataverse.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Empowered.Dataverse.Convenience.Auditing.Tests;

public class AuditingServiceTests
{
    private const string SolutionName = "solution";
    private readonly IDataverseService _dataverseService = Substitute.For<IDataverseService>();
    private readonly IAuditingService _auditingService;

    public AuditingServiceTests()
    {
        _auditingService = new AuditingService(_dataverseService, NullLogger<AuditingService>.Instance);
    }

    [Fact]
    public void ShouldThrowWhenSolutionNotFoundOnGet()
    {
        const string solutionName = "solution";
        _dataverseService.GetSolution(solutionName).ReturnsNull();

        Action actor = () => _auditingService.Get(solutionName);
        actor
            .Should()
            .ThrowExactly<ArgumentOutOfRangeException>()
            .Where(exception => ReferenceEquals(exception.ActualValue, solutionName) &&
                                exception.ParamName == nameof(solutionName));
    }

    [Fact]
    public void ShouldGetAuditingSettings()
    {
        var mocks = SetupMocks();

        var auditSettings = _auditingService.Get(SolutionName);

        auditSettings.Should().NotBeNull();
        auditSettings.IsAuditEnabled.Should().Be(mocks.organization.IsAuditEnabled.Value);
        auditSettings.IsUserAccessAuditEnabled.Should().Be(mocks.organization.IsUserAccessAuditEnabled.Value);
        auditSettings.AuditRetentionPeriod.Should().Be(mocks.organization.AuditRetentionPeriodV2);
        auditSettings.UserAccessRetentionPeriod.Should().Be(mocks.organization.UserAccessAuditingInterval);

        auditSettings.TableAuditSettings
            .Should()
            .ContainSingle(table => table.MetadataId == mocks.accountMetadata.MetadataId
                                    && table.IsAuditEnabled == mocks.accountMetadata.IsAuditEnabled.Value
                                    && table.CanAuditBeChanged == mocks.accountMetadata.IsAuditEnabled.CanBeChanged
                                    && table.DisplayName == mocks.accountMetadata.DisplayName.UserLocalizedLabel.Label
                                    && table.LogicalName == mocks.accountMetadata.LogicalName
                                    && table.Behaviour ==
                                    mocks.accountComponent.RootComponentBehavior.ToEntitySolutionBehaviour()
            );
        var tableAuditSetting = auditSettings.TableAuditSettings.First();
        tableAuditSetting.ColumnAuditSettings
            .Should()
            .ContainSingle(column => column.MetadataId == mocks.accountNameMetadata.MetadataId
                                     && column.IsAuditEnabled == mocks.accountNameMetadata.IsAuditEnabled.Value
                                     && column.DisplayName ==
                                     mocks.accountNameMetadata.DisplayName.UserLocalizedLabel.Label
                                     && column.CanAuditBeChanged ==
                                     mocks.accountNameMetadata.IsAuditEnabled.CanBeChanged
                                     && column.LogicalName == mocks.accountNameMetadata.LogicalName
                                     && column.TypeCode == mocks.accountNameMetadata.AttributeType
                                     && column.EntityLogicalName == mocks.accountNameMetadata.EntityLogicalName
            );
    }

    [Fact]
    public void ShouldEnableAuditing()
    {
        var mocks = SetupMocks();
        mocks.organization.IsAuditEnabled = false;
        mocks.organization.IsUserAccessAuditEnabled = false;
        mocks.accountMetadata.IsAuditEnabled = new BooleanManagedProperty(false);
        mocks.accountNameMetadata.IsAuditEnabled = new BooleanManagedProperty(false);

        var changedAuditSettings = _auditingService.Enable(SolutionName, false, false);

        changedAuditSettings.OrganizationId.Should().Be(mocks.organization.Id);
        changedAuditSettings.IsAuditEnabled.Should().Be(true);
        changedAuditSettings.IsUserAccessAuditEnabled.Should().Be(true);
        changedAuditSettings.WasAuditEnabledBefore.Should().Be(false);
        changedAuditSettings.WasUserAccessAuditEnabledBefore.Should().Be(false);
        changedAuditSettings.AuditRetentionPeriod.Should().Be(mocks.organization.AuditRetentionPeriodV2);
        changedAuditSettings.UserAccessRetentionPeriod.Should().Be(mocks.organization.UserAccessAuditingInterval);

        changedAuditSettings.TableAuditSettings
            .Should()
            .ContainSingle(table => table.IsAuditEnabled == true
                                    && table.WasAuditEnabledBefore == false
                                    && table.LogicalName == mocks.accountMetadata.LogicalName
                                    && table.Behaviour == mocks.accountComponent.RootComponentBehavior
                                        .ToEntitySolutionBehaviour()
                                    && table.DisplayName == mocks.accountMetadata.DisplayName.UserLocalizedLabel.Label
                                    && table.CanAuditBeChanged == mocks.accountMetadata.IsAuditEnabled.CanBeChanged
                                    && table.MetadataId == mocks.accountMetadata.MetadataId!.Value
            );
        var tableAuditSetting = changedAuditSettings.TableAuditSettings.First();
        tableAuditSetting.ColumnAuditSettings
            .Should()
            .ContainSingle(column => column.MetadataId == mocks.accountNameMetadata.MetadataId!.Value
                                     && column.IsAuditEnabled == true
                                     && column.WasAuditEnabledBefore == false
                                     && column.EntityLogicalName == mocks.accountNameMetadata.EntityLogicalName
                                     && column.DisplayName ==
                                     mocks.accountNameMetadata.DisplayName.UserLocalizedLabel.Label
                                     && column.LogicalName == mocks.accountNameMetadata.LogicalName
                                     && column.CanAuditBeChanged ==
                                     mocks.accountNameMetadata.IsAuditEnabled.CanBeChanged
                                     && column.TypeCode == mocks.accountNameMetadata.AttributeType);
    }

    [Fact]
    public void ShouldDisableAuditing()
    {
        var mocks = SetupMocks();
        mocks.organization.IsAuditEnabled = true;
        mocks.organization.IsUserAccessAuditEnabled = true;
        mocks.accountMetadata.IsAuditEnabled = new BooleanManagedProperty(true);
        mocks.accountNameMetadata.IsAuditEnabled = new BooleanManagedProperty(true);
        mocks.accountIdMetadata.IsAuditEnabled = new BooleanManagedProperty(false);
        mocks.primaryContactMetadata.IsAuditEnabled = new BooleanManagedProperty(true)
        {
            CanBeChanged = false
        };

        var changedAuditSettings = _auditingService.Disable(SolutionName, false, false);

        changedAuditSettings.Should().NotBeNull();
        changedAuditSettings.OrganizationId.Should().Be(mocks.organization.Id);
        changedAuditSettings.IsAuditEnabled.Should().Be(false);
        changedAuditSettings.IsUserAccessAuditEnabled.Should().Be(false);
        changedAuditSettings.WasAuditEnabledBefore.Should().Be(true);
        changedAuditSettings.WasUserAccessAuditEnabledBefore.Should().Be(true);
        changedAuditSettings.AuditRetentionPeriod.Should().Be(mocks.organization.AuditRetentionPeriodV2);
        changedAuditSettings.UserAccessRetentionPeriod.Should().Be(mocks.organization.UserAccessAuditingInterval);

        changedAuditSettings.TableAuditSettings
            .Should()
            .ContainSingle(table => table.IsAuditEnabled == false
                                    && table.WasAuditEnabledBefore == true
                                    && table.LogicalName == mocks.accountMetadata.LogicalName
                                    && table.Behaviour == mocks.accountComponent.RootComponentBehavior
                                        .ToEntitySolutionBehaviour()
                                    && table.DisplayName == mocks.accountMetadata.DisplayName.UserLocalizedLabel.Label
                                    && table.CanAuditBeChanged == mocks.accountMetadata.IsAuditEnabled.CanBeChanged
                                    && table.MetadataId == mocks.accountMetadata.MetadataId!.Value
            );
        var tableAuditSetting = changedAuditSettings.TableAuditSettings.First();
        tableAuditSetting.ColumnAuditSettings
            .Should()
            .ContainSingle(column => column.MetadataId == mocks.accountNameMetadata.MetadataId!.Value
                                     && column.IsAuditEnabled == false
                                     && column.WasAuditEnabledBefore == true
                                     && column.EntityLogicalName == mocks.accountNameMetadata.EntityLogicalName
                                     && column.DisplayName ==
                                     mocks.accountNameMetadata.DisplayName.UserLocalizedLabel.Label
                                     && column.LogicalName == mocks.accountNameMetadata.LogicalName
                                     && column.CanAuditBeChanged ==
                                     mocks.accountNameMetadata.IsAuditEnabled.CanBeChanged
                                     && column.TypeCode == mocks.accountNameMetadata.AttributeType);
    }

    private (Solution solution, Organization organization, EntityMetadata accountMetadata, SolutionComponent
        accountComponent, StringAttributeMetadata accountNameMetadata, LookupAttributeMetadata primaryContactMetadata,
        UniqueIdentifierAttributeMetadata accountIdMetadata) SetupMocks()
    {
        var solution = new Solution
        {
            Id = Guid.NewGuid(),
            UniqueName = SolutionName,
            FriendlyName = "Solution",
            Version = "1.1.1.1",
            [Solution.Fields.IsManaged] = false
        };
        _dataverseService.GetSolution(SolutionName).Returns(solution);

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            IsAuditEnabled = false,
            IsUserAccessAuditEnabled = false,
            AuditRetentionPeriodV2 = 180,
            UserAccessAuditingInterval = 4,
        };
        _dataverseService.GetOrganization().Returns(organization);
        _dataverseService.When(mock => mock
            .UpdateAuditingForOrganization(organization.Id, true)
        ).Do(_ => organization.IsAuditEnabled = true);
        _dataverseService.When(mock => mock
            .UpdateAuditingForOrganization(organization.Id, false)
        ).Do(_ => organization.IsAuditEnabled = false);
        _dataverseService.When(mock =>
            mock.UpdateUserAccessAuditing(organization.Id, true)
        ).Do(_ => organization.IsUserAccessAuditEnabled = true);
        _dataverseService.When(mock =>
            mock.UpdateUserAccessAuditing(organization.Id, false)
        ).Do(_ => organization.IsUserAccessAuditEnabled = false);

        var accountMetadata = new EntityMetadata
        {
            MetadataId = Guid.NewGuid(),
            LogicalName = "account",
            DisplayName = new Label
            {
                UserLocalizedLabel = new LocalizedLabel("Account", 1031)
            },
            IsAuditEnabled = new BooleanManagedProperty(false),
        };
        var contactMetadata = new EntityMetadata
        {
            MetadataId = Guid.NewGuid(),
            LogicalName = "contact",
            DisplayName = new Label
            {
                UserLocalizedLabel = new LocalizedLabel("Contact", 1031)
            },
            IsAuditEnabled = new BooleanManagedProperty(true)
        };
        var caseMetadata = new EntityMetadata
        {
            MetadataId = Guid.NewGuid(),
            LogicalName = "incident",
            DisplayName = new Label
            {
                UserLocalizedLabel = new LocalizedLabel("Case", 1031)
            },
            IsAuditEnabled = new BooleanManagedProperty(false)
            {
                CanBeChanged = false
            }
        };
        var caseComponent = new SolutionComponent
        {
            Id = Guid.NewGuid(),
            [SolutionComponent.Fields.SolutionId] = solution.ToEntityReference(),
            [SolutionComponent.Fields.ComponentType] = new OptionSetValue((int)componenttype.Entity),
            [SolutionComponent.Fields.ObjectId] = caseMetadata.MetadataId,
            [SolutionComponent.Fields.RootComponentBehavior] =
                new OptionSetValue((int)solutioncomponent_rootcomponentbehavior.IncludeAsShellOnly),
        };
        var contactComponent = new SolutionComponent
        {
            Id = Guid.NewGuid(),
            [SolutionComponent.Fields.SolutionId] = solution.ToEntityReference(),
            [SolutionComponent.Fields.ComponentType] = new OptionSetValue((int)componenttype.Entity),
            [SolutionComponent.Fields.ObjectId] = contactMetadata.MetadataId,
            [SolutionComponent.Fields.RootComponentBehavior] =
                new OptionSetValue((int)solutioncomponent_rootcomponentbehavior.IncludeAsShellOnly),
        };
        var accountComponent = new SolutionComponent
        {
            Id = Guid.NewGuid(),
            [SolutionComponent.Fields.SolutionId] = solution.ToEntityReference(),
            [SolutionComponent.Fields.ComponentType] = new OptionSetValue((int)componenttype.Entity),
            [SolutionComponent.Fields.ObjectId] = accountMetadata.MetadataId,
            [SolutionComponent.Fields.RootComponentBehavior] =
                new OptionSetValue((int)solutioncomponent_rootcomponentbehavior.IncludeAsShellOnly)
        };
        _dataverseService.GetEntityComponents(solution).Returns([accountComponent, contactComponent, caseComponent]);
        _dataverseService.GetEntityMetadata(accountComponent).Returns((accountMetadata, accountComponent));
        _dataverseService.GetEntityMetadata(contactComponent).Returns((contactMetadata, contactComponent));
        _dataverseService.GetEntityMetadata(caseComponent).Returns((caseMetadata, caseComponent));

        var accountNameMetadata = new StringAttributeMetadata(
        )
        {
            MetadataId = Guid.NewGuid(),
            IsAuditEnabled = new BooleanManagedProperty(true),
            DisplayName = new Label
            {
                UserLocalizedLabel = new LocalizedLabel("Name", 1031)
            },
            LogicalName = "name"
        };
        accountNameMetadata.SetSealedPropertyValue(nameof(StringAttributeMetadata.EntityLogicalName),
            accountMetadata.LogicalName);

        var accountIdMetadata = new UniqueIdentifierAttributeMetadata
        {
            MetadataId = Guid.NewGuid(),
            IsAuditEnabled = new BooleanManagedProperty(false)
            {
                CanBeChanged = false
            },
            DisplayName = new Label
            {
                UserLocalizedLabel = new LocalizedLabel("Account", 1031)
            },
            LogicalName = "accountid"
        };
        accountIdMetadata.SetSealedPropertyValue(nameof(UniqueIdentifierAttributeMetadata.EntityLogicalName),
            "account");

        var primaryContactMetadata = new LookupAttributeMetadata
        {
            MetadataId = Guid.NewGuid(),
            IsAuditEnabled = new BooleanManagedProperty(true),
            DisplayName = new Label
            {
                UserLocalizedLabel = new LocalizedLabel("Contact", 1031)
            },
            LogicalName = "primarycontactid"
        };
        var titleMetadata = new StringAttributeMetadata
        {
            MetadataId = Guid.NewGuid(),
            IsAuditEnabled = new BooleanManagedProperty(true),
            DisplayName = new Label
            {
                UserLocalizedLabel = new LocalizedLabel("Title", 1031)
            },
            LogicalName = "title"
        };
        var fullNameMetadata = new StringAttributeMetadata
        {
            MetadataId = Guid.NewGuid(),
            IsAuditEnabled = new BooleanManagedProperty(true),
            DisplayName = new Label
            {
                UserLocalizedLabel = new LocalizedLabel("Full Name", 1031)
            },
            LogicalName = "fullname"
        };
        primaryContactMetadata.SetSealedPropertyValue(nameof(LookupAttributeMetadata.EntityLogicalName), "account");
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        _dataverseService.UpdateEntityAuditingMetadata(default, true)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            .ReturnsForAnyArgs(info => Result<TableAuditSetting>.Ok(info.Args().First().As<TableAuditSetting>()))
            .AndDoes(_ => accountMetadata.IsAuditEnabled = new BooleanManagedProperty(true));
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        _dataverseService.UpdateEntityAuditingMetadata(default, false)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            .ReturnsForAnyArgs(info => Result<TableAuditSetting>.Ok(info.Args().First().As<TableAuditSetting>()))
            .AndDoes(_ => accountMetadata.IsAuditEnabled = new BooleanManagedProperty(false));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        _dataverseService.UpdateAttributeAuditMetadata(default, SolutionName, true)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            .ReturnsForAnyArgs(info => Result<ColumnAuditSetting>.Ok(info.Args().First().As<ColumnAuditSetting>()))
            .AndDoes(_ => accountNameMetadata.IsAuditEnabled = new BooleanManagedProperty(true));
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        _dataverseService.UpdateAttributeAuditMetadata(default, SolutionName, false)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            .ReturnsForAnyArgs(info => Result<ColumnAuditSetting>.Ok(info.Args().First().As<ColumnAuditSetting>()))
            .AndDoes(_ => accountNameMetadata.IsAuditEnabled = new BooleanManagedProperty(false));

        _dataverseService.GetAttributeComponents(accountComponent.ToEntityReference(), accountMetadata.LogicalName)
            .Returns([accountNameMetadata, accountIdMetadata, primaryContactMetadata]);
        _dataverseService.GetAttributeComponents(contactComponent.ToEntityReference(), contactMetadata.LogicalName)
            .Returns([fullNameMetadata]);
        _dataverseService.GetAttributeComponents(caseComponent.ToEntityReference(), caseMetadata.LogicalName)
            .Returns([titleMetadata]);
        return (solution, organization, accountMetadata, accountComponent, accountNameMetadata, primaryContactMetadata,
            accountIdMetadata);
    }
}
