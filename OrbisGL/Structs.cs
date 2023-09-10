using OrbisGL.GL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpGLES;
using System.Numerics;

namespace OrbisGL
{
    public struct IMEKeyModifier
    {
        public IME_KeyCode Code;
        public bool Shift;
        public bool Alt;
        public bool NumLock;

        public IMEKeyModifier(IME_KeyCode Code, bool Shift, bool Alt, bool NumLock)
        {
            this.Code = Code;
            this.Shift = Shift;
            this.Alt = Alt;
            this.NumLock = NumLock;
        }
    }

    public struct GPUMemoryConfig
    {
        public ulong VideoShared;
        public ulong System;
        public ulong Flexible;
        public ulong VideoPrivate;

        public static GPUMemoryConfig Default = new GPUMemoryConfig()
        {
            VideoShared = 512 * Constants.MB,
            VideoPrivate = 0,
            System = 250 * Constants.MB,
            Flexible = 170 * Constants.MB
        };
    }

    public struct DualshockSettings
    {
        /// <summary>
        /// Sets a virtual mouse mode
        /// </summary>
        public VirtualMouse Mouse;


        /// <summary>
        /// When true, the Left Analog will be converted as Up/Down/Left/Right buttons
        /// </summary>
        public bool LeftAnalogAsPad;

        /// <summary>
        /// When true, the PADs buttons will allow select the controller focus
        /// </summary>
        public bool PadAsSelector;

        //TODO: Implement Deadzone and Sensitivity
        //https://stackoverflow.com/questions/43240440/c-sharp-joystick-sensitivity-formula/43245072#43245072
    }

    public struct ControlLink
    {
        public Controls.Control Up;
        public Controls.Control Down;
        public Controls.Control Left;
        public Controls.Control Right;
    }

    public struct SpriteFrame
    {
        /// <summary>
        /// Frame Coordinates in Target Texture
        /// </summary>
        public Rectangle Coordinates;

        /// <summary>
        /// The frame size
        /// </summary>
        public Vector2 FrameSize;

        /// <summary>
        /// Frame Delta X Offset
        /// </summary>
        public int X;
        /// <summary>
        /// Frame Delta Y Offset
        /// </summary>
        public int Y;
    }

    [DebuggerDisplay("{Name}")]
    public struct SpriteInfo
    {
        /// <summary>
        /// Sprite Animation Name
        /// </summary>
        public string Name;

        /// <summary>
        /// Holds each frame of the given sprite
        /// </summary>
        public SpriteFrame[] Frames;
    }

    internal struct ShaderInfo
    {
        public byte[] Hash;
        public byte[] Data;
        public int Type;
    }
}
