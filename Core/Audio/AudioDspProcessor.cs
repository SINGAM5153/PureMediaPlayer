namespace PureMediaPlayer.Core.Audio;

public sealed class AudioDspProcessor
{
    private float _linearGain = 1.0f;
    private bool _clarityExciterEnabled = true;

    public float MasterVolume
    {
        get => _linearGain;
        set => _linearGain = Math.Clamp(value, 0.0f, 2.0f);
    }

    public bool ClarityExciterEnabled
    {
        get => _clarityExciterEnabled;
        set => _clarityExciterEnabled = value;
    }

    public void ProcessHighResFloatBuffer(Span<float> interleavedSamples)
    {
        float currentGain = _linearGain;
        bool applyExciter = _clarityExciterEnabled;

        for (int i = 0; i < interleavedSamples.Length; i++)
        {
            float sample = interleavedSamples[i] * currentGain;

            if (applyExciter)
            {
                // Subtle harmonic saturation for perceived high-frequency clarity
                sample += 0.08f * (sample - (sample * sample * sample / 3.0f));
            }

            // Soft-knee hyperbolic tangent brickwall limiter prevents digital clipping
            if (sample > 0.95f || sample < -0.95f)
            {
                sample = MathF.Tanh(sample);
            }

            interleavedSamples[i] = sample;
        }
    }

    public float[] ResampleCubicCatmullRom(ReadOnlySpan<float> inputSamples, int sourceRate, int targetRate)
    {
        if (sourceRate == targetRate || inputSamples.IsEmpty)
        {
            return inputSamples.ToArray();
        }

        double ratio = (double)sourceRate / targetRate;
        int outputLength = (int)(inputSamples.Length / ratio);
        float[] resampled = new float[outputLength];

        for (int i = 0; i < outputLength; i++)
        {
            double sourcePos = i * ratio;
            int index = (int)sourcePos;
            float frac = (float)(sourcePos - index);

            float y0 = inputSamples[Math.Max(0, index - 1)];
            float y1 = inputSamples[Math.Min(inputSamples.Length - 1, index)];
            float y2 = inputSamples[Math.Min(inputSamples.Length - 1, index + 1)];
            float y3 = inputSamples[Math.Min(inputSamples.Length - 1, index + 2)];

            float a0 = -0.5f * y0 + 1.5f * y1 - 1.5f * y2 + 0.5f * y3;
            float a1 = y0 - 2.5f * y1 + 2.0f * y2 - 0.5f * y3;
            float a2 = -0.5f * y0 + 0.5f * y2;
            float a3 = y1;

            resampled[i] = ((a0 * frac + a1) * frac + a2) * frac + a3;
        }

        return resampled;
    }
}