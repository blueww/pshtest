$scriptFolder = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
. ($scriptFolder + '.\PowerShell\Tools\SetupEnv.ps1')

$PACKAGEPATH=[System.Environment]::GetEnvironmentVariable("PACKAGEPATH")

mkdir $PACKAGEPATH\package

copy -r $PACKAGEPATH\..\..\0package0 $PACKAGEPATH

$repoName = [System.Guid]::NewGuid().ToString()

Register-PSRepository -Name $repoName -SourceLocation $PACKAGEPATH -PublishLocation $PACKAGEPATH/package -InstallationPolicy Trusted

[System.Environment]::SetEnvironmentVariable("NuGetPublishingSource", $PACKAGEPATH, "Process")

msbuild "$env:AzurePSRoot\build.proj" /t:Publish /p:NuGetKey=a253d1a5-21a2-45a5-ab83-d489611b5dc7

UnRegister-PSRepository -Name $repoName

$FileExists = Test-Path $PACKAGEPATH\0package0

if ($FileExists -eq $True) {
    del -r -force $PACKAGEPATH\0package0
}