using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Audio
{
    public class AudioManager
    {
        private WaveOutEvent waveOut;
        private AudioFileReader audioFileReader;
        private VolumeSampleProvider volumeProvider;
        private Equalizer equalizer;
        private Effects effects;

        public AudioManager()
        {
            waveOut = new WaveOutEvent();
        }

        public void PlayAudio(string filePath, int deviceNumber)
        {
            StopAudio();

            audioFileReader = new AudioFileReader(filePath);
            waveOut.DeviceNumber = deviceNumber;
            volumeProvider = new VolumeSampleProvider(audioFileReader);
            effects = new Effects(volumeProvider);
            equalizer = new Equalizer(effects.GetProcessedProvider());
            waveOut.Init(equalizer);
            waveOut.Play();
        }

        public void StopAudio()
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            audioFileReader?.Dispose();
            audioFileReader = null;
        }

        public void SetVolume(float volume)
        {
            if (volumeProvider != null)
            {
                volumeProvider.Volume = volume;
            }
        }

        public void SkipForward(double seconds)
        {
            if (audioFileReader != null)
            {
                audioFileReader.CurrentTime = TimeSpan.FromSeconds(Math.Min(audioFileReader.TotalTime.TotalSeconds, audioFileReader.CurrentTime.TotalSeconds + seconds));
            }
        }

        public void SkipBack(double seconds)
        {
            if (audioFileReader != null)
            {
                audioFileReader.CurrentTime = TimeSpan.FromSeconds(Math.Max(0, audioFileReader.CurrentTime.TotalSeconds - seconds));
            }
        }

        public void SelectOutputDevice(int deviceId)
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = new WaveOutEvent { DeviceNumber = deviceId };
            waveOut.Init(equalizer);
        }
    }
}