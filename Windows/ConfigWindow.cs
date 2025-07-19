using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace AetherPool.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration configuration;

        public ConfigWindow(Plugin plugin) : base("AetherPool Settings")
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(300, 150),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            this.configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public override void Draw()
        {
            bool isLocked = configuration.IsGameWindowLocked;
            if (ImGui.Checkbox("Lock Game Window Position", ref isLocked))
            {
                configuration.IsGameWindowLocked = isLocked;
                configuration.Save();
            }

            float volume = configuration.MusicVolume;
            if (ImGui.SliderFloat("Music Volume", ref volume, 0.0f, 1.0f))
            {
                configuration.MusicVolume = volume;
                configuration.Save();
            }
        }
    }
}
