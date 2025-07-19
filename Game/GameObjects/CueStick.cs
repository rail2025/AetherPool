using System.Numerics;
using System;

namespace AetherPool.Game.GameObjects
{
    public class CueStick
    {
        public Vector2 Position { get; set; }
        public float Angle { get; set; } // In radians
        public float Power { get; set; } // From 0.0 to 1.0

        public CueStick(Vector2 initialPosition)
        {
            Position = initialPosition;
            Angle = 0f;
            Power = 0f;
        }

        public void AimAt(Vector2 target)
        {
            Angle = MathF.Atan2(target.Y - Position.Y, target.X - Position.X);
        }
    }
}
