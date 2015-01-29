git submodule init
git submodule update

echo "update submodule azcopy"
cd azcopy
git fetch
git reset origin/cli_base --hard
cd ..
echo "update submodule Powershell"
cd PowerShell
git fetch
git reset origin/next --hard
cd ..
echo "update submodule xplat"
cd Xplat
git fetch
git reset origin/next --hard
cd ..

if [%1] == [JENKINS] GOTO :JENKINS

:LOCAL
git add -A
git commit -m "sync submodules to latest"
GOTO :END

:JENKINS
powershell.exe "test\internal\scripts\InjectBuildNumber.ps1"
git add -A
git commit -m "dmshbld update submodules and increase build number to %BUILD_NUMBER%"

:END
