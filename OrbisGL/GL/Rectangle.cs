﻿using System;
using System.Diagnostics;
using System.Numerics;

namespace OrbisGL.GL
{
    [DebuggerDisplay("X: {X}; Y: {Y}; W: {Width}; H: {Height};")]
    public struct Rectangle
    {
        public override bool Equals(object obj)
        {
            if (obj is Rectangle Rect)
            {
                return Position == Rect.Position && Size == Rect.Size;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
            }
        }

        public static bool operator ==(Rectangle A, Rectangle B)
        {
            return A.Equals(B);
        }

        public static bool operator !=(Rectangle A, Rectangle B)
        {
            return !A.Equals(B);
        }

        public static Rectangle Empty => new Rectangle(0, 0, 0, 0);

        public Vector4 Vector;

        public Rectangle(float X, float Y, float Width, float Height)
        {
            Vector = new Vector4(X, Y, Width, Height);
        }

        public static implicit operator Vector4(Rectangle Rectangle)
        {
            return Rectangle.Vector;
        }

        public static implicit operator Rectangle(Vector4 Vector)
        {
            var Rect = new Rectangle();
            Rect.Vector = Vector;
            return Rect;
        }

        /// <summary>
        /// Determine if the given coordinates vector is inside the rectangle
        /// </summary>
        /// <param name="XY">The XY Coordinates Vector</param>
        public bool IsInBounds(Vector2 XY)
        {
            return XY.X >= Left && XY.Y >= Top && XY.X <= Right && XY.Y <= Bottom;
        }

        /// <summary>
        /// Determine if the given coordinates vector is inside the rectangle
        /// </summary>
        /// <param name="X">The X Coordinate</param>
        /// <param name="Y">The Y Coordinate</param>
        public bool IsInBounds(int X, int Y)
        {
            return X >= Left && Y >= Top && X <= Right && Y <= Bottom;
        }

        public Rectangle Intersect(Rectangle B)
        {
            var A = this;
            return Intersect(A, B);
        }

        public static Rectangle Intersect(Rectangle A, Rectangle B)
        {
            float X = Math.Max(A.X, B.X);
            float Left = Math.Min(A.X + A.Width, B.X + B.Width);
            float Y = Math.Max(A.Y, B.Y);
            float Bottom = Math.Min(A.Y + A.Height, B.Y + B.Height);
            if (Left >= X && Bottom >= Y)
                return new Rectangle(X, Y, Left - X, Bottom - Y);
            else
                return Empty;
        }

        /// <summary>
        /// Get an rectangle relative to the <paramref name="InnerRect"/> with bounds limited by <paramref name="OutterRect"/>
        /// </summary>
        /// <param name="OutterRect">An Absolute Rectangle representing the bounds to be applied</param>
        /// <param name="InnerRect">An Absolute Rectangle representing the inner rectangle to be limited</param>
        public static Rectangle GetChildBounds(Rectangle OutterRect, Rectangle InnerRect)
        {
            // Calcula a interseção entre os dois retângulos
            var Intersection = Rectangle.Intersect(OutterRect, InnerRect);

            // Se não houver interseção, retorna um retângulo vazio
            if (Intersection.IsEmpty())
                return Rectangle.Empty;

            // Ajusta a posição para que seja relativa ao `InnerRect`
            Intersection.X -= InnerRect.X;
            Intersection.Y -= InnerRect.Y;

            return Intersection;
        }

        internal bool IsEmpty()
        {
            return Width * Height <= 0;
        }

        public float X { get => Vector.X; set => Vector.X = value; }
        public float Y { get => Vector.Y; set => Vector.Y = value; }
        public float Width { get => Vector.Z; set => Vector.Z = value; }
        public float Height { get => Vector.W; set => Vector.W = value; }


        public float Top
        {
            get => Vector.Y;
            set
            {
                float DeltaY = value - Vector.Y;
                Vector.Y = value;
                Vector.W -= DeltaY;
            }
        }

        public float Left
        {
            get => Vector.X;
            set
            {
                float DeltaX = value - Vector.X;
                Vector.X = value;
                Vector.Z -= DeltaX;
            }
        }

        public float Right { get => Vector.X + Vector.Z; set => Vector.Z = value - Vector.X; }
        public float Bottom { get => Vector.Y + Vector.W; set => Vector.W = value - Vector.Y; }

        public Vector2 Position { get => new Vector2(X, Y); set { X = value.X; Y = value.Y; } }
        public Vector2 Size { get => new Vector2(Width, Height); set { Width = value.X; Height = value.Y; } }

        public Vector2 Center
        {
            get
            {
                return Position + (Size / 2);
            }
        }
    }
}