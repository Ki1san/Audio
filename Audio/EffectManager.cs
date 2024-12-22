using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Windows.Forms;

namespace Audio
{
    // Перечисление для управления эффектами
    public enum AudioEffect
    {
        None,
        Saturation,
        Reverb,
        Chorus,
        Delay
    }

    // Класс для управления эффектами
    public class EffectManager
    {
        private readonly Effects effects; // Предполагаем, что у вас есть класс Effects
        public bool IsSaturationActive { get; set; } = false;
        public bool IsReverbActive { get; set; } = false;
        public bool IsChorusActive { get; set; } = false;
        public bool IsDelayActive { get; set; } = false;

        // Параметры эффектов
        private float saturationAmount;
        private float reverbRoomSize;
        private float reverbDamp;
        private float reverbWetDryMix;

        private float chorusRate;
        private float chorusDepth;
        private float chorusWetDryMix;

        private float delayTime;
        private float delayFeedback;

        public EffectManager(Effects effects)
        {
            this.effects = effects ?? throw new ArgumentNullException(nameof(effects), "Effects manager cannot be null.");
        }

        public ISampleProvider UpdateEffects(ISampleProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider), "Provider cannot be null.");

            if (IsSaturationActive)
            {
                provider = ApplySaturation(provider);
            }

            if (IsReverbActive)
            {
                provider = ApplyReverb(provider);
            }

            if (IsChorusActive)
            {
                provider = ApplyChorus(provider);
            }

            if (IsDelayActive)
            {
                provider = ApplyDelay(provider);
            }

            return provider; // Возвращаем обновленный провайдер
        }

        public ISampleProvider ApplySaturation(ISampleProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider), "Provider cannot be null.");

            // Логика насыщения 
            return new SaturationSampleProvider(provider, saturationAmount);
        }

        private ISampleProvider ApplyReverb(ISampleProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider), "Provider cannot be null.");

            // Применяем реверберацию
            return new SimpleReverbSampleProvider(provider, reverbRoomSize, reverbDamp, reverbWetDryMix);
        }

        private ISampleProvider ApplyChorus(ISampleProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider), "Provider cannot be null.");

            // Применяем хор
            return new ChorusSampleProvider(provider, chorusRate, chorusDepth, chorusWetDryMix);
        }

        private ISampleProvider ApplyDelay(ISampleProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider), "Provider cannot be null.");

            if (effects == null)
            {
                MessageBox.Show("Effects manager is not initialized.");
                return null; // Или выбросьте исключение
            }

            // Логирование значений перед вызовом ApplyDelay
            MessageBox.Show($"Applying delay with time: {delayTime}, feedback: {delayFeedback}");

            // Применяем задержку
            effects.ApplyDelay(delayTime, delayFeedback);
            return effects.GetProcessedProvider(); // Возвращаем обработанный провайдер
        }

        // Методы для обновления параметров эффектов
        public void SetSaturation(float amount)
        {
            saturationAmount = amount;
            IsSaturationActive = amount > 0;
        }

        public void SetReverb(float roomSize, float damp, float wetDryMix)
        {
            reverbRoomSize = roomSize;
            reverbDamp = damp;
            reverbWetDryMix = wetDryMix;
            IsReverbActive = roomSize > 0 || damp > 0 || wetDryMix > 0;
        }

        public void SetChorus(float rate, float depth, float wetDryMix)
        {
            chorusRate = rate;
            chorusDepth = depth;
            chorusWetDryMix = wetDryMix;
            IsChorusActive = rate > 0 || depth > 0 || wetDryMix > 0;
        }

        public void SetDelay(float time, float feedback)
        {
            delayTime = time;
            delayFeedback = feedback;
            IsDelayActive = time > 0 || feedback > 0;
        }
    }
}