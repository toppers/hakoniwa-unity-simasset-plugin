using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hakoniwa.AR.Core
{
    public class HakoAvatorObject : MonoBehaviour
    {
        private IHakoAvatorState avator_obj;

        public void SetPosAndRot(HakoPositionAndRotation pr)
        {
            // GameObjectの位置と姿勢を設定
            this.transform.position = pr.position;
            this.transform.eulerAngles = pr.rotation;
            if (this.avator_obj != null)
            {
                this.avator_obj.SetState(pr.state);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            this.avator_obj = this.GetComponentInChildren<IHakoAvatorState>();
            if (this.avator_obj == null)
            {
                Debug.Log("avator obj is null: " + this.gameObject.name);
            }
            else
            {
                Debug.Log("avator obj is not null: " + this.gameObject.name);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
