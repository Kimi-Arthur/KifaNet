rmdir /s /q publish
dotnet pack -o publish -c Release --include-symbols %1
dotnet nuget push publish\*.symbols.nupkg -s https://api.nuget.org/v3/index.json -k oy2jqpkcvxsj2eudjriaxwmimioty73u7atgnf3wpukr3u
