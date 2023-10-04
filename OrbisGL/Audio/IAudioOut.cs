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
        
        /// <summary>
        /// Starts the ring buffer reproduction
        /// </summary>
        void Play(RingBuffer PCMBuffer);
        
        /// <summary>
        /// Interrupt the Audio Output 
        /// </summary>
        void Stop();

        /// <summary>
        /// Forces the Audio Driver reproduce all buffered audio instead wait an complete block
        /// </summary>
        void Flush();

        /// <summary>
        /// Notify the Audio Driver to clear the buffered audio in the next event loop 
        /// </summary>
        void Reset();

        /// <summary>
        /// Pauses the buffer reproduction
        /// </summary>
        void Suspend();
        
        /// <summary>
        /// Resumes the buffer reproduction
        /// </summary>
        void Resume();
    }
}
