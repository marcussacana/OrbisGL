using OrbisGL.GL;
using System;
using System.Linq;
using System.Numerics;

namespace OrbisGL.GL2D
{
    /// <summary>
    /// Makes any <see cref="GLObject2D"/> acts as an sprite
    /// through the <see cref="SetVisibleRectangle(Rectangle)"/> method,
    /// by ensuring the visible rectangle will allways start in the position
    /// of the <see cref="Sprite2D"/> instance
    /// </summary>
    public class Sprite2D : GLObject2D
    {
        public GLObject2D Target { get; private set; }

        public override RGBColor Color { get => Target.Color; set => Target.Color = value; }
        public override byte Opacity { get => Target.Opacity; set => Target.Opacity = value; }

        public event EventHandler OnAnimationEnd;
        public event EventHandler<int> OnFrameChange;

        int _FrameDelay;

        private Rectangle? CurFrameRect = null;

        /// <summary>
        /// Sets an delay in miliseconds for advance the sprite in the next frame
        /// automatically, where 0 means disabled
        /// </summary>
        public int FrameDelay { get => _FrameDelay; 
            set 
            {
                _FrameDelay = value;
                FrameDelayTicks = value * Constants.ORBIS_MILISECOND;
            }
        }


        int FrameDelayTicks;
        int CurrentFrame = 0;

        public Sprite2D(GLObject2D Content)
        {
            if (Content is null)
                throw new ArgumentNullException(nameof(Content));

            Target = Content;

            base.Width = Target.Width;
            base.Height = Target.Height;

            base.AddChild(Content);
        }

        private void InternalSetVisibleRectangle(Rectangle Area)
        {
            CurFrameRect = Area;
            Target.Position = -Area.Position;
            Width = (int)Area.Width;
            Height = (int)Area.Height;
            Target.SetVisibleRectangle(Area);
        }

        public override void SetVisibleRectangle(Rectangle Area)
        {
            if (CurFrameRect.HasValue)
            {
                var Frame = CurFrameRect.Value;

                if (Zoom != 1)
                {
                    Area.Position = ProcessZoomFactor(Area.Position, true);
                    Area.Size = ProcessZoomFactor(Area.Size, true);
                }

                var AbsFrameArea = new Rectangle(Frame.X + Area.X, Frame.Y + Area.Y, Area.Width, Area.Height);

                var FrameCutArea = Rectangle.GetChildBounds(Frame, AbsFrameArea);

                FrameCutArea.X += Frame.X + Area.X;
                FrameCutArea.Y += Frame.Y + Area.Y;

                Target.Position = -Frame.Position;
                Target.SetVisibleRectangle(FrameCutArea);
            }
            else
            {
                Target.Position = -Area.Position;
                Width = (int)Area.Width;
                Height = (int)Area.Height;

                Target.SetVisibleRectangle(Area);
            }
        }

        public override void ClearVisibleRectangle()
        {
            Target.Position = Vector2.Zero;
            Width = Target.Width;
            Height = Target.Height;
            CurFrameRect = null;
            base.ClearVisibleRectangle();
        }

        public override void AddChild(GLObject2D Child)
        {
            Target.AddChild(Child);
        }

        public override void RemoveChild(GLObject2D Child)
        {
            Target.RemoveChild(Child);
        }

        public override void RemoveChildren(bool Dispose)
        {
            Target.RemoveChildren(Dispose);
        }

        public Rectangle[] Frames { get; set; } = new Rectangle[0];

        /// <summary>
        /// Calculate all frames rectangle by the frame amount
        /// </summary>
        /// <param name="TotalFrames"></param>
        public void ComputeAllFrames(int TotalFrames)
        {
            var Frame = new Rectangle(0, 0, Width, Height);
            var RowCount = Target.Width / Width;

            Frames = GetAllFrames(Frame, RowCount, TotalFrames, Target.Width, Target.Height);
        }

        /// <summary>
        /// Calculate all frame rectangles by the given sprite params
        /// </summary>
        /// <param name="FirstFrame">The first frame rectangle</param>
        /// <param name="TotalFrames">The total frame count</param>
        /// <param name="FramesPerRow">The max frame count in each row</param>
        public void ComputeAllFrames(int TotalFrames, Rectangle? FirstFrame, int? FramesPerRow = null)
        {
            var Frame = FirstFrame ?? new Rectangle(0, 0, Target.Width, Target.Height);
            var RowCount = FramesPerRow ?? Target.Width / Width;

            Frames = GetAllFrames(Frame, RowCount, TotalFrames, Target.Width, Target.Height);
        }

        public static Rectangle[] GetAllFrames(Rectangle FirstFrame, int FramesPerLine, int TotalFrames, int MaxWidth, int MaxHeight)
        {
            var Rects = new Rectangle[TotalFrames];

            var Rect = FirstFrame;

            for (int i = 0; i < TotalFrames; i++)
            {
                Rect.X = (Rect.Width * (i % FramesPerLine)) + FirstFrame.X;
                Rect.Y = (Rect.Height * (i / FramesPerLine)) + FirstFrame.Y;

                if (Rect.Right > MaxWidth)
                {
                    Rect.X = FirstFrame.X;
                    Rect.Y += Rect.Height;
                }

                if (Rect.Bottom > MaxHeight)
                    Rect.Y = FirstFrame.Y;

                Rects[i] = Rect;
            }

            return Rects;
        }



        /// <summary>
        /// Advances to the next available frame.
        /// </summary>
        /// <returns>The index of the newly rendered frame, or -1 if no frames are available.</returns>
        public virtual int NextFrame()
        {
            if (Width == 0)
                throw new ArgumentOutOfRangeException(nameof(Width));

            if (Height == 0)
                throw new ArgumentOutOfRangeException(nameof(Height));

            if (Frames == null || !Frames.Any())
                throw new ArgumentException("Missing Frame Info");

            if (Frames.Length == 0)
                return -1;

            bool AnimationEnd = false;

            if (CurrentFrame >= Frames.Length)
            {
                CurrentFrame = 0;
                AnimationEnd = true;
            }

            var DrawFrame = CurrentFrame;

            InternalSetVisibleRectangle(Frames[CurrentFrame]);
            CurrentFrame++;

            if (CurrentFrame >= Frames.Length)
            {
                CurrentFrame = 0;
                AnimationEnd = true;
            }

            OnFrameChange?.Invoke(this, DrawFrame);

            if (AnimationEnd)
                OnAnimationEnd?.Invoke(this, EventArgs.Empty);

            return DrawFrame;
        }


        /// <summary>
        /// Set the given frame visible
        /// </summary>
        public void SetCurrentFrame(int Step)
        {
            CurrentFrame = Step;

            NextFrame();
        }

        long LastStepTick = -1;
        public override void Draw(long Tick)
        {
            if (Width != 0 && Height != 0 && FrameDelayTicks != 0)
            {
                if ((Tick - LastStepTick) > FrameDelayTicks)
                {
                    LastStepTick = Tick;
                    NextFrame();
                }
            }

            base.Draw(Tick);
        }

        public override void Dispose()
        {
            Target?.Dispose();
            base.Dispose();
        }
    }
}
