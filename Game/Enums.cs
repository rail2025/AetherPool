namespace AetherPool.Game
{
    public enum TurnOutcome { KeepTurn, SwitchTurn, FoulSwitchTurn, PlayerWins, PlayerLoses }

    public enum PlayerAssignment { Unassigned, Solids, Stripes }
    public enum GameState { MainMenu, InGame, PlacingCueBall, Paused, GameOver }
    public enum GameMode { EightBall, NineBall }
    public enum Player { Human, AI }
    public enum CollisionType { Ball, Cushion, Pocket }

    public struct CollisionEvent
    {
        public CollisionType Type;
        public float ImpactVelocity;
        public int? BallNumber;
        public bool ProcessedForSound;
        public int BodyA_ID;
        public int BodyB_ID;
    }
}
