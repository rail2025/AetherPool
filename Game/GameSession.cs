using AetherPool.Game.GameObjects;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Linq;

namespace AetherPool.Game
{
    public class GameSession
    {
        public GameState CurrentState { get; private set; }
        public GameMode CurrentMode { get; private set; }
        public Player CurrentTurn { get; private set; }
        public Player? Winner { get; private set; }
        public PlayerAssignment Player1Assignment { get; set; } = PlayerAssignment.Unassigned;
        public PlayerAssignment Player2Assignment { get; set; } = PlayerAssignment.Unassigned;

        public GameBoard Board { get; private set; }
        public List<PoolBall> Balls { get; private set; } = new();
        public CueStick Cue { get; private set; }

        private Stack<List<PoolBall>> turnHistory = new();
        public List<CollisionEvent> CollisionEvents { get; } = new();
        public List<PoolBall> LastTurnSunkBalls { get; } = new();
        public PoolBall? FirstBallHitThisTurn { get; set; }

        private const float MaxShotSpeed = 1000f;
        private float aiShotDelay = 0f;

        public GameSession()
        {
            Board = new GameBoard(600, 300, 12f);
            Cue = new CueStick(new(Board.Width * 0.75f, Board.Height / 2));
            CurrentState = GameState.MainMenu;
        }

        public void StartNewGame(GameMode mode)
        {
            Balls.Clear();
            turnHistory.Clear();
            LastTurnSunkBalls.Clear();
            CurrentMode = mode;
            Player1Assignment = PlayerAssignment.Unassigned;
            Player2Assignment = PlayerAssignment.Unassigned;
            Winner = null;
            SetupBalls();
            CurrentTurn = Player.Human;
            CurrentState = GameState.InGame;
        }

        public void Update(float deltaTime)
        {
            if (CurrentState == GameState.GameOver || CurrentState == GameState.MainMenu) return;

            var wasMoving = AreBallsMoving();
            if (CurrentState == GameState.InGame)
            {
                CollisionEvents.Clear();
                PhysicsEngine.Update(Balls, Board, deltaTime, CollisionEvents, this);
            }
            var isMoving = AreBallsMoving();

            if (wasMoving && !isMoving)
            {
                var outcome = GameRules.ProcessTurnEnd(this);
                HandleTurnOutcome(outcome);
            }

            if (CurrentTurn == Player.AI && !isMoving && CurrentState == GameState.InGame)
            {
                aiShotDelay -= deltaTime;
                if (aiShotDelay <= 0)
                {
                    var (angle, power, aimOffset) = AIPoolPlayer.FindBestShot(this);
                    FireCueBall(angle, power, aimOffset);
                }
            }
        }

        private void HandleTurnOutcome(TurnOutcome outcome)
        {
            var pocketedThisTurn = Balls.Where(b => b.IsSunk && !LastTurnSunkBalls.Contains(b)).ToList();

            if (outcome == TurnOutcome.FoulSwitchTurn && CurrentMode == GameMode.NineBall)
            {
                foreach (var ball in pocketedThisTurn)
                {
                    if (ball.Number != 0)
                    {
                        SpotBall(ball);
                    }
                }
            }

            LastTurnSunkBalls.AddRange(pocketedThisTurn);

            switch (outcome)
            {
                case TurnOutcome.KeepTurn:
                    if (CurrentTurn == Player.AI)
                    {
                        aiShotDelay = 1.5f;
                    }
                    break;
                case TurnOutcome.SwitchTurn:
                    SwitchTurn();
                    break;
                case TurnOutcome.FoulSwitchTurn:
                    SwitchTurn();
                    if (CurrentTurn == Player.AI)
                    {
                        var bestPos = AIPoolPlayer.FindBestCueBallPlacement(this);
                        PlaceCueBall(bestPos);
                        aiShotDelay = 1.5f;
                    }
                    else
                    {
                        EnterPlaceCueBallState();
                    }
                    break;
                case TurnOutcome.PlayerWins:
                    EndGame(CurrentTurn);
                    break;
                case TurnOutcome.PlayerLoses:
                    EndGame(CurrentTurn == Player.Human ? Player.AI : Player.Human);
                    break;
            }
        }

        public void FireCueBall(float angle, float power, Vector2 aimOffset)
        {
            var cueBall = Balls.FirstOrDefault(b => b.Number == 0 && !b.IsSunk);
            if (cueBall == null) return;

            SaveTurnState();
            FirstBallHitThisTurn = null;
            LastTurnSunkBalls.Clear();

            var initialSpeed = MaxShotSpeed * power;
            cueBall.Velocity = new Vector2(MathF.Cos(angle) * initialSpeed, MathF.Sin(angle) * initialSpeed);
            cueBall.Spin = new Vector3(-aimOffset.Y, aimOffset.X, 0) * power * 100f;
        }

        public bool AreBallsMoving()
        {
            return Balls.Any(b => !b.IsSunk && b.Velocity.LengthSquared() > 0.1f);
        }

        public void SwitchTurn()
        {
            CurrentTurn = (CurrentTurn == Player.Human) ? Player.AI : Player.Human;
            if (CurrentTurn == Player.AI)
            {
                aiShotDelay = 1.5f;
            }
        }

        public void EnterPlaceCueBallState()
        {
            var cueBall = Balls.FirstOrDefault(b => b.Number == 0);
            if (cueBall != null)
            {
                cueBall.IsSunk = true;
                CurrentState = GameState.PlacingCueBall;
            }
        }

        public void PlaceCueBall(Vector2 position)
        {
            var cueBall = Balls.FirstOrDefault(b => b.Number == 0);
            if (cueBall != null)
            {
                cueBall.Position = position;
                cueBall.IsSunk = false;
                cueBall.Velocity = Vector2.Zero;
                CurrentState = GameState.InGame;
            }
        }

        public void EndGame(Player winner)
        {
            Winner = winner;
            CurrentState = GameState.GameOver;
        }

        public void UndoLastShot()
        {
            if (turnHistory.Count > 0)
            {
                var previousState = turnHistory.Pop();
                Balls.Clear();
                foreach (var ballState in previousState)
                {
                    Balls.Add(new PoolBall(ballState.Position, ballState.Radius, ballState.Number, ballState.Color, ballState.IsStriped)
                    {
                        IsSunk = ballState.IsSunk,
                        Velocity = ballState.Velocity,
                        Spin = ballState.Spin
                    });
                }
                CurrentTurn = Player.Human;
                CurrentState = GameState.InGame;
            }
        }

        private void SaveTurnState()
        {
            var currentState = new List<PoolBall>();
            foreach (var ball in Balls)
            {
                currentState.Add(new PoolBall(ball.Position, ball.Radius, ball.Number, ball.Color, ball.IsStriped)
                {
                    IsSunk = ball.IsSunk,
                    Velocity = ball.Velocity,
                    Spin = ball.Spin
                });
            }
            turnHistory.Push(currentState);
        }

        private void SpotBall(PoolBall ball)
        {
            ball.IsSunk = false;
            ball.Velocity = Vector2.Zero;
            ball.Spin = Vector3.Zero;
            ball.Position = new Vector2(Board.Width * 0.25f, Board.Height / 2);

            while (Balls.Any(b => !b.IsSunk && b != ball && Vector2.DistanceSquared(b.Position, ball.Position) < ball.Radius * ball.Radius * 4))
            {
                ball.Position.X += ball.Radius;
            }
        }

        private void SetupBalls()
        {
            float ballRadius = 8f;
            float startY = Board.Height / 2;

            var colors = new uint[]
            {
                0xFFFFFFFF, 0xFF00D4FF, 0xFF0000FF, 0xFFFF0000, 0xFF800080, 0xFFFFA500, 0xFF008000, 0xFF800000,
                0xFF000000, 0xFF00D4FF, 0xFF0000FF, 0xFFFF0000, 0xFF800080, 0xFFFFA500, 0xFF008000, 0xFF800000
            };

            Balls.Add(new PoolBall(new Vector2(Board.Width * 0.75f, startY), ballRadius, 0, colors[0]));

            if (CurrentMode == GameMode.EightBall)
            {
                float startX = Board.Width * 0.25f;
                float xGap = MathF.Sqrt(3) * ballRadius;
                int ballNum = 1;
                for (int row = 0; row < 5; row++)
                {
                    for (int col = 0; col <= row; col++)
                    {
                        float y = startY + (col * 2 * ballRadius) - (row * ballRadius);
                        float x = startX - (row * xGap);
                        bool isStriped = ballNum > 8;
                        Balls.Add(new PoolBall(new Vector2(x, y), ballRadius, ballNum, colors[ballNum], isStriped));
                        ballNum++;
                    }
                }
            }
            else if (CurrentMode == GameMode.NineBall)
            {
                var rackPositions = new List<(int row, int col)>
                {
                    (0, 0), (1, -1), (1, 1), (2, -2), (2, 0), (2, 2), (3, -1), (3, 1), (4, 0)
                };

                var middleNumbers = new List<int> { 2, 3, 4, 5, 6, 7, 8 };
                var random = new Random();
                middleNumbers = middleNumbers.OrderBy(x => random.Next()).ToList();

                var finalNumbers = new List<int> { 1 };
                finalNumbers.AddRange(middleNumbers.Take(3));
                finalNumbers.Add(9);
                finalNumbers.AddRange(middleNumbers.Skip(3));

                for (int i = 0; i < rackPositions.Count; i++)
                {
                    var pos = rackPositions[i];
                    int ballNum = finalNumbers[i];
                    float x = Board.Width * 0.25f - pos.row * (MathF.Sqrt(3) * ballRadius);
                    float y = startY + pos.col * ballRadius;
                    Balls.Add(new PoolBall(new Vector2(x, y), ballRadius, ballNum, colors[ballNum], ballNum > 8));
                }
            }
        }
    }
}
