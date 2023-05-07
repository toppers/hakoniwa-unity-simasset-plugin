#!/bin/bash

CURR_DIR=`pwd`
PARENT_DIR=plugin-srcs/Assets/Plugin
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


ROS_INS_DIR=plugin-srcs/ros_types
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

