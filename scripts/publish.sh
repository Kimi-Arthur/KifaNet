rm -rf publish
dotnet pack -o publish -c Release --include-symbols "$1"
dotnet nuget push publish/*.symbols.nupkg -s https://api.nuget.org/v3/index.json -k oy2matfsxzjesq6zvqsq7g4m62wafpsebh4lnncf6aac2a
