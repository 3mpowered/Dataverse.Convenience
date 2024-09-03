using Empowered.Dataverse.Convenience.Auditing.Model;

namespace Empowered.Dataverse.Convenience.Auditing;

public interface IAuditingService
{
    AuditSettings Get(string solutionName, bool handleAllAttributes = false);

    ChangedAuditSettings Enable(string solutionName, bool handleAllAttributes, bool ignoreUserAccess = true,
        bool publish = true);

    ChangedAuditSettings Disable(string solutionName, bool handleAllAttributes, bool ignoreUserAccess = true,
        bool publish = true);
}
