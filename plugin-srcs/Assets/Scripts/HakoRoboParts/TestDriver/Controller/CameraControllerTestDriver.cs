using Hakoniwa.PluggableAsset.Assets.Robot.Parts;
using Hakoniwa.PluggableAsset.Communication.Connector;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using Hakoniwa.PluggableAsset.Assets.Robot;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts.TestDriver
{
    public class CameraControllerTestDriver : MonoBehaviour, IRobotPartsController, IRobotPartsConfig
    {
        private GameObject root;
        private PduIoConnector pdu_io;
        private IPduWriter pdu_writer;
        private IPduReader pdu_reader;
        public int width = 640;
        public int height = 480;
        public string roboname = "TestObj";
        private string root_name;
        public string[] topic_name = {
            "hako_cmd_camera",
            "hako_camera_data"
        };

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
                this.pdu_io = PduIoConnector.Get(roboname);
                if (this.pdu_io == null)
                {
                    throw new ArgumentException("can not found pdu_io:" + root_name);
                }
                this.pdu_io = PduIoConnector.Get(roboname);
                if (this.pdu_io == null)
                {
                    throw new ArgumentException("can not found pdu_io:" + root_name);
                }
                var pdu_reader_name = roboname + "_" + this.topic_name[0] + "Pdu";
                this.pdu_reader = this.pdu_io.GetReader(pdu_reader_name);
                if (this.pdu_reader == null)
                {
                    throw new ArgumentException("can not found pdu_writer:" + pdu_reader_name);
                }
                var pdu_writer_name = roboname + "_" + this.topic_name[1] + "Pdu";
                this.pdu_writer = this.pdu_io.GetWriter(pdu_writer_name);
                if (this.pdu_writer == null)
                {
                    throw new ArgumentException("can not found pdu_writer:" + pdu_writer_name);
                }
            }
        }

        public int current_id = -1;
        public int request_id = 0;
        public int encode_type = 0;
        public void DoControl()
        {
            var id = this.pdu_writer.GetReadOps().GetDataInt32("request_id");
            if (id == current_id)
            {
                byte[] imageData = this.pdu_writer.GetReadOps().Ref("image").GetDataUInt8Array("data");
                Texture2D texture = new Texture2D(width, height);
                texture.LoadImage(imageData);
                Renderer renderer = GetComponent<Renderer>();
                renderer.material.mainTexture = texture;
                this.pdu_reader.GetWriteOps().Ref("header").SetData("request", false);
            }
            return;
        }
        void DoCmd()
        {
            if (this.pdu_reader.GetReadOps().Ref("header").GetDataBool("request"))
            {
                Debug.Log("Busy...");
                return;
            }
            this.pdu_reader.GetWriteOps().Ref("header").SetData("request", true);
            this.pdu_reader.GetWriteOps().Ref("header").SetData("result", false);
            this.pdu_reader.GetWriteOps().Ref("header").SetData("result_code", 0);
            this.pdu_reader.GetWriteOps().SetData("request_id", request_id);
            this.pdu_reader.GetWriteOps().SetData("encode_type", encode_type);
            current_id = request_id;
            request_id++;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("key down S");
                DoCmd();
            }
        }


        public RosTopicMessageConfig[] getRosConfig()
        {
            return new RosTopicMessageConfig[0];
        }
        public RoboPartsConfigData[] GetRoboPartsConfig()
        {
            return new RoboPartsConfigData[0];
        }

    }

}
