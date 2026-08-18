#!/bin/bash
set -e

KEEP=${1:-10}
SERVER="kimi@kimily.ch"
SSH_CMD="ssh -p 2222 -o IdentitiesOnly=yes"

echo "Cleaning up old releases on $SERVER (keeping latest $KEEP)..."

$SSH_CMD "$SERVER" "bash -s -- $KEEP" <<'EOF'
KEEP_COUNT="${1:-10}"

cd /var/www || exit 1

# Find all 14-digit timestamped release directories
mapfile -t releases < <(find /var/www -maxdepth 1 -mindepth 1 -type d -name '20[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]' | sort)

total=${#releases[@]}
if [ "$total" -eq 0 ]; then
    echo "No timestamped release directories found."
    exit 0
fi

if [ "$total" -le "$KEEP_COUNT" ]; then
    echo "Found $total release(s), which is <= $KEEP_COUNT. No cleanup needed."
    exit 0
fi

remove_count=$((total - KEEP_COUNT))
echo "Found $total release(s). Removing oldest $remove_count release(s)..."

for (( i=0; i<remove_count; i++ )); do
    dir="${releases[i]}"
    if [ -n "$dir" ] && [ -d "$dir" ]; then
        echo "  Removing $dir"
        rm -rf "$dir"
    fi
done

echo "Cleanup complete. Retained latest $KEEP_COUNT releases."
EOF
