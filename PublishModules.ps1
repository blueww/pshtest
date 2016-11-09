
$PACKAGEPATH=[System.Environment]::GetEnvironmentVariable("PACKAGEPATH")

mkdir $PACKAGEPATH\package

# Add nuget.exe path to current path as .\PowerShell\tools\PublishModules.ps1 need it
$nugetPath = Convert-Path .\PowerShell\tools;
if ($env:Path -notcontains $nugetPath)
{
    $env:Path = $env:Path + ";" + ($nugetPath)
}

.\PowerShell\tools\PublishModules.ps1 debug All a253d1a5-21a2-45a5-ab83-d489611b5dc7 $PACKAGEPATH