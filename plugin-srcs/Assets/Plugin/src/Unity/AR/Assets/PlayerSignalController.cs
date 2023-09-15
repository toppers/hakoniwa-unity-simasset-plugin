using System.Collections;
using System.Collections.Generic;
using Hakoniwa.AR.Core;
using UnityEngine;

namespace Hakoniwa.AR.Assets
{
    public class SignalController : MonoBehaviour, IHakoPlayerState
    {
        public Renderer signal_red;
        public Renderer signal_yellow;
        public Renderer signal_blue;

        public Material[] reds;
        public Material[] yellow;
        public Material[] blue;

        int count = 0;

        void Start()
        {
            this.signal_red.material = reds[1];
            this.signal_yellow.material = yellow[0];
            this.signal_blue.material = blue[0];
        }
        int signal_state = 0;
        int next_signal_state = 0;

        void FixedUpdate()
        {
            count++;
            if ((count % 100) != 0)
            {
                return;
            }
            signal_state = next_signal_state;
            if (signal_state == 0)
            {
                this.signal_red.material = reds[0];
                this.signal_yellow.material = yellow[1];
                this.signal_blue.material = blue[0];
                next_signal_state = 1;
            }
            else if (signal_state == 1)
            {
                this.signal_red.material = reds[0];
                this.signal_yellow.material = yellow[0];
                this.signal_blue.material = blue[1];
                next_signal_state = 2;
            }
            else
            {
                this.signal_red.material = reds[1];
                this.signal_yellow.material = yellow[0];
                this.signal_blue.material = blue[0];
                next_signal_state = 0;
            }
        }

        public int GetState()
        {
            return this.signal_state;
        }
    }
}

