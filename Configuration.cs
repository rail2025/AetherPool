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
        public float MusicVolume { get; set; } = 0.5f;

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
