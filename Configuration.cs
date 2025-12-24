#pragma warning disable CA1416 // Suppress platform compatibility warnings

using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace AetherPool
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool IsGameWindowLocked { get; set; } = false;

        public bool OpenOnDeath { get; set; } = true;
        public bool OpenInQueue { get; set; } = false;
        public bool OpenDuringCrafting { get; set; } = false;
        public bool OpenInPartyFinder { get; set; } = false;
        public float MusicVolume { get; set; } = 0.5f;
        public float SfxVolume { get; set; } = .50f;

        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface?.SavePluginConfig(this);
        }
    }
}
