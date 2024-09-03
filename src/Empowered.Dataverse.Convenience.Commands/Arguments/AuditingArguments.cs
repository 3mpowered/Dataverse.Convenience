using CommandDotNet;

namespace Empowered.Dataverse.Convenience.Commands.Arguments;

public class AuditingArguments : IArgumentModel
{
    [Option(Description = "The unique name of the solution to operate on.")]
    public required string Solution { get; set; }

    [Option(Description =
        "A flag to define if all attributes of an solution should be handled or only the added ones.")]
    public required bool HandleAllAttributes { get; set; }

    [Option(Description = "Export the output of the command to a specific format. Defaults to none.")]
    public required ExportFormat ExportFormat { get; set; } = ExportFormat.None;

    [Option(Description = "Directory to export the output to. Defaults to invoking directory.")]
    public DirectoryInfo ExportDirectory { get; set; } = new DirectoryInfo(Environment.CurrentDirectory);
}

public enum ExportFormat
{
    None,
    Json,
    Csv
}
