namespace FolderVerse;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public partial class Game1
{
    private readonly Random _mobRandom = new();
    private Texture2D[] _cutInMobs = Array.Empty<Texture2D>();
    private int _cutInMob = -1;

    private void LoadCutInMobs()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Content", "Images", "Mobs");
        if (!Directory.Exists(directory)) return;
        var files = Directory.GetFiles(directory, "*.png");
        Array.Sort(files, StringComparer.Ordinal);
        var textures = new List<Texture2D>();
        foreach (var file in files)
        {
            using var stream = File.OpenRead(file);
            var texture = Texture2D.FromStream(GraphicsDevice, stream);
            var pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
                        // Explicit empty-space anchors for closed gaps in the current trial poses.
            // Coordinates are normalized, so asset resolution can change.
            var gap = Path.GetFileNameWithoutExtension(file) switch
            {
                "base-02" => new Vector2(.518f, .863f),
                "base-04" => new Vector2(.43f, .613f),
                _ => new Vector2(-1, -1)
            };
            RemoveMobBackdrop(pixels, texture.Width, texture.Height, gap);
            texture.SetData(pixels);
            textures.Add(texture);
        }
        _cutInMobs = textures.ToArray();
    }

    // Only the near-white region connected to the canvas edge is background.
    // Enclosed white clothing stays opaque; the source PNG is never modified.
    private static void RemoveMobBackdrop(Color[] pixels, int width, int height, Vector2 gap)
    {
        var visited = new bool[pixels.Length];
        var queue = new Queue<int>();
        void Visit(int i)
        {
            if (visited[i]) return;
            var c = pixels[i];
            if (c.A != 0 && (c.R < 244 || c.G < 244 || c.B < 244)) return;
            visited[i] = true;
            queue.Enqueue(i);
        }
        if (gap.X >= 0)
            Visit((int)(gap.Y * height) * width + (int)(gap.X * width));
        for (int x = 0; x < width; x++) { Visit(x); Visit((height - 1) * width + x); }
        for (int y = 0; y < height; y++) { Visit(y * width); Visit(y * width + width - 1); }
        while (queue.Count > 0)
        {
            int i = queue.Dequeue(), x = i % width, y = i / width;
            pixels[i] = Color.Transparent;
            if (x > 0) Visit(i - 1);
            if (x + 1 < width) Visit(i + 1);
            if (y > 0) Visit(i - width);
            if (y + 1 < height) Visit(i + width);
        }
        // White gaps between legs can be enclosed by the skirt and touching feet.
        // Require a sizable lower-body component bordered mainly by warm skin:
        // this excludes white clothes, highlights and shoes.
        var examined = (bool[])visited.Clone();
        for (int seed = 0; seed < pixels.Length; seed++)
        {
            if (examined[seed]) continue;
            var initial = pixels[seed];
            if (initial.R < 244 || initial.G < 244 || initial.B < 244) continue;
            var component = new List<int>();
            queue.Enqueue(seed); examined[seed] = true;
            int minY = height, skinEdges = 0, otherEdges = 0;
            while (queue.Count > 0)
            {
                int i = queue.Dequeue(), x = i % width, y = i / width;
                component.Add(i); minY = Math.Min(minY, y);
                void Neighbor(int n)
                {
                    var c = pixels[n];
                    if (c.A == 0) return;
                    if (c.R >= 244 && c.G >= 244 && c.B >= 244)
                    {
                        if (!examined[n]) { examined[n] = true; queue.Enqueue(n); }
                    }
                    else if (c.R > c.G + 12 && c.G > c.B + 5) skinEdges++;
                    else otherEdges++;
                }
                if (x > 0) Neighbor(i - 1);
                if (x + 1 < width) Neighbor(i + 1);
                if (y > 0) Neighbor(i - width);
                if (y + 1 < height) Neighbor(i + width);
            }
            if (component.Count >= 16 && minY > height * .55f &&
                skinEdges > 0 && skinEdges > otherEdges)
                foreach (int i in component) { pixels[i] = Color.Transparent; visited[i] = true; }
        }

        // Unmix the white matte in the one-pixel antialiased boundary.
        // Work from a snapshot so this cannot erode successive layers of the sprite.
        var original = (Color[])pixels.Clone();
        for (int i = 0; i < pixels.Length; i++)
        {
            var c = original[i];
            if (c.A == 0) continue;
            int x = i % width, y = i / width;
            bool touchesBackground = false;
            Color solid = c;
            int darkest = Math.Min(c.R, Math.Min(c.G, c.B));
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                var n = original[ny * width + nx];
                if (n.A == 0) { touchesBackground = true; continue; }
                int dark = Math.Min(n.R, Math.Min(n.G, n.B));
                if (dark < darkest) { darkest = dark; solid = n; }
            }
            if (touchesBackground && darkest < 230)
            {
                float alpha = Math.Clamp((255f - Math.Min(c.R, Math.Min(c.G, c.B))) / (255f - darkest), 0, 1);
                float oldAlpha = c.A / 255f;
                // Premultiplied RGB after removal of the white background contribution.
                pixels[i] = new Color(
                    (int)Math.Clamp((c.R - 255f * (1 - alpha)) * oldAlpha, 0, 255),
                    (int)Math.Clamp((c.G - 255f * (1 - alpha)) * oldAlpha, 0, 255),
                    (int)Math.Clamp((c.B - 255f * (1 - alpha)) * oldAlpha, 0, 255),
                    (int)(255 * alpha * oldAlpha));
            }
            else pixels[i] = new Color(c.R * c.A / 255, c.G * c.A / 255, c.B * c.A / 255, (int)c.A);
        }
    }
    private void ChooseCutInMob()
    {
        if (_cutInMobs.Length == 0) return;
        int next = _mobRandom.Next(_cutInMobs.Length);
        if (_cutInMobs.Length > 1 && next == _cutInMob)
            next = (next + 1 + _mobRandom.Next(_cutInMobs.Length - 1)) % _cutInMobs.Length;
        _cutInMob = next;
    }

    private void DrawCutInMob(int cardX, float fade, float slide)
    {
        if (_cutInMob < 0) return;
        var texture = _cutInMobs[_cutInMob];
        float scale = 1.5f * Math.Min(680f / texture.Height, 460f / texture.Width);
        var position = new Vector2(cardX + 990 + slide * .12f, 1050);
        _spriteBatch.Draw(texture, position, null, Color.White * fade, 0,
            new Vector2(texture.Width / 2f, texture.Height), scale, SpriteEffects.None, 0);
    }

    private void DisposeCutInMobs()
    {
        _setupHost?.Dispose();_setupHost=null;_turnChime?.Dispose();_turnChime=null;
        foreach (var texture in _cutInMobs) texture.Dispose();
        _cutInMobs = Array.Empty<Texture2D>();
    }
}
