using System.Runtime.CompilerServices;
using CommandDotNet;
using Empowered.CommandLine.Extensions.Extensions;
using Empowered.Dataverse.Convenience.Auditing;
using Empowered.Dataverse.Convenience.Auditing.Extensions;
using Empowered.Dataverse.Convenience.Auditing.Model;
using Empowered.Dataverse.Convenience.Commands.Arguments;
using Empowered.Dataverse.Convenience.Commands.Services;
using Spectre.Console;

namespace Empowered.Dataverse.Convenience.Commands;

public class AuditingCommand(
    IAnsiConsole console,
    IAuditingService auditingService,
    IExportService exportService)
{
    public async Task<int> Enable(ChangeAuditingArguments arguments)
    {
        ChangedAuditSettings? changedAuditSettings = null;
        console.Status().Start("Enable auditing ...", context =>
        {
            changedAuditSettings = auditingService.Enable(
                arguments.Solution,
                arguments.HandleAllAttributes,
                arguments.IgnoreUserAccess,
                arguments.Publish);
        });

        if (changedAuditSettings == null)
        {
            return await ExitCodes.Error;
        }

        var successfullyEnabledTables = changedAuditSettings.TableAuditSettings
            .Where(table => table is { WasAuditEnabledBefore: false, IsAuditEnabled: true })
            .ToList();
        var failedTables = changedAuditSettings.TableAuditSettings
            .Where(table => !string.IsNullOrWhiteSpace(table.ErrorMessage))
            .ToList();
        var unchangedTables = changedAuditSettings.TableAuditSettings
            .Where(table => table is { IsAuditEnabled: true, WasAuditEnabledBefore: true }).ToList();
        console.Success(
            $"Enabled auditing for {successfullyEnabledTables.Count}/{changedAuditSettings.TableAuditSettings.Count} tables. Auditing was already enabled for {unchangedTables.Count} tables.");


        if (failedTables.Any())
        {
            console.Warning($"Enabling auditing failed for {failedTables.Count} tables:");
            var failedEntityTable = new Table
            {
                Title = new TableTitle("Tables"),
                BorderStyle = Style.Plain
            };
            failedEntityTable.AddColumns("Display Name", "Logical Name", "Error Message");
            foreach (var table in failedTables)
            {
                failedEntityTable.AddRow(table.DisplayName.EscapeMarkup(), table.LogicalName.EscapeMarkup(),
                    (table.ErrorMessage ?? string.Empty).EscapeMarkup());
            }

            console.WriteLine();
            console.Write(failedEntityTable);
        }

        var failedColumns = changedAuditSettings.TableAuditSettings
            .SelectMany(table => table.ColumnAuditSettings)
            .Where(column => !string.IsNullOrWhiteSpace(column.ErrorMessage))
            .OrderBy(column => column.EntityLogicalName)
            .ThenBy(column => column.LogicalName)
            .ToList();

        if (failedColumns.Any())
        {
            console.Warning($"Enabling auditing failed for {failedColumns.Count} columns:");
            var failedColumnsTable = new Table
            {
                Title = new TableTitle("Columns"),
                BorderStyle = Style.Plain
            };
            failedColumnsTable.AddColumns("Entity", "Column", "Error Message");

            foreach (var column in failedColumns)
            {
                failedColumnsTable.AddRow(column.EntityLogicalName.EscapeMarkup(), column.LogicalName.EscapeMarkup(),
                    (column.ErrorMessage ?? string.Empty).EscapeMarkup());
            }

            console.WriteLine();
            console.Write(failedColumnsTable);
        }

        ExportOutputs(arguments, changedAuditSettings);

        return await ExitCodes.Success;
    }

    public async Task<int> Disable(ChangeAuditingArguments arguments)
    {
        ChangedAuditSettings? changedAuditSettings = null;
        console.Status().Start("Disable auditing ...", context =>
        {
            changedAuditSettings = auditingService.Disable(
                arguments.Solution,
                arguments.HandleAllAttributes,
                arguments.IgnoreUserAccess,
                arguments.Publish);
        });

        if (changedAuditSettings == null)
        {
            return await ExitCodes.Error;
        }

        var successfullyEnabledTables = changedAuditSettings.TableAuditSettings
            .Where(table => table is { WasAuditEnabledBefore: true, IsAuditEnabled: false })
            .ToList();
        var failedTables = changedAuditSettings.TableAuditSettings
            .Where(table => !string.IsNullOrWhiteSpace(table.ErrorMessage))
            .ToList();
        var unchangedTables = changedAuditSettings.TableAuditSettings
            .Where(table => table is { IsAuditEnabled: true, WasAuditEnabledBefore: true }).ToList();
        console.Success(
            $"Disabled auditing for {successfullyEnabledTables.Count}/{changedAuditSettings.TableAuditSettings.Count} tables. Auditing was already disabled for {unchangedTables.Count} tables.");

        if (failedTables.Any())
        {
            console.Warning($"Disabling auditing failed for {failedTables.Count} tables:");
            var failedEntityTable = new Table
            {
                Title = new TableTitle("Tables"),
                BorderStyle = Style.Plain
            };
            failedEntityTable.AddColumns("Display Name", "Logical Name", "Error Message");
            foreach (var table in failedTables)
            {
                failedEntityTable.AddRow(table.DisplayName.EscapeMarkup(), table.LogicalName.EscapeMarkup(),
                    (table.ErrorMessage ?? string.Empty).EscapeMarkup());
            }

            console.WriteLine();
            console.Write(failedEntityTable);
        }

        var failedColumns = changedAuditSettings.TableAuditSettings
            .SelectMany(table => table.ColumnAuditSettings)
            .Where(column => !string.IsNullOrWhiteSpace(column.ErrorMessage))
            .OrderBy(column => column.EntityLogicalName)
            .ThenBy(column => column.LogicalName)
            .ToList();

        if (failedColumns.Any())
        {
            console.Warning($"Enabling auditing failed for {failedColumns.Count} columns:");
            var failedColumnsTable = new Table
            {
                Title = new TableTitle("Columns"),
                BorderStyle = Style.Plain
            };
            failedColumnsTable.AddColumns("Entity", "Column", "Error Message");

            foreach (var column in failedColumns)
            {
                failedColumnsTable.AddRow(column.EntityLogicalName.EscapeMarkup(), column.LogicalName.EscapeMarkup(),
                    (column.ErrorMessage ?? string.Empty).EscapeMarkup());
            }

            console.WriteLine();
            console.Write(failedColumnsTable);
        }

        ExportOutputs(arguments, changedAuditSettings);

        return await ExitCodes.Success;
    }

    public async Task<int> List(AuditingArguments arguments)
    {
        AuditSettings? auditSettings = null;
        console.Status().Start("Retrieve audit settings ...",
            context => { auditSettings = auditingService.Get(arguments.Solution, arguments.HandleAllAttributes); });

        if (auditSettings == null)
        {
            return await ExitCodes.Error;
        }

        PrintGlobalAuditTable(auditSettings);
        console.WriteLine();

        if (!auditSettings.TableAuditSettings.Any())
        {
            console.Warning($"Couldn't found any tables in solution {arguments.Solution.Italic()}");
            return await ExitCodes.Success;
        }

        console.Success(
            $"Found {auditSettings.TableAuditSettings.Count} tables in solution {arguments.Solution.Italic()}:");

        PrintEntityAuditTable(auditSettings);
        PrintColumnAuditTables(auditSettings);
        ExportOutputs(arguments, auditSettings);

        return await ExitCodes.Success;
    }

    private void ExportOutputs<TOutputType>(AuditingArguments arguments, TOutputType changedAuditSettings,
        [CallerMemberName] string callingMethod = "")
    {
        if (arguments.ExportFormat == ExportFormat.Json)
        {
            var exportFile = exportService.Export(changedAuditSettings, arguments.ExportFormat,
                arguments.ExportDirectory, $"auditing-{callingMethod.ToLowerInvariant()}");
            console.Success($"Exported the audit results to file: {exportFile.FullName.EscapeMarkup().Link()}");
        }
    }

    private void PrintColumnAuditTables(AuditSettings auditSettings)
    {
        foreach (var tableData in auditSettings.TableAuditSettings)
        {
            var attributeTable = new Table
            {
                Title = new TableTitle(tableData.LogicalName.EscapeMarkup()),
                Border = TableBorder.None,
            };

            attributeTable.AddColumns("Attribute", "Logical Name", "Type");
            attributeTable.AddColumn(new TableColumn("Is Audit Enabled").Centered());

            foreach (var column in tableData.ColumnAuditSettings)
            {
                attributeTable.AddRow(
                    column.DisplayName.EscapeMarkup(),
                    column.LogicalName.EscapeMarkup(),
                    column.TypeCode.Format().EscapeMarkup(),
                    column.IsAuditEnabled ? "x" : string.Empty
                );
            }

            console.WriteLine();
            console.Write(attributeTable);
        }
    }

    private void PrintEntityAuditTable(AuditSettings auditSettings)
    {
        var entityTable = new Table
        {
            Title = new TableTitle("Entities"),
            Border = TableBorder.None,
        };
        entityTable.AddColumns("Entity", "Logical Name", "Solution Behaviour", "Attribute Count");
        entityTable.AddColumn(new TableColumn("Is Audit Enabled").Centered());

        foreach (var tableData in auditSettings.TableAuditSettings)
        {
            entityTable.AddRow(
                tableData.DisplayName.EscapeMarkup(),
                tableData.LogicalName.EscapeMarkup(),
                tableData.Behaviour.Format().EscapeMarkup(),
                tableData.ColumnAuditSettings.Count.ToString(),
                tableData.IsAuditEnabled ? "x" : string.Empty
            );
        }

        console.Write(entityTable);
    }

    private void PrintGlobalAuditTable(AuditSettings auditSettings)
    {
        var globalTable = new Table
        {
            Title = new TableTitle("Global Audit Settings"),
            Border = TableBorder.None
        };
        globalTable.AddColumns(
            new TableColumn("Setting"),
            new TableColumn("Value")
        );
        globalTable.AddRow(
            "Is Audit Enabled",
            auditSettings.IsAuditEnabled ? "yes" : "no"
        );
        globalTable.AddRow(
            "Audit Retention Period",
            $"{auditSettings.AuditRetentionPeriod} days".EscapeMarkup()
        );
        globalTable.AddRow(
            "Is User Access Audit Enabled",
            auditSettings.IsUserAccessAuditEnabled ? "yes" : "no"
        );
        globalTable.AddRow(
            "User Access Audit Retention Period",
            $"{auditSettings.UserAccessRetentionPeriod} days".EscapeMarkup()
        );

        console.Write(globalTable);
    }
}
