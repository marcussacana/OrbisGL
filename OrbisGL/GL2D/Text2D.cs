﻿using System;
using System.Numerics;
using System.IO;
using System.Collections.Generic;
using SharpGLES;
using OrbisGL.GL;
using OrbisGL.FreeTypeLib;
using static OrbisGL.GL2D.Coordinates2D;

namespace OrbisGL.GL2D
{
    /// <summary>
    /// A 2D Text Object with rendering based on freetype font
    /// </summary>
    public unsafe class Text2D : GLObject2D
    {
        Texture FontTexture;

        bool VertexInitialized = false;

        public GlyphInfo[] GlyphsInfo { get; private set; } = null;

        /// <summary>
        /// Gets the current Text, to change it use the <see cref="SetText(string)"/>
        /// </summary>
        public string Text { get; private set; } = null;

        public FontFaceHandler Font { get; set; }

        int BackColorUniformLocation = -1;
        int TextureUniformLocation = -1;

        public RGBColor BackgroundColor { get; set; } = null;

        public int FontSize { get; set; }

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

        /// <summary>
        /// Creates a text object that directly draws with FreeType.
        /// <para>If all the glyphs to be rendered are known, use <see cref="AtlasText2D"/> instead.</para>
        /// </summary>
        /// <param name="FontPath">The TrueType font filename to be used</param>
        /// <param name="FontSize">The font Size to be used</param>

        public Text2D(int FontSize, string FontPath) : this(GetFont(FontPath, FontSize, out _)) { }

        /// <summary>
        /// Creates a text object that directly draws with FreeType.
        /// <para>If all the glyphs to be rendered are known, use <see cref="AtlasText2D"/> instead.</para>
        /// </summary>
        /// <param name="Font">The TrueType font to be used</param>
        /// <param name="FontSize">The font Size to be used</param>
        public Text2D(FontFaceHandler Font, int FontSize) : this(Font)
        {
            Font.SetFontSize(FontSize);
            this.FontSize = FontSize;
        }

        /// <summary>
        /// Creates a text object that directly draws with FreeType.
        /// <para>If all the glyphs to be rendered are known, use <see cref="AtlasText2D"/> instead.</para>
        /// </summary>
        /// <param name="Font">The TrueType font to be used</param>
        public Text2D(FontFaceHandler Font)
        {
            this.FontSize = Font.CurrentSize;

            var hProgram = new ProgramHandler(ResLoader.GetResource("VertexOffsetTexture"), ResLoader.GetResource("FragmentFont"));
            Program = new GLProgram(hProgram);

            TextureUniformLocation = GLES20.GetUniformLocation(hProgram, "Texture");
            BackColorUniformLocation = GLES20.GetUniformLocation(hProgram, "BackColor");

            Program.AddBufferAttribute("Position", AttributeType.Float, AttributeSize.Vector3);
            Program.AddBufferAttribute("uv", AttributeType.Float, AttributeSize.Vector2);

            BlendMode = BlendMode.ALPHA;

            FontTexture = new Texture(true);
            this.Font = Font;
        }

        static Dictionary<string, FontFaceHandler> FontCache = new Dictionary<string, FontFaceHandler>();

        internal static FontFaceHandler GetFont(string FontPath, int FontSize, out bool Success)
        {
            if (FontPath == null)
                FontPath = FreeType.DefaultFace;

            string FontKey = $"{FontPath}";

            if (FontCache.ContainsKey(FontKey))
            {
                Success = true;
                var CurrentFont = FontCache[FontKey];

                if (!CurrentFont.Disposed)
                {
                    CurrentFont.SetFontSize(FontSize);
                    return CurrentFont;
                }
            }

            if (!FreeType.LoadFont(FontPath, FontSize, out FontFaceHandler Font))
            {
                Success = false;
                foreach (var CurrentFont in FontCache.Values)
                {
                    if (CurrentFont.Disposed)
                        continue;

                    return CurrentFont;
                }

                throw new Exception($"Failed to Load the Font: {Path.GetFileName(FontPath)}");
            }


            FontCache[FontKey] = Font;

            Success = true;
            return Font;
            
        }

        public static void ClearFontCache()
        {
            foreach (var Font in FontCache.Values)
            {
                Font.Dispose();
            }

            FontCache.Clear();
        }

        public override void RefreshVertex()
        {
            if (Text == null)
                return;

            SetText(Text);
            InternalRefreshVertex();
            base.RefreshVertex();
        }

        public void SetFontSize(int FontSize)
        {
            Font.SetFontSize(FontSize);
            RefreshVertex();
        }

        public void SetText(string Text)
        {
            if (Text == this.Text)
                return;

            this.Text = Text;

            if (Text == null)
            {
                FontTexture.SetData(1, 1, new byte[4], PixelFormat.RGBA, true);
                GlyphsInfo = null;
                return;
            }

            Font.SetFontSize(FontSize);

            FreeType.MeasureText(Text, Font, out int Width, out int Height, out GlyphInfo[] Glyphs);

            bool Resized = Width != this.Width || Height != this.Height;

            this.Width = Width;
            this.Height = Height;
            GlyphsInfo = Glyphs;


            byte[] Buffer = new byte[Width * Height * 4];

            FreeType.RenderText(Buffer, Width, Height, Text, Font, RGBColor.White);

            FontTexture.SetData(Width, Height, Buffer, PixelFormat.RGBA, true);

            if (!VertexInitialized || Resized)
                InternalRefreshVertex();

            if (Resized)
                ClearVisibleRectangle();
        }

        private void InternalRefreshVertex()
        {

            VertexInitialized = true;

            //   0 ---------- 1
            //   |            |
            //   |            |
            //   |            |
            //   2 ---------- 3

            var MaxSize = new Vector2(ZoomMaxWidth, ZoomMaxHeight);

            ClearBuffers();

            var PointA = new Vector2(0, 0);
            var PointB = new Vector2(Width, 0);
            var PointC = new Vector2(0, Height);
            var PointD = new Vector2(Width, Height);

            var Center = PointD / 2f;

            PointA = RotatePoint(PointA, Center, Rotate);
            PointB = RotatePoint(PointB, Center, Rotate);
            PointC = RotatePoint(PointC, Center, Rotate);
            PointD = RotatePoint(PointD, Center, Rotate);

            AddArray(PointA.ToPoint(MaxSize), -1);//0
            AddArray(0, 0);

            AddArray(PointB.ToPoint(MaxSize), -1);//1
            AddArray(1, 0);

            AddArray(PointC.ToPoint(MaxSize), -1);//2
            AddArray(0, 1);

            AddArray(PointD.ToPoint(MaxSize), -1);//3
            AddArray(1, 1);

            AddIndex(0, 1, 2, 1, 2, 3);
        }

        public override void Draw(long Tick)
        {
            if (Text != null)
            {
                Program.SetUniform(TextureUniformLocation, FontTexture.Active());

                if (BackgroundColor != null)
                    Program.SetUniform(BackColorUniformLocation, BackgroundColor, Opacity);
                else
                    Program.SetUniform(BackColorUniformLocation, Vector4.Zero);
            } 
            else
            {
                return;
            } 

            base.Draw(Tick);
        }

        public override void Dispose()
        {
            FontTexture?.Dispose();
            base.Dispose();
        }
    }
}
