#!/bin/bash

if [ $# -ne 2 ]
then
    echo "Usage: $0 <robo-conf> <robo-name>"
    exit 1
fi

ROBO_CONF=${1}
ROBO_NAME=${2}

result=$(jq --arg robo_name "$ROBO_NAME" '{robots: .robots | map(select(.name == $robo_name))}' "$ROBO_CONF")

robots_count=$(echo "$result" | jq '.robots | length')

if [[ $robots_count -eq 0 ]]; then
  echo "Error: No robots found with name $robo_name"
  exit 1
fi

echo "$result"
