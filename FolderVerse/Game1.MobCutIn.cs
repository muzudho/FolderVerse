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
            RemoveMobBackdrop(pixels, texture.Width, texture.Height);
            texture.SetData(pixels);
            textures.Add(texture);
        }
        _cutInMobs = textures.ToArray();
    }

    // Only the near-white region connected to the canvas edge is background.
    // Enclosed white clothing stays opaque; the source PNG is never modified.
    private static void RemoveMobBackdrop(Color[] pixels, int width, int height)
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
        for (int i = 0; i < pixels.Length; i++)
        {
            var c = pixels[i];
            pixels[i] = new Color(c.R * c.A / 255, c.G * c.A / 255, c.B * c.A / 255, (int)c.A);
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
        float scale = Math.Min(680f / texture.Height, 460f / texture.Width);
        var position = new Vector2(cardX + 900 + slide * .12f, 840);
        _spriteBatch.Draw(texture, position, null, Color.White * fade, 0,
            new Vector2(texture.Width / 2f, texture.Height), scale, SpriteEffects.None, 0);
    }

    private void DisposeCutInMobs()
    {
        foreach (var texture in _cutInMobs) texture.Dispose();
        _cutInMobs = Array.Empty<Texture2D>();
    }
}