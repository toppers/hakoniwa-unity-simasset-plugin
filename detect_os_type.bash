#!/bin/bash

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
