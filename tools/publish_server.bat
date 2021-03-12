rmdir /s /q web_api
dotnet publish -c Release -o web_api -r linux-x64 -p:PublishSingleFile=true --self-contained true src/Kifa.Web.Api/Kifa.Web.Api.csproj

ssh -t kimi@www.kifa.ga "sudo systemctl stop web_api.service"
scp -r web_api/Kifa.Web.Api kimi@www.kifa.ga:~/
ssh -t kimi@www.kifa.ga "sudo systemctl restart web_api.service"
