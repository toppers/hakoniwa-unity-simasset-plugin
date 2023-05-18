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
    public class CameraSensorTestDriver : MonoBehaviour, IRobotPartsSensor, IRobotPartsConfig
    {
        private GameObject root;
        private PduIoConnector pdu_io;
        private IPduWriter pdu_writer;
        private string root_name;

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
                var pdu_writer_name = roboname + "_" + this.topic_name + "Pdu";
                this.pdu_writer = this.pdu_io.GetWriter(pdu_writer_name);
                if (this.pdu_writer == null)
                {
                    throw new ArgumentException("can not found pdu_reader:" + pdu_writer_name);
                }
            }
        }


        private int count = 0;
        public void UpdateSensorValues()
        {
            this.count++;
            if (this.count < this.update_cycle)
            {
                return;
            }
            this.count = 0;
            byte[] imageData = this.pdu_writer.GetReadOps().GetDataUInt8Array("data");
            Texture2D texture = new Texture2D(640, 400);
            texture.LoadImage(imageData);
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.mainTexture = texture;
            return;
        }


        public bool isAttachedSpecificController()
        {
            return false;
        }

        public int update_cycle = 100;
        public string topic_name = "camera_image_jpg";
        public string roboname = "SampleRobot";

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
