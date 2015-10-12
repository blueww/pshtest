$scriptFolder = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
. ($scriptFolder + '.\PowerShell\Tools\SetupEnv.ps1')

$PACKAGEPATH=[System.Environment]::GetEnvironmentVariable("PACKAGEPATH")

mkdir $PACKAGEPATH\package

[System.Environment]::SetEnvironmentVariable("NuGetPublishingSource", $PACKAGEPATH, "Process")

msbuild "$env:AzurePSRoot\build.proj" /t:Publish /p:NuGetKey=a253d1a5-21a2-45a5-ab83-d489611b5dc7