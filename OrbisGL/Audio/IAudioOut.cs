using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbisGL.Audio
{
    public interface IAudioOut : IDisposable
    {
        bool IsRunnning { get; }
        void SetVolume(byte Value);
        void SetProprieties(int Channels, uint Grain, uint SamplingRate = 48000, bool FloatSample = false);
        void Play(RingBuffer PCMBuffer);
        void Stop();

        void Flush();

        void Suspend();
        void Resume();
    }
}
