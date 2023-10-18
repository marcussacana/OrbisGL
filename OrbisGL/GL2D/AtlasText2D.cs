using OrbisGL.FreeTypeLib;
using OrbisGL.GL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OrbisGL.GL2D
{
    public class AtlasText2D : GLObject2D
    {
        SpriteAtlas2D Texture;

        public Dictionary<char, SpriteAtlas2D> GlyphCache = new Dictionary<char, SpriteAtlas2D>();
        public Dictionary<char, SpriteAtlas2D> OutlineCache = new Dictionary<char, SpriteAtlas2D>();

        List<Vector2> GlyphPosition = new List<Vector2>();
        List<Vector2> OutlinePosition = new List<Vector2>();

        public GlyphInfo[] GlyphsInfo { get; private set; }

        readonly Dictionary<char, string> FrameMap;


        /// <summary>
        /// When set to true, the <see cref="AtlasText2D"/> will be optimized for text that doesn't change frequently. This prioritizes rendering speed. 
        /// When set to false, it optimizes for text that requires frequent updates, prioritizing text update speed.
        /// </summary>
        public bool StaticText { get; set; } = false;

        public override byte Opacity { 
            get => base.Opacity;
            set
            {
                base.Opacity = value;

                foreach (var Child in Childs)
                    Child.Opacity = value;
            }
        }

        public override RGBColor Color {
            get => base.Color;
            set
            {
                base.Color = value;

                foreach (var Child in Childs)
                    Child.Color = value;
            }
        }

        public string Text { get; private set; }

        bool _Negative;
        public bool Negative { 
            get => _Negative;
            set
            {
                bool OldVal = _Negative;
                _Negative = value;

                foreach (var Child in Childs)
                {
                    //Outline glyphs stores an inverted negative value
                    if (Child is SpriteAtlas2D Glyph)
                        Glyph.Negative = OldVal == Glyph.Negative ? value : !value;
                }
            } 
        }

        float _Outline = 0;

        /// <summary>
        /// Creates a text outline effect using the negative color.
        /// The higher the value, the further the outline extends. 
        /// </summary>
        /// 
        public float Outline {
            get => _Outline; 
            set {
                _Outline = value;
                ApplyOutline();
            } 
        }

        public AtlasText2D(SpriteAtlas2D Atlas, Dictionary<char, string> FrameMap)
        {
            Texture = Atlas;
            this.FrameMap = FrameMap;
        }

        public void SetText(string Text)
        {

            var A = QueryGlyph('A');

            if (A == null)
                throw new Exception("The `A` Glyph is required for all font atlas");

            if (Text == this.Text && Childs.Any())
                return;

            this.Text = Text;
            
            RemoveChildren(true);

            var ARect = A.Value.Area;

            float SpaceWidth = ARect.Width;
            float LineBase = ARect.Height;
            float LineAdvance = LineBase + (LineBase * 0.3f);


            GlyphInfo SpaceGlyph = new GlyphInfo(0, 0, SpaceWidth, LineBase, ' ', 0);

            List<GlyphInfo> Glyphs = new List<GlyphInfo>();
            GlyphPosition.Clear();

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

                //Add glyph in the rendering queue
                if (Sprite != null)
                {
                    if (StaticText)
                    {
                        var GlyphSprite = (SpriteAtlas2D)Texture.Clone(false);
                        GlyphSprite.Color = Color;
                        GlyphSprite.Negative = Negative;
                        GlyphSprite.Opacity = Opacity;
                        GlyphSprite.SetActiveAnimation(FrameMap[Text[i]]);
                        var DeltaLineBase = LineBase - Glyph.Area.Height;
                        GlyphSprite.Position = new Vector2(X, Y + DeltaLineBase);
                        GlyphSprite.SetZoom(Zoom);
                        AddChild(GlyphSprite);
                    }
                    else if (!GlyphCache.ContainsKey(Text[i]))
                    {
                        var GlyphSprite = (SpriteAtlas2D)Texture.Clone(false);
                        GlyphSprite.Color = Color;
                        GlyphSprite.Negative = Negative;
                        GlyphSprite.Opacity = Opacity;
                        GlyphSprite.SetActiveAnimation(FrameMap[Text[i]]);
                        var DeltaLineBase = LineBase - Glyph.Area.Height;

                        GlyphCache[Text[i]] = GlyphSprite;
                        GlyphPosition.Add(new Vector2(X, Y + DeltaLineBase));
                        AddChild(GlyphSprite);
                    }
                    else
                    {
                        var GlyphSprite = GlyphCache[Text[i]];
                        var DeltaLineBase = LineBase - Glyph.Area.Height;

                        GlyphCache[Text[i]] = GlyphSprite;
                        GlyphPosition.Add(new Vector2(X, Y + DeltaLineBase));
                    }
                }

                X += Glyph.Area.Width;
            }

            GlyphsInfo = Glyphs.ToArray();

            var XMax = Glyphs.Max(x => x.Area.Right);
            var YMax = Glyphs.Max(x => x.Area.Bottom);

            Width = (int)XMax;
            Height = (int)YMax;

            ApplyOutline();
        }

        private void ApplyOutline()
        {
            foreach (var Child in Childs.SelectMany(x => x.Childs))
            {
                if (Child is SpriteAtlas2D)
                    Child.RemoveChildren(true);
            }

            if (Outline <= 0 || !StaticText)                
                return;

            List<GLObject2D> Outlines = new List<GLObject2D>();

            foreach (var Child in Childs)
            {
                if (Child is SpriteAtlas2D Glyph)
                {
                    var OutlineGlyph = (SpriteAtlas2D)Glyph.Clone(false);

                    OutlineGlyph.Color = Glyph.Color;
                    OutlineGlyph.Negative = !Glyph.Negative;
                    OutlineGlyph.Opacity = Opacity;
                    OutlineGlyph.SetActiveAnimation(Glyph.CurrentSprite);

                    var GlyphMiddle = new Vector2(Glyph.ZoomWidth, Glyph.ZoomHeight) / 2;
                    var GlyphZoom = Coordinates2D.ParseZoomFactor(Zoom);

                    OutlineGlyph.SetZoom(Coordinates2D.ParseZoomFactor(GlyphZoom + Outline));

                    var OutlineMiddle = new Vector2(OutlineGlyph.ZoomWidth, OutlineGlyph.ZoomHeight) / 2;

                    OutlineGlyph.ZoomPosition = Glyph.ZoomPosition - (OutlineMiddle - GlyphMiddle);

                    Outlines.Add(OutlineGlyph);
                }
            }

            List<GLObject2D> Glyphs = new List<GLObject2D>(Childs);

            RemoveChildren(false);


            foreach (var Child in Outlines)
                AddChild(Child);

            foreach (var Child in Glyphs)
                AddChild(Child);
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

        public override void Draw(long Tick)
        {
            if (!StaticText)
            {
                var GlyphZoom = Coordinates2D.ParseZoomFactor(Zoom);
                var OutlineZoom = Coordinates2D.ParseZoomFactor(GlyphZoom + Outline);

                for (int i = 0, x = 0; i < Text.Length; i++)
                {
                    var Char = Text[i];
                    
                    if (!GlyphCache.ContainsKey(Char))
                        continue;

                    var Glyph = GlyphCache[Char];
                    var GlyphPos = GlyphPosition[x++];

                    if (Outline > 0)
                    {
                        var GlyphMiddle = new Vector2(Glyph.ZoomWidth, Glyph.ZoomHeight) / 2;

                        Glyph.Position = GlyphPos;

                        var OriPos = Glyph.ZoomPosition;

                        Glyph.SetZoom(OutlineZoom);

                        var OutlineMiddle = new Vector2(Glyph.ZoomWidth, Glyph.ZoomHeight) / 2;

                        Glyph.ZoomPosition = OriPos - (OutlineMiddle - GlyphMiddle);

                        Glyph.Negative = !Negative;
                        Glyph.Draw(Tick);
                        Glyph.Negative = !Negative;

                        Glyph.SetZoom(1);
                    }

                    Glyph.Color = Color;
                    Glyph.Opacity = Opacity;
                    Glyph.Negative = Negative;
                    Glyph.Position = GlyphPos;

                    Glyph.SetZoom(Zoom);

                    Glyph.Draw(Tick);
                }

                return;
            }

            base.Draw(Tick);
        }

        public override void Dispose()
        {
            foreach (var Glyph in GlyphCache.Values)
                Glyph.Dispose();

            GlyphCache.Clear();

            base.Dispose();
        }
    }
}
