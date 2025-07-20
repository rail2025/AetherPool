using AetherPool.Game.GameObjects;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Linq;

namespace AetherPool.Game
{
    public static class PhysicsEngine
    {
        private const float Friction = 0.984f;
        private const float SpinFriction = 0.96f;

        public static void Update(List<PoolBall> balls, GameBoard board, float deltaTime, List<CollisionEvent> events, GameSession session)
        {
            for (int i = 0; i < balls.Count; i++)
            {
                var ball = balls[i];
                if (ball.IsSunk) continue;

                ball.Velocity *= Friction;
                ball.Spin *= SpinFriction;

                if (ball.Velocity.LengthSquared() < 0.1f) ball.Velocity = Vector2.Zero;
                if (ball.Spin.LengthSquared() < 0.1f) ball.Spin = Vector3.Zero;

                ball.Position += ball.Velocity * deltaTime;

                HandleBallCollisions(ball, balls, i, events, session);
                HandleCushionCollisions(ball, board, events);
                HandlePocketing(ball, board, events);
            }
        }

        public static float DistancePointToLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            float l2 = Vector2.DistanceSquared(a, b);
            if (l2 == 0.0f) return Vector2.Distance(p, a);
            float t = Math.Max(0, Math.Min(1, Vector2.Dot(p - a, b - a) / l2));
            Vector2 proj = a + t * (b - a);
            return Vector2.Distance(p, proj);
        }

        private static void HandleBallCollisions(PoolBall ball1, List<PoolBall> allBalls, int currentIndex, List<CollisionEvent> events, GameSession session)
        {
            for (int i = currentIndex + 1; i < allBalls.Count; i++)
            {
                var ball2 = allBalls[i];
                if (ball2.IsSunk) continue;

                Vector2 delta = ball1.Position - ball2.Position;
                float distSq = delta.LengthSquared();
                float radiusSum = ball1.Radius + ball2.Radius;

                if (distSq < radiusSum * radiusSum && distSq > 0)
                {
                    if (session.FirstBallHitThisTurn == null)
                    {
                        if (ball1.Number == 0) session.FirstBallHitThisTurn = ball2;
                        if (ball2.Number == 0) session.FirstBallHitThisTurn = ball1;
                    }

                    events.Add(new CollisionEvent { Type = CollisionType.Ball, ImpactVelocity = (ball1.Velocity - ball2.Velocity).Length() });

                    float distance = MathF.Sqrt(distSq);
                    Vector2 normal = delta / distance;
                    Vector2 tangent = new(-normal.Y, normal.X);

                    float overlap = 0.5f * (radiusSum - distance);
                    ball1.Position += normal * overlap;
                    ball2.Position -= normal * overlap;

                    float v1n = Vector2.Dot(ball1.Velocity, normal);
                    float v1t = Vector2.Dot(ball1.Velocity, tangent);
                    float v2n = Vector2.Dot(ball2.Velocity, normal);
                    float v2t = Vector2.Dot(ball2.Velocity, tangent);

                    float spinEffect = Vector2.Dot(new Vector2(ball1.Spin.Y, -ball1.Spin.X), tangent) * 0.1f;
                    v1t += spinEffect;

                    float followDrawEffect = ball1.Spin.X * 0.15f;
                    float v1n_after_impact = v2n + followDrawEffect;
                    ball1.Spin *= 0.5f;

                    ball1.Velocity = (normal * v1n_after_impact) + (tangent * v1t);
                    ball2.Velocity = (normal * v1n) + (tangent * v2t);
                }
            }
        }

        private static void HandleCushionCollisions(PoolBall ball, GameBoard board, List<CollisionEvent> events)
        {
            float feltX = board.BorderWidth;
            float feltY = board.BorderWidth;
            float feltWidth = board.Width - (board.BorderWidth * 2);
            float feltHeight = board.Height - (board.BorderWidth * 2);

            if ((ball.Position.X - ball.Radius < feltX && ball.Velocity.X < 0) ||
                (ball.Position.X + ball.Radius > feltX + feltWidth && ball.Velocity.X > 0))
            {
                ball.Velocity.X *= -1;
                events.Add(new CollisionEvent { Type = CollisionType.Cushion });
            }

            if ((ball.Position.Y - ball.Radius < feltY && ball.Velocity.Y < 0) ||
                (ball.Position.Y + ball.Radius > feltY + feltHeight && ball.Velocity.Y > 0))
            {
                ball.Velocity.Y *= -1;
                events.Add(new CollisionEvent { Type = CollisionType.Cushion });
            }
        }

        private static void HandlePocketing(PoolBall ball, GameBoard board, List<CollisionEvent> events)
        {
            float forgivingPocketRadius = board.PocketRadius * 1.5f;
            foreach (var pocket in board.Pockets)
            {
                if (Vector2.DistanceSquared(ball.Position, pocket) < forgivingPocketRadius * forgivingPocketRadius)
                {
                    events.Add(new CollisionEvent { Type = CollisionType.Pocket, ImpactVelocity = ball.Velocity.Length() });
                    ball.IsSunk = true;
                    ball.Velocity = Vector2.Zero;
                }
            }
        }

        public static List<Vector2> PredictBallPath(GameBoard board, Vector2 startPos, float angle, float radius)
        {
            var pathPoints = new List<Vector2> { startPos };
            var currentPos = startPos;
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            int bounces = 0;

            float feltX = board.BorderWidth;
            float feltY = board.BorderWidth;
            float feltWidth = board.Width - (board.BorderWidth * 2);
            float feltHeight = board.Height - (board.BorderWidth * 2);

            for (int i = 0; i < 2000; i++)
            {
                currentPos += velocity;

                if (currentPos.X - radius < feltX) { currentPos.X = feltX + radius; velocity.X *= -1; pathPoints.Add(currentPos); bounces++; }
                else if (currentPos.X + radius > feltX + feltWidth) { currentPos.X = feltX + feltWidth - radius; velocity.X *= -1; pathPoints.Add(currentPos); bounces++; }

                if (currentPos.Y - radius < feltY) { currentPos.Y = feltY + radius; velocity.Y *= -1; pathPoints.Add(currentPos); bounces++; }
                else if (currentPos.Y + radius > feltY + feltHeight) { currentPos.Y = feltY + feltHeight - radius; velocity.Y *= -1; pathPoints.Add(currentPos); bounces++; }

                if (bounces >= 2) break;
            }
            pathPoints.Add(currentPos);
            return pathPoints;
        }
    }
}
