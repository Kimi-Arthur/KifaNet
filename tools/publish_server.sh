rm -rf web_api
dotnet publish -c Release -r linux-x64 --self-contained false -o web_api src/Kifa.Web.Api/Kifa.Web.Api.csproj

version=$(date -u +%Y%m%d%H%M%S)
server="kimi@kimily.ch"

ssh -p 2222 -t $server "cp -ar /var/www/latest /var/www/$version"
rsync --rsh='ssh -p2222' -vrlpic web_api/* $server:/var/www/$version
ssh -p 2222 -t $server "sudo systemctl stop web_api.service; cp -ar /var/www/$version/* /var/www/latest; sudo systemctl restart web_api.service"
