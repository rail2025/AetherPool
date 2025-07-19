namespace AetherPool.Game
{
    public enum GameState
    {
        MainMenu,
        InGame,
        PlacingCueBall,
        Paused,
        GameOver
    }

    public enum GameMode { EightBall, NineBall }

    public enum Player { Human, AI }

    public enum CollisionType { Ball, Cushion, Pocket }

    public struct CollisionEvent
    {
        public CollisionType Type;
        public float ImpactVelocity;
    }
}
