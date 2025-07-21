#pragma warning disable CA1416

using System;
using System.Collections.Generic;
using System.Numerics;
using AetherPool.Game;
using AetherPool.Game.GameObjects;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace AetherPool.Windows
{
    public static class UIManager
    {
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

            var texture = ball.Number == 0 ? textureManager.CueBallTexture : textureManager.GetBallTexture(ball.Number);

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
            float pullback = isAnimating ? (1.0f - animProgress) * power * 50f * scale : power * 50f * scale;
            var direction = new Vector2(MathF.Cos(cue.Angle), MathF.Sin(cue.Angle));
            var cueStartPos = cue.Position * scale - direction * (pullback + cueOffset);
            var endPoint = cueStartPos - direction * cueLength;
            drawList.AddLine(tableOrigin + cueStartPos, tableOrigin + endPoint, ImGui.GetColorU32(new Vector4(0.7f, 0.5f, 0.3f, 1.0f)), 4f * scale);
        }

        public static void DrawAimingLine(ImDrawListPtr drawList, Vector2 tableOrigin, List<Vector2> path, float scale)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                drawList.AddLine(tableOrigin + path[i] * scale, tableOrigin + path[i + 1] * scale, ImGui.GetColorU32(new Vector4(1, 1, 1, 0.5f)), 1f);
            }
        }

        public static void DrawGameOverText(Vector2 tableOrigin, Vector2 tableSize, GameSession session)
        {
            if (session.CurrentState != GameState.GameOver) return;

            string text = session.Winner == Player.Human ? "VICTORY!" : "GAME OVER";
            var textColor = session.Winner == Player.Human ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1);

            ImGui.PushFont(UiBuilder.DefaultFont);
            var textSize = ImGui.CalcTextSize(text);
            var textPos = tableOrigin + (tableSize - textSize) / 2;

            var drawList = ImGui.GetForegroundDrawList();
            drawList.AddText(textPos + new Vector2(2, 2), ImGui.GetColorU32(new Vector4(0, 0, 0, 0.8f)), text); // Shadow
            drawList.AddText(textPos, ImGui.GetColorU32(textColor), text);

            ImGui.PopFont();
        }

        public static void DrawFoulMessage(Vector2 windowContentMin, Vector2 canvasSize, string? foulReason)
        {
            if (string.IsNullOrEmpty(foulReason)) return;

            var text = $"Foul: {foulReason}\nOpponent has ball-in-hand.";
            var padding = new Vector2(10, 10);

            ImGui.PushFont(UiBuilder.DefaultFont);
            var textSize = ImGui.CalcTextSize(text, canvasSize.X * 0.6f);
            ImGui.PopFont();

            var boxSize = textSize + padding * 2;
            var boxPos = windowContentMin + new Vector2((canvasSize.X - boxSize.X) / 2, 10);

            var drawList = ImGui.GetForegroundDrawList();

            drawList.AddRectFilled(boxPos, boxPos + boxSize, ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 0.85f)), 5.0f);
            drawList.AddRect(boxPos, boxPos + boxSize, ImGui.GetColorU32(new Vector4(1, 0, 0, 1)), 5.0f, ImDrawFlags.None, 2.0f);

            var textPos = boxPos + padding;
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), text);
        }

        public static void DrawHUD(
            ImDrawListPtr drawList, GameSession session, Vector2 windowOrigin, Vector2 windowSize,
            Vector2 tableOrigin, Vector2 tableSize, ref Vector2 aimOffset,
            Action onPlaceCueBall, Action onUndo, Action onResetGame)
        {
            var buttonSize = new Vector2(100, 25);
            var startX = windowOrigin.X + 10;
            var startY = tableOrigin.Y;

            ImGui.SetCursorScreenPos(new Vector2(startX, startY));
            if (ImGui.Button("Reset Game", buttonSize)) onResetGame();
            ImGui.SetCursorScreenPos(new Vector2(startX, startY + 30));
            if (ImGui.Button("Undo Shot", buttonSize)) onUndo();
            ImGui.SetCursorScreenPos(new Vector2(startX, startY + 60));
            if (ImGui.Button("Place Cue Ball", buttonSize)) onPlaceCueBall();

            var controlSize = 80f;
            var controlPos = new Vector2(tableOrigin.X + tableSize.X + 20, tableOrigin.Y);

            var label = "English / Spin";
            var labelSize = ImGui.CalcTextSize(label);
            ImGui.SetCursorScreenPos(controlPos + new Vector2((controlSize - labelSize.X) / 2, -labelSize.Y - 5));
            ImGui.Text(label);

            ImGui.SetCursorScreenPos(controlPos);
            ImGui.BeginChild("##AimControlChild", new Vector2(controlSize, controlSize), true, ImGuiWindowFlags.NoScrollbar);

            var childDrawList = ImGui.GetWindowDrawList();
            var childOrigin = ImGui.GetWindowPos();
            var controlCenter = childOrigin + new Vector2(controlSize / 2, controlSize / 2);
            var controlRadius = controlSize / 2 - 5;

            childDrawList.AddCircleFilled(controlCenter, controlRadius, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
            if (ImGui.IsWindowHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                var mouseInControl = ImGui.GetMousePos() - controlCenter;
                if (mouseInControl.Length() < controlRadius) aimOffset = mouseInControl / controlRadius;
            }
            childDrawList.AddCircleFilled(controlCenter + aimOffset * controlRadius, 3, ImGui.GetColorU32(new Vector4(1, 0, 0, 1)));
            ImGui.EndChild();

            var assignment = session.CurrentTurn == Player.Human ? session.Player1Assignment : session.Player2Assignment;
            if (assignment != PlayerAssignment.Unassigned)
            {
                var assignmentText = assignment.ToString();
                var assignmentTextSize = ImGui.CalcTextSize(assignmentText);
                var assignmentPos = controlPos + new Vector2((controlSize - assignmentTextSize.X) / 2, controlSize + 5);
                ImGui.SetCursorScreenPos(assignmentPos);
                ImGui.TextColored(new Vector4(1, 1, 0, 1), assignmentText);
            }
        }
    }
}
#pragma warning restore CA1416
