using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AetherPool.Audio
{
    public class AudioManager : IDisposable
    {
        private readonly Dictionary<string, byte[]> soundCache = new();
        private readonly WaveOutEvent sfxOutputDevice;
        private readonly MixingSampleProvider sfxMixer;

        public AudioManager()
        {
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
                var reader = new WaveFileReader(new MemoryStream(soundBytes));
                var volumeProvider = new VolumeSampleProvider(reader.ToSampleProvider())
                {
                    Volume = Math.Clamp(volume, 0.0f, 1.0f)
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
