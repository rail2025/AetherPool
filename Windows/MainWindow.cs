#pragma warning disable CA1416

using System;
using System.Numerics;
using AetherPool.Game;
using AetherPool.Game.GameObjects;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace AetherPool.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly GameSession gameSession;
        private readonly TextureManager textureManager;

        private bool isDraggingForPower = false;
        private bool isShooting = false;
        private float shootAnimationTimer = 0f;
        private const float ShootAnimationDuration = 0.075f;

        private Vector2 dragStartPos;
        private float lockedAimAngle = 0f;
        private float shotPower = 0f;
        private Vector2 cueballAimOffset = Vector2.Zero;

        public MainWindow(Plugin plugin, GameSession gameSession) : base("AetherPool")
        {
            this.plugin = plugin;
            this.gameSession = gameSession;
            this.textureManager = new TextureManager();
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(800, 500),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
            this.Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        }

        public void Dispose()
        {
            textureManager.Dispose();
        }

        public override void OnClose()
        {
            plugin.TitleWindow.IsOpen = true;
        }

        public bool IsMultiplayer { get; private set; } = false;

        public void StartMultiplayerGame()
        {
            IsMultiplayer = true;
        }

        public override void Draw()
        {
            var deltaTime = ImGui.GetIO().DeltaTime;
            gameSession.Update(deltaTime);
            ProcessCollisionSounds();

            if (isShooting)
            {
                shootAnimationTimer += deltaTime;
                if (shootAnimationTimer >= ShootAnimationDuration)
                {
                    isShooting = false;
                    shotPower = 0;
                }
            }

            DrawInGameUI();
        }

        private void ProcessCollisionSounds()
        {
            for (int i = 0; i < gameSession.CollisionEvents.Count; i++)
            {
                var e = gameSession.CollisionEvents[i];
                if (!e.ProcessedForSound)
                {
                    float volume = Math.Clamp(e.ImpactVelocity / 200f, 0.1f, 1.0f);
                    switch (e.Type)
                    {
                        case CollisionType.Ball:
                            plugin.AudioManager.PlaySfx("ball_hit.wav", volume);
                            break;
                        case CollisionType.Cushion:
                            plugin.AudioManager.PlaySfx("cushion_hit.wav", volume * 0.5f);
                            break;
                        case CollisionType.Pocket:
                            plugin.AudioManager.PlaySfx("pocket.wav", 1.0f);
                            break;
                    }
                    e.ProcessedForSound = true;
                    gameSession.CollisionEvents[i] = e;
                }
            }
        }

        private void DrawInGameUI()
        {
            var drawList = ImGui.GetWindowDrawList();
            var windowContentMin = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
            var canvasSize = ImGui.GetContentRegionAvail();

            float sideMargin = 120f;
            float tableAvailableWidth = canvasSize.X - (sideMargin * 2);
            float tableAspectRatio = 2.0f;
            float tableRenderWidth = tableAvailableWidth;
            float tableRenderHeight = tableRenderWidth / tableAspectRatio;

            if (tableRenderHeight > canvasSize.Y)
            {
                tableRenderHeight = canvasSize.Y;
                tableRenderWidth = tableRenderHeight * tableAspectRatio;
            }

            var tableOrigin = windowContentMin + new Vector2(sideMargin + (tableAvailableWidth - tableRenderWidth) / 2, (canvasSize.Y - tableRenderHeight) / 2);
            var tableSize = new Vector2(tableRenderWidth, tableRenderHeight);
            float scale = tableRenderWidth / gameSession.Board.Width;

            gameSession.Board.UpdateLayout(gameSession.Board.Width, gameSession.Board.Height);
            UIManager.DrawTable(drawList, tableOrigin, tableSize, gameSession.Board);

            foreach (var ball in gameSession.Balls)
            {
                if (!ball.IsSunk)
                {
                    UIManager.DrawBall(drawList, tableOrigin, ball, scale, textureManager);
                }
            }

            ImGui.SetCursorScreenPos(tableOrigin);
            ImGui.InvisibleButton("##PoolTableCanvas", tableSize);

            UIManager.DrawFoulMessage(windowContentMin, canvasSize, gameSession.FoulReason);

            var mousePos = ImGui.GetMousePos();
            var mousePosRelative = (mousePos - tableOrigin) / scale;

            if (gameSession.CurrentState == GameState.PlacingCueBall)
            {
                UIManager.DrawGhostBall(drawList, tableOrigin, mousePosRelative, gameSession.Balls[0].Radius, scale);
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    gameSession.PlaceCueBall(mousePosRelative);
                }
            }
            else if (gameSession.CurrentTurn == Player.Human && !gameSession.AreBallsMoving() && !isShooting)
            {
                var cueBall = gameSession.Balls.Find(b => b.Number == 0 && !b.IsSunk);
                if (cueBall != null && ImGui.IsItemHovered())
                {
                    gameSession.Cue.Position = cueBall.Position;

                    if (!isDraggingForPower)
                    {
                        gameSession.Cue.AimAt(mousePosRelative);
                        var path = PhysicsEngine.PredictBallPath(gameSession.Board, cueBall.Position, gameSession.Cue.Angle, cueBall.Radius);
                        UIManager.DrawAimingLine(drawList, tableOrigin, path, scale);

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            isDraggingForPower = true;
                            dragStartPos = mousePos;
                            lockedAimAngle = gameSession.Cue.Angle;
                        }
                    }
                    else
                    {
                        gameSession.Cue.Angle = lockedAimAngle;
                        var dragVector = dragStartPos - mousePos;
                        shotPower = Math.Clamp(dragVector.Length() / 150f, 0, 1);
                        if (Vector2.Dot(Vector2.Normalize(dragVector), new Vector2(MathF.Cos(lockedAimAngle), MathF.Sin(lockedAimAngle))) < 0) shotPower = 0;

                        var path = PhysicsEngine.PredictBallPath(gameSession.Board, cueBall.Position, lockedAimAngle, cueBall.Radius);
                        UIManager.DrawAimingLine(drawList, tableOrigin, path, scale);

                        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            if (shotPower > 0.05f)
                            {
                                isShooting = true;
                                shootAnimationTimer = 0f;
                                plugin.AudioManager.PlaySfx("cue_hit.wav", shotPower);
                                gameSession.FireCueBall(lockedAimAngle, shotPower, cueballAimOffset);
                            }
                            isDraggingForPower = false;
                        }
                    }
                }
            }

            var activeCueBall = gameSession.Balls.Find(b => b.Number == 0 && !b.IsSunk);
            if (activeCueBall != null && ((gameSession.CurrentTurn == Player.Human && !gameSession.AreBallsMoving()) || isShooting))
            {
                UIManager.DrawCueStick(drawList, tableOrigin, gameSession.Cue, shotPower, scale, isShooting, shootAnimationTimer / ShootAnimationDuration);
            }

            UIManager.DrawGameOverText(tableOrigin, tableSize, gameSession);

            UIManager.DrawHUD(drawList, gameSession, windowContentMin, canvasSize, tableOrigin, tableSize, ref cueballAimOffset,
                () => gameSession.EnterPlaceCueBallState(),
                () => gameSession.UndoLastShot(),
                () => {
                    this.IsOpen = false;
                    plugin.TitleWindow.IsOpen = true;
                }
            );
        }
    }
}
#pragma warning restore CA1416
