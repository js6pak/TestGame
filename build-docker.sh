#!/usr/bin/env bash
set -euo pipefail

IMAGE_NAME=localhost/js6pak/ubuntu:full-24.04

if [ -z "$(sudo docker images -q "$IMAGE_NAME" 2> /dev/null)" ]; then
  echo "Image not found"
  exit 1
fi

UNITY_DIR="$(dirname "$UNITY")"
LICENSE_DIR="$HOME/.local/share/unity3d/Unity"

sudo docker run --user 1000:1000 \
  -e "GITHUB_ACTIONS=1" \
  -e "UNITY=$UNITY" -v "$UNITY_DIR":"$UNITY_DIR":ro \
  -v "/etc/machine-id:/etc/machine-id":ro \
  -v "$LICENSE_DIR:/home/packer/.local/share/unity3d/Unity":ro \
  -v "$PWD":"$PWD" -w "$PWD" \
  --rm -it "$IMAGE_NAME" \
  xvfb-run ./build.sh
