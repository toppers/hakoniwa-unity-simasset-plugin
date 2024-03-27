using System;
using System.Collections;
using System.Collections.Generic;
using Hakoniwa.PluggableAsset.Communication.Connector;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using UnityEngine;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{
    public class MagnetHolder : MonoBehaviour, IRobotPartsController, IRobotPartsSensor, IRobotPartsConfig
    {
        private GameObject root;
        public bool magnet_on = false;
        public float forceMagnitude = 100.0f;
        public float distance = 0.5f;
        private int contact_num;
        public bool contact_on = false;
        private List<Rigidbody> rds = new List<Rigidbody>();
        private string root_name;
        private PduIoConnector pdu_io;
        private IPduWriter pdu_writer;
        private IPduReader pdu_reader;

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
                GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("HakoAssetMagnet");
                foreach (GameObject ob in gameObjects)
                {
                    Rigidbody rb = ob.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rds.Add(rb);
                    }
                }
                Debug.Log($"MagnetHolder Found {rds.Count} Rigidbody components.");
            }
        }
        private List<Rigidbody> GetTargets()
        {
            List<Rigidbody> targets = new List<Rigidbody>();
            foreach (Rigidbody rb in rds)
            {
                if (Vector3.Distance(transform.position, rb.transform.position) <= distance)
                {
                    targets.Add(rb);
                }
            }
            return targets;
        }
        void ContactCheck()
        {
            if (contact_num > 0)
            {
                contact_on = true;
            }
            else
            {
                contact_on = false;
            }
        }
        void OnTriggerEnter(Collider other)
        {
            //if (contact_num == 0)
            {
                contact_num++;
            }
            ContactCheck();
            //Debug.Log("contact_num: " + contact_num);
        }

        void OnTriggerExit(Collider other)
        {
            //if (contact_num > 0)
            {
                contact_num--;
            }
            ContactCheck();
            //Debug.Log("contact_num: " + contact_num);
        }

        public bool isAttachedSpecificController()
        {
            return true;
        }

        public void UpdateSensorValues()
        {
            this.pdu_writer.GetWriteOps().SetData("magnet_on", magnet_on);
            this.pdu_writer.GetWriteOps().SetData("contact_on", contact_on);
        }
        private void DoCmd()
        {
            bool request = this.pdu_reader.GetReadOps().Ref("header").GetDataBool("request");
            if (request)
            {
                this.magnet_on = this.pdu_reader.GetReadOps().GetDataBool("magnet_on");
                //reply
                this.pdu_reader.GetWriteOps().Ref("header").SetData("request", false);
                this.pdu_reader.GetWriteOps().Ref("header").SetData("result", true);
                this.pdu_reader.GetWriteOps().Ref("header").SetData("result_code", 0);
            }
        }
        public void DoControl()
        {
            this.DoCmd();
            if (magnet_on)
            {
                var targets = GetTargets();
                foreach (var rd in targets)
                {
                    Vector3 forceDirection = (transform.position - rd.position).normalized;
                    rd.AddForce(forceDirection * forceMagnitude, ForceMode.Force);
                }
            }
        }
        public string [] topic_type = {
            "hako_msgs/HakoCmdMagnetHolder",
            "hako_msgs/HakoStatusMagnetHolder"
        };
        public string [] topic_name = {
            "hako_cmd_magnet_holder",
            "hako_status_magnet_holder"
        };
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
            configs[0].value.pdu_size = 16;
            configs[0].value.write_cycle = 1;
            configs[0].value.method_type = this.comm_method.ToString();

            configs[1] = new RoboPartsConfigData();
            configs[1].io_dir = IoDir.WRITE;
            configs[1].io_method = this.io_method;
            configs[1].value.org_name = this.topic_name[1];
            configs[1].value.type = this.topic_type[1];
            configs[1].value.class_name = ConstantValues.pdu_writer_class;
            configs[1].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[1].value.pdu_size = 8;
            configs[1].value.write_cycle = 1;
            configs[1].value.method_type = this.comm_method.ToString();
            return configs;
        }

    }

}
