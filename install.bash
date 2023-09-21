#!/bin/bash

source detect_os_type.bash



CURR_DIR=`pwd`
PARENT_DIR=plugin-srcs/Assets/Plugin
if [ ${OS_TYPE} = "wsl2" ]
then
	if [ -d  ${PARENT_DIR}/Libs ]
	then
		:
	else
		wget https://github.com/toppers/hakoniwa-unity-simasset-plugin/releases/download/v0.0.1/Libs.zip
		mv Libs.zip ${PARENT_DIR}/
		cd ${PARENT_DIR}/
		unzip Libs.zip
		rm -f Libs.zip
		cd ${CURR_DIR}
	fi
else
	if [ ${OS_TYPE} = "Mac" ]
	then
		LIB_EXT=dylib
		which wget
		if [ $? -ne 0 ]
		then
			brew install wget
		fi
	else
		LIB_EXT=so
	fi
	if [ -d  ${PARENT_DIR}/Libs ]
	then
		:
	else
		mkdir ${PARENT_DIR}/Libs
		wget https://github.com/toppers/hakoniwa-core-cpp-client/releases/download/v1.0.3/libshakoc.${ARCH_TYPE}.${LIB_EXT}
		mv libshakoc.${ARCH_TYPE}.${LIB_EXT} ${PARENT_DIR}/Libs/libshakoc.${LIB_EXT}
		# REMOVE gRPC codes
		rm -rf plugin-srcs/Assets/Plugin/src/PureCsharp/Gen*
	fi
fi

if [ -z ${INSTALL_FOR_IOS} ]
then
	ROS_INS_DIR=plugin-srcs/ros_types
else
	ROS_INS_DIR=plugin-srcs/Assets/Resources/ros_types
fi

if [ -d ${ROS_INS_DIR} ]
then
	:
else
	mkdir ${ROS_INS_DIR}
fi

if [ -d ${ROS_INS_DIR}/json ]
then
	:
else
	wget https://github.com/toppers/hakoniwa-ros2pdu/releases/download/v1.0.0/json.zip
	mv json.zip ${ROS_INS_DIR}/
	cd ${ROS_INS_DIR}/
	unzip json.zip
	rm -f json.zip
	cd ${CURR_DIR}
fi

if [ -d ${ROS_INS_DIR}/offset ]
then
	:
else
	wget https://github.com/toppers/hakoniwa-ros2pdu/releases/download/v1.0.0/offset.zip
	mv offset.zip ${ROS_INS_DIR}/
	cd ${ROS_INS_DIR}/
	unzip offset.zip
	rm -f offset.zip
	cd ${CURR_DIR}
fi

