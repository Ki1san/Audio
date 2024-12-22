using NAudio.Wave;

public class CustomSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private readonly Equalizer equalizer;

    public CustomSampleProvider(ISampleProvider source, Equalizer equalizer)
    {
        this.source = source;
        this.equalizer = equalizer;
        WaveFormat = source.WaveFormat;
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = source.Read(buffer, offset, count);
        return samplesRead;
    }
}