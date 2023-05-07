#!/bin/bash

PARENT_DIR=plugin-srcs/Assets/Plugin
TARGET_DIR=${PARENT_DIR}/Libs
if [ -d  ${TARGET_DIR} ]
then
	:
else
	wget https://github.com/toppers/hakoniwa-unity-simasset-plugin/releases/download/v0.0.1/Libs.zip
	mv Libs.zip ${PARENT_DIR}/
	cd ${PARENT_DIR}/
	unzip Libs.zip
	rm -f Libs.zip
fi


