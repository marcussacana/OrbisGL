using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace OrbisGL
{
    public static class Geometry
    {
        /// <summary>
        /// Compute a In/Out Cubic Beizer curve for non-linear animations
        /// </summary>
        /// <param name="P1">Any Vector2 value from 0 to 1</param>
        /// <param name="P2">Any Vector2 value from 0 to 1</param>
        /// <param name="Progress">The animation progress value from 0 to 1</param>
        /// <returns>The non-linear animation progress</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CubicBezierInOut(Vector2 P1, Vector2 P2, float Progress)
        {
            if (Progress > 0.5)
                Progress = 1 - ((Progress - 0.5f) * 2);
            else
                Progress = Progress * 2;

            return CubicBezier(P1, P2, Progress);
        }

        /// <summary>
        /// Compute a Cubic Beizer curve for non-linear animations
        /// </summary>
        /// <param name="P1">Any Vector2 value from 0 to 1</param>
        /// <param name="P2">Any Vector2 value from 0 to 1</param>
        /// <param name="Progress">The animation progress value from 0 to 1</param>
        /// <returns>The non-linear animation progress</returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CubicBezier(Vector2 P1, Vector2 P2, float Progress)
        {
            return Bezier(Vector2.Zero, P1, P2, Vector2.One, Progress).Y;
        }

        /// <summary>
        /// Find a Beizer curve point with 4 control points
        /// </summary>
        /// <param name="Progress">The curve progress</param>
        /// <returns>The Cubic CurveProgress</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Bezier(Vector2 PT0, Vector2 PT1, Vector2 PT2, Vector2 PT3, float Progress)
        {
            Vector2 P;

            P.X = (float)(Math.Pow((1 - Progress), 3) * PT0.X + 3 * Progress * Math.Pow((1 - Progress), 2) * PT1.X + 3 * (1 - Progress) * Math.Pow(Progress, 2) * PT2.X + Math.Pow(Progress, 3) * PT3.X);
            P.Y = (float)(Math.Pow((1 - Progress), 3) * 
                PT0.Y + 3 * 
                Progress * 
                Math.Pow((1 - Progress), 2) * 
                PT1.Y + 3 * (1 - Progress) *
                Math.Pow(Progress, 2) * PT2.Y + Math.Pow(Progress, 3) 
                * PT3.Y);

            return P;
        }

        /// <summary>
        /// Find a Beizer curve point with 4 control points
        /// </summary>
        /// <param name="Progress">The curve progress</param>
        /// <returns>The Cubic CurveProgress</returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Bezier(Vector2 PT0, Vector2 PT1, Vector2 PT2, Vector2 PT3, float Progress, bool Restraint)
        {
            if (Restraint)
            {
                float scale;
                float btKey;
                float vx_A;
                float vx_B;
                btKey = PT3.X - PT0.X;
                vx_A = PT1.X - PT0.X;
                vx_B = PT3.X - PT2.X;
                scale = btKey / (vx_A + vx_B);
                if (scale < 1)
                {
                    Vector2 vec_A = PT1 - PT0;
                    Vector2 vec_B = PT2 - PT3;

                    PT1 = vec_A * scale + PT0;
                    PT2 = vec_B * scale + PT3;
                }
            }

            Vector2 _p4, _p5, _p6, _p7, _p8, _p9;

            _p4 = (PT1 - PT0) * Progress + PT0;
            _p5 = (PT2 - PT1) * Progress + PT1;
            _p6 = (PT3 - PT2) * Progress + PT2;

            _p7 = (_p5 - _p4) * Progress + _p4;
            _p8 = (_p6 - _p5) * Progress + _p5;
            _p9 = (_p8 - _p7) * Progress + _p7;

            return _p9;
        }
    }
}
