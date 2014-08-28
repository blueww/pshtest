echo "update submodule azcopy"
cd azcopy
git fetch
git reset origin/v3.0 --hard
cd ..
echo "update submodule Powershell"
cd PowerShell
git fetch
git reset origin/dev --hard
cd ..
echo "update submodule xplat"
cd Xplat
git fetch
git reset origin/dev --hard
cd ..
git add -A
git commit -m "sync submodules to latest"