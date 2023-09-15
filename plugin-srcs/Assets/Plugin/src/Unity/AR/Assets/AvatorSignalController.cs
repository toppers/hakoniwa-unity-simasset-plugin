using System.Collections;
using System.Collections.Generic;
using Hakoniwa.AR.Core;
using UnityEngine;

namespace Hakoniwa.AR.Assets
{
    public class AvatorSignalController : MonoBehaviour, IHakoAvatorState
    {
        public Renderer signal_red;
        public Renderer signal_yellow;
        public Renderer signal_blue;

        public Material[] reds;
        public Material[] yellow;
        public Material[] blue;

        void Start()
        {
            this.signal_red.material = reds[1];
            this.signal_yellow.material = yellow[0];
            this.signal_blue.material = blue[0];
        }
        int signal_state = 0;
        void FixedUpdate()
        {
            if (signal_state == 0)
            {
                this.signal_red.material = reds[0];
                this.signal_yellow.material = yellow[1];
                this.signal_blue.material = blue[0];
            }
            else if (signal_state == 1)
            {
                this.signal_red.material = reds[0];
                this.signal_yellow.material = yellow[0];
                this.signal_blue.material = blue[1];
            }
            else
            {
                this.signal_red.material = reds[1];
                this.signal_yellow.material = yellow[0];
                this.signal_blue.material = blue[0];
            }
        }

        public void SetState(int state)
        {
            this.signal_state = state;
        }
    }

}
