using System.Collections.Generic;
using UnityEngine;

internal sealed class TitleDungeonArt
{
    private readonly List<Texture2D> textures = new();

    public Texture2D Wall() => Paint(256, 192, (x, y) =>
    {
        int row = y / 12;
        int brickX = x + (row % 2) * 12;
        int grain = ((x * 17 ^ y * 37) & 7) / 3;
        int light = ((brickX / 24 * 13 ^ row * 7) & 7) + grain;
        if (y % 12 == 0 || brickX % 24 == 0) light = -8;
        else if (y % 12 == 11 || brickX % 24 == 1) light += 5;

        float nx = (x - 128f) / 128f;
        float ny = (y - 96f) / 96f;
        int shade = Mathf.RoundToInt((nx * nx + ny * ny) * 14f);
        return new Color32((byte)Mathf.Max(6, 34 + light - shade),
            (byte)Mathf.Max(8, 38 + light - shade), (byte)Mathf.Max(15, 52 + light - shade), 255);
    });

    public Texture2D Arch() => Paint(104, 112, (x, y) =>
    {
        int dx = Mathf.Abs(x - 52);
        float dy = Mathf.Max(0, y - 56);
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        if (distance >= 51) return Color.clear;
        if (distance >= 49 || y < 3) return new Color32(13, 16, 25, 255);
        if (distance >= 38)
        {
            int joint = y <= 56 ? y % 12 : (int)(Mathf.Atan2(dy, dx) * 10) % 3;
            if (joint == 0 || distance < 39) return new Color32(34, 35, 44, 255);
            if (y > 94 && dx < 8) return new Color32(119, 97, 76, 255);
            return new Color32(84, 85, 94, 255);
        }
        return new Color32(12, 16, 25, 255);
    });

    public Texture2D Grille() => Paint(76, 88, (x, y) =>
    {
        float dx = x - 38;
        float dy = Mathf.Max(0, y - 50);
        if (dx * dx + dy * dy >= 38 * 38 || y < 2) return Color.clear;
        if (x % 10 == 1 || y % 23 == 9) return new Color32(114, 96, 77, 255);
        if (x % 10 < 5 || y % 23 is >= 10 and <= 12) return new Color32(55, 56, 63, 255);
        return Color.clear;
    });

    public Texture2D Glow() => Paint(32, 32, (x, y) =>
    {
        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
        float light = Mathf.Clamp01(1f - distance / 15.5f);
        return new Color(1f, 0.46f, 0.16f, Mathf.Floor(light * light * 8f) / 16f);
    });

    public Texture2D Icon(params string[] rows) => Paint(rows[0].Length, rows.Length,
        (x, y) => rows[rows.Length - 1 - y][x] == '#' ? Color.white : Color.clear);

    public void Dispose()
    {
        foreach (Texture2D texture in textures) Object.Destroy(texture);
    }

    private Texture2D Paint(int width, int height, System.Func<int, int, Color> color)
    {
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = color(x, y);
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        textures.Add(texture);
        return texture;
    }
}

