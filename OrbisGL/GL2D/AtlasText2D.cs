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


        private bool _StaticText;

        /// <summary>
        /// When set to true, the <see cref="AtlasText2D"/> will be optimized for text that doesn't change frequently. This prioritizes rendering speed. 
        /// When set to false, it optimizes for text that requires frequent updates, prioritizing text update speed.
        /// </summary>
        public bool StaticText { get => _StaticText;
            set
            {
                if (_StaticText != value)
                {
                    _StaticText = value;
                    Invalidate();
                }
            }
        }

        private void Invalidate()
        {
            var Text = this.Text;
            this.Text = null;
            SetText(Text);
        }

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

        /// <summary>
        /// Generate a font atlas for the indicated characters and load it.
        /// <para>It is recommended to export the generated atlas and use the pre-rendered constructor instead of this constructor.</para>
        /// </summary>
        /// <param name="Font">The TrueType font to be used</param>
        /// <param name="FontSize">The font size to create the atlas</param>
        /// <param name="Characters">A list with all characters that should be included in the atlas, if null EXTENDED_ASCII_TABLE will be used</param>
        public AtlasText2D(FontFaceHandler Font, int FontSize, string Characters = null)
        {
            if (Characters == null)
                Characters = Constants.EXTENDED_ASCII_TABLE;

            Font.SetFontSize(FontSize);
            Texture = SpriteAtlas2D.LoadFromFreeType(Font, Characters, out FrameMap);
        }

        /// <summary>
        /// Creates a new AtlasText2D Sharing the same texture memory from other instance
        /// </summary>
        /// <param name="Source">The source instance to share the texture</param>
        public AtlasText2D(AtlasText2D Source)
        {
            Texture = Source.Texture;
            FrameMap = Source.FrameMap;
        }

        /// <summary>
        /// Loads a pre-rendered font atlas
        /// </summary>
        /// <param name="Atlas">The Font Atlas</param>
        /// <param name="FrameMap">A map of the character that each frame represents</param>
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

            if ((Text == this.Text && Childs.Any()) || string.IsNullOrWhiteSpace(Text))
                return;

            this.Text = Text;

            if (StaticText)
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

            if (Outline <= 0 || Text == null)                
                return;

            if (StaticText)
            {
                if (Text == null)
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
            else
            {
                var GlyphZoom = Coordinates2D.ParseZoomFactor(Zoom);
                var OutlineZoom = Coordinates2D.ParseZoomFactor(GlyphZoom + Outline);

                OutlinePosition.Clear();

                for (int i = 0, x = 0; i < Text.Length; i++)
                {
                    var Char = Text[i];

                    if (!GlyphCache.ContainsKey(Char))
                        continue;

                    var Glyph = GlyphCache[Char];
                    var GlyphPos = GlyphPosition[x++];

                    Glyph.SetZoom();

                    Glyph.Position = GlyphPos;

                    Glyph.SetZoom(Zoom);

                    var GlyphMiddle = new Vector2(Glyph.ZoomWidth, Glyph.ZoomHeight) / 2;

                    var OriPos = Glyph.ZoomPosition;

                    Glyph.SetZoom(OutlineZoom);

                    var OutlineMiddle = new Vector2(Glyph.ZoomWidth, Glyph.ZoomHeight) / 2;

                    OutlinePosition.Add(OriPos - (OutlineMiddle - GlyphMiddle));

                }

                return;
            }
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
                IEnumerable<SpriteInfo> Sprite = Texture.Sprites.Where(x => x.Name == Name);
                if (Sprite.Any())
                {
                    return Sprite.First();
                }
            }
            return null;
        }

        public override void SetZoom(float Value = 1)
        {
            base.SetZoom(Value);
            Invalidate();
        }

        public override void Draw(long Tick)
        {
            if (Text == null) return;

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
                        Glyph.SetZoom(OutlineZoom);

                        Glyph.ZoomPosition = OutlinePosition[x - 1];

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
