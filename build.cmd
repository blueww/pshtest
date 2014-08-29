cd azcopy
msbuild /t:Rebuild /p:Configuration=Debug AzCopy.sln
cd ..

cd test
msbuild /t:Rebuild /p:Configuration=Debug CliTest.sln
cd ..

cd PowerShell\tools
powershell -File BuildInstaller.ps1
cd ..\..

cd Xplat
.\tools\windows\build.cmd
cd ..