#!/bin/bash

if [ $# -ne 1 ]
then
    echo "Usage: $0 <application dir>"
    exit 1
fi
APP_DIRPATH=${1}
SCRIPT_PATH=`readlink -f "$0"`
SCRIPT_DIR=`dirname ${SCRIPT_PATH}`

if [ ! -f ${APP_DIRPATH}/core_config.json ]
then
    echo "ERROR: can not find ${APP_DIRPATH}/core_config.json"
    exit 1
fi

if [[ "$(uname -r)" == *microsoft* ]]
then
    export OS_TYPE=wsl2
elif [[ "$(uname)" == "Darwin" ]]
then
    export OS_TYPE=Mac
else
    export OS_TYPE=Linux
fi

export ARCH_TYPE=`arch`

APP_NAME=
if [ ${OS_TYPE} = "wsl2" ]
then
    export ASSET_IPADDR=`cat /etc/resolv.conf  | grep nameserver | awk '{print $NF}'`
    NETWORK_INTERFACE=$(route | grep '^default' | grep -o '[^ ]*$' | tr -d '\n')
    export CORE_IPADDR=$(ip addr | grep inet | grep  "${NETWORK_INTERFACE}"  | awk '{print $2}' | awk -F/ '{print $1}')
    bash ${SCRIPT_DIR}/third-party/mustache/mo  ${SCRIPT_DIR}/template/win_core_config_json.tpl  > ${APP_DIRPATH}/core_config.json
    cd ${APP_DIRPATH}
    APP_NAME=`find .  -name "*.exe"`
    ./${APP_NAME}
elif [ ${OS_TYPE} = "Darwin" ]
then
    cd ${APP_DIRPATH}
    APP_NAME=`find ./Contents/MacOS  -type f`
    ./${APP_NAME}
else
    echo "ERROR: not supported os type: ${OS_TYPE}"
    exit 1
fi

