#!/usr/bin/env bash
set -euo pipefail

rm -rf ./{Library,ProjectSettings,Builds}
rm -f ./**/*.{unity,meta}

# Workaround https://github.com/mono/mono/issues/6752
export TERM=xterm

"$UNITY" -batchmode -nographics -quit -logFile - -createProject "$PWD" -executeMethod TestGame.SetupAndBuild $@
