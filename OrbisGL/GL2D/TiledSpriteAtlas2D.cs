using OrbisGL.GL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml;

namespace OrbisGL.GL2D
{

    /// <summary>
    /// A class that parses Adobe Animate sprite sheet and reproduce it.
    /// </summary>
    public class TiledSpriteAtlas2D : SpriteAtlas2D
    {
        /// <summary>
        /// Get or Set the loaded sprite sheet texture instance
        /// </summary>
        public Texture[] Textures
        {
            get => ((TiledTexture2D)SpriteView.Target).GetTextures();
            set
            {
                if (value == null)
                    ((TiledTexture2D)SpriteView.Target).SetTexture(null, null, null, null);
                else
                    ((TiledTexture2D)SpriteView.Target).SetTexture(value[0], value[1], value[2], value[3]);
            }
        }
        public override RGBColor Color { get => SpriteView.Color; set => SpriteView.Color = value; }
        public override byte Opacity { get => SpriteView.Opacity; set => SpriteView.Opacity = value; }

        public override bool Mirror { get => ((TiledTexture2D)SpriteView.Target).Mirror; set => ((TiledTexture2D)SpriteView.Target).Mirror = value; }
        public override bool Negative { get => ((TiledTexture2D)SpriteView.Target).Negative; set => ((TiledTexture2D)SpriteView.Target).Negative = value; }

        public TiledSpriteAtlas2D() : base(new Sprite2D(new TiledTexture2D())) { }

        /// <summary>
        /// Creates and load a SpriteAtlas2D Instance
        /// </summary>
        /// <param name="Document">An Adobe Animate texture atlas info</param>
        /// <param name="SpriteSheet">An texture compatible with the given texture atlas info</param>
        public TiledSpriteAtlas2D(XmlDocument Document, Texture[] SpriteSheetTiles) : this()
        {
            LoadSprite(Document, SpriteSheetTiles);
        }

        /// <summary>
        /// Creates and load a SpriteAtlas2D Instance
        /// </summary>
        /// <param name="Document">An Adobe Animate texture atlas info</param>
        /// <param name="LoadFile">A function to load the texture data from the given filename</param>
        /// <param name="EnableFiltering">Enables texture Linear filtering</param>
        /// <exception cref="FileNotFoundException">LoadFile hasn't able to load the file</exception>
        public TiledSpriteAtlas2D(XmlDocument Document, Func<string, Stream> LoadFile, bool EnableFiltering) : this()
        {
            LoadSprite(Document, LoadFile, EnableFiltering);
        }

        /// <summary>
        /// Load Sprite Sheet to the <see cref="Sprites"/>
        /// </summary>
        /// <param name="Document">An Adobe Animate texture atlas info</param>
        /// <param name="LoadFile">A function to load the texture data from the given filename</param>
        /// <param name="EnableFiltering">Enables texture Linear filtering</param>
        /// <exception cref="FileNotFoundException">LoadFile hasn't able to load the file</exception>
        public override void LoadSprite(XmlDocument Document, Func<string, Stream> LoadFile, bool EnableFiltering)
        {
            var TexturePath = Document.DocumentElement.GetAttribute("imagePath");

            var TextureTiledPath = Path.Combine(Path.GetDirectoryName(TexturePath), Path.GetFileNameWithoutExtension(TexturePath) + "_t{0}.dds");
            
            var UL = LoadFile.Invoke(string.Format(TextureTiledPath, "UL"));

            using (var UR = LoadFile.Invoke(string.Format(TextureTiledPath, "UR")))
            using (var BL = LoadFile.Invoke(string.Format(TextureTiledPath, "BL")))
            using (var BR = LoadFile.Invoke(string.Format(TextureTiledPath, "BR")))
            {
                if (UL == null)
                    UL = LoadFile.Invoke(Path.ChangeExtension(TexturePath, ".dds"));

                Texture SpriteTexUL = new Texture(true);
                Texture SpriteTexUR = null;
                Texture SpriteTexBL = null;
                Texture SpriteTexBR = null;

                SpriteTexUL.SetDDS(UL, EnableFiltering);

                if (UR != null)
                {
                    SpriteTexUR = new Texture(true);
                    SpriteTexUR.SetDDS(UR, EnableFiltering);
                }

                if (BL != null)
                {
                    SpriteTexBL = new Texture(true);
                    SpriteTexBL.SetDDS(BL, EnableFiltering);
                }

                if (BR != null)
                {
                    SpriteTexBR = new Texture(true);
                    SpriteTexBR.SetDDS(BR, EnableFiltering);
                }

                LoadSprite(Document, new Texture[] { SpriteTexUL, SpriteTexUR, SpriteTexBL, SpriteTexBR });

            }
        }

        public override void LoadSprite(XmlDocument Document, Texture SpriteSheet)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load Sprite Sheet to the <see cref="Sprites"/>
        /// </summary>
        /// <param name="Document">An Adobe Animate texture atlas info</param>
        /// <param name="SpriteSheetTiles">An texture compatible with the given texture atlas info</param>
        public void LoadSprite(XmlDocument Document, Texture[] SpriteSheetTiles)
        {
            var SpriteTex = (TiledTexture2D)SpriteView.Target;

            if (SpriteSheetTiles == null && !SpriteTex.GetTextures().Any(x => x != null))
                throw new ArgumentNullException(nameof(SpriteSheetTiles));

            if (SpriteSheetTiles != null)
            {
                foreach (var Tex in SpriteTex.GetTextures())
                    Tex?.Dispose();

                SpriteTex.SetTexture(SpriteSheetTiles[0], SpriteSheetTiles[1], SpriteSheetTiles[2], SpriteSheetTiles[3]);
                SpriteTex.RefreshVertex();
            }

            var Frames = Document.DocumentElement.GetElementsByTagName("SubTexture");

            ///Check if each subtexture name ends with a number,
            ///if true, we can group each subtexture as frames
            var Groupable = !Frames.Cast<XmlNode>().Any(x => !IsNumberSufix(x));

            List<SpriteInfo> Sprites = new List<SpriteInfo>();

            if (Groupable)
                LoadAsGroup(Frames, Sprites);
            else
                LoadAsFrames(Frames, Sprites);

            this.Sprites = Sprites.ToArray();
        }


        public override GLObject2D Clone(bool AllowDisposal)
        {
            if (Textures == null || Textures.All(x => x == null || x.Disposed))
                throw new ObjectDisposedException("SpriteAtlas can't be cloned without an texture");

            var Clone = new TiledSpriteAtlas2D
            {
                FrameOffsets = FrameOffsets,
                Sprites = Sprites,
                AllowTexDisposal = AllowDisposal,
                Width = Width,
                Height = Height,
                Textures = Textures
            };

            if (!AllowDisposal)
                ((TiledTexture2D)SpriteView.Target).SharedTexture = true;

            return Clone;
        }

        public override void Dispose()
        {
            if (!AllowTexDisposal)
            {
                Textures = null;
            }
            base.Dispose();
        }
    }
}
