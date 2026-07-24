namespace Picker3D.Core
{
    public enum GameState
    {
        Initializing = 0,
        Ready = 1,
        PlayingPart = 2,
        WaitingForDrop = 3,
        ResolvingPart = 4,
        Transitioning = 5,
        FinalRamp = 6,
        LevelCompleted = 7,
        Failed = 8,
        LevelFinished = 9
    }
}
