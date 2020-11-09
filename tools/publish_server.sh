rm -rf publish
dotnet publish -o publish/ServerCore -c Release src/Pimix.Web.Api/Pimix.Web.Api.csproj
scp -r publish/ServerCore kimi@www.kifa.ga:~/
ssh -t kimi@www.kifa.ga "sudo systemctl restart server-core.service"
