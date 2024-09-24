using OrbisGL.GL;
using SharpGLES;
using System;
using System.Numerics;
using static OrbisGL.GL2D.Coordinates2D;

namespace OrbisGL.GL2D
{
    public class PartialElipse2D : GLObject2D
    {
        readonly int CircleConfigUniformLocation;

        /// <summary>
        /// From 0.00 to 1.00, when 1 a pizza like geometry will be rendered
        /// </summary>
        public float Thickness { get; set; } = 0.1f;

        /// <summary>
        /// From -3.14 to 3.14, the min/max value represents the center left angle of the circle.
        /// </summary>
        public float StartAngle { get; set; } = -(float)Math.PI;

        /// <summary>
        /// From 3.14 to -3.14, the min/max value represents the center left angle of the circle.
        /// </summary>
        public float EndAngle { get; set; } = (float)Math.PI;


        /// <summary>
        /// Whe true the <see cref="Thickness"/> is ignored, same effect of set <see cref="Thickness"/> to 1.0
        /// </summary>
        public bool Fill { get; private set; }


        float _BorderDistance = 0f;

        /// <summary>
        /// A value from 0 to 1 to modify the circle distance from the object border
        /// </summary>
        public float BorderDistance 
        {
            get => _BorderDistance; 
            set { 
                _BorderDistance = Math.Max(Math.Min(value, 1), 0);
                RefreshVertex();
            } 
        }


        float _Rotate = 0f;

        /// <summary>
        /// Number in Degrees to rotate the object
        /// <para>if possible use <see cref="StartAngle"/> and <see cref="EndAngle"/> for better performance</para>
        /// </summary>
        public float Rotate
        {
            get => _Rotate;
            set
            {
                _Rotate = value;
                RefreshVertex();
            }
        }

        public PartialElipse2D(int Width, int Height, bool Fill)
        {
            this.Fill = Fill;
            this.Width = Width;
            this.Height = Height;

            var hProg = new ProgramHandler(ResLoader.GetResource("VertexOffsetTexture"), ResLoader.GetResource("FragmentColorElipsePartial"));
            Program = new GLProgram(hProg);

            Program.AddBufferAttribute("Position", AttributeType.Float, AttributeSize.Vector3);
            Program.AddBufferAttribute("uv", AttributeType.Float, AttributeSize.Vector2);

            RenderMode = (int)OrbisGL.RenderMode.Triangle;

            CircleConfigUniformLocation = GLES20.GetUniformLocation(Program.Handler, "CircleConfig");//vec3(startAngle, endAngle, Thickness)

            RefreshVertex();
        }

        public override void RefreshVertex()
        {
            ClearBuffers();

            //   0 ---------- 1
            //   |            |
            //   |            |
            //   |            |
            //   2 ---------- 3

            var PointA = new Vector2(0, 0);
            var PointB = new Vector2(Width, 0);
            var PointC = new Vector2(0, Height);
            var PointD = new Vector2(Width, Height);

            var Center = PointD / 2f;

            PointA = RotatePoint(PointA, Center, Rotate);
            PointB = RotatePoint(PointB, Center, Rotate);
            PointC = RotatePoint(PointC, Center, Rotate);
            PointD = RotatePoint(PointD, Center, Rotate);

            var MinUV = 0 - BorderDistance;
            var MaxUV = 1 + BorderDistance;

            AddArray(XToPoint(PointA.X, ZoomMaxWidth), YToPoint(PointA.Y, ZoomMaxHeight), -1);//0
            AddArray(MinUV, MinUV);

            AddArray(XToPoint(PointB.X, ZoomMaxWidth), YToPoint(PointB.Y, ZoomMaxHeight), -1);//1
            AddArray(MaxUV, MinUV);

            AddArray(XToPoint(PointC.X, ZoomMaxWidth), YToPoint(PointC.Y, ZoomMaxHeight), -1);//2
            AddArray(MinUV, MaxUV);

            AddArray(XToPoint(PointD.X, ZoomMaxWidth), YToPoint(PointD.Y, ZoomMaxHeight), -1);//3
            AddArray(MaxUV, MaxUV);

            AddIndex(0, 1, 2, 1, 2, 3);

            base.RefreshVertex();
        }

        public override void Draw(long Tick)
        {
            if (Fill)
                Program.SetUniform(CircleConfigUniformLocation, StartAngle, EndAngle, 1.0f);
            else
                Program.SetUniform(CircleConfigUniformLocation, StartAngle, EndAngle, Thickness);

            base.Draw(Tick);
        }
    }
}
