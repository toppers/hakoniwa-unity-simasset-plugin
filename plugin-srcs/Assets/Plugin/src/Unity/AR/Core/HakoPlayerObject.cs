using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hakoniwa.AR.Core
{
    public class HakoPlayerObject : MonoBehaviour
    {
        IHakoPlayerState player_obj;
        private HakoPositionAndRotation curr_value = null;
        private HakoPositionAndRotation prev_value = null;

        public HakoPositionAndRotation GetPosAndRot()
        {
            HakoPositionAndRotation pr = new HakoPositionAndRotation();
            pr.position = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);
            pr.rotation = new Vector3(this.transform.eulerAngles.x, this.transform.eulerAngles.y, this.transform.eulerAngles.z);
            pr.name = new string(this.transform.name);
            if (this.player_obj != null)
            {
                pr.state = this.player_obj.GetState();
            }
            this.curr_value = pr;
            return pr;
        }
        public void SetPrevValue(HakoPositionAndRotation value)
        {
            this.prev_value = value;
        }
        public bool IsChanged()
        {
            if (this.curr_value == null)
            {
                return true;
            }
            if (this.prev_value == null)
            {
                return true;
            }
            if (this.curr_value.position.Equals(this.prev_value.position) == false)
            {
                return true;
            }
            if (this.curr_value.rotation.Equals(this.prev_value.rotation) == false)
            {
                return true;
            }
            if (this.curr_value.state != this.prev_value.state)
            {
                return true;
            }
            return false;
        }

        // Start is called before the first frame update
        void Start()
        {
            this.player_obj = this.GetComponentInChildren<IHakoPlayerState>();
            if (this.player_obj == null)
            {
                Debug.Log("player obj is null: " + this.gameObject.name);
            }
            else
            {
                Debug.Log("player obj is not null: " + this.gameObject.name);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
