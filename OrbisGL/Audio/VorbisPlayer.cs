using NVorbis;
using System;
using System.IO;
using System.Threading;

namespace OrbisGL.Audio
{
    public class VorbisPlayer : IAudioPlayer
    {
        Thread DecoderThread;
        bool Stopped;
        bool Paused;
        VorbisReader Reader;
        IAudioOut Driver;
        public bool Playing => Paused;

        public TimeSpan? CurrentTime => Reader.TimePosition;

        public void Close()
        {
            Stopped = true;
        }

        public void Dispose()
        {
            Driver?.Dispose();
            Reader?.Dispose();
        }

        public void Open(Stream File)
        {
            Paused = true;

            Reader = new VorbisReader(File, true);
        }

        private unsafe void Player()
        {
            Driver.SetProprieties(Reader.Channels, 256, (uint)Reader.SampleRate, true);

            int SamplesPerSecond = Reader.SampleRate * Reader.Channels;

            byte[] SamplesBuffer = new byte[SamplesPerSecond * sizeof(float)];

            try
            {
                using (RingBuffer OutBuffer = new RingBuffer(SamplesBuffer.Length * 2))
                {
                    fixed (void* pSamples = &SamplesBuffer[0])
                    {
                        Span<float> SamplesSpan = new Span<float>(pSamples, SamplesBuffer.Length);

                        byte* pBuffer = (byte*)pSamples;
                        while (!Stopped)
                        {
                            while (Paused)
                                Thread.Sleep(50);

                            int Readed = Reader.ReadSamples(SamplesSpan);

                            int ReadedBytes = Readed * sizeof(float);

                            OutBuffer.Write(SamplesBuffer, 0, ReadedBytes);
                        }
                    }
                }
            }
            catch 
            {
                throw;
            }
            finally
            {
                DecoderThread = null;
                Stopped = true;
                Driver.Stop();
            }
        }

        public void Pause()
        {
            Paused = true;
        }

        public void Resume()
        {
            if (Driver == null)
                throw new Exception("Audio Output Driver Not Set");
            
            if (DecoderThread == null)
            {
                DecoderThread = new Thread(Player);
                DecoderThread.Name = "VorbisPlayer";
                DecoderThread.Start();
            }

            Paused = false;
        }

        public void SetAudioDriver(IAudioOut Driver)
        {
            this.Driver = Driver;
        }

        public void SkipTo(TimeSpan Duration)
        {
            Reader.SeekTo(Duration);
        }
    }
}
