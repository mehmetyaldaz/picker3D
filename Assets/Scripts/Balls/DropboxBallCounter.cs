using Picker3D.Collectibles;

namespace Picker3D.Balls
{
    public sealed class DropboxBallCounter : DropboxCollectibleCounter
    {
        public void ConsumeAllCountedBalls()
        {
            ConsumeAllCountedItems();
        }
    }
}
