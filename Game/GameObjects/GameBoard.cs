using System.Collections.Generic;
using System.Numerics;

namespace AetherPool.Game.GameObjects
{
    public class GameBoard
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float BorderWidth { get; private set; }
        public List<Vector2> Pockets { get; private set; } = new();
        public float PocketRadius { get; private set; }
        public (Vector2 Start, Vector2 End)[] Cushions { get; private set; }

        public GameBoard(float width, float height, float pocketRadius)
        {
            PocketRadius = pocketRadius;
            Cushions = new (Vector2, Vector2)[4];
            UpdateLayout(width, height);
        }

        public void UpdateLayout(float width, float height)
        {
            Width = width;
            Height = height;
            BorderWidth = PocketRadius * 1.5f;

            float feltX = BorderWidth;
            float feltY = BorderWidth;

            Pockets.Clear();
            // Pockets on corners and middle of the long (horizontal) sides
            Pockets.Add(new Vector2(feltX, feltY));                                 // Top-left
            Pockets.Add(new Vector2(width / 2, feltY));                             // Top-middle
            Pockets.Add(new Vector2(width - feltX, feltY));                         // Top-right
            Pockets.Add(new Vector2(feltX, height - feltY));                         // Bottom-left
            Pockets.Add(new Vector2(width / 2, height - feltY));                     // Bottom-middle
            Pockets.Add(new Vector2(width - feltX, height - feltY));                 // Bottom-right

            Cushions[0] = (new Vector2(feltX + PocketRadius, feltY), new Vector2(width - feltX - PocketRadius, feltY));
            Cushions[1] = (new Vector2(feltX + PocketRadius, height - feltY), new Vector2(width - feltX - PocketRadius, height - feltY));
            Cushions[2] = (new Vector2(feltX, feltY + PocketRadius), new Vector2(feltX, height - feltY - PocketRadius));
            Cushions[3] = (new Vector2(width - feltX, feltY + PocketRadius), new Vector2(width - feltX, height - feltY - PocketRadius));
        }
    }
}
