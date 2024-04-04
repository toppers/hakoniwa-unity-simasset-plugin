using System;
using System.Collections;
using System.Collections.Generic;
using Hakoniwa.PluggableAsset.Communication.Connector;
using Hakoniwa.PluggableAsset.Communication.Pdu;
using UnityEngine;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{
    public class VirtualPositionController : MonoBehaviour, IRobotPartsController, IRobotPartsConfig
    {
        private GameObject root;
        private string root_name;
        private PduIoConnector pdu_io;
        private IPduReader pdu_reader;
        private float base_rotation_y;
        private Rigidbody rd; // need rigidbody for trasporting baggage..
        public string[] topic_type = {
            "geometry_msgs/Twist"
        };
        public string[] topic_name = {
            "cmd_pos"
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
                this.base_rotation_y = this.root.transform.eulerAngles.y;
                var pdu_reader_name = root_name + "_" + this.topic_name[0] + "Pdu";
                this.pdu_reader = this.pdu_io.GetReader(pdu_reader_name);
                if (this.pdu_reader == null)
                {
                    throw new ArgumentException("can not found pdu_writer:" + pdu_reader_name);
                }
                this.rd = this.root.GetComponentInChildren<Rigidbody>();
                if (this.rd == null)
                {
                    throw new ArgumentException("Can not find Rigidbody on " + root_name);
                }
            }
        }
        public void DoControl()
        {
            Vector3 pos = new Vector3();
            Vector3 euler = new Vector3();

            pos.x = -(float)this.pdu_reader.GetReadOps().Ref("linear").GetDataFloat64("y");
            pos.y = (float)this.pdu_reader.GetReadOps().Ref("linear").GetDataFloat64("z");
            pos.z = (float)this.pdu_reader.GetReadOps().Ref("linear").GetDataFloat64("x");

            euler.x = -(float)this.pdu_reader.GetReadOps().Ref("angular").GetDataFloat64("y");
            euler.y = (float)this.pdu_reader.GetReadOps().Ref("angular").GetDataFloat64("z") + this.base_rotation_y;
            euler.z = (float)this.pdu_reader.GetReadOps().Ref("angular").GetDataFloat64("x");

            //Debug.Log("pos: " + pos);
            //Debug.Log("euler: " + euler);
            //this.rd.MovePosition(pos); // can not move smothly and baggage is dropped down..
            Vector3 startPosition = this.rd.position;
            Vector3 endPosition = pos;
            float speed = 1.0f;
            float step = speed * Time.deltaTime;
            this.rd.MovePosition(Vector3.Lerp(startPosition, endPosition, step));

        }
        public RosTopicMessageConfig[] getRosConfig()
        {
            return RoboPartsConfigData.getRosConfig(this.GetRoboPartsConfig(), this.topic_name, this.topic_type, null);
        }
        public IoMethod io_method = IoMethod.SHM;
        public CommMethod comm_method = CommMethod.DIRECT;
        public RoboPartsConfigData[] GetRoboPartsConfig()
        {
            RoboPartsConfigData[] configs = new RoboPartsConfigData[1];
            configs[0] = new RoboPartsConfigData();
            configs[0].io_dir = IoDir.READ;
            configs[0].io_method = this.io_method;
            configs[0].value.org_name = this.topic_name[0];
            configs[0].value.type = this.topic_type[0];
            configs[0].value.class_name = ConstantValues.pdu_writer_class;
            configs[0].value.conv_class_name = ConstantValues.conv_pdu_writer_class;
            configs[0].value.pdu_size = ConstantValues.Twist_pdu_size;
            configs[0].value.write_cycle = 1;
            configs[0].value.method_type = this.comm_method.ToString();
            return configs;
        }

    }

}

