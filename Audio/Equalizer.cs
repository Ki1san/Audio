using Audio;
using NAudio.Wave;
using static Audio.Effects;

public class Equalizer : ISampleProvider
{
    private readonly ISampleProvider source;
    private float bassGain = 1.0f;
    private float midGain = 1.0f;
    private float trebleGain = 1.0f;

    public Equalizer(ISampleProvider source)
    {
        this.source = source;
        WaveFormat = source.WaveFormat;
    }

    public WaveFormat WaveFormat { get; }

    public float BassGain => bassGain; // Свойство для получения bassGain
    public float MidGain => midGain;   // Свойство для получения midGain
    public float TrebleGain => trebleGain; // Свойство для получения trebleGain

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = source.Read(buffer, offset, count);

        for (int i = 0; i < samplesRead; i++)
        {
            float sample = buffer[offset + i];

            // Применение усиления для низких, средних и высоких частот
            buffer[offset + i] *= (bassGain + midGain + trebleGain) / 3; // Пример простой обработки
        }

        return samplesRead;
    }

    public void UpdateBassGain(float gain) => bassGain = gain;
    public void UpdateMidGain(float gain) => midGain = gain;
    public void UpdateTrebleGain(float gain) => trebleGain = gain;
}