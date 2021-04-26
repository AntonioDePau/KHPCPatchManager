@echo off
set "msbuild=C:\Program Files (x86)\MSBuild\14.0\Bin\msbuild" 
set "msbuild=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\msbuild"
"%msbuild%" KHPCPatchManager.csproj
pause