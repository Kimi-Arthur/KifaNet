#!/bin/bash
set -e

CLEAN_LATEST=true
KEEP_RELEASES=10

while [[ $# -gt 0 ]]; do
  case $1 in
    --no-clean|--skip-clean)
      CLEAN_LATEST=false
      shift
      ;;
    --keep)
      KEEP_RELEASES="$2"
      shift 2
      ;;
    *)
      shift
      ;;
  esac
done

rm -rf web_api
dotnet publish -c Release -r linux-x64 --self-contained false -o web_api src/Kifa.Web.Api/Kifa.Web.Api.csproj

version=$(date -u +%Y%m%d%H%M%S)
server="kimi@kimily.ch"

ssh -p 2222 -o IdentitiesOnly=yes -t $server "mkdir -p /var/www/$version"
rsync --rsh='ssh -p2222 -o IdentitiesOnly=yes' -vrlpic --delete web_api/ $server:/var/www/$version/

if [ "$CLEAN_LATEST" = true ]; then
  ssh -p 2222 -o IdentitiesOnly=yes -t $server "sudo systemctl stop web_api.service; rm -rf /var/www/latest/*; cp -ar /var/www/$version/* /var/www/latest/; sudo systemctl restart web_api.service"
else
  ssh -p 2222 -o IdentitiesOnly=yes -t $server "sudo systemctl stop web_api.service; cp -ar /var/www/$version/* /var/www/latest; sudo systemctl restart web_api.service"
fi

if [ "$KEEP_RELEASES" -gt 0 ]; then
  "$(dirname "$0")/cleanup_releases.sh" "$KEEP_RELEASES"
fi
