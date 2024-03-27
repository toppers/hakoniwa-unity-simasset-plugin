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
    public class MagnetHolderTestDriver : MonoBehaviour, IRobotPartsSensor, IRobotPartsConfig
    {
        private Renderer my_renderer;
        private Color initial_color;
        private GameObject root;
        private PduIoConnector pdu_io;
        private IPduReader pdu_reader;
        private IPduWriter pdu_writer;
        private string root_name;
        public bool isTouched = false;

        public int update_cycle = 100;
        public string[] topic_name = {
            "hako_cmd_magnet_holder",
            "hako_status_magnet_holder"
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
                this.my_renderer = GetComponent<Renderer>();
                if (my_renderer == null)
                {
                    this.my_renderer = GetComponentInChildren<Renderer>();
                }
                this.initial_color = my_renderer.material.color;
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
                var pdu_writer_name = roboname + "_" + this.topic_name[1] + "Pdu";
                this.pdu_writer = this.pdu_io.GetWriter(pdu_writer_name);
                if (this.pdu_writer == null)
                {
                    throw new ArgumentException("can not found pdu_writer:" + pdu_writer_name);
                }
            }
        }

        public bool isAttachedSpecificController()
        {
            return false;
        }

        public void UpdateSensorValues()
        {
            this.isTouched = this.pdu_writer.GetReadOps().GetDataBool("contact_on");
        }
        public RoboPartsConfigData[] GetRoboPartsConfig()
        {
            return new RoboPartsConfigData[0];
        }
        void DoCmd(bool magnet_on)
        {
            if (this.pdu_reader.GetReadOps().Ref("header").GetDataBool("request"))
            {
                Debug.Log("Busy...");
                return;
            }
            Debug.Log("DoCmd: magnet_on=" +magnet_on);
            this.pdu_reader.GetWriteOps().Ref("header").SetData("request", true);
            this.pdu_reader.GetWriteOps().Ref("header").SetData("result", false);
            this.pdu_reader.GetWriteOps().Ref("header").SetData("result_code", 0);
            this.pdu_reader.GetWriteOps().SetData("magnet_on", magnet_on);
        }

        void Update()
        {
            if (this.isTouched)
            {
                my_renderer.material.color = Color.red;
            }
            else
            {
                my_renderer.material.color = Color.blue;
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                Debug.Log("key down J");
                DoCmd(true);
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                Debug.Log("key down K");
                DoCmd(false);
            }

        }
    }
}