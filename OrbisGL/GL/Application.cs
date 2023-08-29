using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Orbis.Internals;
using OrbisGL.Controls;
using OrbisGL.Controls.Events;
using OrbisGL.GL2D;
using OrbisGL.Input;
using OrbisGL.Input.Dualshock;
using SharpGLES;

namespace OrbisGL.GL
{
    public class Application : IRenderable
    {
        public static Application Default { get; private set; }
        public static bool PhysicalKeyboardAvailable { get; set; }

        public int UserID = -1;
        
        /// <summary>
        /// An mouse driver to control the cursor
        /// </summary>
        public IMouse MouseDriver { get; set; } = null;

        /// <summary>
        /// Delay in seconds to hide the mouse cursor, zero means always hide, negative means never hide
        /// </summary>
        public int CursorHideDelay { get; set; } = 5;

        private IntPtr Handler = IntPtr.Zero;
        
        /// <summary>
        /// The current rendering width resolution
        /// </summary>
        public int Width { get; private set; }
        
        /// <summary>
        /// The current rendering height resolution
        /// </summary>
        public int Height { get; private set; }
        
        /// <summary>
        /// If set, each new frame the screen will be cleared with the given color
        /// </summary>
        public RGBColor ClearColor = null;

        /// <summary>
        /// Delay in Ticks for each frame
        /// </summary>
        public readonly int FrameDelay = 0;

        /// <summary>
        /// Enumerates the controllers currently stored at Objects
        /// </summary>
        public IEnumerable<Control> Controllers => Objects.Where(x => x is Control).Cast<Control>();

        /// <summary>
        /// A list of objects to be rendered, it can be an Control or an OpenGL object
        /// </summary>
        public IEnumerable<IRenderable> Objects => _Objects;

        private EGLDisplay GLDisplay;

        private IList<IRenderable> _Objects = new List<IRenderable>();

        private Control[] ControllersSnaptshot => Controllers.ToArray();

        private bool Initialized;


        #if ORBIS
        /// <summary>
        /// Create an OpenGL ES 2 Environment 
        /// </summary>
        /// <param name="Width">Sets the rendering Width</param>
        /// <param name="Height">Sets the rendering Height</param>
        /// <param name="FramePerSecond">Set the default frame delay</param>
        public Application(int Width, int Height, int FramePerSecond) : this(Width, Height, FramePerSecond, GPUMemoryConfig.Default)
        {
        }

        /// <summary>
        /// Create an OpenGL ES 2 Environment 
        /// </summary>
        /// <param name="Width">Sets the rendering Width</param>
        /// <param name="Height">Sets the rendering Height</param>
        /// <param name="FramePerSecond">Set the default frame delay</param>
        /// <param name="GPUMemoryConfig">Set the memory sharing settings</param>
        public Application(int Width, int Height, int FramePerSecond, GPUMemoryConfig Config)
        #else
        /// <summary>
        /// Create an OpenGL ES 2 Environment 
        /// </summary>
        /// <param name="Width">Sets the rendering Width</param>
        /// <param name="Height">Sets the rendering Height</param>
        /// <param name="FramePerSecond">Set the default frame delay</param>
        /// <param name="Handler">Set the Control Render Handler</param>
        public Application(int Width, int Height, int FramePerSecond, IntPtr Handler)
        #endif
        {
#if ORBIS
            FrameDelay = Constants.ORBIS_SECOND / FramePerSecond;
#else
            FrameDelay = 1000 / FramePerSecond;
            this.Handler = Handler;
#endif
            Default = this;

            Coordinates2D.SetSize(Width, Height);

            
#if ORBIS
            GLDisplay = new EGLDisplay(IntPtr.Zero, Width, Height, Config.Video, Config.System, Config.Flexible);
            
            Kernel.LoadStartModule("libSceMbus.sprx");//For Mouse and Dualshock Support
#endif

#if DEBUG && ORBIS
            if (GLES20.HasShaderCompiler)
                Shader.PrecompileShaders();
#endif

            this.Width = Width;
            this.Height = Height;
        }


        public void Run() => Run(CancellationToken.None);
        public virtual void Run(CancellationToken Abort)
        {

            Initialize();

            if (Control.EnableSelector && !Controllers.Any(x => x.Focused))
            {
                foreach (var Control in Controllers)
                {
                    if (Control.Focus())
                        break;
                }
            }

            long LastDrawTick = 0;
            while (!Abort.IsCancellationRequested)
            {
#if ORBIS
                long CurrentTick = 0;
                Kernel.sceRtcGetCurrentTick(out CurrentTick);

                long NextDrawTick = LastDrawTick + FrameDelay;

                if (NextDrawTick > CurrentTick)
                {
                    uint ReamingTicks = (uint)(NextDrawTick - CurrentTick);
                    Kernel.sceKernelUsleep(ReamingTicks);
                } 
#if DEBUG
                if (CurrentTick > NextDrawTick)
                    Debugger.Log(1, "WARN", "Frame Loop too Late\n");
#endif
#else
                long CurrentTick = DateTime.UtcNow.Ticks;
                
                long NextDrawTick = LastDrawTick + FrameDelay;
                
                if (NextDrawTick > CurrentTick)
                {
                    int ReamingTicks = (int)(NextDrawTick - CurrentTick);
                    Thread.Sleep(ReamingTicks);
                }
#endif

                LastDrawTick = CurrentTick;

                ProcessEvents(CurrentTick);

#if ORBIS
                Draw(CurrentTick);
#else
                Draw(CurrentTick/10);
#endif
                GLDisplay.SwapBuffers();

            }
        }
        public GamepadListener Gamepad { get; private set; } = null;

        private bool LeftAnalogCentered = true;
        private bool DualshockEnabled = false;
        public void EnableDualshock(DualshockSettings Settings)
        {
            if (DualshockEnabled)
                return;

#if ORBIS
            if (UserID == -1)
            {
                UserService.Initialize();
                UserService.GetInitialUser(out UserID);
            }

            Gamepad = new GamepadListener(UserID);

            Gamepad.OnButtonDown += (sender, args) =>
            {
                foreach (var Child in ControllersSnaptshot.Reverse())
                {
                    if (args.Handled)
                        break;

                    Child.ProcessButtonDown(sender, args);
                }
            };

            Gamepad.OnButtonUp += (sender, args) =>
            {
                foreach (var Child in ControllersSnaptshot.Reverse())
                {
                    if (args.Handled)
                        break;

                    Child.ProcessButtonUp(sender, args);
                }
            };

            if (Settings.LeftAnalogAsPad)
            {
                Gamepad.OnLeftStickMove += (sender, args) =>
                {
                    var Offset = args.CurrentOffset;

                    bool XCentered = Offset.X <= 0.2 && Offset.X >= -0.2;
                    bool YCentered = Offset.Y <= 0.2 && Offset.Y >= -0.2;

                    bool Centered = XCentered && YCentered;

                    if (!LeftAnalogCentered && Centered)
                    {
                        LeftAnalogCentered = Centered;
                        args.Handled = true;
                        return;
                    }

                    if (!Centered && LeftAnalogCentered)
                    {
                        LeftAnalogCentered = Centered;
                        args.Handled = true;

                        EmulatePad(Offset, XCentered, YCentered);
                        return;
                    }
                };
            }

            if (Settings.Mouse == VirtualMouse.Touchpad)
            {
                MouseDriver = new TouchpadMouse(Gamepad);
            }

            Control.EnableSelector = Settings.PadAsSelector;

            DualshockEnabled = true;
#endif
        }

        private void EmulatePad(Vector2 Offset, bool XCentered, bool YCentered)
        {
            ButtonEventArgs EventDown = null, EventUp = null;

            if (!XCentered && !YCentered)
            {
                if (Math.Abs(Offset.X) > Math.Abs(Offset.Y))
                    YCentered = true;
                else
                    XCentered = true;
            }

            if (Offset.X <= -0.2 && YCentered)
            {
                EventDown = new ButtonEventArgs(OrbisPadButton.Left);
                EventUp = new ButtonEventArgs(OrbisPadButton.Left);
            }

            if (Offset.X >= 0.2 && YCentered)
            {
                EventDown = new ButtonEventArgs(OrbisPadButton.Right);
                EventUp = new ButtonEventArgs(OrbisPadButton.Right);
            }

            if (Offset.Y >= 0.2 && XCentered)
            {
                EventDown = new ButtonEventArgs(OrbisPadButton.Up);
                EventUp = new ButtonEventArgs(OrbisPadButton.Up);
            }
            if (Offset.Y <= -0.2 && XCentered)
            {
                EventDown = new ButtonEventArgs(OrbisPadButton.Down);
                EventUp = new ButtonEventArgs(OrbisPadButton.Down);
            }

            foreach (var Child in ControllersSnaptshot.Reverse())
            {
                if (EventDown.Handled && EventUp.Handled)
                    break;

                Child.ProcessButtonDown(Child, EventDown);
                Child.ProcessButtonUp(Child, EventUp);
            }
        }

        public IKeyboard KeyboardDriver;

        private bool KeyboardEnabled = false;

        public void EnableKeyboard()
        {
            if (KeyboardEnabled)
                return;
#if ORBIS

            if (UserID == -1)
            {
                UserService.Initialize();
                UserService.GetInitialUser(out UserID);
            }

            KeyboardDriver = new OrbisKeyboard();
#endif

            KeyboardEnabled = true;

            KeyboardDriver.Initialize(UserID);

            KeyboardDriver.OnKeyDown += (sender, args) =>
            {
                foreach (var Child in ControllersSnaptshot.Reverse())
                {
                    if (args.Handled)
                        break;

                    Child.ProcessKeyDown(sender, args);
                }
            };

            KeyboardDriver.OnKeyUp += (sender, args) =>
            {
                foreach (var Child in ControllersSnaptshot.Reverse())
                {
                    if (args.Handled)
                        break;

                    Child.ProcessKeyUp(sender, args);
                }
            };
        }

        public Vector2 CursorPosition { get; private set; } = Vector2.Zero;
        public MouseButtons MousePressedButtons { get; private set; }

        IMouse InitializedMouse = null;

        private long LastMouseMove;

        private void ProcessEvents(long Tick)
        {
            if (MouseDriver != null)
            {
                if (InitializedMouse != MouseDriver)
                {
                    EnableMouse();
                }

                MouseDriver.RefreshData(Tick);

                var CurrentPosition = MouseDriver.GetPosition();
                bool Moved = CursorPosition != CurrentPosition;

                if (Moved)
                {
                    LastMouseMove = Tick;
                    CursorPosition = CurrentPosition;
                    Control.Cursor.Visible = true;
                    foreach (var Child in ControllersSnaptshot.Reverse())
                    {
                        if (Child.AbsoluteRectangle.IsInBounds(CurrentPosition))
                        {
                            Child.ProcessMouseMove(CurrentPosition);
                            break;
                        }
                    }
                }
                else if (CursorHideDelay >= 0 && (Tick - LastMouseMove) / Constants.ORBIS_SECOND > CursorHideDelay)
                {
                    Control.Cursor.Visible = false;
                }

                var CurrentButtons = MouseDriver.GetMouseButtons();
                bool Changed = CurrentButtons != MousePressedButtons;

                if (Changed)
                {
                    var OldButtons = MousePressedButtons;
                    MousePressedButtons = CurrentButtons;
                    Control.Cursor.Visible = true;

                    foreach (var Child in ControllersSnaptshot.Reverse())
                    {
                        if (Child.AbsoluteRectangle.IsInBounds(CurrentPosition))
                        {
                            Child.ProcessMouseButtons(OldButtons, CurrentButtons);
                        }
                    }
                }
            }

#if ORBIS
            KeyboardDriver?.RefreshData();
            Gamepad?.RefreshData();
#endif
        }

        public void EnableMouse()
        {
#if ORBIS
            if (UserID == -1)
            {
                UserService.Initialize();
                UserService.GetInitialUser(out UserID);
            }
#endif
            
            Control.Cursor = new Cursor()
            {
                ContourWidth = Coordinates2D.Height / 720f,
                Height = (int)((Coordinates2D.Height / 720f) * 19),
                Visible = false
            };

            InitializedMouse = MouseDriver;
            MouseDriver.Initialize(UserID);
            
            Control.Cursor.RefreshVertex();
        }

        public void DrawOnce()
        {
            Initialize();
            
#if ORBIS
            Kernel.sceRtcGetCurrentTick(out long Ticks);
#else
            var Ticks = DateTime.Now.Ticks / 10;
#endif
            ProcessEvents(Ticks);
            Draw(Ticks);
#if ORBIS
            GLDisplay.SwapBuffers();
#endif
        }

        private void Initialize()
        {
            if (Initialized)
                return;
            
            Initialized = true;
            
#if ORBIS
            UserService.Initialize();
            UserService.HideSplashScreen();
            GLES20.Viewport(0, 0, GLDisplay.Width, GLDisplay.Height);
#else
            if (GLDisplay == null)
                GLDisplay = new EGLDisplay(Handler, Width, Height, GPUMemoryConfig.Default.Video, GPUMemoryConfig.Default.System, GPUMemoryConfig.Default.Flexible);
#endif
        }

        public virtual void Draw(long Tick)
        {
            if (ClearColor != null)
            {
                GLES20.ClearColor(ClearColor.RedF, ClearColor.GreenF, ClearColor.BlueF, 1);
                GLES20.Clear(GLES20.GL_COLOR_BUFFER_BIT);
            }
                
            foreach (var Object in Objects.ToArray())
            {
                if (Object is Control Controller)
                    Controller.FlushMouseEvents(Tick);

                Object.Draw(Tick);
            }

            Control.Selector?.Draw(Tick);
            Control.Cursor?.Draw(Tick);
        }

        /// <summary>
        /// Add an Object to be rendred
        /// </summary>
        /// <param name="Object">The renderable object</param>
        public void AddObject(IRenderable Object)
        {
            if (Object is Control Controller)
                Controller._Application = this;

            _Objects.Add(Object);
        }

        /// <summary>
        /// Remove an object to be rendered
        /// </summary>
        /// <param name="Object">The renderable object</param>
        public void RemoveObject(IRenderable Object)
        {
            _Objects.Remove(Object);

            if (Object is Control Controller)
                Controller._Application = null;
        }

        /// <summary>
        /// Remove all renderable objects
        /// </summary>
        public void RemoveObjects()
        {
            foreach (var Object in _Objects)
            {
                if (Object is Control Controller)
                    Controller._Application = null;
            }

            _Objects.Clear();
        }
        
#if !ORBIS
        public void SwapBuffers() => GLDisplay?.SwapBuffers();
        public void ChangeResolution(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;

            Coordinates2D.SetSize(Width, Height);

            foreach (var Object in _Objects)
            {
                if (Object is Control Controller)
                    Controller.Invalidate();

                if (Object is GLObject2D Obj2D)
                    Obj2D.RefreshVertex();
            }

            GLDisplay.Dispose();
            GLDisplay = new EGLDisplay(Handler, Width, Height, GPUMemoryConfig.Default.Video, GPUMemoryConfig.Default.System, GPUMemoryConfig.Default.Flexible);

            GLES20.Viewport(0, 0, Width, Height);
        }
#endif
        public void Dispose()
        {
            foreach (var Object in Objects)
            {
                Object.Dispose();
            }
            
            GLDisplay?.Dispose();
        }
    }
}