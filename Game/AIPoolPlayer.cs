using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AetherPool.Game.GameObjects;

namespace AetherPool.Game
{
    public static class AIPoolPlayer
    {
        private static readonly Random Rng = new();

        public static (float Angle, float Power, Vector2 AimOffset) FindBestShot(GameSession session)
        {
            var legalTargets = GameRules.GetLegalTargets(session);
            if (!legalTargets.Any()) return (0, 0, Vector2.Zero);

            var cueBall = session.Balls.First(b => b.Number == 0);
            var potentialShots = new List<(float Score, float Angle, float Power, Vector2 AimOffset)>();

            foreach (var targetBall in legalTargets)
            {
                foreach (var pocket in session.Board.Pockets)
                {
                    Vector2 targetToPocket = Vector2.Normalize(pocket - targetBall.Position);
                    Vector2 contactPoint = targetBall.Position - targetToPocket * (cueBall.Radius + targetBall.Radius);
                    float aimAngle = MathF.Atan2(contactPoint.Y - cueBall.Position.Y, contactPoint.X - cueBall.Position.X);

                    bool isPathClear = !IsPathBlocked(cueBall.Position, contactPoint, session.Balls.Where(b => b != cueBall && b != targetBall));

                    if (isPathClear)
                    {
                        float distance = Vector2.Distance(cueBall.Position, contactPoint);
                        float power = Math.Clamp(distance / 200f, 0.4f, 1.0f);
                        float score = 100f - distance;
                        potentialShots.Add((score, aimAngle, power, Vector2.Zero));
                    }
                }
            }

            if (potentialShots.Any())
            {
                var bestShot = potentialShots.OrderByDescending(s => s.Score).First();
                return (bestShot.Angle, bestShot.Power, bestShot.AimOffset);
            }

            // Failsafe: No clear pocketing shot, attempt a "safety" by hitting the first legal target gently.
            var failsafeTarget = legalTargets.First();
            float failsafeAngle = MathF.Atan2(failsafeTarget.Position.Y - cueBall.Position.Y, failsafeTarget.Position.X - cueBall.Position.X);
            return (failsafeAngle, 0.3f, Vector2.Zero);
        }

        public static Vector2 FindBestCueBallPlacement(GameSession session)
        {
            var legalTargets = GameRules.GetLegalTargets(session);
            if (!legalTargets.Any()) return new Vector2(session.Board.Width * 0.75f, session.Board.Height / 2);

            var bestTarget = legalTargets.First(); // Simplistic: just target the first legal ball

            // Try to find a position with a clear line of sight to the target
            for (int i = 0; i < 50; i++)
            {
                var randomPos = new Vector2(
                    (float)(session.Board.Width * 0.6 + Rng.NextDouble() * session.Board.Width * 0.2),
                    (float)(Rng.NextDouble() * session.Board.Height)
                );

                if (!IsPathBlocked(randomPos, bestTarget.Position, session.Balls.Where(b => b.Number != 0)))
                {
                    return randomPos;
                }
            }

            // Failsafe placement
            return new Vector2(session.Board.Width * 0.75f, session.Board.Height / 2);
        }

        private static bool IsPathBlocked(Vector2 start, Vector2 end, IEnumerable<PoolBall> obstacles)
        {
            foreach (var obstacle in obstacles)
            {
                if (PhysicsEngine.DistancePointToLineSegment(obstacle.Position, start, end) < obstacle.Radius * 2)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
