
$PACKAGEPATH=[System.Environment]::GetEnvironmentVariable("PACKAGEPATH")

mkdir $PACKAGEPATH\package

Pushd PowerShell\Tools

# Add nuget.exe path to current path as .\PowerShell\tools\PublishModules.ps1 need it
$nugetPath = Convert-Path .;
if ($env:Path -notlike "*" + $nugetPath + "*")
{
    $env:Path = $env:Path + ";" + ($nugetPath)
}
echo "$env:Path: " + $env:Path 

.\PublishModules.ps1 debug All a253d1a5-21a2-45a5-ab83-d489611b5dc7 $PACKAGEPATH

popd
popd