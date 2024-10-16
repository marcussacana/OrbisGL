﻿using OrbisGL.GL;
using SharpGLES;
using System.Collections.Generic;
using System.Numerics;
using static OrbisGL.GL2D.Coordinates2D;

namespace OrbisGL.GL2D
{
    public abstract class GLObject2D : GLObject
    {
        public bool Visible { get; set; } = true;

        public float Zoom { get; private set; } = 1f;

        public int Width { get; set; }
        public int Height { get; set; }

        public Vector2 Size { get => new Vector2(Width, Height); }

        public Vector2 ZoomSize { get => new Vector2(ZoomWidth, ZoomHeight); }

        public bool InRoot => Parent == null;

        public virtual RGBColor Color { get; set; } = RGBColor.White;

        /// <summary>
        /// An opacity value in the range 0-255
        /// </summary>
        public virtual byte Opacity { get; set; } = 255;

        public IEnumerable<GLObject2D> Childs => Children;

        /// <summary>
        /// XY in Pixels of the object drawing location
        /// </summary>
        public Vector2 Position
        {
            get => _Position;
            set
            {
                _Position = value;
                Offset = new Vector2(PixelOffset.X * value.X, PixelOffset.Y * value.Y);
            }
        }

        /// <summary>
        /// A coordinate with the current zoom applied,
        /// if you use <see cref="Position"/> and the zoom at 200%,
        /// The poistion value will be keep the same, but the real position
        /// has been changed due the zoom, therefore you can use <see cref="ZoomPosition"/> 
        /// to get the real position after the zoom be applied
        /// </summary>
        public Vector2 ZoomPosition
        {
            get
            {
                return new Vector2(Offset.X / XOffset, Offset.Y / YOffset);
            }
            set
            {
                Offset = new Vector2(XOffset * value.X, YOffset * value.Y);
                _Position = Offset / PixelOffset;
            }
        }

        public int ZoomWidth
        {
            get
            {
                return (int)PointToX(XToPoint(Width, ZoomMaxWidth), Coordinates2D.Width);
            }
            set
            {
                Width = (int)PointToX(XToPoint(value, Coordinates2D.Width), Width);
            }
        }

        public int ZoomHeight
        {
            get
            {
                return (int)PointToY(YToPoint(Height, ZoomMaxHeight), Coordinates2D.Height);
            }
            set
            {
                Height = (int)PointToY(YToPoint(value, Coordinates2D.Height), Height);
            }
        }

        /// <summary>
        /// The MaxWidth to be computed at <see cref="XToPoint(float, int)"/>
        /// </summary>
        protected int ZoomMaxWidth => (int)(Coordinates2D.Width * Zoom);

        /// <summary>
        /// The MaxHeight to be computed at <see cref="YToPoint(float, int)"/>
        /// </summary>
        protected int ZoomMaxHeight => (int)(Coordinates2D.Height * Zoom);


        public Rectangle? VisibleRectangle { get; protected set; }

        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle(Position.X, Position.Y, Width, Height);
            }
            set
            {
                Position = value.Position;
                Width = (int)value.Width;
                Height = (int)value.Height;
            }
        }

        public GLObject2D Parent { get; protected set; } = null;

        private List<GLObject2D> Children = new List<GLObject2D>();

        /// <summary>
        /// Represents an vertex offset of the object's drawing location.
        /// Calculate the offset pixels using <see cref="XOffset"/> and <see cref="YOffset"/>.
        /// </summary>
        protected Vector2 Offset { get; set; }


        private Vector2 _Position;

        protected Vector2 AbsoluteOffset => Parent?.AbsoluteOffset + Offset ?? Offset;

        /// <summary>
        /// Represents the XY drawing coordinates relative to the screen.
        /// </summary>
        protected Vector2 AbsolutePosition => Parent?.AbsolutePosition + Position ?? Position;

        /// <summary>
        /// Represents the XY drawing coordinates relative to the screen, considering the computed zoom distance.
        /// </summary>
        protected Vector2 AbsoluteZoomPosition => Parent?.AbsoluteZoomPosition + ZoomPosition ?? ZoomPosition;


        private Vector2? _PixelOffsetOverrider = null;
        private Vector2 PixelOffset {
            get {
                return _PixelOffsetOverrider ?? new Vector2(XOffset, YOffset);
            }
            set {
                _PixelOffsetOverrider = value;
            }
        }


        int OffsetUniform = int.MinValue;
        int VisibleUniform = int.MinValue;
        int ColorUniform = int.MinValue;
        int ResolutionUniform = int.MinValue;

        public void UpdateUniforms()
        {
            if (OffsetUniform >= 0)
            {
                Program.SetUniform(OffsetUniform, AbsoluteOffset.X, AbsoluteOffset.Y, 1);
            }
            else if (OffsetUniform == int.MinValue)
            {
                OffsetUniform = GLES20.GetUniformLocation(Program.Handler, "Offset");
                Program.SetUniform(OffsetUniform, AbsoluteOffset.X, AbsoluteOffset.Y, 1);
            }

            if (VisibleUniform >= 0)
            {
                Program.SetUniform(VisibleUniform, VisibleRectUV);
            }
            else if (VisibleUniform == int.MinValue)
            {
                VisibleUniform = GLES20.GetUniformLocation(Program.Handler, "VisibleRect");
                Program.SetUniform(VisibleUniform, VisibleRectUV);
            }

            if (ColorUniform >= 0)
            {
                Program.SetUniform(ColorUniform, Color, Opacity);
            }
            else if (ColorUniform == int.MinValue)
            {
                ColorUniform = GLES20.GetUniformLocation(Program.Handler, "Color");
                Program.SetUniform(ColorUniform, Color, Opacity);
            }

            if (ResolutionUniform >= 0)
            {
                Program.SetUniform(ResolutionUniform, (float)Width, Height);
            }
            else if (ResolutionUniform == int.MinValue)
            {
                ResolutionUniform = GLES20.GetUniformLocation(Program.Handler, "Resolution");
                Program.SetUniform(ResolutionUniform, (float)Width, Height);
            }
        }

        private bool InvisibleRect = false;
        private Rectangle VisibleRectUV = Vector4.Zero;

        public void SetVisibleRectangle(float X, float Y, int Width, int Height) => SetVisibleRectangle(new Rectangle(X, Y, Width, Height));
        public virtual void SetVisibleRectangle(Rectangle Area)
        {
            if (Area.IsEmpty())
            {
                InvisibleRect = true;
                return;
            }

            InvisibleRect = false;

            var UVRect = new Rectangle(Area.X, Area.Y, Area.Width, Area.Height);

            float MinU = GetU(UVRect.X, Width);
            float MaxU = GetU(UVRect.Width, Width);

            float MinV = GetV(UVRect.Y, Height);
            float MaxV = GetV(UVRect.Height, Height);

            VisibleRectUV = new Vector4(MinU, MinV, MaxU, MaxV);

            SetChildrenVisibleRectangle(Area);
        }

        public virtual void ClearVisibleRectangle()
        {
            VisibleRectUV = Vector4.Zero;

            ClearChildrenVisibleRectangle();
        }

        protected void SetChildrenVisibleRectangle(Rectangle Area)
        {
            VisibleRectangle = Area;

            var AbsArea = new Rectangle(Area.X, Area.Y, Area.Width, Area.Height);

            AbsArea.Position += AbsoluteZoomPosition;

            foreach (var Child in Childs)
            {
                Rectangle AbsChildArea;

                if (Child.Zoom != 1)
                    AbsChildArea = new Rectangle(Child.AbsoluteZoomPosition.X, Child.AbsoluteZoomPosition.Y, Child.ZoomWidth, Child.ZoomHeight);
                else
                    AbsChildArea = new Rectangle(Child.AbsolutePosition.X, Child.AbsolutePosition.Y, Child.Width, Child.Height);

                var Bounds = Rectangle.GetChildBounds(AbsArea, AbsChildArea);

                Child.SetVisibleRectangle(Bounds);
            }
        }

        protected void ClearChildrenVisibleRectangle()
        {
            VisibleRectangle = null;
            InvisibleRect = false;

            foreach (var Child in Childs)
            {
                Child.ClearVisibleRectangle();
            }
        }

        public virtual void RefreshVertex()
        {
            foreach (var Child in Childs)
                Child.RefreshVertex();
        }

        /// <summary>
        /// Scale object coordinates, where 1.0 is 100% and 0.5 is 200%;
        /// The method <see cref="ParseZoomFactor(float)"/> can be used to get the multiplier value as well
        /// </summary>
        public virtual void SetZoom(float Value = 1f)
        {
            if (Zoom == Value)
                return;

            SetChildrenZoom(Value);
            RefreshVertex();
        }

        private void SetChildrenZoom(float Value)
        {
            Zoom = Value;

            var VirtualSize = new Vector2(Coordinates2D.Width * Zoom, Coordinates2D.Height * Zoom);
            PixelOffset = MeasurePixelOffset(VirtualSize);

            Position = Position;

            foreach (var Child in Childs)
                Child.SetChildrenZoom(Zoom);
        }

        public override void Draw(long Tick)
        {
            if (!Visible || InvisibleRect)
                return;

            if (Program != null)
            {
                UpdateUniforms();
                base.Draw(Tick);
            }

            foreach (var Child in Children.ToArray())
            {
                Child.Draw(Tick);
            }
        }

        public virtual void AddChild(GLObject2D Child)
        {
            if (Child.Parent != null)
                Child.Parent.RemoveChild(Child);

            Children.Add(Child);
            Child.Parent = this;
            Child.RefreshVertex();
        }

        public virtual void RemoveChild(GLObject2D Child)
        {
            
            Children.Remove(Child);
            Child.Parent = null;

            if (!Child.Disposed)
                Child.RefreshVertex();
        }

        public virtual void RemoveChildren(bool Dispose)
        {
            foreach (var Child in Children)
            {
                Child.Parent = null;

                if (Dispose)
                    Child.Dispose();
                else
                    Child.RefreshVertex();
            }

            Children.Clear();
        }

        public override void Dispose()
        {
            foreach (var Child in Children.ToArray())
            {
                Child.Dispose();
            }
            
            base.Dispose();
            
            if (Parent != null)
                Parent.RemoveChild(this);
        }

        internal Vector2 ProcessZoomFactor(Vector2 Vector, bool ApplyZoom)
        {
            if (ApplyZoom)
            {
                Vector.X = PointToX(XToPoint(Vector.X, Coordinates2D.Width), ZoomMaxWidth);
                Vector.Y = PointToY(YToPoint(Vector.Y, Coordinates2D.Height), ZoomMaxHeight);
            }
            else
            {
                Vector.X = PointToX(XToPoint(Vector.X, ZoomMaxWidth), Coordinates2D.Width);
                Vector.Y = PointToY(YToPoint(Vector.Y, ZoomMaxHeight), Coordinates2D.Height);
            }

            return Vector;
        }
    }
}