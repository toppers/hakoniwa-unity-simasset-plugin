using System;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{
    public interface IRobotPartsConfig
    {
        RoboPartsConfigData[] GetRoboPartsConfig();
    }
    public enum IoMethod
    {
        SHM,
        RPC
    }
    public enum IoDir
    {
        READ,
        WRITE
    }
    public enum CommMethod
    {
        UDP,
        MQTT,
        DIRECT
    }
    public class RoboPartsConfigData
    {
        public IoMethod io_method = IoMethod.RPC;
        public IoDir io_dir = IoDir.WRITE;
        public RobotPartsConfig value = new RobotPartsConfig();

        //RosTopicMessageConfig Conversion from RoboPartsConfigData
        public static RosTopicMessageConfig[] getRosConfig(RoboPartsConfigData[] pcfg, string[] topic_name, string[] topic_type, int[] update_cycles)
        {
            RosTopicMessageConfig[] cfg = new RosTopicMessageConfig[pcfg.Length];
            for (int i = 0; i < pcfg.Length; i++)
            {
                if (update_cycles == null)
                {
                    cfg[i] = getRosConfigSingle(pcfg[i], topic_name[i], topic_type[i], 1);
                }
                else
                {
                    cfg[i] = getRosConfigSingle(pcfg[i], topic_name[i], topic_type[i], update_cycles[i]);
                }
            }

            return cfg;
        }
        public static RosTopicMessageConfig getRosConfigSingle(RoboPartsConfigData pcfg, string topic_name, string topic_type, int update_cycle)
        {
            RosTopicMessageConfig cfg = new RosTopicMessageConfig();
            cfg.topic_message_name = topic_name;
            cfg.topic_type_name = topic_type;
            if (pcfg.io_dir == IoDir.READ)
            {
                cfg.sub = true;
            }
            else
            {
                cfg.sub = false;
            }
            cfg.pub_option = new RostopicPublisherOption();
            cfg.pub_option.cycle_scale = update_cycle;
            cfg.pub_option.latch = false;
            cfg.pub_option.queue_size = 1;
            return cfg;
        }

    }
    [System.Serializable]
    public class RobotPartsConfig
    {
        public string type = null;
        public string org_name = null;
        public string name = null;
        public string class_name = null;
        public string conv_class_name = null;
        public int channel_id = 0;
        public int pdu_size = 0;
        public int write_cycle = 1;
        public string method_type = "UDP";
    }
    [System.Serializable]
    public class RobotPartsConfigContainer
    {
        public string name = "micon_setting";
        public RobotPartsConfig[] rpc_pdu_readers = null;
        public RobotPartsConfig[] rpc_pdu_writers = null;
        public RobotPartsConfig[] shm_pdu_readers = null;
        public RobotPartsConfig[] shm_pdu_writers = null;
    }
    public static class ConstantValues
    {
        public static readonly string pdu_reader_class = "Hakoniwa.PluggableAsset.Communication.Pdu.Raw.RawPduReader";
        public static readonly string pdu_writer_class = "Hakoniwa.PluggableAsset.Communication.Pdu.Raw.RawPduWriter";
        public static readonly string conv_pdu_reader_class = "Hakoniwa.PluggableAsset.Communication.Pdu.Raw.RawPduReaderConverter";
        public static readonly string conv_pdu_writer_class = "Hakoniwa.PluggableAsset.Communication.Pdu.Raw.RawPduWriterConverter";
        public static readonly uint PduMetaDataVersion = 1;
        public static readonly uint PduMetaDataMagicNo = 0x12345678;
        public static readonly int PduMetaDataSize = 24;
        public static readonly int Twist_pdu_size = 48 + PduMetaDataSize;
        public static readonly int JointState_pdu_size = 440 + PduMetaDataSize;
        public static readonly int Imu_pdu_size = 432 + PduMetaDataSize;
        public static readonly int Odometry_pdu_size = 944 + PduMetaDataSize;
        public static readonly int TFMessage_pdu_size = 320 + PduMetaDataSize;
        public static readonly int Image_pdu_size = 1229080 + PduMetaDataSize;
        public static readonly int CompressedImage_pdu_size = 102664 + PduMetaDataSize;
        public static readonly int CameraInfo_pdu_size = 580 + PduMetaDataSize;
        public static readonly int LaserScan_pdu_size = 256 + PduMetaDataSize;
        public static readonly int Bool_pdu_size = 4 + PduMetaDataSize;
        public static readonly int IMU_pdu_size = 432 + PduMetaDataSize;
    }
}
