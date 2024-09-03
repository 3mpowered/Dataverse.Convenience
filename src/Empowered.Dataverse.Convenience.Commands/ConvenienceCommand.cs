using CommandDotNet;

namespace Empowered.Dataverse.Convenience.Commands;

public class ConvenienceCommand
{
    [Subcommand(RenameAs = "auditing")] public AuditingCommand Auditing { get; set; }
}
