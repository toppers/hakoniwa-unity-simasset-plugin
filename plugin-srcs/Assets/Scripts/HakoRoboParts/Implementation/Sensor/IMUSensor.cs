using Hakoniwa.PluggableAsset.Assets.Robot;
using Hakoniwa.PluggableAsset.Assets.Robot.Parts;
using Hakoniwa.PluggableAsset.Communication.Connector;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{
    public class IMUSensor : MonoBehaviour, IRobotPartsSensor, IRobotPartsConfig
    {
        private float deltaTime;
        private Vector3 prev_velocity = Vector3.zero;
        private Rigidbody my_rigidbody;
        private Vector3 prev_angle = Vector3.zero;
        private Vector3 delta_angle = Vector3.zero;

        private GameObject root;
        private GameObject sensor;
        private PduIoConnector pdu_io;
        private IPduWriter pdu_writer;
        private string root_name;

        public string topic_type = "sensor_msgs/Imu";
        public int update_cycle = 1;
        public string topic_name = "imu";

        public IoMethod io_method = IoMethod.RPC;
        public CommMethod comm_method = CommMethod.UDP;
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
            configs[0].value.pdu_size = ConstantValues.IMU_pdu_size;
            configs[0].value.write_cycle = this.update_cycle;
            configs[0].value.method_type = this.comm_method.ToString();
            return configs;
        }

        public RosTopicMessageConfig[] getRosConfig()
        {
            RosTopicMessageConfig[] cfg = new RosTopicMessageConfig[1];
            cfg[0] = new RosTopicMessageConfig();
            cfg[0].topic_message_name = this.topic_name;
            cfg[0].topic_type_name = this.topic_type;
            cfg[0].sub = false;
            cfg[0].pub_option = new RostopicPublisherOption();
            cfg[0].pub_option.cycle_scale = this.update_cycle;
            cfg[0].pub_option.latch = false;
            cfg[0].pub_option.queue_size = 1;
            return cfg;
        }

        public void Initialize(System.Object obj)
        {
            GameObject tmp = null;
            try
            {
                tmp = obj as GameObject;
            }
            catch (InvalidCastException e)
            {
                Debug.LogError("Initialize error: " + e.Message);
                return;
            }

            if (this.root == null)
            {
                this.root = tmp;
                this.root_name = string.Copy(this.root.transform.name);
                this.pdu_io = PduIoConnector.Get(root_name);
                if (this.pdu_io == null)
                {
                    throw new ArgumentException("can not found pdu_io:" + root_name);
                }
                var pdu_writer_name = root_name + "_" + this.topic_name + "Pdu";
                this.pdu_writer = this.pdu_io.GetWriter(pdu_writer_name);
                if (this.pdu_writer == null)
                {
                    throw new ArgumentException("can not found pdu_reader:" + pdu_writer_name);
                }
                this.sensor = this.gameObject;
                this.my_rigidbody = this.GetComponentInChildren<Rigidbody>();
                if (this.my_rigidbody == null)
                {
                    throw new ArgumentException("IMUSensor can not find RigidBody: " + this.sensor.name);
                }
                this.deltaTime = Time.fixedDeltaTime;
            }
        }

        public bool isAttachedSpecificController()
        {
            return false;
        }

        public void UpdateSensorValues()
        {
            this.UpdateSensorData(this.pdu_writer.GetWriteOps().Ref(null));
        }
        private void UpdateSensorData(Pdu pdu)
        {
            TimeStamp.Set(pdu);
            pdu.Ref("header").SetData("frame_id", "imu_link");

            //orientation
            UpdateOrientation(pdu);

            //angular_velocity
            UpdateAngularVelocity(pdu);

            //linear_acceleration
            UpdateLinearAcceleration(pdu);
        }

        private void UpdateLinearAcceleration(Pdu pdu)
        {
            Vector3 current_velocity = this.sensor.transform.InverseTransformDirection(my_rigidbody.velocity);
            Vector3 acceleration = (current_velocity - prev_velocity) / deltaTime;
            this.prev_velocity = current_velocity;
            this.delta_angle = this.GetCurrentEulerAngle() - prev_angle;
            this.prev_angle = this.GetCurrentEulerAngle();
            //gravity element
            acceleration += transform.InverseTransformDirection(Physics.gravity);

            pdu.Ref("linear_acceleration").SetData("x", (double)acceleration.z);
            pdu.Ref("linear_acceleration").SetData("y", (double)-acceleration.x);
            pdu.Ref("linear_acceleration").SetData("z", (double)acceleration.y);
        }
        private void UpdateOrientation(Pdu pdu)
        {
            pdu.Ref("orientation").SetData("x", (double)this.sensor.transform.rotation.z);
            pdu.Ref("orientation").SetData("y", (double)-this.sensor.transform.rotation.x);
            pdu.Ref("orientation").SetData("z", (double)this.sensor.transform.rotation.y);
            pdu.Ref("orientation").SetData("w", (double)-this.sensor.transform.rotation.w);
        }
        private void UpdateAngularVelocity(Pdu pdu)
        {
            pdu.Ref("angular_velocity").SetData("x", (double)my_rigidbody.angularVelocity.z);
            pdu.Ref("angular_velocity").SetData("y", (double)-my_rigidbody.angularVelocity.x);
            pdu.Ref("angular_velocity").SetData("z", (double)my_rigidbody.angularVelocity.y);
        }
        private Vector3 GetCurrentEulerAngle()
        {
            return this.sensor.transform.rotation.eulerAngles;
        }
    }
}
