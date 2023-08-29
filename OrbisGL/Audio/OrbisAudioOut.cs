using Orbis.Internals;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using static OrbisGL.Constants;

namespace OrbisGL.Audio
{
    public class OrbisAudioOut : IAudioOut
    {
        static OrbisAudioOut()
        {
            //Made to early JIT the methods for faster initialization
            //when the user ask to play some audio
            new OrbisAudioOut().Player();
        }
        
        RingBuffer Buffer;

        private int handle;

        bool PausePlayer = false;
        bool StopPlayer = false;

        bool FloatSample = false;

        int Channels;
        uint Grain;
        uint Sampling;

        Thread SoundThread;

        private static bool Initialized;

        public static bool Ready { get; private set; }

        public void SetProprieties(int Channels, uint Grain, uint SamplingRate = 48000, bool FloatSample = false)
        {
            if (!(new uint[] { 256, 512, 768, 1024, 1280, 1536, 1792, 2048 }).Contains(Grain))
                throw new ArgumentException("Grain must be one of the given values:\n256, 512, 768, 1024, 1280, 1536, 1792, 2048");

            if (SamplingRate != 48000)
                throw new ArgumentException("Playstation 4 accept only sampling rates at 48000hz");
            
            this.Channels = Channels;
            this.Grain = Grain;
            this.Sampling = SamplingRate;
            this.FloatSample = FloatSample;
        }

        public void Play(RingBuffer PCMBuffer)
        {
            if (!Initialized)
            {
                var Rst = sceAudioOutInit();
                
                if (Rst < 0 && Rst != unchecked((int)ORBIS_AUDIO_OUT_ERROR_ALREADY_INIT))
                    throw new Exception($"Failed to Init the Audio Driver Library, Error Code: {Rst}");
                
                Initialized = true;
            }

            if (SoundThread != null)
            {
                StopPlayer = true;
                while (SoundThread != null)
                    Thread.Sleep(100);
            }

            if (Sampling == 0 || Grain == 0)
                throw new Exception("Audio Output Proprieties not set.");
            
            Buffer = PCMBuffer;

            Ready = true;
            SoundThread = new Thread(Player);
            SoundThread.Name = "AudioOut";
            SoundThread.Start();
        }

        private unsafe void Player()
        {
            if (!Ready)
            {
                Ready = true;
                return;
            }

            uint Param;

            if (FloatSample)
                Param = (uint)(Channels > 2 ? ORBIS_AUDIO_OUT_PARAM_FORMAT_FLOAT_8CH : ORBIS_AUDIO_OUT_PARAM_FORMAT_FLOAT_STEREO);
            else
                Param = (uint)(Channels > 2 ? ORBIS_AUDIO_OUT_PARAM_FORMAT_S16_8CH : ORBIS_AUDIO_OUT_PARAM_FORMAT_S16_STEREO);

            handle = sceAudioOutOpen(
                ORBIS_USER_SERVICE_USER_ID_SYSTEM, 
                ORBIS_AUDIO_OUT_PORT_TYPE_MAIN, 0,
                Grain, Sampling, Param);

            if (handle < 0)
                throw new Exception("Failed to Initialize the Audio Driver Instance");

            SetVolume(80);

            int BlockSize = (int)(Grain * Channels * sizeof(short));

            var WavBufferA = new byte[Grain * (int)OrbisAudioOutChannel.MAX];
            var WavBufferB = new byte[Grain * (int)OrbisAudioOutChannel.MAX];

            var fWavBufferA = new byte[Grain * (int)OrbisAudioOutChannel.MAX];
            var fWavBufferB = new byte[Grain * (int)OrbisAudioOutChannel.MAX];

            bool CurrentBuffer = false;

            fixed (byte* pWavBufferA = WavBufferA, pWavBufferB = WavBufferB)
            fixed (byte* pfWaveBufferA = fWavBufferA, pfWaveBufferB = fWavBufferB)
            {
                try
                {
                    while (!StopPlayer)
                    {
                        short* WaveBuffer = (short*)(CurrentBuffer ? pWavBufferA : pWavBufferB);
                        float* fWaveBuffer = (float*)(CurrentBuffer ? pfWaveBufferA : pfWaveBufferB);

                        while (PausePlayer)
                        {
                            Kernel.sceKernelUsleep(100);
                        }

                        if (Buffer.Length >= BlockSize)
                        {
                            int Readed = 0;

                            if (FloatSample)
                            {
                                Readed = Buffer.Read(CurrentBuffer ? fWavBufferA : fWavBufferB, 0, BlockSize);
                            }
                            else
                            {
                                Readed = Buffer.Read(CurrentBuffer ? WavBufferA : WavBufferB, 0, BlockSize);
                            }

                            if (Readed < BlockSize)
                            {
                                if (FloatSample)
                                {
                                    for (int i = Readed / 4; i < BlockSize / 4; i++)
                                    {
                                        fWaveBuffer[i / 4] = 0;
                                    }
                                }
                                else
                                {
                                    for (int i = Readed / 2; i < BlockSize / 2; i++)
                                    {
                                        WaveBuffer[i / 2] = 0;
                                    }
                                }
                            }

                            if (FloatSample)
                            {
                                sceAudioOutOutput(handle, fWaveBuffer);
                            }
                            else
                            {
                                sceAudioOutOutput(handle, WaveBuffer);
                            }

                            CurrentBuffer = !CurrentBuffer;
                        }
                        else
                        {
                            Kernel.sceKernelUsleep(1000);
                        }
                    }
                }
                catch (ThreadAbortException abort) { }
            }

            sceAudioOutOutput(handle, null);
            sceAudioOutClose(handle);
            StopPlayer = false;
            SoundThread = null;
        }

        public void SetVolume(byte Value)
        {
            Value = Math.Min(Value, (byte)100);

            int[] Volume = new int[(int)OrbisAudioOutChannel.MAX];

            for (int i = 0; i < Volume.Length; i++)
            {
                Volume[i] = (int)(ORBIS_AUDIO_VOLUME_0DB * (Value/100f));
            }

            sceAudioOutSetVolume(handle, ORBIS_AUDIO_VOLUME_FLAG_ALL, Volume);
        }

        public void Suspend()
        {
            PausePlayer = true;
        }

        public void Resume()
        {
            PausePlayer = false;
        }

        public void Stop()
        {
            StopPlayer = true;
        }

        public void Dispose()
        {
            StopPlayer = true;
            SetVolume(0);
            
            while (StopPlayer)
                Thread.Sleep(100);
            
            if (handle != 0)
                sceAudioOutClose(handle);
            
            handle = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OrbisAudioOutPortState
        {
            public ushort output;             // SceAudioOutStateOutput (bitwise OR)
            public byte channel;              // SceAudioOutStateChannel
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] reserved8_1;        // reserved
            public short volume;
            public ushort rerouteCounter;
            public ulong flag;                // SceAudioOutStateFlag (bitwise OR)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public ulong[] reserved64;        // reserved
        }
        

        [DllImport("libSceAudioOut.sprx")]
        static extern int sceAudioOutClose(int handle);


        [DllImport("libSceAudioOut.sprx")]
        static extern int sceAudioOutGetPortState(int handle, ref OrbisAudioOutPortState state);


        [DllImport("libSceAudioOut.sprx")]
        static extern int sceAudioOutInit();


        [DllImport("libSceAudioOut.sprx")]
        static extern int sceAudioOutOpen(int userId, int type, int index, uint len, uint freq, uint param);


        [DllImport("libSceAudioOut.sprx")]
        static unsafe extern int sceAudioOutOutput(int handle, void* Buffer);


        [DllImport("libSceAudioOut.sprx")]
        static extern int sceAudioOutOutputs();


        [DllImport("libSceAudioOut.sprx")]
        static extern int sceAudioOutSetVolume(int handle, int flags, int[] Volume);


        [DllImport("libSceAudioOut.sprx")]
        static extern int sceAudioOutInitIpmiGetSession();
    }
}
