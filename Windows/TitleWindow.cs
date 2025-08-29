#pragma warning disable CA1416

using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using AetherPool.Game;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AetherPool.Windows
{
    public class TitleWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly GameSession gameSession;
        private IDalamudTextureWrap? backgroundTexture;

        public TitleWindow(Plugin plugin, GameSession gameSession) : base("AetherPool###AetherPoolTitleWindow")
        {
            this.plugin = plugin;
            this.gameSession = gameSession;

            this.Size = new Vector2(400, 300);
            this.Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar;

            LoadBackgroundTexture();
        }

        private void LoadBackgroundTexture()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = "AetherPool.Assets.Images.icon.png";
            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null) return;

            try
            {
                using var image = Image.Load<Rgba32>(stream);
                var rgbaBytes = new byte[image.Width * image.Height * 4];
                image.CopyPixelDataTo(rgbaBytes);

                this.backgroundTexture = Plugin.TextureProvider.CreateFromRaw(RawImageSpecification.Rgba32(image.Width, image.Height), rgbaBytes);
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            backgroundTexture?.Dispose();
        }

        public override void Draw()
        {
            var windowSize = ImGui.GetWindowSize();

            if (this.backgroundTexture != null)
            {
                var windowPos = ImGui.GetWindowPos();
                ImGui.GetWindowDrawList().AddImage(this.backgroundTexture.Handle, windowPos, windowPos + windowSize);
            }

            var buttonSize = new Vector2(windowSize.X * 0.6f, 30);
            var centerPosX = (windowSize.X - buttonSize.X) / 2;

            var title = "AetherPool";
            var titleSize = ImGui.CalcTextSize(title);
            var titlePos = ImGui.GetCursorScreenPos();
            titlePos.X += (windowSize.X - titleSize.X) / 2;
            titlePos.Y += 30f;
            DrawTextWithOutline(title, titlePos, 0xFFFFFFFF, 0xFF000000);

            ImGui.SetCursorPos(new Vector2(centerPosX, 80f));
            if (DrawButtonWithOutline("Play8Ball", "Play 8-Ball vs. AI", buttonSize))
            {
                gameSession.StartNewGame(GameMode.EightBall);
                this.IsOpen = false;
                plugin.MainWindow.IsOpen = true;
            }

            ImGui.SetCursorPos(new Vector2(centerPosX, 115f));
            if (DrawButtonWithOutline("Play9Ball", "Play 9-Ball vs. AI", buttonSize))
            {
                gameSession.StartNewGame(GameMode.NineBall);
                this.IsOpen = false;
                plugin.MainWindow.IsOpen = true;
            }

            ImGui.SetCursorPos(new Vector2(centerPosX, 150f));
            if (DrawButtonWithOutline("Multiplayer", "Multiplayer", buttonSize))
            {
                plugin.MultiplayerWindow.IsOpen = true;
                this.IsOpen = false;
            }

            ImGui.SetCursorPos(new Vector2(centerPosX, 190f));
            if (DrawButtonWithOutline("Settings", "Settings", buttonSize))
            {
                plugin.ConfigWindow.IsOpen = true;
            }

            ImGui.SetCursorPos(new Vector2(centerPosX, 225f));
            if (DrawButtonWithOutline("About", "About", buttonSize))
            {
                plugin.AboutWindow.IsOpen = true;
            }
        }

        private void DrawTextWithOutline(string text, Vector2 pos, uint textColor, uint outlineColor)
        {
            var drawList = ImGui.GetWindowDrawList();
            var outlineOffset = new Vector2(1, 1);

            drawList.AddText(pos - outlineOffset, outlineColor, text);
            drawList.AddText(pos + new Vector2(outlineOffset.X, -outlineOffset.Y), outlineColor, text);
            drawList.AddText(pos + new Vector2(-outlineOffset.X, outlineOffset.Y), outlineColor, text);
            drawList.AddText(pos + outlineOffset, outlineColor, text);

            drawList.AddText(pos, textColor, text);
        }

        private bool DrawButtonWithOutline(string id, string text, Vector2 size)
        {
            var clicked = ImGui.Button($"##{id}", size);
            if (clicked)
            {
                plugin.AudioManager.PlaySfx("cue_hit.wav", 0.6f);
            }
            var buttonPos = ImGui.GetItemRectMin();
            var buttonSize = ImGui.GetItemRectSize();
            var textSize = ImGui.CalcTextSize(text);
            var textPos = buttonPos + (buttonSize - textSize) * 0.5f;

            DrawTextWithOutline(text, textPos, 0xFFFFFFFF, 0xFF000000);

            return clicked;
        }
    }
}
