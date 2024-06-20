using System;
using Hakoniwa.PluggableAsset.Communication.Connector;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using UnityEngine;
using UnityEngine.UI;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{

    public class CameraController : MonoBehaviour, IRobotPartsController, IRobotPartsConfig
    {
        public string game_ops_name = "hako_cmd_game";
        public int game_ops_camera_button_index = 2;
        public int game_ops_camera_move_up_index = 11;
        public int game_ops_camera_move_down_index = 12;
        public float camera_move_up_deg = -15.0f;
        public float camera_move_down_deg = 90.0f;
        private IPduReader pdu_reader_game_ops;
        private IPduReader pdu_reader_camera_move;
        private GameObject root;
        private string root_name;
        private PduIoConnector pdu_io;
        private IPduWriter pdu_writer;
        private IPduReader pdu_reader;
        private GameObject sensor;
        private RenderTexture RenderTextureRef;
        private Texture2D tex;
        public int width = 640;
        public int height = 480;
        private byte[] raw_bytes;
        private byte[] compressed_bytes;
        private string sensor_name;
        private Camera my_camera;
        public RawImage displayImage; // 映像を表示するUIのRawImage


        public string[] topic_type = {
            "hako_msgs/HakoCmdCamera",
            "hako_msgs/HakoCameraData",
            "hako_msgs/HakoCmdCameraMove"
        };
        public string[] topic_name = {
            "hako_cmd_camera",
            "hako_camera_data",
            "hako_cmd_camera_move"
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
                pdu_reader_name = root_name + "_" + this.topic_name[2] + "Pdu";
                this.pdu_reader_camera_move = this.pdu_io.GetReader(pdu_reader_name);
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
                pdu_reader_name = root_name + "_" + this.game_ops_name + "Pdu";
                this.pdu_reader_game_ops = this.pdu_io.GetReader(pdu_reader_name);
                if (this.pdu_reader_game_ops == null)
                {
                    throw new ArgumentException("can not found pdu_reader:" + pdu_reader_name);
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
            RoboPartsConfigData[] configs = new RoboPartsConfigData[3];
            configs[0] = new RoboPartsConfigData();
            configs[0].io_dir = IoDir.READ;
            configs[0].io_method = this.io_method;
            configs[0].value.org_name = this.topic_name[0];
            configs[0].value.type = this.topic_type[0];
            configs[0].value.class_name = ConstantValues.pdu_writer_class;
            configs[0].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[0].value.pdu_size = 20 + ConstantValues.PduMetaDataSize;
            configs[0].value.write_cycle = 1;
            configs[0].value.method_type = this.comm_method.ToString();

            configs[1] = new RoboPartsConfigData();
            configs[1].io_dir = IoDir.WRITE;
            configs[1].io_method = this.io_method;
            configs[1].value.org_name = this.topic_name[1];
            configs[1].value.type = this.topic_type[1];
            configs[1].value.class_name = ConstantValues.pdu_writer_class;
            configs[1].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[1].value.pdu_size = ConstantValues.CompressedImage_pdu_size + 4 + ConstantValues.PduMetaDataSize;
            configs[1].value.write_cycle = 1;
            configs[1].value.method_type = this.comm_method.ToString();

            Debug.Log("topic len: " + this.topic_name.Length);
            configs[2] = new RoboPartsConfigData();
            configs[2].io_dir = IoDir.READ;
            configs[2].io_method = this.io_method;
            configs[2].value.org_name = this.topic_name[2];
            configs[2].value.type = this.topic_type[2];
            configs[2].value.class_name = ConstantValues.pdu_writer_class;
            configs[2].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[2].value.pdu_size = 40 + ConstantValues.PduMetaDataSize;
            configs[2].value.write_cycle = 1;
            configs[2].value.method_type = this.comm_method.ToString();

            return configs;
        }

        public int current_id = -1;
        public int request_id = 0;
        public int encode_type = 0;
        public float move_step = 1.0f;  // 一回の動きのステップ量
        private float camera_move_button_time_duration = 0f;
        public float camera_move_button_threshold_speedup = 1.0f;
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
            bool[] button_array = this.pdu_reader_game_ops.GetReadOps().GetDataBoolArray("button");
            if (button_array[this.game_ops_camera_button_index])
            {
                 //Debug.Log("SHOT!!");
                this.Scan();
                this.WriteCameraDataPdu(this.pdu_writer.GetWriteOps().Ref("image"));
            }
            if (button_array[this.game_ops_camera_move_up_index])
            {
                camera_move_button_time_duration += Time.fixedDeltaTime;
                if (camera_move_button_time_duration > camera_move_button_threshold_speedup)
                {
                    RotateCamera(-move_step * 3f);
                }
                else
                {
                    RotateCamera(-move_step);
                }

            }
            if (button_array[this.game_ops_camera_move_down_index])
            {
                camera_move_button_time_duration += Time.fixedDeltaTime;
                if (camera_move_button_time_duration > camera_move_button_threshold_speedup)
                {
                    RotateCamera(move_step * 3f);
                }
                else
                {
                    RotateCamera(move_step);
                }
            }
            if (!button_array[this.game_ops_camera_move_down_index] && !button_array[this.game_ops_camera_move_up_index])
            {
                camera_move_button_time_duration = 0f;
            }
            if (displayImage != null)
            {
                displayImage.texture = this.RenderTextureRef;
            }
        }
        private void RotateCamera(float step)
        {
            Vector3 currentRotation = my_camera.transform.localEulerAngles;
            float newPitch = currentRotation.x + step;
            if (newPitch > 180) newPitch -= 360; // Convert angles greater than 180 to negative values

            // Clamp the pitch to be within the desired range
            newPitch = Mathf.Clamp(newPitch, camera_move_up_deg, camera_move_down_deg);

            my_camera.transform.localEulerAngles = new Vector3(newPitch, currentRotation.y, currentRotation.z);
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
