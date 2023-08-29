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
    public class SpriteAtlas2D : GLObject2D
    {

        Sprite2D SpriteView = new Sprite2D(new Texture2D());

        private Vector2[] FrameOffsets;

        /// <summary>
        /// Get a list of all sprites available
        /// </summary>
        public SpriteInfo[] Sprites { get; private set; } = null;

        /// <summary>
        /// Get the Active sprite group name
        /// </summary>
        public string CurrentSprite { get; private set; }

        /// <summary>
        /// Get or Set the loaded sprite sheet texture instance
        /// </summary>
        public Texture Texture
        {
            get => ((Texture2D)SpriteView.Target).Texture;
            set
            {
                ((Texture2D)SpriteView.Target).Texture = value;

                if (value != null)
                    ((Texture2D)SpriteView.Target).RefreshVertex();
            }
        }
        public override RGBColor Color { get => SpriteView.Color; set => SpriteView.Color = value; }
        public override byte Opacity { get => SpriteView.Opacity; set => SpriteView.Opacity = value; }

        private bool AllowTexDisposal = false;

        public event EventHandler OnAnimationEnd;

        public SpriteAtlas2D()
        {
            AddChild(SpriteView);
            SpriteView.OnAnimationEnd += (sender, e) => OnAnimationEnd?.Invoke(this, e);
        }

        /// <summary>
        /// Creates and load a SpriteAtlas2D Instance
        /// </summary>
        /// <param name="Document">An Adobe Animate texture atlas info</param>
        /// <param name="SpriteSheet">An texture compatible with the given texture atlas info</param>
        public SpriteAtlas2D(XmlDocument Document, Texture SpriteSheet) : this()
        {
            LoadSprite(Document, SpriteSheet);
        }

        /// <summary>
        /// Creates and load a SpriteAtlas2D Instance
        /// </summary>
        /// <param name="Document">An Adobe Animate texture atlas info</param>
        /// <param name="LoadFile">A function to load the texture data from the given filename</param>
        /// <param name="EnableFiltering">Enables texture Linear filtering</param>
        /// <exception cref="FileNotFoundException">LoadFile hasn't able to load the file</exception>
        public SpriteAtlas2D(XmlDocument Document, Func<string, Stream> LoadFile, bool EnableFiltering) : this()
        {
            LoadSprite(Document, LoadFile, EnableFiltering);
        }

        /// <summary>
        /// Select an sprite animation with the given name
        /// </summary>
        /// <param name="Name">The Animation Name</param>
        /// <exception cref="Exception"><see cref="LoadSprite"/> Not called</exception>
        /// <exception cref="KeyNotFoundException">No animations found with the given name</exception>
        public bool SetActiveAnimation(string Name)
        {
            if (Sprites == null)
                throw new Exception("Sprite Sheet not Loaded");

            if (Name == CurrentSprite)
                return true;

            var Animation = Sprites.Where(x => x.Name.ToLowerInvariant().Trim() == Name.ToLowerInvariant().Trim());
            if (!Animation.Any())
                return false;

            CurrentSprite = Animation.First().Name;

            var Frames = Animation.SelectMany(x => x.Frames);

            SpriteView.Frames = Frames.Select(x => x.Coordinates).ToArray();
            var FirstFrame = Frames.First();

            Width = SpriteView.Width = (int)FirstFrame.Coordinates.Width;
            Height = SpriteView.Height = (int)FirstFrame.Coordinates.Height;

            FrameOffsets = Frames.Select(x => new Vector2(x.X, x.Y)).ToArray();

            SpriteView.SetCurrentFrame(0);
            return true;
        }

        /// <summary>
        /// Load Sprite Sheet to the <see cref="Sprites"/>
        /// </summary>
        /// <param name="Document">An Adobe Animate texture atlas info</param>
        /// <param name="LoadFile">A function to load the texture data from the given filename</param>
        /// <param name="EnableFiltering">Enables texture Linear filtering</param>
        /// <exception cref="FileNotFoundException">LoadFile hasn't able to load the file</exception>
        public void LoadSprite(XmlDocument Document, Func<string, Stream> LoadFile, bool EnableFiltering)
        {
            var TexturePath = Document.DocumentElement.GetAttribute("imagePath");

            var TextureDDSPath = Path.ChangeExtension(TexturePath, "dds");

            bool ForceDDS = true;
            var Stream = LoadFile.Invoke(TextureDDSPath);

            if (Stream == null)
            {
                ForceDDS = false;
                Stream = LoadFile.Invoke(TexturePath);
            }

            if (Stream == null)
            {
                throw new FileNotFoundException("Failed to Open the Texture");
            }

            Texture SpriteTex = new Texture(true);

            MemoryStream Buffer = null;

            try
            {
                if (ForceDDS)
                {
                    Stream.Position = 0;
                    SpriteTex.SetDDS(Stream, EnableFiltering);
                    Stream.Dispose();
                }
                else
                {
                    if (Stream is MemoryStream mStream)
                    {
                        Buffer = mStream;
                    }
                    else
                    {
                        Buffer = new MemoryStream();
                        Stream.Position = 0;
                        Stream.CopyTo(Buffer);

                        Stream.Dispose();
                    }

                    SpriteTex.SetImage(Buffer.ToArray(), PixelFormat.RGBA, EnableFiltering);
                }
            }
            finally {
                Buffer?.Dispose();
            }

            LoadSprite(Document, SpriteTex);
        }

        /// <summary>
        /// Load Sprite Sheet to the <see cref="Sprites"/>
        /// </summary>
        /// <param name="Document">An Adobe Animate texture atlas info</param>
        /// <param name="SpriteSheet">An texture compatible with the given texture atlas info</param>
        public void LoadSprite(XmlDocument Document, Texture SpriteSheet)
        {
            var SpriteTex = (Texture2D)SpriteView.Target;

            if (SpriteSheet == null && SpriteTex.Texture == null)
                throw new ArgumentNullException(nameof(SpriteSheet));

            if (SpriteSheet != null)
            {
                SpriteTex.Texture?.Dispose();
                SpriteTex.Texture = SpriteSheet;
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

        private static void LoadAsFrames(XmlNodeList Frames, List<SpriteInfo> Sprites)
        {
            List<SpriteFrame> SpriteFrames = new List<SpriteFrame>();
            foreach (var Frame in Frames.Cast<XmlNode>())
            {
                SpriteInfo Info = new SpriteInfo();
                Info.Name = Frame.Attributes["name"].Value.Trim();

                SpriteFrame FrameInfo = LoadFrameInfo(Frame);

                SpriteFrames.Add(FrameInfo);

                Info.Frames = SpriteFrames.ToArray();

                Sprites.Add(Info);
            }
        }

        private void LoadAsGroup(XmlNodeList Frames, List<SpriteInfo> Sprites)
        {
            foreach (var Anim in Frames.Cast<XmlNode>().GroupBy(GetGroupName))
            {
                var FirstFrame = Anim.First();

                SpriteInfo Info = new SpriteInfo();
                Info.Name = GetGroupName(FirstFrame);

                List<SpriteFrame> SpriteFrames = new List<SpriteFrame>();
                foreach (var Frame in Anim.OrderBy(x => GetNumberSufix(x)))
                {
                    SpriteFrame FrameInfo = LoadFrameInfo(Frame);

                    SpriteFrames.Add(FrameInfo);
                }

                Info.Frames = SpriteFrames.ToArray();

                Sprites.Add(Info);
            }
        }

        private static SpriteFrame LoadFrameInfo(XmlNode Frame)
        {
            var Name = Frame.Attributes["name"];
            var X = Frame.Attributes["x"];
            var Y = Frame.Attributes["y"];

            var Width = Frame.Attributes["width"];
            var Height = Frame.Attributes["height"];

            var FrameX = Frame.Attributes["frameX"];
            var FrameY = Frame.Attributes["frameY"];


            bool OK = int.TryParse(X.Value, out int iX);
            OK &= int.TryParse(Y.Value, out int iY);
            OK &= int.TryParse(Width.Value, out int iWidth);
            OK &= int.TryParse(Height.Value, out int iHeight);

            if (!OK)
                throw new FormatException($"Missing Texture Attributes from {Name.Value}0");


            SpriteFrame FrameInfo = new SpriteFrame();
            FrameInfo.Coordinates = new Rectangle(iX, iY, iWidth, iHeight);

            int DeltaX = 0;
            if (FrameX != null)
                DeltaX = int.Parse(FrameX.Value);

            int DeltaY = 0;
            if (FrameY != null)
                DeltaY = int.Parse(FrameY.Value);

            FrameInfo.X = DeltaX;
            FrameInfo.Y = DeltaY;
            return FrameInfo;
        }

        /// <summary>
        /// Clone this sprite sharing the same texture memory
        /// </summary>
        /// <param name="AllowDisposal">When false, the clone instance can't dispose the shared texture</param>
        public SpriteAtlas2D Clone(bool AllowDisposal)
        {
            return new SpriteAtlas2D()
            {
                FrameOffsets = FrameOffsets,
                Sprites = Sprites,
                Texture = Texture,
                AllowTexDisposal = AllowDisposal,
                Width = Width,
                Height = Height
            };
        }

        private bool IsNumberSufix(XmlNode x)
        {
            return GetNumberSufixString(x) != null;
        }

        private int GetNumberSufix(XmlNode x)
        {
            return int.Parse(GetNumberSufixString(x));
        }

        private string GetNumberSufixString(XmlNode x)
        {
            var Name = x.Attributes["name"].Value.Trim();

            if (Name == null)
                return null;

            string NumberSufix = string.Empty;
            while (Name.Length > 0 && char.IsNumber(Name.Last()))
            {
                NumberSufix = Name.Last() + NumberSufix;
                Name = Name.Substring(0, Name.Length - 1);
            }

            return NumberSufix;
        }

        public void NextFrame()
        {
            var AnimName = CurrentSprite;
            var FrameID = SpriteView.NextFrame();
            
            if (CurrentSprite != AnimName)
                return;

            SpriteView.Position = -FrameOffsets[FrameID];
        }

        public void SetCurrentFrame(int FrameID)
        {
            SpriteView.SetCurrentFrame(FrameID);
            SpriteView.Position = -FrameOffsets[FrameID];
        }

        private string GetGroupName(XmlNode x)
        {
            var FrameName = x.Attributes["name"].Value;
            
            if (FrameName == null)
                return null;

            var Sufix = GetNumberSufixString(x);

            return FrameName.Substring(0, FrameName.Length - Sufix.Length).Trim();
        }

        public override void Dispose()
        {
            if (!AllowTexDisposal)
            {
                Texture = null;
            }
            base.Dispose();
        }
    }
}
