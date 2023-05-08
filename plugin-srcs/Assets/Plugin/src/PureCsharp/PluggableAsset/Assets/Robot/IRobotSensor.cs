using System.Collections;
using System.Collections.Generic;

namespace Hakoniwa.PluggableAsset.Assets.Robot
{
    public interface IRobotSensor: IRobotComponent
    {
        void UpdateSensorValues();
    }
}

