using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using AetherPool.Game.GameObjects;
using AetherPool.Networking;

namespace AetherPool.Game
{
    public class MultiplayerGameSession
    {
        private readonly NetworkManager networkManager;

        public GameState CurrentState { get; private set; }
        public Player CurrentTurn { get; private set; }
        public bool IsMyTurn => CurrentTurn == myPlayer;
        public GameBoard Board { get; private set; }
        public List<PoolBall> Balls { get; private set; } = new();
        public CueStick Cue { get; private set; }

        private readonly Player myPlayer;
        private bool isHost;
        private bool receivedInitialState = false;

        public MultiplayerGameSession(NetworkManager manager)
        {
            this.networkManager = manager;
            this.Board = new GameBoard(600, 300, 12f);
            this.Cue = new CueStick(new(Board.Width * 0.75f, Board.Height / 2));
            this.CurrentState = GameState.InGame;

            this.isHost = true; // This should be determined by a network handshake
            this.myPlayer = isHost ? Player.Human : Player.AI;
            this.CurrentTurn = Player.Human;

            if (isHost)
            {
                SetupBalls(); // Calls its own private method now
                SendFullGameState();
                receivedInitialState = true;
            }
        }

        public void Update(float deltaTime)
        {
            if (CurrentState == GameState.GameOver || !receivedInitialState) return;

            var wasMoving = Balls.Any(b => b.Velocity.LengthSquared() > 0.1f);
            if (wasMoving)
            {
                // In a multiplayer session, the physics engine doesn't need the single-player session context
                PhysicsEngine.Update(Balls, Board, deltaTime, new List<CollisionEvent>(), null);
            }
        }

        public void FireCueBall(float angle, float power, Vector2 aimOffset)
        {
            if (!IsMyTurn) return;

            var cueBall = Balls.FirstOrDefault(b => b.Number == 0 && !b.IsSunk);
            if (cueBall == null) return;

            var initialSpeed = 1000f * power;
            cueBall.Velocity = new Vector2(MathF.Cos(angle) * initialSpeed, MathF.Sin(angle) * initialSpeed);
            cueBall.Spin = new Vector3(-aimOffset.Y, aimOffset.X, 0) * power * 100f;

            SendShootAction(angle, power, aimOffset);
        }

        public void GoToMainMenu()
        {
            this.CurrentState = GameState.MainMenu;
        }

        #region Network Handling
        public void HandleNetworkPayload(NetworkPayload payload)
        {
            switch (payload.Action)
            {
                case PayloadActionType.FullGameState:
                    ReceiveFullGameState(payload.Data);
                    break;
                case PayloadActionType.Shoot:
                    ReceiveShootAction(payload.Data);
                    break;
            }
        }

        private void SendFullGameState()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(Balls.Count);
                foreach (var ball in Balls)
                {
                    writer.Write(ball.Number);
                    writer.Write(ball.Position.X);
                    writer.Write(ball.Position.Y);
                }
                var payload = new NetworkPayload { Action = PayloadActionType.FullGameState, Data = ms.ToArray() };
                _ = networkManager.SendStateUpdateAsync(payload);
            }
        }

        private void ReceiveFullGameState(byte[]? data)
        {
            if (data == null || isHost) return;

            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                Balls.Clear();
                int ballCount = reader.ReadInt32();
                for (int i = 0; i < ballCount; i++)
                {
                    int number = reader.ReadInt32();
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    Balls.Add(new PoolBall(new Vector2(x, y), 8f, number, 0));
                }
            }
            receivedInitialState = true;
        }

        private void SendShootAction(float angle, float power, Vector2 aimOffset)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(angle);
                writer.Write(power);
                writer.Write(aimOffset.X);
                writer.Write(aimOffset.Y);
                var payload = new NetworkPayload { Action = PayloadActionType.Shoot, Data = ms.ToArray() };
                _ = networkManager.SendStateUpdateAsync(payload);
            }
        }

        private void ReceiveShootAction(byte[]? data)
        {
            if (data == null || IsMyTurn) return;

            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                float angle = reader.ReadSingle();
                float power = reader.ReadSingle();
                float aimOffsetX = reader.ReadSingle();
                float aimOffsetY = reader.ReadSingle();

                var cueBall = Balls.FirstOrDefault(b => b.Number == 0 && !b.IsSunk);
                if (cueBall != null)
                {
                    var initialSpeed = 1000f * power;
                    cueBall.Velocity = new Vector2(MathF.Cos(angle) * initialSpeed, MathF.Sin(angle) * initialSpeed);
                    cueBall.Spin = new Vector3(-aimOffsetY, aimOffsetX, 0) * power * 100f;
                }
            }
        }
        #endregion

        private void SetupBalls()
        {
            var random = new Random(12345); // Using a fixed seed ensures both clients have the same rack
            this.Balls.Clear();
            float ballRadius = 8f;
            float startY = this.Board.Height / 2;
            var colors = new uint[]
            {
                0xFFFFFFFF, 0xFF00D4FF, 0xFF0000FF, 0xFFFF0000, 0xFF800080, 0xFFFFA500, 0xFF008000, 0xFF800000,
                0xFF000000, 0xFF00D4FF, 0xFF0000FF, 0xFFFF0000, 0xFF800080, 0xFFFFA500, 0xFF008000, 0xFF800000
            };
            this.Balls.Add(new PoolBall(new Vector2(this.Board.Width * 0.75f, startY), ballRadius, 0, colors[0]));

            float startX = this.Board.Width * 0.25f;
            var solids = new List<int> { 1, 2, 3, 4, 5, 6, 7 }.OrderBy(x => random.Next()).ToList();
            var stripes = new List<int> { 9, 10, 11, 12, 13, 14, 15 }.OrderBy(x => random.Next()).ToList();
            var rackNumbers = new int[15];
            rackNumbers[4] = 8;
            rackNumbers[10] = solids.First();
            solids.RemoveAt(0);
            rackNumbers[14] = stripes.First();
            stripes.RemoveAt(0);
            var remainingBalls = solids.Concat(stripes).OrderBy(x => random.Next()).ToList();
            int remainingIndex = 0;
            for (int i = 0; i < 15; i++)
            {
                if (rackNumbers[i] == 0)
                {
                    rackNumbers[i] = remainingBalls[remainingIndex++];
                }
            }
            int ballIdx = 0;
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col <= row; col++)
                {
                    float x = startX - (row * (MathF.Sqrt(3) * ballRadius));
                    float y = startY + (col * 2 * ballRadius) - (row * ballRadius);
                    int ballNum = rackNumbers[ballIdx++];
                    this.Balls.Add(new PoolBall(new Vector2(x, y), ballRadius, ballNum, colors[ballNum], ballNum > 8));
                }
            }
        }
    }
}
