rm -rf web_api
dotnet publish -c Release -o web_api -r linux-x64 -p:PublishSingleFile=true --self-contained true src/Kifa.Web.Api/Kifa.Web.Api.csproj

version=$(date -u +%Y%m%d%H%M%S)
server="kimi@kimily.ch"

ssh -p 2222 -t $server "mkdir /var/www/$version/"
scp -P 2222 -r web_api/Kifa.Web.Api* $server:/var/www/$version/
ssh -p 2222 -t $server "sudo systemctl stop web_api.service; cp /var/www/$version/* /var/www/; sudo systemctl restart web_api.service"
