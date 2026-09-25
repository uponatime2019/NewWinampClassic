using NAudio.Wave;

namespace NewWinampClassic.Services;

/// <summary>
/// Wraps an IWaveProvider, collects samples into a circular buffer
/// for FFT visualization. Handles both 16-bit PCM and 32-bit float.
/// </summary>
public sealed class SampleAggregator : IWaveProvider
{
    private readonly IWaveProvider _source;
    private readonly int _fftLength;
    private readonly float[] _circularBuffer;
    private readonly float[] _fftReal;
    private readonly float[] _fftImag;
    private readonly float[] _prevMagnitudes;
    private readonly bool _isFloat;
    private int _writePos;

    public WaveFormat WaveFormat => _source.WaveFormat;

    /// <summary>
    /// Raised when <see cref="PerformFft"/> computes new spectrum data.
    /// </summary>
    public event EventHandler<FftEventArgs>? FftCalculated;

    public SampleAggregator(IWaveProvider source, int fftLength = 1024)
    {
        _source = source;
        _fftLength = fftLength;
        _isFloat = source.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat;
        _circularBuffer = new float[fftLength];
        _fftReal = new float[fftLength];
        _fftImag = new float[fftLength];
        _prevMagnitudes = new float[fftLength / 2];
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _source.Read(buffer, offset, count);
        if (_isFloat)
        {
            ReadFloat(buffer, offset, bytesRead);
        }
        else
        {
            Read16Bit(buffer, offset, bytesRead);
        }
        return bytesRead;
    }

    private void ReadFloat(byte[] buffer, int offset, int bytesRead)
    {
        var channels = WaveFormat.Channels;
        var bytesPerFrame = sizeof(float) * channels;
        var frames = bytesRead / bytesPerFrame;
        for (var i = 0; i < frames; i++)
        {
            // Mix to mono for visualization
            float sum = 0;
            for (var ch = 0; ch < channels; ch++)
                sum += BitConverter.ToSingle(buffer, offset + i * bytesPerFrame + ch * sizeof(float));
            _circularBuffer[_writePos] = sum / channels;
            _writePos = (_writePos + 1) % _fftLength;
        }
    }

    private void Read16Bit(byte[] buffer, int offset, int bytesRead)
    {
        var channels = WaveFormat.Channels;
        var bytesPerFrame = 2 * channels;
        var frames = bytesRead / bytesPerFrame;
        for (var i = 0; i < frames; i++)
        {
            float sum = 0;
            for (var ch = 0; ch < channels; ch++)
                sum += BitConverter.ToInt16(buffer, offset + i * bytesPerFrame + ch * 2);
            _circularBuffer[_writePos] = sum / (channels * 32768f);
            _writePos = (_writePos + 1) % _fftLength;
        }
    }

    /// <summary>
    /// Performs FFT on buffered samples and fires FftCalculated.
    /// </summary>
    public void PerformFft()
    {
        for (var i = 0; i < _fftLength; i++)
        {
            var sample = _circularBuffer[(_writePos + i) % _fftLength];
            var window = (float)(0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / _fftLength)));
            _fftReal[i] = sample * window;
            _fftImag[i] = 0;
        }

        FftHelper.Fft(_fftReal, _fftImag, false);

        var halfLength = _fftLength / 2;
        for (var i = 0; i < halfLength; i++)
        {
            var magnitude = (float)Math.Sqrt(_fftReal[i] * _fftReal[i] + _fftImag[i] * _fftImag[i]);
            _prevMagnitudes[i] = Math.Max(magnitude, _prevMagnitudes[i] * 0.75f);
        }

        FftCalculated?.Invoke(this, new FftEventArgs(_prevMagnitudes));
    }
}

public class FftEventArgs(float[] result) : EventArgs
{
    public float[] Result { get; } = result;
}

/// <summary>
/// Radix-2 Cooley-Tukey FFT.
/// </summary>
internal static class FftHelper
{
    public static void Fft(float[] real, float[] imag, bool inverse)
    {
        var n = real.Length;
        if (n == 0 || (n & (n - 1)) != 0) return;

        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            while ((j & bit) != 0) { j ^= bit; bit >>= 1; }
            j ^= bit;
            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imag[i], imag[j]) = (imag[j], imag[i]);
            }
        }

        var dir = inverse ? 1.0 : -1.0;
        for (var len = 2; len <= n; len <<= 1)
        {
            var ang = 2 * Math.PI / len * dir;
            var wRe = (float)Math.Cos(ang);
            var wIm = (float)Math.Sin(ang);

            for (var i = 0; i < n; i += len)
            {
                var curRe = 1f;
                var curIm = 0f;
                for (var j = 0; j < len / 2; j++)
                {
                    var idx = i + j;
                    var idx2 = idx + len / 2;
                    var uRe = real[idx];
                    var uIm = imag[idx];
                    var vRe = real[idx2] * curRe - imag[idx2] * curIm;
                    var vIm = real[idx2] * curIm + imag[idx2] * curRe;

                    real[idx] = uRe + vRe;
                    imag[idx] = uIm + vIm;
                    real[idx2] = uRe - vRe;
                    imag[idx2] = uIm - vIm;

                    var tmp = curRe * wRe - curIm * wIm;
                    curIm = curRe * wIm + curIm * wRe;
                    curRe = tmp;
                }
            }
        }

        if (inverse)
        {
            for (var i = 0; i < n; i++) { real[i] /= n; imag[i] /= n; }
        }
    }
}
