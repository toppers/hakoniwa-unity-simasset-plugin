using System;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{
    public interface IRobotPartsMotor : IRobotPartsActuator
    {
        void SetForce(int force);
        void SetTargetVelicty(float targetVelocity);
    }
}
