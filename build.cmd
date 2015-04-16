set PRODUCT=%1

# cd azcopy
# msbuild /t:Rebuild /p:Configuration=Debug AzCopy.sln
# cd ..

cd test
msbuild /t:Rebuild /p:Configuration=Debug CliTest.sln
cd ..


if [%PRODUCT%] == [xPlat] GOTO :xPlat
if [%PRODUCT%] == [PSH] GOTO :PSH

:PSH
cd PowerShell\tools
powershell -File BuildInstaller.ps1
cd ..\..

if [%PRODUCT%] == [] GOTO :xPlat
GOTO :END

:xPlat
pushd .
CALL .\Xplat\tools\windows\scripts\prepareRepoClone.cmd
popd
msbuild /t:rebuild /p:Configuration=Release .\Xplat\tools\windows\azure-cli.sln

:END
