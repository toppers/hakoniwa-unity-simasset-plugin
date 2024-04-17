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
    public struct PointCloudFieldType
    {
        public string name;
        public uint offset;
        /*
            uint8 INT8    = 1
            uint8 UINT8   = 2
            uint8 INT16   = 3
            uint8 UINT16  = 4
            uint8 INT32   = 5
            uint8 UINT32  = 6
            uint8 FLOAT32 = 7
            uint8 FLOAT64 = 8
         */
        public byte datatype; /* FLOAT32 */
        public uint count; /* 1 */
        public PointCloudFieldType(string n, uint off, byte type, uint c)
        {
            this.name = n;
            this.offset = off;
            this.datatype = type;
            this.count = c;
        }
    }

    public class LiDAR3D : MonoBehaviour, IRobotPartsSensor, IRobotPartsConfig
    {
        private GameObject root;
        private GameObject sensor;
        private string root_name;
        private PduIoConnector pdu_io;
        private IPduWriter pdu_writer_lidar;
        private IPduWriter pdu_writer_pos;
        public float scale = 1.0f;

        private Quaternion init_angle;
        public static bool is_debug = true;
        public float contact_distance = 10f; /* m */

        readonly public int max_data_array_size = 176656;
        public float min_pitch = -15f;
        public float max_pitch = 15f;
        public float min_yaw = -60f;
        public float max_yaw = 60f;
        public float deg_interval = 1f;
        public int height = 61;
        public int width = 181;
        private int point_step = 16;
        private int row_step = 0;
        private bool is_bigendian = false;
        private PointCloudFieldType[] fields =
        {
            new PointCloudFieldType("x", 0, 7, 1),
            new PointCloudFieldType("y", 4, 7, 1),
            new PointCloudFieldType("z", 8, 7, 1),
            new PointCloudFieldType("intensity", 12, 7, 1),
        };
        private byte[] data;
        private Pdu[] pdu_fields;

        public float view_interval = 5;

        private float GetSensorValue(float degreeYaw, float degreePitch)
        {
            // センサーの基本の前方向を取得
            Vector3 forward = sensor.transform.forward;

            // Quaternionを使用してヨー、ピッチ、ロールを一度に計算
            Quaternion yawRotation = Quaternion.AngleAxis(degreeYaw, sensor.transform.up);
            Quaternion pitchRotation = Quaternion.AngleAxis(degreePitch, yawRotation * sensor.transform.right);

            // 最終的な回転を適用
            Quaternion finalRotation = yawRotation * pitchRotation;
            Vector3 finalDirection = finalRotation * forward;

            RaycastHit hit;

            if (Physics.Raycast(sensor.transform.position, finalDirection, out hit, contact_distance))
            {
                if (is_debug && (degreeYaw % view_interval) < 0.0001 && (degreePitch % view_interval) < 0.0001)
                {
                    Debug.DrawRay(sensor.transform.position, finalDirection * hit.distance, Color.red, 0.05f, false);
                }
                return hit.distance;
            }
            else
            {
                if (is_debug && (degreeYaw % view_interval) < 0.0001 && (degreePitch % view_interval) < 0.0001)
                {
                    Debug.DrawRay(sensor.transform.position, finalDirection * contact_distance, Color.green, 0.05f, false);
                }
                return contact_distance;
            }
        }


        private void ScanEnvironment()
        {
            int totalPoints = height * width;
            int dataIndex = 0;
            float fixedIntensity = 1.0f;

            for (float pitch = min_pitch; pitch <= max_pitch; pitch += deg_interval)
            {
                for (float yaw = min_yaw; yaw <= max_yaw; yaw += deg_interval)
                {
                    float distance = GetSensorValue(yaw, pitch);
                    Vector3 point = CalculatePoint(distance, yaw, pitch);

                    Buffer.BlockCopy(BitConverter.GetBytes(point.x), 0, data, dataIndex, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(point.y), 0, data, dataIndex + 4, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(point.z), 0, data, dataIndex + 8, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(fixedIntensity), 0, data, dataIndex + 12, 4);

                    dataIndex += point_step;
                }
            }
        }
        private Vector3 CalculatePoint(float distance, float yaw, float pitch)
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 direction = rotation * Vector3.forward;
            return direction * distance;
        }
        public void UpdateLidarPdu(Pdu pdu)
        {
            TimeStamp.Set(pdu);
            pdu.Ref("header").SetData("frame_id", "front_lidar_frame");

            pdu.SetData("height", (uint)this.height);
            pdu.SetData("width", (uint)this.width);
            pdu.SetData("is_bigendian", this.is_bigendian);
            pdu.SetData("fields", this.pdu_fields);
            pdu.SetData("point_step", (uint)this.point_step);
            pdu.SetData("row_step", (uint)this.row_step);
            pdu.SetData("data", this.data);
            pdu.SetData("is_dense", true);
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
                var pdu_writer_name = root_name + "_" + this.topic_name[0] + "Pdu";
                this.pdu_writer_lidar = this.pdu_io.GetWriter(pdu_writer_name);
                if (this.pdu_writer_lidar == null)
                {
                    throw new ArgumentException("can not found pdu_reader:" + pdu_writer_name);
                }
                pdu_writer_name = root_name + "_" + this.topic_name[1] + "Pdu";
                this.pdu_writer_pos = this.pdu_io.GetWriter(pdu_writer_name);
                if (this.pdu_writer_pos == null)
                {
                    throw new ArgumentException("can not found pdu_reader:" + pdu_writer_name);
                }
                this.sensor = this.gameObject;
                this.init_angle = this.sensor.transform.localRotation;
                this.width = Mathf.CeilToInt((max_yaw - min_yaw) / deg_interval) + 1;
                this.height = Mathf.CeilToInt((max_pitch - min_pitch) / deg_interval) + 1;
                this.row_step = this.width * this.point_step;

                if ((this.row_step * this.height) > this.max_data_array_size)
                {
                    throw new ArgumentException("ERROR: oveflow data size: " +(this.row_step * this.height) + " max: " + this.max_data_array_size);
                }


                this.data = new byte[this.row_step * this.height];
                this.pdu_fields = new Pdu[this.fields.Length];
                for (int i = 0; i < this.fields.Length; i++)
                {
                    this.pdu_fields[i] = new Pdu("sensor_msgs/PointField");
                    this.pdu_fields[i].SetData("name", this.fields[i].name);
                    this.pdu_fields[i].SetData("offset", (uint)this.fields[i].offset);
                    this.pdu_fields[i].SetData("datatype", (byte)this.fields[i].datatype);
                    this.pdu_fields[i].SetData("count", (uint)this.fields[i].count);
                }
                this.pdu_writer_lidar.GetWriteOps().SetData("fields", this.pdu_fields);
            }
        }

        public void UpdateSensorValues()
        {
            this.count++;
            if (this.count < this.update_cycle)
            {
                return;
            }
            this.count = 0;
            this.ScanEnvironment();
            this.UpdateLidarPdu(this.pdu_writer_lidar.GetWriteOps().Ref(null));
            this.UpdatePosPdu(this.pdu_writer_pos.GetWriteOps().Ref(null));
            return;
        }

        private void UpdatePosPdu(Pdu pdu)
        {
            //Unity FRAME TO ROS FRAME
            pdu.Ref("linear").SetData("x", (double)this.sensor.transform.position.z);
            pdu.Ref("linear").SetData("y", -(double)this.sensor.transform.position.x);
            pdu.Ref("linear").SetData("z", (double)this.sensor.transform.position.y);

            var euler = this.sensor.transform.transform.eulerAngles;

            pdu.Ref("angular").SetData("x", (double)((MathF.PI/180) * euler.z));
            pdu.Ref("angular").SetData("y", -(double)((MathF.PI / 180) * euler.x));
            pdu.Ref("angular").SetData("z", (double)((MathF.PI / 180) * euler.y));
        }

        public string[] topic_type = {
            "sensor_msgs/PointCloud2",
            "geometry_msgs/Twist"
        };
        public string[] topic_name = {
            "lidar_points",
            "lidar_pos"
        };
        public int update_cycle = 1;
        private int count = 0;
        public RosTopicMessageConfig[] getRosConfig()
        {
            RosTopicMessageConfig[] cfg = new RosTopicMessageConfig[1];
            return RoboPartsConfigData.getRosConfig(this.GetRoboPartsConfig(), this.topic_name, this.topic_type, null);
        }
        public IoMethod io_method = IoMethod.SHM;
        public CommMethod comm_method = CommMethod.DIRECT;
        public RoboPartsConfigData[] GetRoboPartsConfig()
        {
            RoboPartsConfigData[] configs = new RoboPartsConfigData[2];
            configs[0] = new RoboPartsConfigData();
            configs[0].io_dir = IoDir.WRITE;
            configs[0].io_method = this.io_method;
            configs[0].value.org_name = this.topic_name[0];
            configs[0].value.type = this.topic_type[0];
            configs[0].value.class_name = ConstantValues.pdu_writer_class;
            configs[0].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[0].value.pdu_size = 177376;
            configs[0].value.write_cycle = this.update_cycle;
            configs[0].value.method_type = this.comm_method.ToString();

            configs[1] = new RoboPartsConfigData();
            configs[1].io_dir = IoDir.WRITE;
            configs[1].io_method = this.io_method;
            configs[1].value.org_name = this.topic_name[1];
            configs[1].value.type = this.topic_type[1];
            configs[1].value.class_name = ConstantValues.pdu_writer_class;
            configs[1].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[1].value.pdu_size = ConstantValues.Twist_pdu_size;
            configs[1].value.write_cycle = this.update_cycle;
            configs[1].value.method_type = this.comm_method.ToString();
            return configs;
        }

        public bool isAttachedSpecificController()
        {
            return false;
        }
    }

}
