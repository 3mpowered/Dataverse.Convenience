if (-Not(Get-Command "pac" -errorAction SilentlyContinue))
{
    Write-Error "This script requires the Power Platform CLI. See https://learn.microsoft.com/en-us/power-platform/developer/cli/introduction for more information."
    exit $exitCode
}

Remove-Item $PSScriptRoot\Entities -Recurse -ErrorAction SilentlyContinue
Remove-Item $PSScriptRoot\OptionSets -Recurse -ErrorAction SilentlyContinue
Remove-Item $PSScriptRoot\Messages -Recurse -ErrorAction SilentlyContinue
Remove-Item $PSScriptRoot\DataverseServiceContext.cs -ErrorAction SilentlyContinue
Remove-Item $PSScriptRoot\EntityOptionSetEnum.cs -ErrorAction SilentlyContinue

& pac auth select --name "empwrd"

$settingFilePath = Join-Path -Path $PSScriptRoot -ChildPath "builderSettings.json"
pac modelbuilder build --settingsTemplateFile $settingFilePath  --outdirectory $PSScriptRoot --logLevel "Information"
