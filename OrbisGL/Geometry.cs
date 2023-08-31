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
            return Bezier(Vector2.Zero, P1, Vector2.One, P2, Progress).X;
        }

        /// <summary>
        /// Find a Beizer curve point with 4 control points
        /// </summary>
        /// <param name="Progress">The curve progress</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Bezier(Vector2 PT0, Vector2 PT1, Vector2 PT2, Vector2 PT3, float Progress)
        {
            Vector2 P;

            P.X = (float)(Math.Pow((1 - Progress), 3) * PT0.X + 3 * Progress * Math.Pow((1 - Progress), 2) * PT1.X + 3 * (1 - Progress) * Math.Pow(Progress, 2) * PT2.X + Math.Pow(Progress, 3) * PT3.X);
            P.Y = (float)(Math.Pow((1 - Progress), 3) * PT0.Y + 3 * Progress * Math.Pow((1 - Progress), 2) * PT1.Y + 3 * (1 - Progress) * Math.Pow(Progress, 2) * PT2.Y + Math.Pow(Progress, 3) * PT3.Y);

            return P;
        }
    }
}
