
@echo off
setlocal

REM ===== Find MSBuild (VS2017/2019/2022) =====
set MSBUILD=
for %%V in (17.0,16.0,15.0) do (
  if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\%V%\Enterprise\MSBuild\%%V\Bin\MSBuild.exe" set MSBUILD="%ProgramFiles(x86)%\Microsoft Visual Studio\%V%\Enterprise\MSBuild\%%V\Bin\MSBuild.exe"
  if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\%V%\Professional\MSBuild\%%V\Bin\MSBuild.exe" set MSBUILD="%ProgramFiles(x86)%\Microsoft Visual Studio\%V%\Professional\MSBuild\%%V\Bin\MSBuild.exe"
  if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\%V%\Community\MSBuild\%%V\Bin\MSBuild.exe" set MSBUILD="%ProgramFiles(x86)%\Microsoft Visual Studio\%V%\Community\MSBuild\%%V\Bin\MSBuild.exe"
)

if "%MSBUILD%"=="" (
  echo MSBuild not found. Please install Visual Studio with MSBuild.
  exit /b 1
)

echo Using %MSBUILD%

REM ===== Restore & Build =====
%MSBUILD% ClassSummaryInline.csproj /t:Restore /p:Configuration=Release /m || exit /b 1
%MSBUILD% ClassSummaryInline.csproj /t:Rebuild /p:Configuration=Release /m || exit /b 1

echo.
echo === If build succeeds, VSIX should be in: ===
echo   %~dp0bin\Release\

echo Done.
