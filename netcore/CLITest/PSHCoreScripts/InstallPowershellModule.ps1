
$PackagePath=$1

echo $0

# Register-PSRepository –Name AzurePowerShellNetCore1 –SourceLocation “$PackagePathage” –PublishLocation “$PackagePath” –InstallationPolicy Trusted

# Install-Module Az.Profile –Repository AzurePowerShellNetCore1 –AllowPrerelease –AllowClobber –Force

# Install-Module Az.Storage –Repository AzurePowerShellNetCore1 –AllowPrerelease –AllowClobber –Force