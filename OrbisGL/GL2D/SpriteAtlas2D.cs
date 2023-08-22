using OrbisGL.GL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml;

namespace OrbisGL.GL2D
{
    public class SpriteAtlas2D : GLObject2D
    {
        Sprite2D SpriteView = new Sprite2D(new Texture2D());

        public SpriteAtlas2D()
        {
            AddChild(SpriteView);
        }

        private Vector2[] FrameOffsets;

        public string CurrentSprite { get; private set; }

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

            var Animation = Sprites.Where(x => x.Name.ToLowerInvariant() == Name.ToLowerInvariant());
            if (!Animation.Any())
                return false;

            CurrentSprite = Animation.First().Name;

            var Frames = Animation.SelectMany(x => x.Frames);

            SpriteView.Frames = Frames.Select(x => x.Coordinates).ToArray();
            var FirstFrame = Frames.First();

            SpriteView.Width = (int)FirstFrame.Coordinates.Width;
            SpriteView.Height = (int)FirstFrame.Coordinates.Height;

            FrameOffsets = Frames.Select(x => new Vector2(x.X, x.Y)).ToArray();

            SpriteView.SetCurrentFrame(0);
            return true;
        }

        public SpriteInfo[] Sprites { get; private set; } = null;

        /// <summary>
        /// Load Sprite Sheet to the <see cref="Sprites"/>
        /// </summary>
        /// <param name="Document">An Adobe Animate Texture Atlas Info</param>
        /// <param name="LoadFile">A function to load the texture data from the given filename</param>
        /// <exception cref="FileNotFoundException">LoadFile hasn't able to load the file</exception>
        public void LoadSprite(XmlDocument Document, Func<string, Stream> LoadFile)
        {
            var TexturePath = Document.DocumentElement.GetAttribute("imagePath");

            var Stream = LoadFile.Invoke(TexturePath);

            if (Stream == null)
            {
                throw new FileNotFoundException("Failed to Open the Texture");
            }

            var SpriteTex = (Texture2D)SpriteView.Target;

            MemoryStream Buffer = null;

            try
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

                SpriteTex.Texture?.Dispose();
                SpriteTex.Texture = new Texture(true);
                SpriteTex.Texture.SetImage(Buffer.ToArray(), PixelFormat.RGBA);
            }
            finally {
                Buffer.Dispose();
            }

            var Frames = Document.DocumentElement.GetElementsByTagName("SubTexture");

            ///Check if each subtexture name ends with a number,
            ///if true, we can group each subtexture as frames
            var Groupable = !Frames.Cast<XmlNode>().Any(x => !IsNumberSufix(x));

            List<SpriteInfo> Sprites = new List<SpriteInfo>();

            if (Groupable)
            {
                foreach (var Anim in Frames.Cast<XmlNode>().GroupBy(GetGroupName))
                {
                    var FirstFrame = Anim.First();

                    SpriteInfo Info = new SpriteInfo();
                    Info.Name = FirstFrame.Attributes["name"].Value;
                    Info.Name = Info.Name.Substring(0, Info.Name.Length - 4);

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
            else
            {
                List<SpriteFrame> SpriteFrames = new List<SpriteFrame>();
                foreach (var Frame in Frames.Cast<XmlNode>())
                {
                    SpriteInfo Info = new SpriteInfo();
                    Info.Name = Frame.Attributes["name"].Value;

                    SpriteFrame FrameInfo = LoadFrameInfo(Frame);

                    SpriteFrames.Add(FrameInfo);

                    Info.Frames = SpriteFrames.ToArray();

                    Sprites.Add(Info);
                }
            }

            this.Sprites = Sprites.ToArray();
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

        private bool IsNumberSufix(XmlNode x)
        {
            var Name = x.Attributes["name"].Value;

            if (Name == null)
                return false;

            return Name.Length >= 4 && int.TryParse(Name.Substring(Name.Length - 4), out _);
        }

        private int GetNumberSufix(XmlNode x)
        {
            if (!IsNumberSufix(x))
                return 0;

            var Name = x.Attributes["name"].Value;

            return int.Parse(Name.Substring(Name.Length - 4));
        }

        public void NextFrame()
        {
            var FrameID = SpriteView.NextFrame();
            SpriteView.Position = -FrameOffsets[FrameID];
        }

        private string GetGroupName(XmlNode x)
        {
            var FrameName = x.Attributes["name"].Value;
            
            if (FrameName == null)
                return null;

            return FrameName.Substring(0, FrameName.Length - 4);
        }
    }
}
