using System;
using OrbisGL.GL;
using SharpGLES;
using System.Numerics;
using static OrbisGL.GL2D.Coordinates2D;

namespace OrbisGL.GL2D
{
    /// <summary>
    /// Allow usage of textures with up 8192x8192 using tiles
    /// </summary>
    public class TiledTexture2D : GLObject2D
    {
        int Texture00UniformLocation;
        int Texture10UniformLocation;
        int Texture01UniformLocation;
        int Texture11UniformLocation;
        int MirrorUniformLocation;
        int TileSizeUniformLocation;

        float _Rotate = 0f;
        public float Rotate
        {
            get => _Rotate;
            set
            {
                _Rotate = value;
                RefreshVertex();
            }
        }

        Vector2 TileSize = new Vector2(1, 1);
        
        public bool Mirror { get; set; }

        Texture TextureTile00 { get; set; }
        Texture TextureTile10 { get; set; }
        Texture TextureTile01 { get; set; }
        Texture TextureTile11 { get; set; }


        /// <summary>
        /// Set the texture tiles
        /// </summary>
        /// <param name="X0Y0">The Top Left Tile</param>
        /// <param name="X1Y0">The Top Right Tile</param>
        /// <param name="X0Y1">The Bottom Left</param>
        /// <param name="X1Y1">The Bottom Right</param>
        /// <exception cref="Exception"></exception>
        public void SetTexture(Texture X0Y0, Texture X1Y0, Texture X0Y1, Texture X1Y1)
        {
            if ((X0Y0 == null || X0Y1 == null) && X1Y1 != null)
                throw new Exception("The texture X1Y1 can be set only if X0Y1 and X0Y1 is set too");

            TextureTile00 = X0Y0;
            TextureTile01 = X0Y1;
            TextureTile10 = X1Y0;
            TextureTile11 = X1Y1;

            if (X0Y0 == null)
                return;

            Width = X0Y0.Width + (X1Y0?.Width ?? 0);
            Height = X0Y0.Height + (X0Y1?.Height ?? 0);
            
            RefreshVertex();
        }

        public Texture[] GetTextures()
        {
            return new Texture[] { TextureTile00, TextureTile10, TextureTile01, TextureTile11 };
        } 

        public TiledTexture2D()
        {
            var hProgram = Shader.GetProgram(ResLoader.GetResource("VertexOffsetTextureTiled"), ResLoader.GetResource("FragmentTextureTiled"));
            Program = new GLProgram(hProgram);

            Texture00UniformLocation = GLES20.GetUniformLocation(hProgram, "Texture00");
            Texture01UniformLocation = GLES20.GetUniformLocation(hProgram, "Texture01");
            Texture10UniformLocation = GLES20.GetUniformLocation(hProgram, "Texture10");
            Texture11UniformLocation = GLES20.GetUniformLocation(hProgram, "Texture11");
            TileSizeUniformLocation = GLES20.GetUniformLocation(hProgram, "TileSize");

            MirrorUniformLocation = GLES20.GetUniformLocation(hProgram, "Mirror");

            Program.AddBufferAttribute("Position", AttributeType.Float, AttributeSize.Vector3);
            Program.AddBufferAttribute("uv", AttributeType.Float, AttributeSize.Vector2);
            Program.AddBufferAttribute("uv00", AttributeType.Float, AttributeSize.Vector2);
            Program.AddBufferAttribute("uv01", AttributeType.Float, AttributeSize.Vector2);
            Program.AddBufferAttribute("uv10", AttributeType.Float, AttributeSize.Vector2);
            Program.AddBufferAttribute("uv11", AttributeType.Float, AttributeSize.Vector2);

            BlendMode = BlendMode.ALPHA;
        }

        public override void RefreshVertex()
        {

            //TODO: Clone 6,8,4 points but with Half Local UV

            if (TextureTile00 == null)
                return;

            var MaxSize = new Vector2(Coordinates2D.Width * Zoom, Coordinates2D.Height * Zoom);

            bool DoubleWidth = TextureTile10 != null;
            bool DoubleHeight = TextureTile01 != null;

            if (DoubleWidth && DoubleHeight)
                Create2x2(MaxSize);
            else if (DoubleWidth)
                Create2x1(MaxSize);
            else if (DoubleHeight)
                Create1x2(MaxSize);
            else
                Create1x1(MaxSize);

            base.RefreshVertex();
        }

        private void Create2x2(Vector2 MaxSize)
        {
            TileSize = new Vector2(2, 2);

            //   0 ---------- 1 ---------- 4
            //   |            |            |
            //   |            |            |
            //   |            |            |
            //   2 ---------- 3 ---------- 5
            //   |            |            |
            //   |            |            |
            //   |            |            |
            //   6 ---------- 7 ---------- 8


            //Global UVs
            // 0,0         0.5,0          1,0
            // 0,0.5f      0.5,0.5        1,0.5
            // 0,1         0.5,1          1,1


            //Local UVs
            // 0,0      1,0 | 0,0         1,0
            // 0,1      1,1 | 0,1         1,1
            //-------------------------------
            // 0,0      1,0 | 0,0         1,0
            // 0,1      1,1 | 0,1         1,1

            //UVs
            // 00 | 10
            // 01 | 11

            //Points
            // 0 1 4
            // 2 3 5
            // 6 7 8

            var Point0 = new Vector2(0, 0);
            var Point1 = new Vector2(TextureTile00.Width, 0);
            var Point2 = new Vector2(0, TextureTile00.Height);
            var Point3 = new Vector2(TextureTile00.Width, TextureTile00.Height);

            var Point4 = new Vector2(Width, 0);
            var Point5 = new Vector2(Width, TextureTile00.Height);
            var Point6 = new Vector2(0, Height);
            var Point7 = new Vector2(TextureTile00.Width, Height);
            var Point8 = new Vector2(Width, Height);

            var Center = Point8 / 2f;

            Point0 = RotatePoint(Point0, Center, Rotate);
            Point1 = RotatePoint(Point1, Center, Rotate);
            Point2 = RotatePoint(Point2, Center, Rotate);
            Point3 = RotatePoint(Point3, Center, Rotate);
            Point4 = RotatePoint(Point4, Center, Rotate);
            Point5 = RotatePoint(Point5, Center, Rotate);
            Point6 = RotatePoint(Point6, Center, Rotate);
            Point7 = RotatePoint(Point7, Center, Rotate);

            ClearBuffers();

            AddArray(Point0.ToPoint(MaxSize), -1);//0
            AddArray(0, 0);//UV
            AddArray(0, 0);//UV00
            AddArray(0, 0);//UV01
            AddArray(1, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point1.ToPoint(MaxSize), -1);//1
            AddArray(0.5f, 0);//UV
            AddArray(1, 0);//UV00
            AddArray(0, 0);//UV01
            AddArray(0, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point2.ToPoint(MaxSize), -1);//2
            AddArray(0, 0.5f);
            AddArray(0, 1);//UV00
            AddArray(0, 0);//UV01
            AddArray(0, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point3.ToPoint(MaxSize), -1);//3
            AddArray(0.5f, 0.5f);
            AddArray(1, 1);//UV00
            AddArray(1, 0);//UV01
            AddArray(0, 1);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point4.ToPoint(MaxSize), -1);
            AddArray(1, 0);//UV
            AddArray(1, 0);//UV00
            AddArray(1, 0);//UV01
            AddArray(1, 0);//UV10
            AddArray(1, 0);//UV11


            AddArray(Point5.ToPoint(MaxSize), -1);
            AddArray(1, 0.5f);//UV
            AddArray(1, 1);//UV00
            AddArray(1, 1);//UV01
            AddArray(1, 1);//UV10
            AddArray(1, 0);//UV11


            AddArray(Point6.ToPoint(MaxSize), -1);
            AddArray(0, 1);//UV
            AddArray(0, 1);//UV00
            AddArray(0, 1);//UV01
            AddArray(0, 1);//UV10
            AddArray(0, 1);//UV11


            AddArray(Point7.ToPoint(MaxSize), -1);
            AddArray(0.5f, 1);//UV
            AddArray(1, 1);//UV00
            AddArray(1, 1);//UV01
            AddArray(0, 1);//UV10
            AddArray(0, 1);//UV11

            AddArray(Point8.ToPoint(MaxSize), -1);
            AddArray(1, 1);//UV
            AddArray(1, 1);//UV00
            AddArray(1, 1);//UV01
            AddArray(1, 1);//UV10
            AddArray(1, 1);//UV11

            AddIndex(0, 1, 2, 1, 2, 3);

            AddIndex(1, 3, 4, 3, 4, 5);

            AddIndex(2, 6, 3, 6, 7, 3);

            AddIndex(3, 7, 5, 7, 5, 8);
        }

        private void Create2x1(Vector2 MaxSize)
        {
            TileSize = new Vector2(2, 1);

            //   0 ---------- 1 ---------- 4
            //   |            |            |
            //   |            |            |
            //   |            |            |
            //   2 ---------- 3 ---------- 5

            //Global UVs
            // 0,0         0.5,0          1,0
            // 0,1         0.5,1          1,1


            //Local UVs
            // 0,0      1,0 | 0,0         1,0
            // 0,1      1,1 | 0,1         1,1

            //UVs
            // 00 | 10

            //Points
            // 0 1 4
            // 2 3 5

            var Point0 = new Vector2(0, 0);
            var Point1 = new Vector2(TextureTile00.Width, 0);
            var Point2 = new Vector2(0, TextureTile00.Height);
            var Point3 = new Vector2(TextureTile00.Width, TextureTile00.Height);

            var Point4 = new Vector2(Width, 0);
            var Point5 = new Vector2(Width, TextureTile00.Height);

            var Center = Point5 / 2f;

            Point0 = RotatePoint(Point0, Center, Rotate);
            Point1 = RotatePoint(Point1, Center, Rotate);
            Point2 = RotatePoint(Point2, Center, Rotate);
            Point3 = RotatePoint(Point3, Center, Rotate);
            Point4 = RotatePoint(Point4, Center, Rotate);
            Point5 = RotatePoint(Point5, Center, Rotate);

            ClearBuffers();

            AddArray(Point0.ToPoint(MaxSize), -1);//0
            AddArray(0, 0);//UV
            AddArray(0, 0);//UV00
            AddArray(0, 0);//UV01
            AddArray(1, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point1.ToPoint(MaxSize), -1);//1
            AddArray(0.5f, 0);//UV
            AddArray(1, 0);//UV00
            AddArray(0, 0);//UV01
            AddArray(0, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point2.ToPoint(MaxSize), -1);//2
            AddArray(0, 1);//UV
            AddArray(0, 1);//UV00
            AddArray(0, 0);//UV01
            AddArray(0, 1);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point3.ToPoint(MaxSize), -1);//3
            AddArray(0.5f, 1);//UV
            AddArray(1, 1);//UV00
            AddArray(1, 0);//UV01
            AddArray(0, 1);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point4.ToPoint(MaxSize), -1);
            AddArray(1, 0);//UV
            AddArray(1, 0);//UV00
            AddArray(1, 0);//UV01
            AddArray(1, 0);//UV10
            AddArray(1, 0);//UV11


            AddArray(Point5.ToPoint(MaxSize), -1);
            AddArray(1, 1);//UV
            AddArray(1, 1);//UV00
            AddArray(1, 1);//UV01
            AddArray(1, 1);//UV10
            AddArray(1, 0);//UV11

            AddIndex(0, 1, 2, 1, 2, 3);

            AddIndex(1, 3, 4, 3, 4, 5);
        }

        private void Create1x2(Vector2 MaxSize)
        {
            TileSize = new Vector2(1, 2);

            //   0 ---------- 1
            //   |            |
            //   |            |
            //   |            |
            //   2 ---------- 3
            //   |            |
            //   |            |
            //   |            |
            //   6 ---------- 7 

            //Global UVs
            // 0,0         1,0
            // 0,0.5f      1,0.5
            // 0,1         1,1


            //Local UVs
            // 0,0      1,0 |
            // 0,1      1,1 |
            // -------------|
            // 0,0      1,0 |
            // 0,1      1,1 |

            //UVs
            // 00 |
            // 01 |

            //Points
            // 0 1
            // 2 3
            // 4 5

            var Point0 = new Vector2(0, 0);
            var Point1 = new Vector2(TextureTile00.Width, 0);
            var Point2 = new Vector2(0, TextureTile00.Height);
            var Point3 = new Vector2(TextureTile00.Width, TextureTile00.Height);

            var Point6 = new Vector2(0, Height);
            var Point7 = new Vector2(TextureTile00.Width, Height);

            var Center = Point7 / 2f;

            Point0 = RotatePoint(Point0, Center, Rotate);
            Point1 = RotatePoint(Point1, Center, Rotate);
            Point2 = RotatePoint(Point2, Center, Rotate);
            Point3 = RotatePoint(Point3, Center, Rotate);
            Point6 = RotatePoint(Point6, Center, Rotate);
            Point7 = RotatePoint(Point7, Center, Rotate);

            ClearBuffers();

            AddArray(Point0.ToPoint(MaxSize), -1);//0
            AddArray(0, 0);//UV
            AddArray(0, 0);//UV00
            AddArray(0, 0);//UV01
            AddArray(1, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point1.ToPoint(MaxSize), -1);//1
            AddArray(1, 0);//UV
            AddArray(1, 0);//UV00
            AddArray(0, 0);//UV01
            AddArray(0, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point2.ToPoint(MaxSize), -1);//2
            AddArray(0, 0.5f);//UV
            AddArray(0, 1);//UV00
            AddArray(0, 0);//UV01
            AddArray(0, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point3.ToPoint(MaxSize), -1);//3
            AddArray(1, 0.5f);//UV
            AddArray(1, 1);//UV00
            AddArray(1, 0);//UV01
            AddArray(0, 1);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point6.ToPoint(MaxSize), -1);//4
            AddArray(0, 1);//UV
            AddArray(0, 1);//UV00
            AddArray(0, 1);//UV01
            AddArray(0, 1);//UV10
            AddArray(0, 1);//UV11


            AddArray(Point7.ToPoint(MaxSize), -1);//5
            AddArray(1, 1);//UV
            AddArray(1, 1);//UV00
            AddArray(1, 1);//UV01
            AddArray(0, 1);//UV10
            AddArray(0, 1);//UV11

            AddIndex(0, 1, 2, 1, 2, 3);

            AddIndex(2, 3, 4, 4, 5, 3);
        }

        private void Create1x1(Vector2 MaxSize)
        {
            TileSize = new Vector2(1, 1);

            //   0 ---------- 1 
            //   |            |
            //   |            | 
            //   |            |
            //   2 ---------- 3


            //Global UVs
            // 0,0         1,0
            // 0,1         1,1


            //Local UVs
            // 0,0      1,0 |
            // 0,1      1,1 |
            //---------------

            //UVs
            // 00

            //Points
            // 0 1
            // 2 3

            var Point0 = new Vector2(0, 0);
            var Point1 = new Vector2(TextureTile00.Width, 0);
            var Point2 = new Vector2(0, TextureTile00.Height);
            var Point3 = new Vector2(TextureTile00.Width, TextureTile00.Height);

            var Center = Point3 / 2f;

            Point0 = RotatePoint(Point0, Center, Rotate);
            Point1 = RotatePoint(Point1, Center, Rotate);
            Point2 = RotatePoint(Point2, Center, Rotate);
            Point3 = RotatePoint(Point3, Center, Rotate);

            ClearBuffers();

            AddArray(Point0.ToPoint(MaxSize), -1);//0
            AddArray(0, 0);//UV
            AddArray(0, 0);//UV00
            AddArray(0, 0);//UV01
            AddArray(1, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point1.ToPoint(MaxSize), -1);//1
            AddArray(1, 0);//UV
            AddArray(1, 0);//UV00
            AddArray(0, 0);//UV01
            AddArray(0, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point2.ToPoint(MaxSize), -1);//2
            AddArray(0, 1);
            AddArray(0, 1);//UV00
            AddArray(0, 0);//UV01
            AddArray(0, 0);//UV10
            AddArray(0, 0);//UV11

            AddArray(Point3.ToPoint(MaxSize), -1);//3
            AddArray(1, 1);
            AddArray(1, 1);//UV00
            AddArray(1, 0);//UV01
            AddArray(0, 1);//UV10
            AddArray(0, 0);//UV11

            AddIndex(0, 1, 2, 1, 2, 3);
        }

        public override void Draw(long Tick)
        {
            Program.SetUniform(MirrorUniformLocation, Mirror ? 1 : 0);

            if (TextureTile00 != null)
                Program.SetUniform(Texture00UniformLocation, TextureTile00.Active());
            if (TextureTile01 != null)
                Program.SetUniform(Texture01UniformLocation, TextureTile01.Active());
            if (TextureTile10 != null)
                Program.SetUniform(Texture10UniformLocation, TextureTile10.Active());
            if (TextureTile11 != null)
                Program.SetUniform(Texture11UniformLocation, TextureTile11.Active());

            Program.SetUniform(TileSizeUniformLocation, TileSize);

            base.Draw(Tick);
        }

        public override void Dispose()
        {
            TextureTile00?.Dispose();
            TextureTile01?.Dispose();
            TextureTile10?.Dispose();
            TextureTile11?.Dispose();

            base.Dispose();
        }
    }
}
