using UnityEngine;

namespace HorizonFaser.UI
{
    public static class UISpriteFactory
    {
        private static Sprite _roundedSprite;
        private static Sprite _pillSprite;

        public static Sprite GetRoundedSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;
            int size = 64;
            int r = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Color transparent = new Color(1, 1, 1, 0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < r && y < r && Vector2.Distance(new Vector2(x, y), new Vector2(r - 1, r - 1)) > r)
                        pixels[y * size + x] = transparent;
                    else if (x > size - r && y < r && Vector2.Distance(new Vector2(x, y), new Vector2(size - r, r - 1)) > r)
                        pixels[y * size + x] = transparent;
                    else if (x < r && y > size - r && Vector2.Distance(new Vector2(x, y), new Vector2(r - 1, size - r)) > r)
                        pixels[y * size + x] = transparent;
                    else if (x > size - r && y > size - r && Vector2.Distance(new Vector2(x, y), new Vector2(size - r, size - r)) > r)
                        pixels[y * size + x] = transparent;
                    else
                        pixels[y * size + x] = Color.white;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return _roundedSprite;
        }

        public static Sprite GetPillSprite()
        {
            if (_pillSprite != null) return _pillSprite;
            int size = 64;
            int r = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Color transparent = new Color(1, 1, 1, 0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < r && y < r && Vector2.Distance(new Vector2(x, y), new Vector2(r - 1, r - 1)) > r)
                        pixels[y * size + x] = transparent;
                    else if (x > size - r && y < r && Vector2.Distance(new Vector2(x, y), new Vector2(size - r, r - 1)) > r)
                        pixels[y * size + x] = transparent;
                    else if (x < r && y > size - r && Vector2.Distance(new Vector2(x, y), new Vector2(r - 1, size - r)) > r)
                        pixels[y * size + x] = transparent;
                    else if (x > size - r && y > size - r && Vector2.Distance(new Vector2(x, y), new Vector2(size - r, size - r)) > r)
                        pixels[y * size + x] = transparent;
                    else
                        pixels[y * size + x] = Color.white;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            _pillSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return _pillSprite;
        }
    }
}
