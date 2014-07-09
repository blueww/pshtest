echo "update submodule azcopy"
cd azcopy
git fetch
git checkout origin/master
cd ..
echo "update submodule Powershell"
cd PowerShell
git fetch
git checkout origin/dev
cd ..
echo "update submodule xplat"
cd Xplat
git fetch
git checkout origin/dev
cd ..
git add -A
git commit -m "sync submodules to latest"