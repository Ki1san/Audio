using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

namespace Audio
{
    public class Effects
    {
        public float CurrentDelayTime { get; private set; }
        public float CurrentFeedback { get; private set; }
        public float CurrentReverbLevel { get; private set; }
        public float CurrentReverbTime { get; private set; }

        private ISampleProvider sourceProvider;

        public Effects(ISampleProvider sourceProvider)
        {
            this.sourceProvider = sourceProvider;
            CurrentDelayTime = 0;
            CurrentFeedback = 0;
            CurrentReverbLevel = 0;
            CurrentReverbTime = 0;
        }

        public ISampleProvider ApplySaturation(float amount)
        {
            var saturationProvider = new SaturationSampleProvider(sourceProvider, amount);
            sourceProvider = saturationProvider;
            return sourceProvider;
        }

        public void ApplyDelay(float delayTime, float feedback)
        {
            var delayProvider = new DelaySampleProvider(sourceProvider, delayTime, feedback);
            sourceProvider = delayProvider;
            CurrentDelayTime = delayTime;
            CurrentFeedback = feedback;
        }

        public ISampleProvider ApplyChorus(float rate, float depth, float wetDryMix)
        {
            var chorusProvider = new ChorusSampleProvider(sourceProvider, rate, depth, wetDryMix);
            sourceProvider = chorusProvider;
            return sourceProvider;
        }

        public ISampleProvider ApplyReverb(float roomSize, float damp, float wetDryMix)
        {
            var reverbProvider = new SimpleReverbSampleProvider(sourceProvider, roomSize, damp, wetDryMix);
            sourceProvider = reverbProvider;
            return sourceProvider;
        }

        public ISampleProvider GetProcessedProvider()
        {
            return sourceProvider;
        }
    }

    public class SaturationSampleProvider : ISampleProvider
    {
        private ISampleProvider sourceProvider;
        private float saturationAmount;

        public SaturationSampleProvider(ISampleProvider sourceProvider, float saturationAmount)
        {
            this.sourceProvider = sourceProvider;
            this.saturationAmount = saturationAmount;
        }

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);
            for (int n = 0; n < samplesRead; n++)
            {
                buffer[offset + n] = Saturate(buffer[offset + n], saturationAmount);
            }
            return samplesRead;
        }

        private float Saturate(float sample, float amount)
        {
            float saturatedValue = sample * amount;
            if (saturatedValue > 1.0f) return 1.0f;
            if (saturatedValue < -1.0f) return -1.0f;

            if (amount > 1.0f)
            {
                saturatedValue = (float)(Math.Sign(saturatedValue) * Math.Pow(Math.Abs(saturatedValue), amount));
            }
            return saturatedValue;
        }
    }

    public class DelaySampleProvider : ISampleProvider
    {
        private ISampleProvider sourceProvider;
        private float delayTime;
        private float feedback;
        private float[] delayBuffer;
        private int delayBufferSize;
        private int writeIndex;

        public DelaySampleProvider(ISampleProvider sourceProvider, float delayTime, float feedback)
        {
            this.sourceProvider = sourceProvider;
            SetDelay(delayTime);
            this.feedback = feedback;
        }

        public WaveFormat WaveFormat => sourceProvider.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = sourceProvider.Read(buffer, offset, count);
            for (int n = 0; n < samplesRead; n++)
            {
                float delayedSample = delayBuffer[writeIndex];
                delayBuffer[writeIndex] = buffer[offset + n] + (delayedSample * feedback);
                buffer[offset + n] += delayedSample;
                writeIndex = (writeIndex + 1) % delayBufferSize;
            }
            return samplesRead;
        }

        public void SetDelay(float delayTime)
        {
            this.delayTime = delayTime;
            delayBufferSize = (int)(delayTime * sourceProvider.WaveFormat.SampleRate);
            delayBuffer = new float[delayBufferSize];
            Array.Clear(delayBuffer, 0, delayBuffer.Length);  // Очистка буфера
            writeIndex = 0;
        }

        public void SetFeedback(float feedback)
        {
            this.feedback = feedback;
        }
    }

    public class ChorusSampleProvider : ISampleProvider
    {
        private ISampleProvider source;
        private float depth;
        private float rate;
        private float wetDryMix;
        private int sampleRate;

        public ChorusSampleProvider(ISampleProvider source, float rate, float depth, float wetDryMix)
        {
            this.source = source;
            this.sampleRate = source.WaveFormat.SampleRate;
            UpdateParameters(rate, depth, wetDryMix);
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public void UpdateParameters(float rate, float depth, float wetDryMix)
        {
            this.rate = Clamp(rate, 0f, 10f);
            this.depth = Clamp(depth, 0f, 1f);
            this.wetDryMix = Clamp(wetDryMix, 0f, 1f);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);
            for (int n = 0; n < samplesRead; n++)
            {
                float input = buffer[offset + n];
                // Применение хора
                float modulation = depth * (float)Math.Sin(2 * Math.PI * rate * n / sampleRate);
                float chorusSample = input + modulation;

                // Нормализация
                buffer[offset + n] = NormalizeSample(wetDryMix * chorusSample + (1 - wetDryMix) * input);
            }
            return samplesRead;
        }

        private float NormalizeSample(float sample)
        {
            return Math.Max(-1.0f, Math.Min(1.0f, sample));
        }

        private float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }

    public class SimpleReverbSampleProvider : ISampleProvider
    {
        private ISampleProvider source;
        private float roomSize;
        private float damp;
        private float wetDryMix;
        private float[] buffer;
        private int bufferIndex;

        public SimpleReverbSampleProvider(ISampleProvider source, float roomSize, float damp, float wetDryMix)
        {
            this.source = source;
            this.roomSize = roomSize;
            this.damp = damp;
            this.wetDryMix = wetDryMix;
            buffer = new float[44100]; // 1 секунда буфера
            bufferIndex = 0;
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] outputBuffer, int offset, int count)
        {
            int samplesRead = source.Read(outputBuffer, offset, count);
            for (int n = 0; n < samplesRead; n++)
            {
                float input = outputBuffer[offset + n];

                // Обрабатываем реверберацию
                float reverbSample = buffer[bufferIndex];
                buffer[bufferIndex] = (input + reverbSample * damp) * roomSize;

                // Нормализация выходного сигнала
                outputBuffer[offset + n] = Clamp(
                    (wetDryMix * buffer[bufferIndex]) + ((1 - wetDryMix) * input),
                    -1f, 1f
                );

                bufferIndex = (bufferIndex + 1) % buffer.Length;
            }
            return samplesRead;
        }

        private float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
