using System;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{
    public interface IRobotPartsController : IRobotParts
    {
        void DoControl();
    }
}
