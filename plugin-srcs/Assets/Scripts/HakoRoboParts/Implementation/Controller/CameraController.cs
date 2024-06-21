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
        private IPduWriter pdu_writer_camera_info;
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
            "hako_msgs/HakoCmdCameraMove",
            "hako_msgs/HakoCameraInfo"
        };
        public string[] topic_name = {
            "hako_cmd_camera",
            "hako_camera_data",
            "hako_cmd_camera_move",
            "hako_camera_info"
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
                if (this.pdu_reader_camera_move == null)
                {
                    throw new ArgumentException("can not found pdu_writer:" + pdu_reader_name);
                }
                var pdu_writer_name = root_name + "_" + this.topic_name[1] + "Pdu";
                this.pdu_writer = this.pdu_io.GetWriter(pdu_writer_name);
                if (this.pdu_writer == null)
                {
                    throw new ArgumentException("can not found pdu_writer:" + pdu_writer_name);
                }
                pdu_writer_name = root_name + "_" + this.topic_name[3] + "Pdu";
                this.pdu_writer_camera_info = this.pdu_io.GetWriter(pdu_writer_name);
                if (this.pdu_writer_camera_info == null)
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
            RoboPartsConfigData[] configs = new RoboPartsConfigData[4];
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


            configs[3] = new RoboPartsConfigData();
            configs[3].io_dir = IoDir.WRITE;
            configs[3].io_method = this.io_method;
            configs[3].value.org_name = this.topic_name[3];
            configs[3].value.type = this.topic_type[3];
            configs[3].value.class_name = ConstantValues.pdu_writer_class;
            configs[3].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[3].value.pdu_size = 32 + ConstantValues.PduMetaDataSize;
            configs[3].value.write_cycle = 1;
            configs[3].value.method_type = this.comm_method.ToString();

            return configs;
        }

        /*
         * Camera Image
         */
        public int current_id = -1;
        public int request_id = 0;
        public int encode_type = 0;
        /*
         * Camera Move
         */
        public int move_current_id = -1;
        public int move_request_id = 0;
        public float move_step = 1.0f;  // 一回の動きのステップ量
        private float camera_move_button_time_duration = 0f;
        public float camera_move_button_threshold_speedup = 1.0f;

        void LateUpdate()
        {
            Vector3 parentEulerAngles = my_camera.transform.parent.eulerAngles;
            my_camera.transform.localEulerAngles = new Vector3(manual_rotation_deg - parentEulerAngles.x, 0, -parentEulerAngles.z);
        }

        public void DoControl()
        {
            /*
             * Camera Image Request
             */
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


            /*
             * Camera Move Request
             */
            bool request_move = this.pdu_reader_camera_move.GetReadOps().Ref("header").GetDataBool("request");
            if (request_move)
            {
                move_request_id = this.pdu_reader_camera_move.GetReadOps().GetDataInt32("request_id");
                if (move_current_id != move_request_id)
                {
                    move_current_id = move_request_id;
                    var target_degree = (float)this.pdu_reader_camera_move.GetReadOps().Ref("angle").GetDataFloat64("y");
                    SetCameraAngle(-target_degree);
                    Debug.Log("reqest move: " + target_degree + " current deg: " + this.manual_rotation_deg);
                    this.pdu_writer_camera_info.GetWriteOps().SetData("request_id", move_current_id);
                    this.pdu_writer_camera_info.GetWriteOps().Ref("angle").SetData("x", (double)0);
                    this.pdu_writer_camera_info.GetWriteOps().Ref("angle").SetData("y", -(double)this.manual_rotation_deg);
                    this.pdu_writer_camera_info.GetWriteOps().Ref("angle").SetData("z", (double)0);
                }
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
        private float manual_rotation_deg = 0;
        private void RotateCamera(float step)
        {
            float newPitch = manual_rotation_deg + step;

            // ピッチを-90度から15度の間に制限
            if (newPitch > 180) newPitch -= 360; // Convert angles greater than 180 to negative values
            newPitch = Mathf.Clamp(newPitch, this.camera_move_up_deg, this.camera_move_down_deg);
            manual_rotation_deg = newPitch;
        }
        private void SetCameraAngle(float angle)
        {
            float newPitch = angle;

            // ピッチを-90度から15度の間に制限
            if (newPitch > 180) newPitch -= 360; // Convert angles greater than 180 to negative values
            newPitch = Mathf.Clamp(newPitch, this.camera_move_up_deg, this.camera_move_down_deg);
            manual_rotation_deg = newPitch;
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
