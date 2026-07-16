rm -rf publish
dotnet pack -o publish -c Release --include-symbols "$1"

key_file="$(dirname "$0")/nuget_key"
if [ ! -f "$key_file" ]; then
    echo "Error: $key_file not found. Please create it with your NuGet API key."
    exit 1
fi
key=$(cat "$key_file")

dotnet nuget push publish/*.symbols.nupkg -s https://api.nuget.org/v3/index.json -k "$key"

