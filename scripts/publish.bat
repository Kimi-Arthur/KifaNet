rmdir -d publish
dotnet pack -o publish -c Release --include-symbols "%1"

if not exist "%~dp0nuget_key" (
    echo Error: "%~dp0nuget_key" not found. Please create it with your NuGet API key.
    exit /b 1
)
set /p key=<"%~dp0nuget_key"

dotnet nuget push publish\*.symbols.nupkg -s https://api.nuget.org/v3/index.json -k %key%

