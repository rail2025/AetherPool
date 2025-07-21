#pragma warning disable CA1416

using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace AetherPool.Windows
{
    public class TextureManager : IDisposable
    {
        private readonly Dictionary<int, IDalamudTextureWrap> ballTextures = new();
        public IDalamudTextureWrap? CueBallTexture { get; private set; }

        public TextureManager()
        {
            GenerateAllBallTextures();
            LoadCueBallTexture();
        }

        private void LoadCueBallTexture()
        {
            string resourcePath = "AetherPool.Assets.Images.bomb.png";

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(resourcePath);

                if (stream == null)
                {
                    return;
                }

                using var image = Image.Load<Rgba32>(stream);

                var center = new Vector2(image.Width / 2f, image.Height / 2f);
                float radiusSq = center.X * center.X;

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            // If the pixel is outside the circle's radius, make it transparent.
                            if (Vector2.DistanceSquared(new Vector2(x, y), center) > radiusSq)
                            {
                                pixelRow[x].A = 0;
                            }
                        }
                    }
                });
                
                var rgbaBytes = new byte[image.Width * image.Height * 4];
                image.CopyPixelDataTo(rgbaBytes);

                this.CueBallTexture = Plugin.TextureProvider.CreateFromRaw(RawImageSpecification.Rgba32(image.Width, image.Height), rgbaBytes);
            }
            catch (Exception)
            {
            }
        }

        public IDalamudTextureWrap? GetBallTexture(int ballNumber)
        {
            return ballTextures.GetValueOrDefault(ballNumber);
        }

        private void GenerateAllBallTextures()
        {
            var colors = new uint[]
            {
                0xFFFFFFFF, 0xFF00D4FF, 0xFF0000FF, 0xFFFF0000, 0xFF800080, 0xFFFFA500, 0xFF008000, 0xFF800000,
                0xFF000000, 0xFF00D4FF, 0xFF0000FF, 0xFFFF0000, 0xFF800080, 0xFFFFA500, 0xFF008000, 0xFF800000
            };

            for (int i = 1; i <= 15; i++)
            {
                bool isStriped = i > 8;
                var texture = GenerateBallTexture(i, colors[i], isStriped);
                if (texture != null)
                {
                    ballTextures[i] = texture;
                }
            }
        }

        private static IDalamudTextureWrap? GenerateBallTexture(int number, uint color, bool isStriped)
        {
            const int size = 64;
            using var image = new Image<Rgba32>(size, size);

            var center = new PointF(size / 2f, size / 2f);
            var radius = size / 2f - 2;

            var baseColor = number == 0 ? Color.White : Color.FromRgba(
                (byte)((color >> 0) & 0xFF),
                (byte)((color >> 8) & 0xFF),
                (byte)((color >> 16) & 0xFF),
                (byte)((color >> 24) & 0xFF)
            );

            image.Mutate(ctx => ctx.Fill(baseColor, new EllipsePolygon(center, radius)));

            if (isStriped)
            {
                float stripeHeight = radius * 1.4f;
                var stripeRect = new RectangleF(center.X - radius, center.Y - stripeHeight / 2, radius * 2, stripeHeight);
                image.Mutate(ctx => ctx.Fill(Color.White, stripeRect));
            }

            if (number > 0)
            {
                var numberCircleRadius = radius / 1.8f;
                image.Mutate(ctx => ctx.Fill(Color.White, new EllipsePolygon(center, numberCircleRadius)));

                if (SystemFonts.TryGet("Arial", out var fontFamily))
                {
                    var fontSize = number < 10 ? numberCircleRadius * 1.6f : numberCircleRadius * 1.3f;
                    var font = new Font(fontFamily, fontSize, FontStyle.Bold);

                    var textOptions = new RichTextOptions(font)
                    {
                        Origin = center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    image.Mutate(ctx => ctx.DrawText(textOptions, number.ToString(), Color.Black));
                }
            }

            var rgbaBytes = new byte[size * size * 4];
            image.CopyPixelDataTo(rgbaBytes);
            return Plugin.TextureProvider.CreateFromRaw(RawImageSpecification.Rgba32(size, size), rgbaBytes);
        }

        public void Dispose()
        {
            foreach (var texture in ballTextures.Values)
            {
                texture.Dispose();
            }
            CueBallTexture?.Dispose();
        }
    }
}
