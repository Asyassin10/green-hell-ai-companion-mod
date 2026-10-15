using System;
using UnityEngine;

namespace GreenHellAI
{
    // Original procedural artwork: no game textures or external image files are used.
    // Construct and dispose only on Unity's main thread.
    internal sealed class NotebookSkin : IDisposable
    {
        internal static readonly Color Ink = new Color(0.22f, 0.20f, 0.15f);
        internal static readonly Color Faded = new Color(0.39f, 0.35f, 0.25f);
        internal static readonly Color Green = new Color(0.24f, 0.30f, 0.20f);
        private readonly Texture2D _paper;
        private readonly Texture2D _field;
        private readonly Texture2D _button;
        private readonly Texture2D _hover;
        private readonly Font _handwriting;
        private int _size;
        internal GUIStyle Body, Small, Title, Heading, Field, Button, Window, History;

        internal NotebookSkin()
        {
            _paper = Paper();
            _field = Solid(new Color(0.81f, 0.75f, 0.60f, 0.30f));
            _button = Solid(new Color(0.28f, 0.34f, 0.23f));
            _hover = Solid(new Color(0.36f, 0.42f, 0.28f));
            try { _handwriting = Font.CreateDynamicFontFromOSFont(new[] { "Segoe Print", "Georgia", "Arial" }, 20); }
            catch { _handwriting = null; } // The built-in font remains a usable fallback.
        }

        internal void Configure(int size)
        {
            if (Body != null && _size == size) return;
            _size = size;
            Body = Label(size, Ink, FontStyle.Normal);
            History = Label(size, Ink, FontStyle.Normal);
            History.richText = true;
            Small = Label(Math.Max(11, size - 2), Faded, FontStyle.Normal);
            Title = Label(size + 11, Ink, FontStyle.Bold);
            Heading = Label(size + 2, Green, FontStyle.Bold);
            Field = new GUIStyle(GUI.skin.textArea) {
                font = _handwriting != null ? _handwriting : GUI.skin.font,
                fontSize = size, wordWrap = true, richText = false,
                padding = new RectOffset(10, 10, 6, 6), border = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.UpperLeft
            };
            foreach (GUIStyleState state in new[] { Field.normal, Field.hover, Field.active, Field.focused })
            { state.background = _field; state.textColor = Ink; }
            Button = new GUIStyle(GUI.skin.button) {
                font = _handwriting != null ? _handwriting : GUI.skin.font,
                fontSize = size, richText = false, border = new RectOffset(0, 0, 0, 0)
            };
            foreach (GUIStyleState state in new[] { Button.normal, Button.active, Button.focused })
            { state.background = _button; state.textColor = new Color(0.94f, 0.90f, 0.77f); }
            Button.hover.background = _hover;
            Button.hover.textColor = Button.normal.textColor;
            Window = new GUIStyle { padding = new RectOffset(0, 0, 0, 0) };
        }

        private GUIStyle Label(int size, Color color, FontStyle fontStyle)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) {
                font = _handwriting != null ? _handwriting : GUI.skin.font,
                fontSize = size, fontStyle = fontStyle, richText = false, wordWrap = true,
                padding = new RectOffset(0, 0, 0, 0)
            };
            style.normal.textColor = color;
            return style;
        }

        internal void DrawBook(float width, float height)
        {
            float center = width / 2;
            Fill(new Rect(11, 14, width - 16, height - 17), new Color(0, 0, 0, 0.32f));
            Fill(new Rect(3, 5, width - 12, height - 14), new Color(0.24f, 0.19f, 0.13f));
            PaperRect(new Rect(9, 7, center - 12, height - 22), new Color(0.98f, 0.95f, 0.86f));
            PaperRect(new Rect(center + 3, 10, center - 15, height - 22), new Color(1, 0.98f, 0.90f));
            for (int i = 0; i < 4; i++)
            {
                Line(10, height - 18 + i, center - 4, height - 20 + i, new Color(0.36f, 0.29f, 0.19f, 0.34f), 1);
                Line(center + 4, height - 20 + i, width - 13, height - 16 + i, new Color(0.36f, 0.29f, 0.19f, 0.34f), 1);
            }
            // Soft gutter, page folds and metal binding rings.
            for (int i = 0; i < 17; i++)
                Fill(new Rect(center - 17 + i * 2, 10, 2, height - 30),
                    new Color(0.22f, 0.18f, 0.11f, 0.015f + 0.14f * (1 - Math.Abs(i - 8) / 8f)));
            Fill(new Rect(center - 3, 9, 6, height - 25), new Color(0.16f, 0.14f, 0.10f, 0.70f));
            int rings = Math.Max(8, (int)(height / 38));
            for (int i = 0; i < rings; i++)
            {
                float y = 30 + i * (height - 70) / (rings - 1);
                Fill(new Rect(center - 15, y - 2, 5, 6), new Color(0.20f, 0.17f, 0.12f, 0.65f));
                Fill(new Rect(center + 10, y - 2, 5, 6), new Color(0.20f, 0.17f, 0.12f, 0.65f));
                Arc(center, y + 2, 15, 6, new Color(0.13f, 0.14f, 0.12f), 3);
                Arc(center, y, 15, 5, new Color(0.49f, 0.48f, 0.35f), 1);
            }
            // Worn paper tape in opposite corners.
            Fill(new Rect(8, 28, 25, 32), new Color(0.77f, 0.74f, 0.62f, 0.55f));
            Fill(new Rect(width - 38, height - 62, 26, 34), new Color(0.77f, 0.74f, 0.62f, 0.55f));
        }

        internal static void Rule(Rect rect, float spacing)
        {
            for (float y = rect.y + spacing; y < rect.yMax; y += spacing)
                Line(rect.x, y, rect.xMax, y, new Color(0.40f, 0.34f, 0.22f, 0.15f), 1);
        }

        internal static void LeafSketch(Rect rect)
        {
            Color pencil = new Color(0.28f, 0.32f, 0.22f, 0.40f);
            float x = rect.x + rect.width * 0.25f;
            float y = rect.yMax;
            float topX = rect.x + rect.width * 0.72f;
            Line(x, y, topX, rect.y, pencil, 1);
            for (int i = 1; i <= 5; i++)
            {
                float t = i / 7f;
                float stemX = x + (topX - x) * t;
                float stemY = y - rect.height * t;
                float side = i % 2 == 0 ? 1 : -1;
                float tipX = stemX + side * rect.width * 0.25f;
                float tipY = stemY - rect.height * 0.23f;
                Line(stemX, stemY, tipX, tipY, pencil, 1);
                Line(stemX, stemY, tipX + side * 7, tipY + 16, pencil, 1);
                Line(tipX + side * 7, tipY + 16, tipX, tipY, pencil, 1);
            }
        }

        private void PaperRect(Rect rect, Color tint)
        {
            Color old = GUI.color;
            try { GUI.color = old * tint; GUI.DrawTexture(rect, _paper); }
            finally { GUI.color = old; }
        }
        internal static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            try { GUI.color = old * color; GUI.DrawTexture(rect, Texture2D.whiteTexture); }
            finally { GUI.color = old; }
        }
        internal static void Line(float x1, float y1, float x2, float y2, Color color, float thickness)
        {
            Matrix4x4 old = GUI.matrix;
            float dx = x2 - x1, dy = y2 - y1;
            try
            {
                GUIUtility.RotateAroundPivot((float)(Math.Atan2(dy, dx) * 180 / Math.PI), new Vector2(x1, y1));
                Fill(new Rect(x1, y1, (float)Math.Sqrt(dx * dx + dy * dy), thickness), color);
            }
            finally { GUI.matrix = old; }
        }
        private static void Arc(float x, float y, float rx, float ry, Color color, float thickness)
        {
            for (int i = 0; i < 14; i++)
            {
                double a = Math.PI + i * Math.PI / 14, b = Math.PI + (i + 1) * Math.PI / 14;
                Line(x + (float)Math.Cos(a) * rx, y + (float)Math.Sin(a) * ry,
                    x + (float)Math.Cos(b) * rx, y + (float)Math.Sin(b) * ry, color, thickness);
            }
        }
        private static Texture2D Solid(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, color); texture.Apply(false, true);
            return texture;
        }
        private static Texture2D Paper()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[size * size];
            System.Random noise = new System.Random(6137); // Does not consume Unity's game RNG.
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float edge = Math.Min(Math.Min(x, size - 1 - x), Math.Min(y, size - 1 - y)) / 128f;
                    double cloud = Math.Sin(x * 0.045) * Math.Cos(y * 0.036) * 3;
                    int shade = (int)(13 * (1 - Math.Min(1, edge * 7)) + noise.Next(0, 7) + cloud);
                    pixels[y * size + x] = new Color32((byte)(225 - shade), (byte)(211 - shade), (byte)(178 - shade), 255);
                }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return texture;
        }
        public void Dispose()
        {
            UnityEngine.Object.Destroy(_paper); UnityEngine.Object.Destroy(_field);
            UnityEngine.Object.Destroy(_button); UnityEngine.Object.Destroy(_hover);
            if (_handwriting != null) UnityEngine.Object.Destroy(_handwriting);
        }
    }
}
