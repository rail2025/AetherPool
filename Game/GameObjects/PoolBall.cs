using System.Numerics;

namespace AetherPool.Game.GameObjects
{
    public class PoolBall
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector3 Spin; // X-axis for top/back spin, Y-axis for left/right spin
        public float Radius;
        public uint Color;
        public int Number;
        public bool IsStriped;
        public bool IsSunk;

        public PoolBall(Vector2 position, float radius, int number, uint color, bool isStriped = false)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Spin = Vector3.Zero;
            Radius = radius;
            Number = number;
            Color = color;
            IsStriped = isStriped;
            IsSunk = false;
        }
    }
}
