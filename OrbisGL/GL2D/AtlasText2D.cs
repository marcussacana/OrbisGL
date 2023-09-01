using OrbisGL.FreeTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OrbisGL.GL2D
{
    public class AtlasText2D : GLObject2D
    {
        SpriteAtlas2D Texture;

        public GlyphInfo[] GlyphsInfo { get; private set; }

        readonly Dictionary<char, string> FrameMap;

        public AtlasText2D(SpriteAtlas2D Atlas, Dictionary<char, string> FrameMap)
        {
            Texture = Atlas;
            this.FrameMap = FrameMap;
        }

        public void SetText(string Text)
        {
            RemoveChildren(true);

            var A = QueryGlyph('A');

            if (A == null)
                throw new Exception("The `A` Glyph is required for all font atlas");

            var ARect = A.Value.Area;

            float SpaceWidth = ARect.Width;
            float LineBase = ARect.Height;
            float LineAdvance = LineBase + (LineBase * 0.3f);


            GlyphInfo SpaceGlyph = new GlyphInfo(0, 0, SpaceWidth, LineBase, ' ', 0);

            List<GlyphInfo> Glyphs = new List<GlyphInfo>();

            float X = 0, Y = 0;

            for (int i = 0; i < Text.Length; i++)
            {
                var Glyph = QueryGlyph(Text[i]) ?? SpaceGlyph;
                var Sprite = QuerySprite(Text[i]);

                if (Text[i] == '\n')
                {
                    Glyphs.Add(new GlyphInfo(X, Y, 0, Glyph.Area.Height, Text[i], i));

                    Y += LineAdvance;
                    X = 0;
                    continue;
                }

                Glyphs.Add(new GlyphInfo(X, Y, Glyph.Area.Width, Glyph.Area.Height, Text[i], i));

                if (Sprite != null)
                {
                    var GlyphSprite = (SpriteAtlas2D)Texture.Clone(false);
                    GlyphSprite.SetActiveAnimation(FrameMap[Text[i]]);
                    var DeltaLineBase = LineBase - Glyph.Area.Height;
                    GlyphSprite.Position = new Vector2(X, Y + DeltaLineBase);
                    AddChild(GlyphSprite);
                }

                X += Glyph.Area.Width;
            }

            GlyphsInfo = Glyphs.ToArray();

            var XMax = Glyphs.Max(x => x.Area.Right);
            var YMax = Glyphs.Max(x => x.Area.Bottom);

            Width = (int)XMax;
            Height = (int)YMax;
        }

        private GlyphInfo? QueryGlyph(char Char)
        {
            var SpriteInfo = QuerySprite(Char);
            if (SpriteInfo == null) return null;

            var Frame = SpriteInfo.Value.Frames.First();

            return new GlyphInfo(0, 0, Frame.Coordinates.Width, Frame.Coordinates.Height, Char, 0);
        }

        private SpriteInfo? QuerySprite(char Char)
        {
            if (FrameMap.TryGetValue(Char, out string Name))
            {
                var Sprite = Texture.Sprites.Where(x => x.Name.ToLowerInvariant().Trim() == Name.ToLowerInvariant().Trim());
                if (Sprite.Any())
                {
                    return Sprite.First();
                }
            }
            return null;
        }
    }
}
