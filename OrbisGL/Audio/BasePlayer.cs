using System;
using System.IO;
using System.Threading;
using Orbis.Internals;

namespace OrbisGL.Audio
{
    public abstract class BasePlayer : IAudioPlayer
    {
        bool _ThreadStarted = false;
        bool Ready = false;
        bool Stopped = true;
        bool FullStop = false;
        bool Paused;
        IAudioOut Driver;
        Thread PlayerThread = null;

        protected virtual string ThreadName => "BasePlayer";

        public event EventHandler OnTrackEnd;
        public bool Loop { get; set; }

        public TimeSpan? CurrentTime { get; private set; }

        public bool Playing => !Paused && !Stopped;

        public void Close()
        {
            Ready = false;
            Stopped = true;
        }

        public virtual void Dispose()
        {
            Close();
            Driver?.Dispose();
        }


        public virtual void Open(Stream File)
        {
            Ready = true;
        }

        public void Pause()
        {
            Paused = true;
        }
        

        public void Resume()
        {
            if (Driver == null)
                throw new Exception("Audio Output Driver Not Set");

            if (!Ready)
                return;

            if (FullStop)
            {
                FullStop = true;
                PlayerThread = null;
            }
            
            if (PlayerThread == null)
            {
                WaitFullStop();
                
                _ThreadStarted = false;
                PlayerThread = new Thread(PlayerEntrypoint);
                PlayerThread.Name = ThreadName;
                PlayerThread.Start();
            } 

            while (!_ThreadStarted && PlayerThread != null)
            {
                Kernel.sceKernelUsleep(1000);
            }
            
            Driver.Resume();
            Paused = false;
        }

        public void Restart()
        {
            FullStop = true;
            Driver.Stop();
            Driver.Reset();
            SkipTo(TimeSpan.Zero);
            Resume();
        }

        private void WaitFullStop()
        {
            while (Driver.IsRunnning)
            {
                Driver.Stop();
                Kernel.sceKernelUsleep(1000);
            }
            
           //while (PlayerThread != null)
           //     Kernel.sceKernelUsleep(1000);
        }

        /// <summary>
        /// The player thread entrypoint, Use to call the <see cref="Player(Stream, int, int, uint)"/> with the required parameters.
        /// </summary>
        protected abstract void PlayerEntrypoint();

        /// <summary>
        /// Initialize the Audio Output Loop
        /// </summary>
        /// <param name="Input">The stream with the samples to be played</param>
        /// <param name="BufferSize">The buffer size</param>
        /// <param name="Channels">The total channels count</param>
        /// <param name="SamplesPerSec">The total samples per second</param>
        /// <param name="BeginOffset">The sample data begin offset in the given stream</param>
        /// <param name="DataSize">The sample data size in the given stream</param>
        protected void Player(Stream Input, int BufferSize, int Channels, uint SamplesPerSec, long BeginOffset, long DataSize)
        {
            var CurrentThread = Thread.CurrentThread;
            
            Func<bool> PlayerAlive = () => CurrentThread.ManagedThreadId == PlayerThread?.ManagedThreadId;
            
            using (var Buffer = new RingBuffer(BufferSize*3))
            {
                Input.Position = BeginOffset;
                var EndPos = BeginOffset + DataSize;

                const int Grain = 512;
        
                Driver.SetProprieties(Channels, Grain, SamplesPerSec);
                Driver.Play(Buffer);

                CurrentTime = TimeSpan.Zero;

                byte[] DataBuffer = new byte[BufferSize];

                Stopped = false;

                //wait driver thread initializes
                while (!Stopped && !Driver.IsRunnning)
                    Thread.Sleep(10);

                _ThreadStarted = true;

                try
                {
                    while (Input != null && Input.Position < EndPos && !Stopped && Driver.IsRunnning && PlayerAlive())
                    {
                        int Readed = Input.Read(DataBuffer, 0, DataBuffer.Length);
                        
                        while (Driver.IsRunnning && (Driver.ToBeFlushed || Buffer.CantWrite(Readed)))
                        {
                            Thread.Sleep(10);
                        }
                        
                        Buffer.Write(DataBuffer, 0, Readed);

                        CurrentTime += TimeSpan.FromSeconds(1);

                        if (Loop && Input.Position >= EndPos)
                        {
                            Input.Position = BeginOffset;
                            CurrentTime = TimeSpan.Zero;
                        }

                        while ((Paused || Buffer.Length >= BufferSize * 2) && !Stopped && Driver.IsRunnning)
                            Thread.Sleep(100);
                    }

                    if (!Stopped && PlayerAlive())
                        OnTrackEnd?.Invoke(this, EventArgs.Empty);
                }
                catch (ObjectDisposedException ex) {}
                finally
                {
                    //Wait the Audio Output Driver read the reaming buffered data
                    while (Buffer.Length > 0 && Driver.IsRunnning)
                    {
                        Driver.Flush();
                        Thread.Sleep(100);
                    }

                    if (PlayerAlive())
                    {
                        Stopped = true;
                        PlayerThread = null;
                        Driver?.Stop();
                    }
                }
            }
        }

        public void SetAudioDriver(IAudioOut Driver)
        {
            this.Driver = Driver;
        }

        public abstract void SkipTo(TimeSpan Duration);
        

    }
}
