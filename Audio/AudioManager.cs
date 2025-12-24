#pragma warning disable CA1416
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AetherPool.Audio
{
    public class AudioManager : IDisposable
    {
        private readonly Dictionary<string, byte[]> soundCache = new();
        private readonly WaveOutEvent sfxOutputDevice;
        private readonly MixingSampleProvider sfxMixer;
        private readonly Configuration configuration;

        public AudioManager(Plugin plugin)
        {
            this.configuration = plugin.Configuration;

            var mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            this.sfxMixer = new MixingSampleProvider(mixerFormat) { ReadFully = true };
            this.sfxOutputDevice = new WaveOutEvent();
            this.sfxOutputDevice.Init(this.sfxMixer);
            this.sfxOutputDevice.Play();

            LoadSounds();
        }

        private void LoadSounds()
        {
            LoadSoundFromResource("ball_hit.wav");
            LoadSoundFromResource("cushion_hit.wav");
            LoadSoundFromResource("pocket.wav");
            LoadSoundFromResource("cue_hit.wav"); 
        }

        private void LoadSoundFromResource(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = $"AetherPool.Assets.Sfx.{fileName}";
            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null) return;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            soundCache[fileName] = ms.ToArray();
        }

        public void PlaySfx(string fileName, float volume)
        {
            if (!soundCache.TryGetValue(fileName, out var soundBytes)) return;

            try
            {
                // DEBUG LOG: Confirm the actual audio request to NAudio
                Plugin.Log.Debug($"[AUDIO_LOG] Audio Manager Playing: {fileName} at Volume {volume}");

                var reader = new WaveFileReader(new MemoryStream(soundBytes));
                float finalVolume = Math.Clamp(volume * configuration.SfxVolume, 0.0f, 1.0f);

                var volumeProvider = new VolumeSampleProvider(reader.ToSampleProvider())
                {
                    Volume = finalVolume
                };

                sfxMixer.AddMixerInput(ConvertToStereo(volumeProvider));
            }
            catch (Exception)
            {
                // Errors go here
            }
        }

        private static ISampleProvider ConvertToStereo(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == 1)
                return new MonoToStereoSampleProvider(input);
            return input;
        }

        public void Dispose()
        {
            sfxOutputDevice.Dispose();
        }
    }
}
