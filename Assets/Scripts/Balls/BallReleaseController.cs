using Picker3D.Collectibles;

namespace Picker3D.Balls
{
    public sealed class BallReleaseController : CollectibleReleaseController
    {
        public void ReleaseCollectedBalls()
        {
            ReleaseCollectedItems();
        }
    }
}
