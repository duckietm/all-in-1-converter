@echo off
REM Publish Windows x64 single-file self-contained binary.
REM Output: publish\win-x64\Habbo Downloader.exe (+ Tools\ffdec\ kept as Content)

setlocal
set CONFIG=Release
set RID=win-x64
set OUT=%~dp0publish\%RID%

if exist "%OUT%" rmdir /s /q "%OUT%"

pushd "%~dp0SourceCode"
dotnet publish "Habbo Downloader.csproj" ^
    -c %CONFIG% ^
    -r %RID% ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -o "%OUT%"
popd

echo.
echo Published Windows build at: %OUT%
echo Run: "%OUT%\Habbo Downloader.exe"
