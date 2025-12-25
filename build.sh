#!/usr/bin/env bash
set -euo pipefail

rm -rf "$HOME/.cache/unity3d/" /tmp/GiCache/
rm -f ./Assets/**/*.{unity,meta}
rm -rf ./Assets/MainScene
rm -rf ./{.utmp,Library,Logs,Packages,ProjectSettings,Temp,UserSettings,Builds}

# Workaround https://github.com/mono/mono/issues/6752
export TERM=xterm

if ! "$UNITY" -batchmode -quit -logFile /dev/stdout -createProject "$PWD" $@; then
  echo "Project creation failed"
fi

if ! "$UNITY" -batchmode -quit -logFile /dev/stdout -projectPath "$PWD" -executeMethod TestGame.SetupAndBuild $@; then
  echo "Build failed"
fi
