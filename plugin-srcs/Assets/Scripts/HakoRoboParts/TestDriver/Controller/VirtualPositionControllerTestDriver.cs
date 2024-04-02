using System;
using System.Collections;
using System.Collections.Generic;
using Hakoniwa.PluggableAsset.Communication.Connector;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using UnityEngine;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts.TestDriver
{
    public class VirtualPositionControllerTestDriver : MonoBehaviour, IRobotPartsController, IRobotPartsConfig
    {
        private GameObject root;
        private PduIoConnector pdu_io;
        private IPduReader pdu_reader;
        private string root_name;
        public bool isTouched = false;

        public int update_cycle = 1;
        public string[] topic_name = {
            "cmd_pos"
        };
        public string roboname = "TestObj";
        public RosTopicMessageConfig[] getRosConfig()
        {
            return new RosTopicMessageConfig[0];
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
            }
        }
        private double pos_x = 0;
        private double euler_yaw = 0;
        private double delta_x = 0.1;
        private double delta_yaw = 1;
        public void DoControl()
        {
            this.pdu_reader.GetWriteOps().Ref("linear").SetData("x", pos_x);
            this.pdu_reader.GetWriteOps().Ref("angular").SetData("z", euler_yaw);
        }
        public bool isAttachedSpecificController()
        {
            return false;
        }

        public RoboPartsConfigData[] GetRoboPartsConfig()
        {
            return new RoboPartsConfigData[0];
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                pos_x += delta_x;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                pos_x -= delta_x;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                euler_yaw += delta_yaw;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                euler_yaw -= delta_yaw;
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                pos_x = 0;
                euler_yaw = 0;
            }

        }
    }
}
