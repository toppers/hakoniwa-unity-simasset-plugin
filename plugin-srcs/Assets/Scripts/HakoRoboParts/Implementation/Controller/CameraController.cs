using System;
using Hakoniwa.PluggableAsset.Communication.Connector;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using UnityEngine;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{

    public class CameraController : MonoBehaviour, IRobotPartsController, IRobotPartsConfig
    {
        private GameObject root;
        private string root_name;
        private PduIoConnector pdu_io;
        private IPduWriter pdu_writer;
        private IPduReader pdu_reader;
        private GameObject sensor;
        private RenderTexture RenderTextureRef;
        private Texture2D tex;
        private int width = 640;
        private int height = 480;
        private byte[] raw_bytes;
        private byte[] compressed_bytes;
        private string sensor_name;
        private Camera my_camera;

        public string[] topic_type = {
            "hako_msgs/HakoCmdCamera",
            "hako_msgs/HakoCameraData"
        };
        public string[] topic_name = {
            "hako_cmd_camera",
            "hako_camera_data"
        };

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
                this.root = tmp;
                this.root_name = string.Copy(this.root.transform.name);
                this.pdu_io = PduIoConnector.Get(root_name);
                if (this.pdu_io == null)
                {
                    throw new ArgumentException("can not found pdu_io:" + root_name);
                }
                var pdu_reader_name = root_name + "_" + this.topic_name[0] + "Pdu";
                this.pdu_reader = this.pdu_io.GetReader(pdu_reader_name);
                if (this.pdu_reader == null)
                {
                    throw new ArgumentException("can not found pdu_writer:" + pdu_reader_name);
                }
                var pdu_writer_name = root_name + "_" + this.topic_name[1] + "Pdu";
                this.pdu_writer = this.pdu_io.GetWriter(pdu_writer_name);
                if (this.pdu_writer == null)
                {
                    throw new ArgumentException("can not found pdu_writer:" + pdu_writer_name);
                }
                this.sensor_name = string.Copy(this.transform.name);
                this.my_camera = this.GetComponentInChildren<Camera>();
                var texture = new Texture2D(this.width, this.height, TextureFormat.RGB24, false);
                this.RenderTextureRef = new RenderTexture(texture.width, texture.height, 32);
                this.my_camera.targetTexture = this.RenderTextureRef;
                this.sensor = this.gameObject;
            }
        }
        public RosTopicMessageConfig[] getRosConfig()
        {
            return RoboPartsConfigData.getRosConfig(this.GetRoboPartsConfig(), this.topic_name, this.topic_type, null);
        }
        public IoMethod io_method = IoMethod.SHM;
        public CommMethod comm_method = CommMethod.DIRECT;
        public RoboPartsConfigData[] GetRoboPartsConfig()
        {
            RoboPartsConfigData[] configs = new RoboPartsConfigData[2];
            configs[0] = new RoboPartsConfigData();
            configs[0].io_dir = IoDir.READ;
            configs[0].io_method = this.io_method;
            configs[0].value.org_name = this.topic_name[0];
            configs[0].value.type = this.topic_type[0];
            configs[0].value.class_name = ConstantValues.pdu_writer_class;
            configs[0].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[0].value.pdu_size = 20;
            configs[0].value.write_cycle = 1;
            configs[0].value.method_type = this.comm_method.ToString();

            configs[1] = new RoboPartsConfigData();
            configs[1].io_dir = IoDir.WRITE;
            configs[1].io_method = this.io_method;
            configs[1].value.org_name = this.topic_name[1];
            configs[1].value.type = this.topic_type[1];
            configs[1].value.class_name = ConstantValues.pdu_writer_class;
            configs[1].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[1].value.pdu_size = ConstantValues.CompressedImage_pdu_size + 4;
            configs[1].value.write_cycle = 1;
            configs[1].value.method_type = this.comm_method.ToString();
            return configs;
        }

        public int current_id = -1;
        public int request_id = 0;
        public int encode_type = 0;

        public void DoControl()
        {
            bool request = this.pdu_reader.GetReadOps().Ref("header").GetDataBool("request");
            if (request)
            {
                request_id = this.pdu_reader.GetReadOps().GetDataInt32("request_id");
                encode_type = this.pdu_reader.GetReadOps().GetDataInt32("encode_type");
                if (current_id != request_id)
                {
                    current_id = request_id;
                    this.Scan();
                    this.pdu_writer.GetWriteOps().SetData("request_id", current_id);
                    this.WriteCameraDataPdu(this.pdu_writer.GetWriteOps().Ref("image"));
                }
            }
        }

        private void Scan()
        {
            tex = new Texture2D(RenderTextureRef.width, RenderTextureRef.height, TextureFormat.RGB24, false);
            RenderTexture.active = RenderTextureRef;
            int width = RenderTextureRef.width;
            int height = RenderTextureRef.height;
            int step = width * 3;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            byte[] _byte = tex.GetRawTextureData();
            raw_bytes = new byte[_byte.Length];
            for (int i = 0; i < height; i++)
            {
                System.Array.Copy(_byte, i * step, raw_bytes, (height - i - 1) * step, step);
            }

            // Encode texture
            if (encode_type == 0)
            {
                compressed_bytes = tex.EncodeToPNG();
            }
            else
            {
                compressed_bytes = tex.EncodeToJPG();
            }
            UnityEngine.Object.Destroy(tex);
        }
        private void WriteCameraDataPdu(Pdu pdu)
        {
            TimeStamp.Set(pdu);
            pdu.Ref("header").SetData("frame_id", this.sensor_name);
            if (encode_type == 0)
            {
                pdu.SetData("format", "png");
            }
            else
            {
                pdu.SetData("format", "jpeg");
            }
            pdu.SetData("data", compressed_bytes);
        }
    }

}
