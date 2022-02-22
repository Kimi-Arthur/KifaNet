rm -rf web_api
dotnet publish -c Release -o web_api -r linux-x64 -p:PublishSingleFile=true --self-contained true src/Kifa.Web.Api/Kifa.Web.Api.csproj

ssh -p 2222 -t kimi@www.kimily.ga "sudo systemctl stop web_api.service"
scp -P 2222 -r web_api/Kifa.Web.Api* kimi@www.kimily.ga:/var/www/
ssh -p 2222 -t kimi@www.kimily.ga "sudo systemctl restart web_api.service"
