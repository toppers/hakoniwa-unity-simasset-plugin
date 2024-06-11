using System;
using System.Collections;
using System.Collections.Generic;
using Hakoniwa.PluggableAsset.Communication.Connector;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using UnityEngine;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{

    public class LightSensor4way : MonoBehaviour, IRobotPartsSensor, IRobotPartsConfig
    {
        private GameObject root;
        private string root_name;
        private IPduWriter pdu_writer;
        private PduIoConnector pdu_io;


        public static bool is_debug = true;
        private float contact_distance = 10.0f; /* m */
        public float distanceValue; /* m */
        private Quaternion init_angle;

        public void Initialize(object root)
        {
            if (this.root != null)
            {
                return;
            }
            this.root = (GameObject)root;
            this.root_name = string.Copy(this.root.transform.name);
            this.pdu_io = PduIoConnector.Get(this.root_name);
            this.pdu_writer = this.pdu_io.GetWriter(this.root_name + "_"+ this.topic_name  + "Pdu");
            if (this.pdu_writer == null)
            {
                throw new ArgumentException("can not found ultrasonic_sensor pdu:" + this.root_name + "_" + this.topic_name + "Pdu");
            }

            this.init_angle = this.transform.localRotation;
        }

        private float GetSensorValue(Color color)
        {
            Vector3 fwd = this.transform.TransformDirection(Vector3.forward);
            RaycastHit hit;
            if (Physics.Raycast(transform.position, fwd, out hit, contact_distance))
            {
                //Debug.Log("green");
                if (is_debug)
                {
                    //Debug.Log("deg=" + degree + " dist=" + hit.distance);
                    Debug.DrawRay(this.transform.position, fwd * hit.distance, color, 0.1f, false);
                }
                return hit.distance;

            }
            else
            {
                //Debug.Log("red");
                Debug.DrawRay(this.transform.position, fwd * contact_distance, Color.red, 0.1f, false);
                return contact_distance;
            }
        }
        private float forward_r = 0f;
        private float foward_l = 0f;
        private float right = 0f;
        private float left = 0f;
        public float degree_fr = 10;
        public float degree_fl = -10;
        public float degree_r = 90;
        public float degree_l = -90;
        private void UpdateSensorValuesLocal()
        {
            this.transform.localRotation = this.init_angle;
            this.transform.Rotate(0, degree_fr, 0);
            this.forward_r = GetSensorValue(Color.green);

            this.transform.localRotation = this.init_angle;
            this.transform.Rotate(0, degree_r, 0);
            this.right = GetSensorValue(Color.green);

            this.transform.localRotation = this.init_angle;
            this.transform.Rotate(0, degree_fl, 0);
            this.foward_l = GetSensorValue(Color.green);

            this.transform.localRotation = this.init_angle;
            this.transform.Rotate(0, degree_l, 0);
            this.left = GetSensorValue(Color.green);

        }

        public bool isAttachedSpecificController()
        {
            return false;
        }

        public void UpdateSensorValues()
        {
            this.UpdateSensorValuesLocal();
            this.pdu_writer.GetWriteOps().SetData("forward_r", (short)(this.forward_r * 100));
            this.pdu_writer.GetWriteOps().SetData("forward_l", (short)(this.foward_l * 100));
            this.pdu_writer.GetWriteOps().SetData("left", (short)(this.left * 100));
            this.pdu_writer.GetWriteOps().SetData("right", (short)(this.right * 100));
        }
        public string topic_type = "pico_msgs/LightSensor";
        public string topic_name = "ultrasonic_sensor";
        public int update_cycle = 1;
        public RosTopicMessageConfig[] getRosConfig()
        {
            RosTopicMessageConfig[] cfg = new RosTopicMessageConfig[1];
            cfg[0] = RoboPartsConfigData.getRosConfigSingle(this.GetRoboPartsConfig()[0], this.topic_name, this.topic_type, this.update_cycle);
            return cfg;
        }
        public IoMethod io_method = IoMethod.SHM;
        public CommMethod comm_method = CommMethod.DIRECT;
        public RoboPartsConfigData[] GetRoboPartsConfig()
        {
            RoboPartsConfigData[] configs = new RoboPartsConfigData[1];
            configs[0] = new RoboPartsConfigData();
            configs[0].io_dir = IoDir.WRITE;
            configs[0].io_method = this.io_method;
            configs[0].value.org_name = this.topic_name;
            configs[0].value.type = this.topic_type;
            configs[0].value.class_name = ConstantValues.pdu_writer_class;
            configs[0].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[0].value.pdu_size = 8  + ConstantValues.PduMetaDataSize;
            configs[0].value.write_cycle = this.update_cycle;
            configs[0].value.method_type = this.comm_method.ToString();
            return configs;
        }


    }
}