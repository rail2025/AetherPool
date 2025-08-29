#pragma warning disable CA1416

using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace AetherPool.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration configuration;

        public ConfigWindow(Plugin plugin) : base("AetherPool Settings")
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(300, 250),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
            this.SizeCondition = ImGuiCond.FirstUseEver;

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

            ImGui.Separator();

            ImGui.Text("Advanced Triggers");

            var openOnDeath = this.configuration.OpenOnDeath;
            if (ImGui.Checkbox("Open on Death", ref openOnDeath))
            {
                this.configuration.OpenOnDeath = openOnDeath;
                this.configuration.Save();
            }

            var openInQueue = this.configuration.OpenInQueue;
            if (ImGui.Checkbox("Open in Duty Queue", ref openInQueue))
            {
                this.configuration.OpenInQueue = openInQueue;
                this.configuration.Save();
            }

            var openInPartyFinder = this.configuration.OpenInPartyFinder;
            if (ImGui.Checkbox("Open in Party Finder Queue", ref openInPartyFinder))
            {
                this.configuration.OpenInPartyFinder = openInPartyFinder;
                this.configuration.Save();
            }

            var openDuringCrafting = this.configuration.OpenDuringCrafting;
            if (ImGui.Checkbox("Open during long craft", ref openDuringCrafting))
            {
                this.configuration.OpenDuringCrafting = openDuringCrafting;
                this.configuration.Save();
            }
            
        }
    }
}
