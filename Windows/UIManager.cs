using System;
using System.Collections.Generic;
using System.Numerics;
using AetherPool.Game.GameObjects;
using ImGuiNET;

namespace AetherPool.Windows
{
    public static class UIManager
    {
        public static void DrawMainMenu(Action onStartGame)
        {
            var windowSize = ImGui.GetWindowSize();
            var buttonSize = new Vector2(120, 40);
            ImGui.SetCursorPos(new Vector2((windowSize.X - buttonSize.X) * 0.5f, (windowSize.Y - buttonSize.Y) * 0.5f));

            if (ImGui.Button("Start Game", buttonSize))
            {
                onStartGame();
            }
        }

        public static void DrawTable(ImDrawListPtr drawList, Vector2 origin, Vector2 size, GameBoard board)
        {
            var woodColor = ImGui.GetColorU32(new Vector4(0.35f, 0.2f, 0.1f, 1.0f));
            var feltColor = ImGui.GetColorU32(new Vector4(0.0f, 0.4f, 0.1f, 1.0f));

            drawList.AddRectFilled(origin, origin + size, woodColor);

            var scaledBorder = board.BorderWidth * (size.X / board.Width);
            var feltOrigin = origin + new Vector2(scaledBorder, scaledBorder);
            var feltSize = size - new Vector2(scaledBorder * 2, scaledBorder * 2);
            drawList.AddRectFilled(feltOrigin, feltOrigin + feltSize, feltColor);

            var scaledPocketRadius = board.PocketRadius * (size.X / board.Width);
            foreach (var pocket in board.Pockets)
            {
                var scaledPocketPos = origin + new Vector2(pocket.X / board.Width * size.X, pocket.Y / board.Height * size.Y);
                drawList.AddCircleFilled(scaledPocketPos, scaledPocketRadius, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), 12);
            }
        }

        public static void DrawBall(ImDrawListPtr drawList, Vector2 tableOrigin, PoolBall ball, float scale, TextureManager textureManager)
        {
            var screenPos = tableOrigin + ball.Position * scale;
            var screenRadius = ball.Radius * scale;

            var shadowOffset = new Vector2(2 * scale, 2 * scale);
            drawList.AddCircleFilled(screenPos + shadowOffset, screenRadius, ImGui.GetColorU32(new Vector4(0, 0, 0, 0.3f)), 32);

            var texture = textureManager.GetBallTexture(ball.Number);
            if (texture != null)
            {
                drawList.AddImage(texture.ImGuiHandle, screenPos - new Vector2(screenRadius), screenPos + new Vector2(screenRadius));
            }
            else
            {
                drawList.AddCircleFilled(screenPos, screenRadius, ball.Color, 32);
            }
        }

        public static void DrawGhostBall(ImDrawListPtr drawList, Vector2 tableOrigin, Vector2 position, float radius, float scale)
        {
            var screenPos = tableOrigin + position * scale;
            var screenRadius = radius * scale;
            drawList.AddCircle(screenPos, screenRadius, ImGui.GetColorU32(new Vector4(1, 1, 1, 0.5f)), 32, 2f);
        }

        public static void DrawCueStick(ImDrawListPtr drawList, Vector2 tableOrigin, CueStick cue, float power, float scale, bool isAnimating, float animProgress)
        {
            var cueLength = 200f * scale;
            var cueOffset = 10f * scale;

            float pullback;
            if (isAnimating)
            {
                // Animate from pulled-back position to impact position
                pullback = (1.0f - animProgress) * power * 50f * scale;
            }
            else
            {
                // Static pullback based on drag distance
                pullback = power * 50f * scale;
            }

            var direction = new Vector2(MathF.Cos(cue.Angle), MathF.Sin(cue.Angle));
            var cueStartPos = cue.Position * scale - direction * (pullback + cueOffset);
            var endPoint = cueStartPos - direction * cueLength;

            drawList.AddLine(tableOrigin + cueStartPos, tableOrigin + endPoint, ImGui.GetColorU32(new Vector4(0.7f, 0.5f, 0.3f, 1.0f)), 4f * scale);
        }

        public static void DrawAimingLine(ImDrawListPtr drawList, Vector2 tableOrigin, List<Vector2> path, float scale)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                var startPos = tableOrigin + path[i] * scale;
                var endPos = tableOrigin + path[i + 1] * scale;
                drawList.AddLine(startPos, endPos, ImGui.GetColorU32(new Vector4(1, 1, 1, 0.5f)), 1f);
            }
        }

        public static void DrawHUD(
            ImDrawListPtr drawList, Vector2 windowOrigin, Vector2 windowSize,
            Vector2 tableOrigin, Vector2 tableSize, ref Vector2 aimOffset,
            Action onPlaceCueBall, Action onUndo, Action onResetGame)
        {
            // --- Game Controls on the left side ---
            var buttonSize = new Vector2(100, 25);
            var startX = windowOrigin.X + 10;
            var startY = tableOrigin.Y;

            ImGui.SetCursorScreenPos(new Vector2(startX, startY));
            if (ImGui.Button("Reset Game", buttonSize)) onResetGame();

            ImGui.SetCursorScreenPos(new Vector2(startX, startY + 30));
            if (ImGui.Button("Undo Shot", buttonSize)) onUndo();

            ImGui.SetCursorScreenPos(new Vector2(startX, startY + 60));
            if (ImGui.Button("Place Cue Ball", buttonSize)) onPlaceCueBall();

            // --- Aiming Control Window on the right side ---
            var controlSize = 80f;
            var controlPos = new Vector2(tableOrigin.X + tableSize.X + 20, tableOrigin.Y);

            ImGui.SetCursorScreenPos(controlPos);
            ImGui.BeginChild("##AimControlChild", new Vector2(controlSize, controlSize), false, ImGuiWindowFlags.NoScrollbar);

            var childDrawList = ImGui.GetWindowDrawList();
            var childOrigin = ImGui.GetWindowPos();
            var controlCenter = childOrigin + new Vector2(controlSize / 2, controlSize / 2);
            var controlRadius = controlSize / 2 - 5;

            childDrawList.AddCircleFilled(controlCenter, controlRadius, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));

            if (ImGui.IsWindowHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                var mouseInControl = ImGui.GetMousePos() - controlCenter;
                if (mouseInControl.Length() < controlRadius)
                {
                    aimOffset = mouseInControl / controlRadius;
                }
            }

            childDrawList.AddCircleFilled(controlCenter + aimOffset * controlRadius, 3, ImGui.GetColorU32(new Vector4(1, 0, 0, 1)));

            ImGui.EndChild();
        }
    }
}
