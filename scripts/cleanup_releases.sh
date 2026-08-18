#!/bin/bash
set -e

KEEP=${1:-10}
SERVER="kimi@kimily.ch"
SSH_CMD="ssh -p 2222 -o IdentitiesOnly=yes"

echo "Cleaning up old releases on $SERVER (keeping latest $KEEP)..."

$SSH_CMD "$SERVER" bash -s <<EOF
cd /var/www || exit 1

releases=\$(ls -d 20[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9] 2>/dev/null | sort -n || true)

total=\$(echo "\$releases" | grep -c . || true)
if [ "\$total" -le "$KEEP" ]; then
    echo "Found \$total release(s), which is <= $KEEP. No cleanup needed."
    exit 0
fi

remove_count=\$((total - KEEP))
echo "Found \$total releases. Removing oldest \$remove_count release(s)..."

echo "\$releases" | head -n "\$remove_count" | while read -r dir; do
    if [ -n "\$dir" ] && [ -d "\$dir" ]; then
        echo "  Removing /var/www/\$dir"
        rm -rf "\$dir"
    fi
done

echo "Cleanup complete. Retained latest $KEEP releases."
EOF
