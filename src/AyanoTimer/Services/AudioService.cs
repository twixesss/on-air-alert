using System;
using System.IO;
using NAudio.Wave;

namespace AyanoTimer.Services;

public class AudioService : IDisposable
{
    private WaveOutEvent? _outputDevice;
    private AudioFileReader? _audioFile;

    public void Play(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        Stop();

        try
        {
            _audioFile = new AudioFileReader(filePath);
            _outputDevice = new WaveOutEvent();
            _outputDevice.Init(_audioFile);
            _outputDevice.Play();
        }
        catch
        {
            Stop();
        }
    }

    public void Stop()
    {
        _outputDevice?.Stop();
        _outputDevice?.Dispose();
        _outputDevice = null;

        _audioFile?.Dispose();
        _audioFile = null;
    }

    public void Dispose()
    {
        Stop();
    }
}
