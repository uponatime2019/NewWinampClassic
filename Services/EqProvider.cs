using NAudio.Wave;

namespace NewWinampClassic.Services;

/// <summary>
/// IWaveProvider that applies a classic 10-band parametric EQ plus preamp
/// and stereo balance. Handles both 16-bit PCM and 32-bit float audio.
/// Bands: 60, 170, 310, 600, 1k, 3k, 6k, 12k, 14k, 16k
/// </summary>
public sealed class EqProvider : IWaveProvider
{
    private readonly IWaveProvider _source;
    private readonly EqFilterEq[] _filters;
    private readonly int _bytesPerSample;
    private readonly int _channels;
    private readonly bool _isFloat;

    private float _preamp = 1f;
    private float _leftGain = 1f;
    private float _rightGain = 1f;

    /// <summary>
    /// Center frequencies for the 10 classic Winamp EQ bands.
    /// </summary>
    public static readonly int[] BandFrequencies = [60, 170, 310, 600, 1000, 3000, 6000, 12000, 14000, 16000];

    /// <summary>Preamp in dB (-12..+12).</summary>
    public float PreampDb
    {
        get => _preampDb;
        set
        {
            _preampDb = value;
            _preamp = MathF.Pow(10f, value / 20f);
        }
    }
    private float _preampDb;

    /// <summary>Stereo balance (-1 = full left, 0 = center, +1 = full right).</summary>
    public double Balance
    {
        get => _balance;
        set
        {
            _balance = Math.Clamp(value, -1.0, 1.0);
            // center => both 1.0; +1 (full right) => L=0, R=1; -1 (full left) => L=1, R=0
            _leftGain = (float)Math.Clamp(1.0 - Math.Max(0.0, _balance), 0.0, 1.0);
            _rightGain = (float)Math.Clamp(1.0 + Math.Min(0.0, _balance), 0.0, 1.0);
        }
    }
    private double _balance;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public EqProvider(IWaveProvider source)
    {
        _source = source;
        _bytesPerSample = source.WaveFormat.BitsPerSample / 8;
        _channels = source.WaveFormat.Channels;
        _isFloat = source.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat;
        _filters = new EqFilterEq[BandFrequencies.Length];
        for (var i = 0; i < _filters.Length; i++)
        {
            _filters[i] = EqFilterEq.CreatePeakingEQ(WaveFormat.SampleRate, BandFrequencies[i], 0, 1.0f);
        }
    }

    /// <summary>
    /// Sets the gain for a specific EQ band.
    /// </summary>
    public void SetBand(int band, float gainDb)
    {
        if (band < 0 || band >= _filters.Length)
            return;
        _filters[band] = EqFilterEq.CreatePeakingEQ(WaveFormat.SampleRate, BandFrequencies[band], gainDb, 1.0f);
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _source.Read(buffer, offset, count);
        var stride = _bytesPerSample * _channels;
        var totalSamples = bytesRead / stride;

        for (var i = 0; i < totalSamples; i++)
        {
            for (var ch = 0; ch < _channels; ch++)
            {
                var byteOff = offset + (i * _channels + ch) * _bytesPerSample;
                float sample;

                if (_isFloat)
                {
                    sample = BitConverter.ToSingle(buffer, byteOff);
                }
                else
                {
                    sample = BitConverter.ToInt16(buffer, byteOff) / 32768f;
                }

                // Preamp + balance
                if (ch == 0)
                    sample *= _preamp * _leftGain;
                else
                    sample *= _preamp * _rightGain;

                foreach (var f in _filters)
                    sample = f.Transform(sample);

                if (_isFloat)
                {
                    var bytes = BitConverter.GetBytes(sample);
                    buffer[byteOff] = bytes[0];
                    buffer[byteOff + 1] = bytes[1];
                    buffer[byteOff + 2] = bytes[2];
                    buffer[byteOff + 3] = bytes[3];
                }
                else
                {
                    var pcm = (short)Math.Clamp(sample * 32768f, -32768, 32767);
                    var pcmBytes = BitConverter.GetBytes(pcm);
                    buffer[byteOff] = pcmBytes[0];
                    buffer[byteOff + 1] = pcmBytes[1];
                }
            }
        }

        return bytesRead;
    }
}

/// <summary>
/// Custom BiQuad filter for parametric EQ.
/// </summary>
internal sealed class EqFilterEq
{
    private double bx0, bx1, bx2, ba1, ba2;
    private double prevX1, prevX2, prevY1, prevY2;

    private EqFilterEq() { }

    public float Transform(float sample)
    {
        var x = sample;
        var y = bx0 * x + bx1 * prevX1 + bx2 * prevX2 - ba1 * prevY1 - ba2 * prevY2;
        prevX2 = prevX1;
        prevX1 = x;
        prevY2 = prevY1;
        prevY1 = y;
        return (float)y;
    }

    public static EqFilterEq CreatePeakingEQ(float sampleRate, float centreFrequency, float gainDb, float bandwidthOrQ)
    {
        var filter = new EqFilterEq();
        var normalFreq = centreFrequency / sampleRate;
        var a = Math.Sqrt(Math.Pow(10.0, gainDb / 20.0));
        var beta = Math.Sqrt(normalFreq) / bandwidthOrQ;
        var a0 = 1.0 + beta / a;
        filter.bx0 = (1.0 + beta * a) / a0;
        filter.bx1 = -2.0 * Math.Cos(2.0 * Math.PI * normalFreq) / a0;
        filter.bx2 = (1.0 - beta * a) / a0;
        filter.ba1 = -2.0 * Math.Cos(2.0 * Math.PI * normalFreq) / a0;
        filter.ba2 = (1.0 - beta / a) / a0;
        return filter;
    }
}
