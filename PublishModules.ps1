$scriptFolder = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
. ($scriptFolder + '.\PowerShell\Tools\SetupEnv.ps1')

$PACKAGEPATH=[System.Environment]::GetEnvironmentVariable("PACKAGEPATH")

mkdir $PACKAGEPATH\package

$repoName = [System.Guid]::NewGuid().ToString()

Register-PSRepository -Name $repoName -SourceLocation $PACKAGEPATH -PublishLocation $PACKAGEPATH/package -InstallationPolicy Trusted

[System.Environment]::SetEnvironmentVariable("NuGetPublishingSource", $PACKAGEPATH, "Process")

"$env:AzurePSRoot\tools\PublishModules.ps1" "Debug" ServiceManagement a253d1a5-21a2-45a5-ab83-d489611b5dc7 $PACKAGEPATH

msbuild "$env:AzurePSRoot\build.proj" /t:Publish /p:NuGetKey=a253d1a5-21a2-45a5-ab83-d489611b5dc7

UnRegister-PSRepository -Name $repoName