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
        private bool is_radio_control = false;
        public string game_ops_name = "hako_cmd_game";
        public int game_ops_arm_button_index = 0;
        public int game_ops_magnet_button_index = 1;
        private IPduReader pdu_reader_game_ops;
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
        public struct RigidbodyInfo
        {
            public Rigidbody Rigidbody;
            public Transform OriginalParent;
            public GameObject gameObject;
        }
        private List<RigidbodyInfo> attachedRigidbodyInfos = new List<RigidbodyInfo>();
        private List<RigidbodyInfo> targets;

        static public List<RigidbodyInfo> GetTargets()
        {
            List<RigidbodyInfo> targets = new List<RigidbodyInfo>();
            GameObject[] magnetObjects = GameObject.FindGameObjectsWithTag("HakoAssetMagnet");

            foreach (GameObject magnetObj in magnetObjects)
            {
                Rigidbody rb = magnetObj.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    targets.Add(new RigidbodyInfo
                    {
                        Rigidbody = rb,
                        OriginalParent = magnetObj.transform.parent,
                        gameObject = magnetObj
                    });
                }
            }
            return targets;
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
                this.targets = GetTargets();
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
                pdu_reader_name = root_name + "_" + this.game_ops_name + "Pdu";
                this.pdu_reader_game_ops = this.pdu_io.GetReader(pdu_reader_name);
                if (this.pdu_reader_game_ops == null)
                {
                    throw new ArgumentException("can not found pdu_reader:" + pdu_reader_name);
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

        void OnTriggerEnter(Collider other)
        {
            if (magnet_on == false)
            {
                return;
            }
            if (other.gameObject.name == "Magnet")
            {
                return;
            }
            //Debug.Log("contact obj:" + other.gameObject);
            var rb = other.gameObject.GetComponentInChildren<Rigidbody>();
            foreach (var info in attachedRigidbodyInfos)
            {
                if (info.Rigidbody == rb)
                {
                    //Debug.Log("already attached");
                    return;
                }
            }
            foreach (var info in targets)
            {
                if (info.Rigidbody == rb)
                {
                    //Debug.Log("attached: " + other.gameObject);
                    RigidbodyInfo newInfo = new RigidbodyInfo
                    {
                        Rigidbody = rb,
                        OriginalParent = info.OriginalParent,
                        gameObject = info.gameObject
                    };
                    attachedRigidbodyInfos.Add(newInfo);
                    rb.isKinematic = true;
                    info.gameObject.transform.parent = this.transform;
                    //Debug.Log("target = " + info.gameObject);
                    //Debug.Log("isKinematic = " + rb.isKinematic);
                    //Debug.Log("parent = " + info.gameObject.transform.parent);
                    contact_on = true;
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
        }

        public bool isAttachedSpecificController()
        {
            return false;
        }

        public void UpdateSensorValues()
        {
            //Debug.Log("contact_on: " + contact_on);
            //Debug.Log("magnet_on: " + magnet_on);
            this.pdu_writer.GetWriteOps().SetData("magnet_on", magnet_on);
            this.pdu_writer.GetWriteOps().SetData("contact_on", contact_on);
        }
        private void DoCmd()
        {
            bool request = this.pdu_reader.GetReadOps().Ref("header").GetDataBool("request");
            if (request)
            {
                this.magnet_on = this.pdu_reader.GetReadOps().GetDataBool("magnet_on");
            }
        }


        public void DoControl()
        {
            bool[] button_array = this.pdu_reader_game_ops.GetReadOps().GetDataBoolArray("button");
            if (button_array[this.game_ops_arm_button_index])
            {
                is_radio_control = true;
            }
            if (is_radio_control)
            {
                this.magnet_on = button_array[this.game_ops_magnet_button_index];
            }
            else
            {
                this.DoCmd();
            }
            if (magnet_on)
            {
                foreach (var obj in targets)
                {
                    if (!attachedRigidbodyInfos.Exists(info => info.gameObject == obj.gameObject))
                    {
                        if (Vector3.Distance(transform.position, obj.Rigidbody.transform.position) <= distance)
                        {
                            Vector3 forceDirection = (transform.position - obj.Rigidbody.transform.position).normalized;
                            obj.Rigidbody.AddForce(forceDirection * forceMagnitude, ForceMode.Force);
                            //Debug.Log("add force");
                        }
                    }
                }
            }
            else
            {
                foreach (var info in attachedRigidbodyInfos)
                {
                    info.gameObject.transform.parent = info.OriginalParent;
                    info.Rigidbody.isKinematic = false;
                    //Debug.Log("detached target: " + info.gameObject);
                    //Debug.Log("detached org parent " + info.OriginalParent);
                }
                attachedRigidbodyInfos.Clear();
                contact_on = false;
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
