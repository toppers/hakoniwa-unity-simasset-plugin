using System;
using Hakoniwa.PluggableAsset.Communication.Connector;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using UnityEngine;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{

    [Serializable]
    public class DetectionDistance
    {
        public float Min { get; set; }
        public float Max { get; set; }
    }

    [Serializable]
    public class DistanceAccuracy
    {
        public float Percentage { get; set; }
        public string NoiseDistribution { get; set; }
    }

    [Serializable]
    public class AngleRange
    {
        public float Min { get; set; }
        public float Max { get; set; }
        public float Resolution { get; set; }
        public int ScanFrequency { get; set; }
    }

    [Serializable]
    public class InvalidMeasurement
    {
        public float Probability { get; set; }
        public string InvalidValue { get; set; }
    }

    [Serializable]
    public class LiDAR2DSensorParameters
    {
        public string frame_id { get; set; }
        public DetectionDistance DetectionDistance { get; set; }
        public DistanceAccuracy DistanceAccuracy { get; set; }
        public AngleRange AngleRange { get; set; }
        public InvalidMeasurement InvalidMeasurement { get; set; }
    }
    public class LiDAR2D : MonoBehaviour, IRobotPartsSensor, IRobotPartsConfig
    {
        static int CalculateDistanceArraySize(AngleRange angleRange)
        {
            //Debug.Log("angleRange: " + angleRange);
            int size = (int)Math.Round((angleRange.Max - angleRange.Min) / angleRange.Resolution);
            return size;
        }
        static int CalculateUpdateCycle(float fixedUpdatePeriod, int scanFrequency)
        {
            // Calculate the period of a single LiDAR scan
            float scanPeriod = 1.0f / scanFrequency;

            // Calculate the update cycle
            int updateCycle = Mathf.RoundToInt(scanPeriod / fixedUpdatePeriod);
            return updateCycle;
        }

        public string config_filepath = "./lidar2d_spec.json";
        private GameObject root;
        private GameObject sensor;
        private string root_name;
        private PduIoConnector pdu_io;
        private IPduWriter pdu_writer;
        public int update_cycle = 10;
        public LiDAR2DSensorParameters sensorParameters;
        public int max_count = 360;
        private float[] distances;
        private float angle_min = 0.0f;
        private float angle_max = 6.26573181152f;
        private float range_min = 0.119999997318f;
        private float range_max = 3.5f;
        private float angle_increment = 0.0174532923847f;
        private float time_increment = 2.98800005112e-05f;
        private float scan_time = 0.0f;
        private float[] intensities = new float[0];

        private Quaternion init_angle;
        public static bool is_debug = true;

        void CalculateLaserScanParameters()
        {
            // Convert degrees to radians
            this.angle_min = Mathf.Deg2Rad * (float)sensorParameters.AngleRange.Min;
            this.angle_max = Mathf.Deg2Rad * (float)sensorParameters.AngleRange.Max;

            // Convert mm to meters
            this.range_min = (float)sensorParameters.DetectionDistance.Min / 1000.0f;
            this.range_max = (float)sensorParameters.DetectionDistance.Max / 1000.0f;

            // Convert degrees to radians
            this.angle_increment = Mathf.Deg2Rad * (float)sensorParameters.AngleRange.Resolution;

            // Scan time in seconds
            this.scan_time = 1.0f / sensorParameters.AngleRange.ScanFrequency;

            // Time increment
            int numberOfMeasurements = Mathf.RoundToInt((angle_max - angle_min) / angle_increment) + 1;
            this.time_increment = scan_time / numberOfMeasurements;

            // Output the results
            Debug.Log($"angle_min: {angle_min}");
            Debug.Log($"angle_max: {angle_max}");
            Debug.Log($"range_min: {range_min}");
            Debug.Log($"range_max: {range_max}");
            Debug.Log($"angle_increment: {angle_increment}");
            Debug.Log($"time_increment: {time_increment}");
            Debug.Log($"scan_time: {scan_time}");
        }

        public void Initialize(object obj)
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
                sensorParameters = AssetConfigLoader.LoadJsonFile<LiDAR2DSensorParameters>(config_filepath);
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
                this.init_angle = this.sensor.transform.localRotation;
                this.max_count = CalculateDistanceArraySize(sensorParameters.AngleRange);
                this.distances = new float[max_count];
                this.update_cycle = CalculateUpdateCycle(Time.fixedDeltaTime, sensorParameters.AngleRange.ScanFrequency);
                CalculateLaserScanParameters();
            }
        }
        public string topic_type = "sensor_msgs/LaserScan";
        public string topic_name = "scan";
        private int count = 0;
        public RosTopicMessageConfig[] getRosConfig()
        {
            sensorParameters = AssetConfigLoader.LoadJsonFile<LiDAR2DSensorParameters>(config_filepath);
            this.max_count = CalculateDistanceArraySize(sensorParameters.AngleRange);
            this.update_cycle = CalculateUpdateCycle(Time.fixedDeltaTime, sensorParameters.AngleRange.ScanFrequency);
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
        public IoMethod io_method = IoMethod.SHM;
        public CommMethod comm_method = CommMethod.DIRECT;
        public RoboPartsConfigData[] GetRoboPartsConfig()
        {
            sensorParameters = AssetConfigLoader.LoadJsonFile<LiDAR2DSensorParameters>(config_filepath);
            this.max_count = CalculateDistanceArraySize(sensorParameters.AngleRange);
            this.update_cycle = CalculateUpdateCycle(Time.fixedDeltaTime, sensorParameters.AngleRange.ScanFrequency);
            RoboPartsConfigData[] configs = new RoboPartsConfigData[1];
            configs[0] = new RoboPartsConfigData();
            configs[0].io_dir = IoDir.WRITE;
            configs[0].io_method = this.io_method;
            configs[0].value.org_name = this.topic_name;
            configs[0].value.type = this.topic_type;
            configs[0].value.class_name = ConstantValues.pdu_writer_class;
            configs[0].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[0].value.pdu_size = ConstantValues.LaserScan_pdu_size + this.max_count * 2 * sizeof(float);
            configs[0].value.write_cycle = this.update_cycle;
            configs[0].value.method_type = this.comm_method.ToString();
            return configs;
        }

        public bool isAttachedSpecificController()
        {
            return false;
        }

        public void UpdateSensorValues()
        {
            this.count++;
            if (this.count < this.update_cycle)
            {
                return;
            }
            this.count = 0;
            this.Scan();
            this.UpdatePdu(this.pdu_writer.GetWriteOps().Ref(null));
            return;
        }
        public void UpdatePdu(Pdu pdu)
        {
            TimeStamp.Set(pdu);
            pdu.Ref("header").SetData("frame_id", this.sensorParameters.frame_id);

            pdu.SetData("angle_min", angle_min);
            pdu.SetData("angle_max", angle_max);
            pdu.SetData("range_min", range_min);
            pdu.SetData("range_max", range_max);
            pdu.SetData("ranges", distances);
            pdu.SetData("angle_increment", angle_increment);
            pdu.SetData("time_increment", time_increment);
            pdu.SetData("scan_time", scan_time);
            pdu.SetData("intensities", intensities);
        }

        private void Scan()
        {
            this.sensor.transform.localRotation = this.init_angle;
            int i = 0;
            for (float yaw = this.sensorParameters.AngleRange.Min; i < this.max_count; yaw += this.sensorParameters.AngleRange.Resolution)
            {
                float distance = GetSensorValue(yaw, 0, is_debug);
                //Debug.Log("v[" + i + "]=" + distances[i]);
                distances[i] = distance;
                i++;
            }
        }
        private float AddNoiseToDistance(float distance)
        {
            // 距離の3%を精度の範囲として設定
            float accuracyPercentage = sensorParameters.DistanceAccuracy.Percentage / 100.0f;
            float noiseMean = 0;
            float noiseStandardDeviation = distance * accuracyPercentage;

            // ガウス分布ノイズを生成
            float noise = GenerateGaussianNoise(noiseMean, noiseStandardDeviation);
            float noisyDistance = distance + noise;

            // 最大値の上限を考慮
            return Mathf.Min(noisyDistance, this.range_max);
        }

        private float GenerateGaussianNoise(float mean, float standardDeviation)
        {
            System.Random random = new System.Random();
            double u1 = 1.0 - random.NextDouble(); // (0, 1] の一様分布
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // 標準正規分布
            double randNormal = mean + standardDeviation * randStdNormal; // 指定された平均と標準偏差による正規分布
            return (float)randNormal;
        }


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

            if (Physics.Raycast(sensor.transform.position, finalDirection, out hit, this.range_max))
            {
                float distance = hit.distance;

                // ノイズを追加
                distance = AddNoiseToDistance(distance);

                if (debug)
                {
                    Debug.DrawRay(sensor.transform.position, finalDirection * distance, Color.red, 0.05f, false);
                }

                return distance;
            }
            else
            {
                if (debug)
                {
                    Debug.DrawRay(sensor.transform.position, finalDirection * this.range_max, Color.green, 0.05f, false);
                }
                return this.range_max;
            }
        }

    }
}

