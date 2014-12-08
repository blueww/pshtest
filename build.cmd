set PRODUCT=%1

cd azcopy
msbuild /t:Rebuild /p:Configuration=Debug AzCopy.sln
cd ..

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
cd Xplat
.\tools\windows\build.cmd
cd ..

:END