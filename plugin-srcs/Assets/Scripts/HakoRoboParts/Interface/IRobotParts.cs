using System;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts
{
    public interface IRobotParts
    {
        void Initialize(System.Object root);
        RosTopicMessageConfig[] getRosConfig();
    }
}
