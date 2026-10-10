namespace Opoint8182.Game
{
    /// <summary>Why a run ended. Carried by <see cref="GameManager.RunEnded"/> so the Game Over screen can say so truthfully.</summary>
    public enum RunEndCause
    {
        OutOfFuel,
        Wrecked,
        FlewTooHigh,
        HitTheDeck,
        OutOfBounds
    }
}
