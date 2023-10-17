using OrbisGL.GL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting;
using System.Xml;

namespace OrbisGL.GL2D
{

    /// <summary>
    /// A class that parses Adobe Animate sprite sheet and reproduce it.
    /// </summary>
    public class SpriteAtlas2D : GLObject2D
    {

        protected Sprite2D SpriteView { get; set; }

        protected Vector2[] FrameOffsets;

        /// <summary>
        /// Get a list of all sprites available
        /// </summary>
        public SpriteInfo[] Sprites { get; protected set; } = null;

        /// <summary>
        /// Get the Active sprite group name
        /// </summary>
        public string CurrentSprite { get; protected set; }


        /// <summary>
        /// Sets an delay in miliseconds for advance the sprite in the next frame
        /// automatically, where 0 means disabled
        /// </summary>
        public int FrameDelay { get => SpriteView.FrameDelay; set => SpriteView.FrameDelay = value; }

        /// <summary>
        /// Get or Set the loaded sprite sheet texture instance
        /// </summary>
        public Texture Texture
        {
            get
            {
                if (SpriteView.Target is Texture2D Tex)
                {
                    return Tex.Texture;
                }

                return null;
            }
            set
            {
                if (SpriteView.Target is Texture2D Tex)
                {
                    Tex.Texture = value;

                    if (value != null)
                        Tex.RefreshVertex();
                }
            }
        }

        /// <summary>
        /// When set to true the texture displays horizontally inverted
        /// </summary>
        public virtual bool Mirror { get => ((Texture2D)SpriteView.Target).Mirror; set => ((Texture2D)SpriteView.Target).Mirror = value; }

        /// <summary>
        /// When set to true the texture color displays in negative
        /// </summary>
        public virtual bool Negative { get => ((Texture2D)SpriteView.Target).Negative; set => ((Texture2D)SpriteView.Target).Negative = value; }
        
        public override RGBColor Color { get => SpriteView.Color; set => SpriteView.Color = value; }
        public override byte Opacity { get => SpriteView.Opacity; set => SpriteView.Opacity = value; }

        protected bool AllowTexDisposal = true;

        public event EventHandler OnAnimationEnd;

        public SpriteAtlas2D() : this(new Sprite2D(new Texture2D())) { }

        protected SpriteAtlas2D(Sprite2D View)
        {
            SpriteView = View;
            AddChild(SpriteView);
            SpriteView.OnFrameChange += (sender, e) => SpriteView.Position = -FrameOffsets[e];
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
        public bool SetActiveAnimation(string Name) => SetActiveAnimation(Name, 0);

        /// <summary>
        /// Select an sprite animation with the given name
        /// </summary>
        /// <param name="Name">The Animation Name</param>
        /// <para name="Loops">Create a loop X times by duplicating the animation frames</para>
        /// <exception cref="Exception"><see cref="LoadSprite"/> Not called</exception>
        /// <exception cref="KeyNotFoundException">No animations found with the given name</exception>
        public bool SetActiveAnimation(string Name, int Loops)
        {
            if (Sprites == null)
                throw new Exception("Sprite Sheet not Loaded");

            if (Name == null)
                return false;

            if (Name == CurrentSprite)
                return true;

            var Animation = Sprites.Where(x => x.Name.ToLowerInvariant().Trim() == Name.ToLowerInvariant().Trim());
            if (!Animation.Any())
                return false;

            CurrentSprite = Animation.First().Name;

            var Frames = Animation.SelectMany(x => x.Frames);

            Frames = ApplyMirrorEffect(Frames);

            var FrameCoords = Frames.Select(x => x.Coordinates);
            var FrameOffsets = Frames.Select(x => new Vector2(x.X, x.Y));

            for (int i = 0; i < Loops; i++)
            {
                FrameCoords = FrameCoords.Concat(Frames.Select(x => x.Coordinates));
                FrameOffsets = FrameOffsets.Concat(Frames.Select(x => new Vector2(x.X, x.Y)));
            }

            SpriteView.Frames = FrameCoords.ToArray();
            var FirstFrame = Frames.First();

            Width = SpriteView.Width = (int)FirstFrame.FrameSize.X;
            Height = SpriteView.Height = (int)FirstFrame.FrameSize.Y;

            this.FrameOffsets = FrameOffsets.ToArray();

            SpriteView.SetCurrentFrame(0);
            return true;
        }

        private IEnumerable<SpriteFrame> ApplyMirrorEffect(IEnumerable<SpriteFrame> Frames)
        {
            if (!Mirror) 
            {
                foreach (var Frame in Frames)
                    yield return Frame;
                yield break;
            }

            int MaxWidth = SpriteView.Target.Width;

            float MaxFrameWidth = Frames.Max(x => x.FrameSize.X);

            foreach (var x in Frames)
            {
                int DeltaWidth = (int)(MaxFrameWidth - x.Coordinates.Width);

                yield return new SpriteFrame()
                {
                    Coordinates = new Rectangle(MaxWidth - x.Coordinates.Right, x.Coordinates.Y, x.Coordinates.Width, x.Coordinates.Height),
                    FrameSize = x.FrameSize,
                    X = -x.X - DeltaWidth,
                    Y = x.Y
                };
            }
        }

        /// <summary>
        /// Create an new animation with the given frame coordinates
        /// </summary>
        /// <param name="Animation">The Animation name</param>
        /// <param name="FrameCoord">The frame rectangle in texture</param>
        /// <param name="FrameOffset">The frame display offset (optional)</param>
        /// <param name="FrameSize">The frame display size (optional)</param>
        public void CreateAnimation(string Animation, Rectangle[] FrameCoord, Vector2[] FrameOffset = null, Vector2[] FrameSize = null)
        {
            if (FrameOffset != null && FrameCoord.Length != FrameOffset.Length && FrameOffset.Length != 0)
                throw new Exception("The frame offset count must be the same of the frame coordinates count");

            if (FrameSize != null && FrameCoord.Length != FrameSize.Length && FrameSize.Length != 0)
                throw new Exception("The frame size count must be the same of the frame coordinates count");

            if (FrameCoord == null || FrameCoord.Length == 0)
                throw new Exception("At least one frame coordinate must be specified");

            if (Sprites == null)
                Sprites = new SpriteInfo[0];

            //Remove old animation if have one
            Sprites = Sprites.Where(x => x.Name.ToLowerInvariant().Trim() != Animation.ToLowerInvariant().Trim()).ToArray();


            if (FrameOffset == null || FrameOffset.Length == 0)
                FrameOffset = FrameCoord.Select(x => Vector2.Zero).ToArray();

            if (FrameSize?.Length == 0)
                FrameSize = null;

            SpriteInfo NewSprite = new SpriteInfo();
            NewSprite.Name = Animation;

            List<SpriteFrame> Frames = new List<SpriteFrame>();
            for (int i = 0; i < FrameCoord.Length; i++)
            {
                var Frame = new SpriteFrame();
                Frame.Coordinates = FrameCoord[i];
                Frame.X = (int)FrameOffset[i].X;
                Frame.Y = (int)FrameOffset[i].Y;
                Frame.FrameSize = FrameSize?[i] ?? FrameCoord[i].Size;

                Frames.Add(Frame);
            }

            NewSprite.Frames = Frames.ToArray();

            var Spr = Sprites;
            Array.Resize(ref Spr, Sprites.Length + 1);
            Sprites = Spr;

            Sprites[Sprites.Length - 1] = NewSprite;
        }

        /// <summary>
        /// Creates an new animation by selecting the frames from other animation
        /// </summary>
        /// <param name="OriginAnimation">The original animation name to pick the frames</param>
        /// <param name="NewAnimation">The new animation name</param>
        /// <param name="DeleteOldAnimation">If true, the original animation will be deleted</param>
        /// <param name="FrameIndex">The index of the frames in the OriginAnimation to be used in the new animation</param>
        /// <exception cref="KeyNotFoundException">The given OriginAnimation name does not matches with any animation loaded</exception>
        public void CreateAnimationByIndex(string OriginAnimation, string NewAnimation, bool DeleteOldAnimation, params int[] FrameIndex)
        {
            var OriSprite = Sprites.Where(x => x.Name.ToLowerInvariant().Trim() == OriginAnimation.ToLowerInvariant().Trim());
            if (!OriSprite.Any())
                throw new KeyNotFoundException(OriginAnimation);

            var SrcFrames = OriSprite.SelectMany(x => x.Frames).ToArray();
            
            var DstFrames = new SpriteFrame[FrameIndex.Length];
            
            for (int i = 0; i < FrameIndex.Length; i++)
            {
                DstFrames[i] = SrcFrames[FrameIndex[i]];
            }

            SpriteInfo NewSprite = new SpriteInfo()
            {
                Frames = DstFrames,
                Name = NewAnimation
            };

            if (DeleteOldAnimation)
                Sprites = Sprites.Where(x => x.Name.ToLowerInvariant().Trim() != OriginAnimation.ToLowerInvariant().Trim()).ToArray();

            Sprites = Sprites.Concat(new SpriteInfo[] { NewSprite }).ToArray();
        }

        /// <summary>
        /// Load Sprite Sheet to the <see cref="Sprites"/>
        /// </summary>
        /// <param name="Document">An Adobe Animate texture atlas info</param>
        /// <param name="LoadFile">A function to load the texture data from the given filename</param>
        /// <param name="EnableFiltering">Enables texture Linear filtering</param>
        /// <exception cref="FileNotFoundException">LoadFile hasn't able to load the file</exception>
        public virtual void LoadSprite(XmlDocument Document, Func<string, Stream> LoadFile, bool EnableFiltering)
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
        public virtual void LoadSprite(XmlDocument Document, Texture SpriteSheet)
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

        protected static void LoadAsFrames(XmlNodeList Frames, List<SpriteInfo> Sprites)
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

        protected void LoadAsGroup(XmlNodeList Frames, List<SpriteInfo> Sprites)
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

        protected static SpriteFrame LoadFrameInfo(XmlNode Frame)
        {
            var Name = Frame.Attributes["name"];
            var X = Frame.Attributes["x"];
            var Y = Frame.Attributes["y"];

            var Width = Frame.Attributes["width"];
            var Height = Frame.Attributes["height"];

            var FrameX = Frame.Attributes["frameX"];
            var FrameY = Frame.Attributes["frameY"];

            var FrameWidth = Frame.Attributes["frameWidth"] ?? Width;
            var FrameHeight = Frame.Attributes["frameHeight"] ?? Height;


            bool OK = int.TryParse(X.Value, out int iX);
            OK &= int.TryParse(Y.Value, out int iY);
            OK &= int.TryParse(Width.Value, out int iWidth);
            OK &= int.TryParse(Height.Value, out int iHeight);

            if (!OK)
                throw new FormatException($"Missing Texture Attributes from {Name.Value}0");


            SpriteFrame FrameInfo = new SpriteFrame();
            FrameInfo.Coordinates = new Rectangle(iX, iY, iWidth, iHeight);
            FrameInfo.FrameSize = new Vector2(int.Parse(FrameWidth.Value), int.Parse(FrameHeight.Value));

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
        public virtual GLObject2D Clone(bool AllowDisposal)
        {
            if (Texture == null || Texture.Disposed)
                throw new ObjectDisposedException("SpriteAtlas can't be cloned without an texture");

            var Clone = new SpriteAtlas2D()
            {
                FrameOffsets = FrameOffsets,
                Sprites = Sprites,
                Texture = Texture,
                AllowTexDisposal = AllowDisposal,
                Width = Width,
                Height = Height
            };

            if (!AllowDisposal)
                ((Texture2D)Clone.SpriteView.Target).SharedTexture = true;

            return Clone;
        }

        protected bool IsNumberSufix(XmlNode x)
        {
            return GetNumberSufixString(x) != null;
        }

        protected int GetNumberSufix(XmlNode x)
        {
            return int.Parse(GetNumberSufixString(x));
        }

        protected string GetNumberSufixString(XmlNode x)
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
            if (CurrentSprite == null)
                throw new Exception("No Sprite Activated");

            var AnimName = CurrentSprite;
            var FrameID = SpriteView.NextFrame();
            
            if (CurrentSprite != AnimName)
                return;
        }

        public void SetCurrentFrame(int FrameID)
        {
            SpriteView.SetCurrentFrame(FrameID);
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
            Texture?.Dispose();
            SpriteView.Dispose();
            base.Dispose();
        }
    }
}
