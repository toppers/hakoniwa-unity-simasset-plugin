#!/bin/bash

INSTALL_WIN="FALSE"
if [ $# -eq 1 ]
then
	if [ $1 = "win" ]
	then
		echo "INFO: installing windows"
		INSTALL_WIN="TRUE"
	elif [ $1 != "win" ]
	then
		echo "Usage: $0 [win]"
		exit 1
	fi
fi
source detect_os_type.bash

CPP_RELEASE_VER=v1.3.0
CURR_DIR=$(pwd)
PARENT_DIR=plugin-srcs/Assets/Plugin
if [ ${INSTALL_WIN} = "FALSE" -a ${OS_TYPE} = "wsl2" ]
then
	if [ -d "${PARENT_DIR}/Libs" ]
	then
		:
	else
		wget https://github.com/toppers/hakoniwa-unity-simasset-plugin/releases/download/v0.0.1/Libs.zip || { echo "ERROR: failed to download"; exit 1; }
		mv Libs.zip ${PARENT_DIR}/
		cd ${PARENT_DIR}/
		unzip Libs.zip || { echo "ERROR: failed to unzip"; exit 1; }
		rm -f Libs.zip
		cd ${CURR_DIR}
	fi
else
	if [ ${OS_TYPE} = "Mac" ]
	then
		LIB_EXT=dylib
		which wget || brew install wget
		if [ $ARCH_TYPE = "arm64" ]
		then
			LIB=libshakoc.arm64.dylib
		else
			LIB=libshakoc.dylib
		fi
	elif [ $INSTALL_WIN = "TRUE" ]
	then
		LIB=shakoc.dll
	else
		LIB=libshakoc.so
	fi
	if [ -d  ${PARENT_DIR}/Libs ]
	then
		:
	else
		mkdir ${PARENT_DIR}/Libs
		wget https://github.com/toppers/hakoniwa-core-cpp-client/releases/download/${CPP_RELEASE_VER}/${LIB} || { echo "ERROR: failed to download"; exit 1; }
		if [ ${LIB} = "libshakoc.arm64.dylib" ]
		then
			mv ${LIB} ${PARENT_DIR}/Libs/libshakoc.dylib
		else
			mv ${LIB} ${PARENT_DIR}/Libs/
		fi
		# REMOVE gRPC codes
		rm -rf plugin-srcs/Assets/Plugin/src/PureCsharp/Gen*
	fi
fi

if [ -z ${INSTALL_FOR_IOS} ]
then
	ROS_INS_DIR=plugin-srcs/ros_types
else
	ROS_INS_DIR=plugin-srcs/Assets/Resources/ros_types
	cp -rp plugin-srcs/ros_types plugin-srcs/Assets/Resources/
fi
