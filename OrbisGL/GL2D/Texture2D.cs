using OrbisGL.GL;
using SharpGLES;
using System;
using System.IO;
using System.Numerics;
using static OrbisGL.GL2D.Coordinates2D;

namespace OrbisGL.GL2D
{
    public class Texture2D : GLObject2D
    {
        int TextureUniformLocation;
        int MirrorUniformLocation;

        
        /// <summary>
        /// When true the vertex will update the Texture2D instance size to fit the texture size automatically.
        /// </summary>
        public bool AutoSize { get; set; } = true;

        /// <summary>
        /// When set to true, indicates that the Texture should be treated as a shared texture.
        /// This prevents accidental disposal of the texture by the garbage collector (GC).
        /// If you set this to true, you must handle the texture disposal manually.
        /// </summary>
        public bool SharedTexture { get; set; } = false;


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
        public bool Mirror { get; set; }

        /// <summary>
        /// Get or Set the texture instance, 
        /// if is the first set or the new texture size has changed, you must call <see cref="RefreshVertex"/>
        /// </summary>
        public Texture Texture { get; set; }

        /// <summary>
        /// Creates a texture from raw pixel data
        /// </summary>
        /// <param name="Width">The texture width size in pixels</param>
        /// <param name="Height">The texture height size in pixels</param>
        /// <param name="PixelData">The pixel data</param>
        /// <param name="Format">The pixel data format</param>
        /// <param name="EnableFiltering">Enables/Disable texture magnification filtering</param>
        public Texture2D(int Width, int Height, byte[] PixelData, PixelFormat Format, bool EnableFiltering) : this(new Texture(true))
        {
            Texture.SetData(Width, Height, PixelData, Format, EnableFiltering);
        }

        /// <summary>
        /// Creates a texture from DDS file stream
        /// </summary>
        /// <param name="DDS">The DDS file stream</param>
        /// <param name="EnableFiltering">Enables/Disable texture magnification filtering</param>
        public Texture2D(Stream DDS, bool EnableFiltering) : this(new Texture(true))
        {
            Texture.SetDDS(DDS, EnableFiltering);
        }

        /// <summary>
        /// Creates an texture render for the given texture
        /// </summary>
        /// <param name="Texture">The 2D Texture Instance</param>
        public Texture2D(Texture Texture) : this()
        {
            if (!Texture.Is2D)
                throw new Exception("The given texture isn't an GL2D texture");


            this.Texture = Texture;
        }

        /// <summary>
        /// Creates an 2D texture render object
        /// </summary>
        public Texture2D()
        {
            var hProgram = Shader.GetProgram(ResLoader.GetResource("VertexOffsetTexture"), ResLoader.GetResource("FragmentTexture"));
            Program = new GLProgram(hProgram);

            TextureUniformLocation = GLES20.GetUniformLocation(hProgram, "Texture");
            MirrorUniformLocation = GLES20.GetUniformLocation(hProgram, "Mirror");

            Program.AddBufferAttribute("Position", AttributeType.Float, AttributeSize.Vector3);
            Program.AddBufferAttribute("uv", AttributeType.Float, AttributeSize.Vector2);

            BlendMode = BlendMode.ALPHA;

            RefreshVertex();
        }

        public override void RefreshVertex()
        {
            //   0 ---------- 1
            //   |            |
            //   |            |
            //   |            |
            //   2 ---------- 3

            var MaxSize = new Vector2(ZoomMaxWidth, ZoomMaxHeight);

            if (Texture != null && AutoSize)
            {
                Width = Texture.Width;
                Height = Texture.Height;
            }

            var PointA = new Vector2(0, 0);
            var PointB = new Vector2(Width, 0);
            var PointC = new Vector2(0, Height);
            var PointD = new Vector2(Width, Height);

            var Center = PointD / 2f;

            PointA = RotatePoint(PointA, Center, Rotate);
            PointB = RotatePoint(PointB, Center, Rotate);
            PointC = RotatePoint(PointC, Center, Rotate);
            PointD = RotatePoint(PointD, Center, Rotate);

            ClearBuffers();

            AddArray(PointA.ToPoint(MaxSize), -1);//0
            AddArray(0, 0);

            AddArray(PointB.ToPoint(MaxSize), -1);//1
            AddArray(1, 0);

            AddArray(PointC.ToPoint(MaxSize), -1);//2
            AddArray(0, 1);

            AddArray(PointD.ToPoint(MaxSize), -1);//3
            AddArray(1, 1);

            AddIndex(0, 1, 2, 1, 2, 3);

            base.RefreshVertex();
        }
        public override void Draw(long Tick)
        {
            if (Texture != null)
            {
                Program.SetUniform(TextureUniformLocation, Texture.Active());
                Program.SetUniform(MirrorUniformLocation, Mirror ? 1 : 0);
            }
            base.Draw(Tick);
        }

        public override void Dispose()
        {
            if (!SharedTexture)
                Texture?.Dispose();

            base.Dispose();
        }
    }
}
