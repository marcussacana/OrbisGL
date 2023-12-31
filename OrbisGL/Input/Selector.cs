﻿using OrbisGL.Controls;
using OrbisGL.GL;
using OrbisGL.GL2D;
using System;
using System.Linq;

namespace OrbisGL.Input
{
    internal class Selector : IRenderable
    {
        Control TargetControl = null;
        Control SelectedControl = null;
        Rectangle2D Rectangle = new Rectangle2D(1, 1, false)
        {
            Color = RGBColor.Black,
            ContourWidth = 2f
        };

        public event EventHandler SelectionChanged;

        public void Select(Control Controller)
        {
            TargetControl = Controller;
        }
        public void Dispose()
        {
            Rectangle?.Dispose();
        }

        const int AnimDuration = Constants.ORBIS_SECOND;

        public void Draw(long Tick)
        {
            var Percentage = ((float)(Tick % AnimDuration) * 2) / AnimDuration;
            var Opacity = Math.Abs(Percentage - 1) * 255;

            if (Opacity < 10 && TargetControl != SelectedControl)
            {
                Refresh();
            }

            if (Invalidated)
                Refresh();

            Rectangle.Visible = TargetControl?.Visible ?? false;
            Rectangle.Opacity = (byte)Opacity;
            Rectangle.Draw(Tick);
        }

        private void Refresh()
        {
            if (SelectedControl != TargetControl)
            {
                if (SelectedControl != null)
                    SelectedControl.OnControlInvalidated -= TargetInvalidated;
                
                TargetControl.OnControlInvalidated += TargetInvalidated;
                TargetControl.Focus();

                SelectionChanged?.Invoke(TargetControl, EventArgs.Empty);
            }

            SelectedControl = TargetControl;
            
            //if the selected target is invisible find in the parent tree for an new controller to give focus
            if (!TargetControl.Visible)
            {
                Control Current = TargetControl;
                
                do
                {
                    Current = TargetControl.Parent;
                } while (Current != null && !Current.Visible);

                //no visible parent found, try find for alternative in reaming root controllers
                if (Current == null)
                {
                    var Alternatives = Application.Default.Controllers.Where(x => x != TargetControl.RootControl);
                    if (Alternatives.Any())
                    {
                        Alternatives.First().Focus();
                        return;
                    }
                }
                
                //if visible parent has been found, send the focus
                if (Current != null && Current != TargetControl)
                {
                    Current.Focus();
                    return;
                }
            }

            var Rect = TargetControl.AbsoluteRectangle;

            Rect.Top -= 3;
            Rect.Left -= 3;
            Rect.Right += 3;
            Rect.Bottom += 3;

            if (Rectangle.Rectangle != Rect)
            {
                Rectangle.Rectangle = Rect;
                Rectangle.RefreshVertex();
            }

            Invalidated = false;
        }

        bool Invalidated = false;
        private void TargetInvalidated(object sender, EventArgs e)
        {
            Invalidated = true;
        }
    }
}
