using Hakoniwa.Core.Utils.Logger;
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
    public struct LiDAR3DParams
    {
        public bool Enabled;
        public int NumberOfChannels;
        public int RotationsPerSecond;
        public int PointsPerSecond;
        public float MaxDistance;
        public float VerticalFOVUpper;
        public float VerticalFOVLower;
        public float HorizontalFOVStart;
        public float HorizontalFOVEnd;
        public bool DrawDebugPoints;

    }

    public class LiDAR3D : MonoBehaviour, IRobotPartsSensor, IRobotPartsConfig
    {
        public bool Enabled = true;
        public int NumberOfChannels = 16;
        public int RotationsPerSecond = 10;
        public int PointsPerSecond = 10000;
        public float MaxDistance = 10;
        public float VerticalFOVUpper = -15f;
        public float VerticalFOVLower = -25f;
        public float HorizontalFOVStart = - 20f;
        public float HorizontalFOVEnd = 20f;
        public bool DrawDebugPoints = true;


        /*
         * パラメータ経緯(MaxHeight, MaxWidth)：
         *  周波数：5Hz
         *  垂直：-30° 〜 30°
         *  水平：-90° 〜 90°
         *  分解能：1° 
         */
        public const int MaxHeight = 61;
        public const int MaxWidth = 181;

        public float deg_interval_h = 1f;
        public float deg_interval_v = 1f;

        public int height = 61;
        public int width = 181;
        public int update_cycle = 1;

        public int PointsPerRotation;
        public int HorizontalPointsPerRotation;
        public float HorizontalRanges;
        public float VerticalRanges;
        public float SecondsPerRotation;

        public bool SetParams(LiDAR3DParams param)
        {
            PointsPerRotation = param.PointsPerSecond / param.RotationsPerSecond;
            HorizontalPointsPerRotation = PointsPerRotation / param.NumberOfChannels;
            HorizontalRanges = param.HorizontalFOVEnd - param.HorizontalFOVStart;
            VerticalRanges = param.VerticalFOVUpper - param.VerticalFOVLower;
            SecondsPerRotation = 1.0f / (float)param.RotationsPerSecond;

            if (param.NumberOfChannels > MaxHeight)
            {
                SimpleLogger.Get().Log(Level.ERROR, "NumberOfChannels is invalid: " + param.NumberOfChannels);
                return false;
            }
            if (HorizontalPointsPerRotation > MaxWidth)
            {
                SimpleLogger.Get().Log(Level.ERROR, "PointsPerRotation("+ PointsPerRotation + ") / NumberOfChannels(" + param.NumberOfChannels  + ") is invalid: " + HorizontalPointsPerRotation);
                return false;
            }

            this.height = param.NumberOfChannels;
            this.width = HorizontalPointsPerRotation;
            this.deg_interval_h = HorizontalRanges / HorizontalPointsPerRotation;
            this.deg_interval_v = VerticalRanges / param.NumberOfChannels;
            this.update_cycle = Mathf.RoundToInt(SecondsPerRotation / Time.fixedDeltaTime);

            this.Enabled = param.Enabled;
            this.NumberOfChannels = param.NumberOfChannels;
            this.RotationsPerSecond = param.RotationsPerSecond;
            this.PointsPerSecond = param.PointsPerSecond;
            this.MaxDistance = param.MaxDistance;
            this.VerticalFOVLower = param.VerticalFOVLower;
            this.VerticalFOVUpper = param.VerticalFOVUpper;
            this.HorizontalFOVStart = param.HorizontalFOVStart;
            this.HorizontalFOVEnd = param.HorizontalFOVEnd;
            this.DrawDebugPoints = param.DrawDebugPoints;
            return true;
        }
        public LiDAR3DParams GetParams()
        {
            LiDAR3DParams param = new LiDAR3DParams
            {
                Enabled = this.Enabled,
                NumberOfChannels = this.NumberOfChannels,
                RotationsPerSecond = this.RotationsPerSecond,
                PointsPerSecond = this.PointsPerSecond,
                MaxDistance = this.MaxDistance,
                VerticalFOVUpper = this.VerticalFOVUpper,
                VerticalFOVLower = this.VerticalFOVLower,
                HorizontalFOVStart = this.HorizontalFOVStart,
                HorizontalFOVEnd = this.HorizontalFOVEnd,
                DrawDebugPoints = this.DrawDebugPoints
            };
            return param;
        }

        private GameObject root;
        private GameObject sensor;
        private string root_name;
        private PduIoConnector pdu_io;
        private IPduWriter pdu_writer_lidar;
        private IPduWriter pdu_writer_pos;

        readonly public int max_data_array_size = 176656;
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

        public float view_cycle_h = 2;
        public float view_cycle_v = 2;

        private float GetSensorValue(float degreeYaw, float degreePitch, bool debug)
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

            if (Physics.Raycast(sensor.transform.position, finalDirection, out hit, MaxDistance))
            {
                if (debug)
                {
                    Debug.DrawRay(sensor.transform.position, finalDirection * hit.distance, Color.red, 0.05f, false);
                }
                return hit.distance;
            }
            else
            {
                if (debug)
                {
                    Debug.DrawRay(sensor.transform.position, finalDirection * MaxDistance, Color.green, 0.05f, false);
                }
                return MaxDistance;
            }
        }


        private void ScanEnvironment()
        {
            int totalPoints = height * width;
            int dataIndex = 0;
            float fixedIntensity = 1.0f;

            bool debug_h = false;
            bool debug_v = false;
            int i_h = 0;
            int i_v = 0;
            for (float pitch = VerticalFOVLower; pitch <= VerticalFOVUpper; pitch += deg_interval_v)
            {
                debug_v = ((i_v % view_cycle_v) == 0);
                i_v++;
                i_h = 0;
                for (float yaw = HorizontalFOVStart; yaw <= HorizontalFOVEnd; yaw += deg_interval_h)
                {
                    debug_h = ((i_h % view_cycle_h) == 0);
                    i_h++;
                    float distance = GetSensorValue(yaw, pitch, (DrawDebugPoints && debug_h && debug_v));
                    Vector3 point = CalculatePoint(distance, yaw, pitch);

                    Buffer.BlockCopy(BitConverter.GetBytes(point.z), 0, data, dataIndex, 4);//x
                    Buffer.BlockCopy(BitConverter.GetBytes(-point.x), 0, data, dataIndex + 4, 4);//y
                    Buffer.BlockCopy(BitConverter.GetBytes(point.y), 0, data, dataIndex + 8, 4);//z
                    Buffer.BlockCopy(BitConverter.GetBytes(fixedIntensity), 0, data, dataIndex + 12, 4);

                    dataIndex += point_step;
                }
            }
        }
        private Vector3 CalculatePoint(float distance, float degreeYaw, float degreePitch)
        {
            // ユーラー角を四元数に変換
            Quaternion rotation = Quaternion.Euler(degreePitch, degreeYaw, 0);

            // ローカル座標系での前方ベクトル
            Vector3 forwardInLocal = rotation * this.sensor.transform.forward;

            // 衝突点の計算
            Vector3 collisionPoint = forwardInLocal * distance;
            return collisionPoint;
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
                this.width = Mathf.CeilToInt((HorizontalFOVEnd - HorizontalFOVStart) / deg_interval_h) + 1;
                this.height = Mathf.CeilToInt((VerticalFOVUpper - VerticalFOVLower) / deg_interval_v) + 1;
                this.row_step = this.width * this.point_step;

                if ((this.row_step * this.height) > this.max_data_array_size)
                {
                    throw new ArgumentException("ERROR: oveflow data size: " + (this.row_step * this.height) + " max: " + this.max_data_array_size);
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
            if (this.Enabled == false)
            {
                return;
            }
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

            pdu.Ref("angular").SetData("x", (double)((MathF.PI / 180) * euler.z));
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
            this.SetParams(this.GetParams());
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
