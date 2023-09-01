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

        public event EventHandler OnTrackEnd;

        public bool Playing => Paused;
        
        public TimeSpan? CurrentTime => Reader.TimePosition;

        public bool Loop { get; set; }

        public void Open(Stream File)
        {
            Paused = true;
            Reader = new VorbisReader(File, true);
            
            if (Reader.SampleRate != 48000)
            {
                Reader = null;
                throw new Exception("Currently Only Audio in 48khz is supported");
            }
        }

        private unsafe void Player()
        {
            if (Reader == null)
                return;
            
            Driver.SetProprieties(Reader.Channels, 512, (uint)Reader.SampleRate, true);

            int SamplesPerSecond = Reader.SampleRate * Reader.Channels;

            byte[] SamplesBuffer = new byte[SamplesPerSecond * sizeof(float)];
            
            try
            {
                using (RingBuffer OutBuffer = new RingBuffer(SamplesBuffer.Length * 3))
                {
                    Driver.Play(OutBuffer);
                    
                    fixed (void* pSamples = &SamplesBuffer[0])
                    {
                        Span<float> SamplesSpan = new Span<float>(pSamples, SamplesPerSecond);
                        
                        while (!Stopped && Driver.IsRunnning)
                        {
                            while ((Paused || OutBuffer.Length >= SamplesBuffer.Length * 2) && Driver.IsRunnning)
                                Thread.Sleep(100);

                            int Readed = Reader.ReadSamples(SamplesSpan);

                            int ReadedBytes = Readed * sizeof(float);

                            OutBuffer.Write(SamplesBuffer, 0, ReadedBytes);

                            if (Readed == 0)
                            {
                                if (Loop)
                                    Reader.TimePosition = TimeSpan.Zero;
                                break;
                            }
                        }

                        OnTrackEnd?.Invoke(this, EventArgs.Empty);
                    }
                    
                    while (OutBuffer.Length > 0 && Driver.IsRunnning)
                    {
                        Driver.Flush();
                        Thread.Sleep(100);
                    }
                }
            }
            finally
            {
                DecoderThread = null;
                Stopped = true;
                Driver?.Stop();
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

            if (Reader == null)
                return;
            
            if (DecoderThread == null)
            {
                DecoderThread = new Thread(Player);
                DecoderThread.Name = "VorbisPlayer";
                DecoderThread.Start();
            }

            Driver.Resume();
            Paused = false;
        }

        public void Restart()
        {
            SkipTo(TimeSpan.Zero);
            Resume();
        }

        public void SetAudioDriver(IAudioOut Driver)
        {
            this.Driver = Driver;
        }

        public void SkipTo(TimeSpan Duration)
        {
            Reader.SeekTo(Duration);
        }

        public void Close()
        {
            Stopped = true;
        }

        public void Dispose()
        {
            Stopped = true;
            DecoderThread?.Abort();
            Driver?.Dispose();
            Reader?.Dispose();
        }

    }
}
