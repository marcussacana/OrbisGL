﻿using OrbisGL.GL;
using OrbisGL.GL2D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OrbisGL.Controls
{
    public class Panel : Control
    {
        VerticalScrollBar ScrollBar;

        public Panel(int Width, int Height) : this(new Vector2(Width, Height)) { }
        public Panel(Vector2 Size)
        {
            this.Size = Size;

            OnControlResized += (s, e) =>
            {
                Background.Width = (int)this.Size.X;
                Background.Height = (int)this.Size.Y;
            };

            SetBackground(new Rectangle2D((int)Size.X, (int)Size.Y, true));
        }

        public bool AllowScroll { get; set; }
        public byte ScrollBarWidth { get; set; } = 20;

        public byte BackgroundTransparency { get => Background.Opacity; set => Background.Opacity = value; }

        public override bool Focusable => false;

        public override string Name { get; }

        public override string Text { get; set; }

        GLObject2D Background;

        Vector2 BGMargin = Vector2.Zero;

        int _ScrollX = 0;
        int _ScrollY = 0;

        public int ScrollX { get => _ScrollX; set { if (value == _ScrollX) return;  _ScrollX = value; Invalidate(); } }
        public int ScrollY { get => _ScrollY; set { if (value == _ScrollY) return;  _ScrollY = value; Invalidate(); } }

        public override IEnumerable<Control> Childs => base.Childs.Where(x => x != ScrollBar);

        public int MaxScrollX { 
            get
            {
                var Objs = PositionMap.Where(x => x.Key != ScrollBar);

                if (!Objs.Any())
                    return 0;

                var MaxX = Objs.Max(x => x.Value.X + x.Key.Size.X) - Size.X;
                
                MaxX = Math.Max(MaxX, 0);

                return (int)MaxX;
            }
        }

        public int MaxScrollY
        {
            get
            {
                var Objs = PositionMap.Where(x => x.Key != ScrollBar);
                if (!Objs.Any())
                    return 0;

                var MaxY = Objs.Max(x => x.Value.Y + x.Key.Size.Y) - Size.Y;

                MaxY = Math.Max(MaxY, 0);

                return (int)MaxY;
            }
        }

        private int TotalHeight => MaxScrollY + (int)Size.Y;

        private Rectangle _CurrentVisibleArea;
        public Rectangle CurrentVisibleArea { get => _CurrentVisibleArea; private set => _CurrentVisibleArea = value; }

        public override void Refresh()
        {
            if (AllowScroll)
            {
                if (ScrollBar != null && (ScrollBar.TotalHeight != TotalHeight || ScrollBarWidth != (int)ScrollBar.Size.X))
                {
                    ScrollBar.Dispose();
                    ScrollBar = null;
                }

                if (ScrollBar == null)
                {
                    if (Childs.Any(x => x is VerticalScrollBar))
                    {
                        foreach (var OldScrollBar in Childs.Where(x => x is VerticalScrollBar).ToArray())
                        {
                            OldScrollBar.Dispose();
                        }
                    }

                    ScrollBar = new VerticalScrollBar((int)Size.Y, TotalHeight, ScrollBarWidth);
                    ScrollBar.SetScrollByScrollValue(ScrollY);
                    ScrollBar.Refresh();
                    ScrollBar.ScrollChanged += (s, e) => { ScrollY = (int)((VerticalScrollBar)s).CurrentScroll; };

                    AddChild(ScrollBar);
                }
            }

            Background.Width = (int)Size.X;
            Background.Height = (int)Size.Y;
            Background.Color = BackgroundColor;

            ScrollX = Math.Max(ScrollX, 0);
            ScrollY = Math.Max(ScrollY, 0);

            ScrollX = Math.Min(ScrollX, MaxScrollX);
            ScrollY = Math.Min(ScrollY, MaxScrollY);

            if (ScrollBar != null)
                ScrollBar.SetScrollByScrollValue(ScrollY);

            var AreaRect = AbsoluteRectangle;

            if (VisibleRectangle.HasValue)
            {
                AreaRect = VisibleRectangle.Value;
                AreaRect.Position += AbsolutePosition;

                if (Parent != null && Parent.VisibleRectangle.HasValue)
                {
                    var ParentVisibleArea = Parent.VisibleRectangle.Value;
                    ParentVisibleArea.Position += Parent.AbsolutePosition;

                    AreaRect = AreaRect.Intersect(ParentVisibleArea);
                }
            }

            _CurrentVisibleArea = AreaRect;
            _CurrentVisibleArea.Position += new Vector2(ScrollX, ScrollY);

            try
            {
                Moving = true;
                foreach (var Child in Childs)
                {
                    var ChildPos = PositionMap[Child];
                    Child.Position = ChildPos - new Vector2(ScrollX, ScrollY);

                    Child.SetAbsoluteVisibleArea(AreaRect);
                }

                if (ScrollBar != null)
                {
                    ScrollBar.Position = new Vector2(Size.X - ScrollBarWidth - (int)(ScrollBarWidth * 0.3), 0);
                    ScrollBar.SetAbsoluteVisibleArea(AreaRect);
                }
            }
            finally 
            {
                Moving = false;
            }

            Background.ClearVisibleRectangle();

            var BGVisibleRect = Rectangle.GetChildBounds(AreaRect, AbsoluteRectangle);
            Background.Position = BGVisibleRect.Position + BGMargin;
            Background.Width = (int)BGVisibleRect.Width;
            Background.Height = (int)BGVisibleRect.Height;

            GLObject.RefreshVertex();
        }

        public void SetBackgroundMargin(Vector2 Margin) => BGMargin = Margin;

        public void SetBackground(GLObject2D Background)
        {
            if (this.Background != null)
                GLObject.RemoveChild(this.Background);

            Background.Position = BGMargin;

            GLObject.AddChild(Background);

            this.Background = Background;
        }

        protected readonly Dictionary<Control, Vector2> PositionMap = new Dictionary<Control, Vector2>();
        
        public override void AddChild(Control Child)
        {
            PositionMap[Child] = Child.Position;
            Child.OnControlMoved += Child_OnControlMoved;
            base.AddChild(Child);
            Invalidate();
        }

        bool Moving;
        private void Child_OnControlMoved(object sender, EventArgs e)
        {
            if (sender == null || Moving)
                return;                          

            var Child = (Control)sender;
            PositionMap[Child] = Child.Position;
        }

        public override void RemoveChild(Control Child)
        {
            if (!PositionMap.ContainsKey(Child))
                base.RemoveChild(Child);

            Child.OnControlMoved -= Child_OnControlMoved;
            PositionMap.Remove(Child);
            base.RemoveChild(Child);
        }

        public override void RemoveChildren(bool Dispose)
        {
            foreach (var Child in Childs)
            {
                Child.OnControlMoved -= Child_OnControlMoved;
            }

            PositionMap.Clear();

            if (ScrollBar != null)
                RemoveChild(ScrollBar);

            base.RemoveChildren(Dispose);

            if (ScrollBar != null)
                AddChild(ScrollBar);
        }

        protected override void OnFocus(object Sender, EventArgs Args)
        {
            EnsureVisible((Control)Sender);
            base.OnFocus(Sender, Args);
        }

        private void EnsureVisible(Control Target)
        {
            if (!Target.IsDescendantOf(this))
                return;
            
            var tAbsRect = Target.AbsoluteRectangle;
            var Absrect = AbsoluteRectangle;
            
            if (tAbsRect.Bottom >= Absrect.Bottom)
            {
                ScrollY = (int)GetChildPosition(Target).Y;
            }

            if (tAbsRect.Top <= Absrect.Top)
            {
                ScrollY = (int)(GetChildPosition(Target).Y - Size.Y + Target.Size.Y);
            }
            if (tAbsRect.Right >= Absrect.Right)
            {
                ScrollX = (int)(GetChildPosition(Target).X + Size.X);
            }
            
            if (tAbsRect.Left <= Absrect.Left)
            {
                ScrollX = (int)(GetChildPosition(Target).X - Size.X + Target.Size.X);
            }
        }

        public Vector2 GetChildPosition(Control Child)
        {
            if (PositionMap.TryGetValue(Child, out Vector2 Result))
                return Result;
            
            return Vector2.Zero;
        }

    }
}
