cd azcopy
msbuild /t:Rebuild /p:Configuration=Debug AzCopy.sln
cd ..

cd test
msbuild /t:Rebuild /p:Configuration=Debug CliTest.sln
cd ..

cd Xplat
.\tools\windows\build.cmd
cd ..