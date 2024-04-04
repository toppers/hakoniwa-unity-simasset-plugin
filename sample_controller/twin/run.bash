#!/bin/bash

cleanup() {
  echo "SIGINT received, stopping the background process..."
  kill -9 $bg_pid
}

ASSET_ONLY=
if [ $# -gt 0 ]
then
  ASSET_ONLY=1
fi

trap cleanup SIGINT

python3.12 robot_controller.py ../../custom.json ${ASSET_ONLY} &
bg_pid=$!

wait $bg_pid
