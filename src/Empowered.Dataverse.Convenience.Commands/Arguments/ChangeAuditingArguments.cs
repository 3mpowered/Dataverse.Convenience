using CommandDotNet;

namespace Empowered.Dataverse.Convenience.Commands.Arguments;

public class ChangeAuditingArguments : AuditingArguments
{
    [Option(Description = "A flag to publish the auditing changes. Defaults to yes.")]
    public required bool Publish { get; init; } = true;

    [Option(Description = "A flag to ignore the user access audit flag when changing the auditing settings.")]
    public bool IgnoreUserAccess { get; set; }
}
