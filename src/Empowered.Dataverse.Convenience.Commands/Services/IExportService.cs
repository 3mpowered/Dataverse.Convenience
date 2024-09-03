using Empowered.Dataverse.Convenience.Commands.Arguments;

namespace Empowered.Dataverse.Convenience.Commands.Services;

public interface IExportService
{
    FileInfo Export<TExportableType>(TExportableType exportData, ExportFormat format,
        DirectoryInfo targetDirectory, string fileName);
}
