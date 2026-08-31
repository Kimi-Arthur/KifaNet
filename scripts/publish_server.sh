#!/bin/bash
set -e

CLEAN_LATEST=false
KEEP_RELEASES=10
REVERT=false
REVERT_VERSION=""
SERVER="kimi@kimily.ch"
SSH_CMD="ssh -p 2222 -o IdentitiesOnly=yes"

while [[ $# -gt 0 ]]; do
  case $1 in
    -c|--clean)
      CLEAN_LATEST=true
      shift
      ;;
    --no-clean|--skip-clean)
      CLEAN_LATEST=false
      shift
      ;;
    --keep)
      KEEP_RELEASES="$2"
      shift 2
      ;;
    -r|--revert|--rollback)
      REVERT=true
      if [[ -n "$2" && "$2" != -* ]]; then
        REVERT_VERSION="$2"
        shift 2
      else
        shift
      fi
      ;;
    --revert=*|--rollback=*)
      REVERT=true
      REVERT_VERSION="${1#*=}"
      shift
      ;;
    -h|--help)
      echo "Usage: $0 [options]"
      echo ""
      echo "Options:"
      echo "  -r, --revert [version]    Revert to a previous release (default: immediately preceding release)"
      echo "  -c, --clean               Clean /var/www/latest/* before copying release files"
      echo "  --no-clean, --skip-clean  Do not clean /var/www/latest/* before copying (default)"
      echo "  --keep <count>            Number of releases to retain after publish (default: 10, 0 to skip)"
      echo "  -h, --help                Show this help message"
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      exit 1
      ;;
  esac
done

if [ "$REVERT" = true ]; then
  echo "Reverting server deployment on $SERVER..."
  $SSH_CMD -t "$SERVER" bash -c "'
set -e
target_arg=\"\$1\"
clean_latest=\"\$2\"

cd /var/www || exit 1
releases=()
while IFS= read -r line; do
  [ -n \"\$line\" ] && releases+=(\"\$line\")
done < <(find /var/www -maxdepth 1 -mindepth 1 -type d -name \"20[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]\" | sort)
total=\${#releases[@]}

if [ -n \"\$target_arg\" ]; then
  target_ver=\$(basename \"\$target_arg\")
  target_dir=\"/var/www/\$target_ver\"
  if [ ! -d \"\$target_dir\" ]; then
    echo \"Error: Release directory \$target_dir does not exist.\" >&2
    if [ \"\$total\" -gt 0 ]; then
      echo \"Available releases in /var/www:\" >&2
      printf \"  %s\n\" \"\${releases[@]}\" >&2
    fi
    exit 1
  fi
else
  if [ \"\$total\" -lt 2 ]; then
    echo \"Error: Cannot revert automatically. Found \$total release(s) in /var/www (need at least 2).\" >&2
    if [ \"\$total\" -eq 1 ]; then
      echo \"Only available release is: \${releases[0]}\" >&2
    fi
    exit 1
  fi
  target_dir=\"\${releases[\$((total-2))]}\"
  target_ver=\$(basename \"\$target_dir\")
fi

echo \"Reverting to release: \$target_ver (\$target_dir)...\"
echo \"Stopping web_api.service...\"
sudo systemctl stop web_api.service

if [ \"\$clean_latest\" = \"true\" ]; then
  echo \"Cleaning /var/www/latest/*...\"
  rm -rf /var/www/latest/*
  cp -ar \"\$target_dir\"/* /var/www/latest/
else
  echo \"Copying \$target_dir/* to /var/www/latest...\"
  cp -ar \"\$target_dir\"/* /var/www/latest
fi

echo \"Restarting web_api.service...\"
sudo systemctl restart web_api.service
echo \"Successfully reverted to release \$target_ver.\"
'" bash "$REVERT_VERSION" "$CLEAN_LATEST"
  exit 0
fi

rm -rf web_api
dotnet publish -c Release -r linux-x64 --self-contained false -o web_api src/Kifa.Web.Api/Kifa.Web.Api.csproj

version=$(date -u +%Y%m%d%H%M%S)

$SSH_CMD -t "$SERVER" "mkdir -p /var/www/$version"
rsync --rsh='ssh -p2222 -o IdentitiesOnly=yes' -vrlpic --delete web_api/ "$SERVER:/var/www/$version/"

if [ "$CLEAN_LATEST" = true ]; then
  $SSH_CMD -t "$SERVER" "sudo systemctl stop web_api.service; rm -rf /var/www/latest/*; cp -ar /var/www/$version/* /var/www/latest/; sudo systemctl restart web_api.service"
else
  $SSH_CMD -t "$SERVER" "sudo systemctl stop web_api.service; cp -ar /var/www/$version/* /var/www/latest; sudo systemctl restart web_api.service"
fi

if [ "$KEEP_RELEASES" -gt 0 ]; then
  "$(dirname "$0")/cleanup_releases.sh" "$KEEP_RELEASES"
fi
