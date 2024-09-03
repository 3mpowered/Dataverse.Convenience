using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Empowered.Dataverse.Convenience.Commands.Arguments;
using Microsoft.Extensions.Logging;

namespace Empowered.Dataverse.Convenience.Commands.Services;

internal class ExportService(ILogger<ExportService> logger) : IExportService
{
    public FileInfo Export<TExportableType>(TExportableType exportData, ExportFormat format,
        DirectoryInfo targetDirectory, string fileName)
    {
        logger.LogDebug(
            "Export data of type {ExportType} as format {ExportFormat} to target directory {TargetDirectory} with file name {FileName}",
            typeof(TExportableType).Name, format, targetDirectory, fileName);

        if (!targetDirectory.Exists)
        {
            targetDirectory.Create();
        }

        var exportFile = format switch
        {
            ExportFormat.Json => ExportAsJson(exportData, targetDirectory, fileName),
            ExportFormat.Csv => throw new NotImplementedException("CSV export is not implemented yet"),
            ExportFormat.None => throw new ArgumentException($"Invalid Export Format: {format}", nameof(format)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        logger.LogDebug("Exported data of type {ExportType} in format {ExportFormat} to file {ExportFile}",
            typeof(TExportableType).Name, format, exportFile);
        return exportFile;
    }

    private FileInfo ExportAsJson<TExportableType>(TExportableType exportData, DirectoryInfo targetDirectory,
        string fileName)
    {
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        });
        logger.LogDebug("Serialized type {Type} to json: {Json}", typeof(TExportableType).Name, json);

        var exportFilePath = Path.Join(targetDirectory.FullName, $"{fileName}.json");
        File.WriteAllText(exportFilePath, json, Encoding.UTF8);
        logger.LogDebug("Exported json {Json} to path {FilePath}", json, exportFilePath);
        return new FileInfo(exportFilePath);
    }
}
