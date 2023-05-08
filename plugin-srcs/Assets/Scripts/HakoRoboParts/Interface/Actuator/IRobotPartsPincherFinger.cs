using System;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{
    public interface IRobotPartsPincherFinger : IRobotPartsActuator
    {
        float CurrentGrip();
        System.Numerics.Vector3 GetOpenPosition();

        void UpdateGrip(float grip);
        void ForceOpen();
    }
}

