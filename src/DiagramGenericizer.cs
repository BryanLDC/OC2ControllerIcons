using System;
using System.Collections.Generic;
using UnityEngine;

namespace OC2ControllerIcons
{
    // Controls diagrams (Controls screen, controller settings panels...) are shown with
    // an unbranded controller.
    //
    // No game art is redistributed: at runtime the original Xbox controller sprite is
    // copied, its logo and A/B/X/Y letters are erased and replaced with empty circles
    // (filled where an arrow points at them).
    // Coordinates were measured on the original sprites (top-left origin).
    public static class DiagramGenericizer
    {
        private class Logo
        {
            public float X, Y, R;
            public Logo(float x, float y, float r) { X = x; Y = y; R = r; }
        }

        private class FaceGroup
        {
            public Vector2 Y, B, A, X;
            public string Filled; // letters of the buttons to fill ("XA"...)
            public FaceGroup(float yx, float yy, float bx, float by, float ax, float ay, float xx, float xy, string filled)
            {
                Y = new Vector2(yx, yy); B = new Vector2(bx, by); A = new Vector2(ax, ay); X = new Vector2(xx, xy);
                Filled = filled;
            }
        }

        private class Spec
        {
            public float RefWidth, RefHeight;
            public Logo[] Logos;
            public FaceGroup[] Groups;
        }

        private static readonly Dictionary<string, Spec> s_specs = new Dictionary<string, Spec>();
        // PS4 variants of the same diagram -> replaced by the generic version of the Xbox one.
        private static readonly Dictionary<string, string> s_aliases = new Dictionary<string, string>();

        private static readonly Dictionary<int, Sprite> s_cache = new Dictionary<int, Sprite>(); // original id -> generic (or null)
        private static readonly Dictionary<Sprite, Sprite> s_originals = new Dictionary<Sprite, Sprite>(); // generic -> original

        static DiagramGenericizer()
        {
            Spec controls = new Spec();
            controls.RefWidth = 2048; controls.RefHeight = 1229;
            controls.Logos = new Logo[] { new Logo(544f, 701.5f, 30f), new Logo(1524f, 648f, 23f) };
            controls.Groups = new FaceGroup[]
            {
                new FaceGroup(714.7f, 729.2f, 758.5f, 765f, 709.9f, 802f, 666.3f, 765.8f, ""),
                new FaceGroup(1661.7f, 672.4f, 1697f, 709.1f, 1661.6f, 745.2f, 1626.2f, 709.1f, "XA"),
            };
            s_specs["Controls_XB1_split"] = controls;

            Spec split = new Spec();
            split.RefWidth = 879; split.RefHeight = 435;
            split.Logos = new Logo[] { new Logo(445f, 191.5f, 23f) };
            split.Groups = new FaceGroup[] { new FaceGroup(573f, 206.7f, 605.7f, 232.5f, 569.3f, 258.5f, 536.6f, 232.6f, "") };
            s_specs["UI_ControllerSettingsPanel_02"] = split;

            Spec whole = new Spec();
            whole.RefWidth = 873; whole.RefHeight = 449;
            whole.Logos = new Logo[] { new Logo(440.5f, 129.5f, 16.5f) };
            whole.Groups = new FaceGroup[] { new FaceGroup(542.3f, 147.1f, 568.2f, 172.6f, 542.2f, 198f, 516.2f, 172.5f, "") };
            s_specs["UI_ControllerSettingsPanel_06"] = whole;

            s_aliases["Controls_PS4_split"] = "Controls_XB1_split";
            s_aliases["UI_ControllerSettingsPanel_01"] = "UI_ControllerSettingsPanel_02";
            s_aliases["UI_ControllerSettingsPanel_05"] = "UI_ControllerSettingsPanel_06";
        }

        public static bool IsGenerated(Sprite sprite)
        {
            return (object)sprite != null && s_originals.ContainsKey(sprite);
        }

        public static Sprite GetOriginal(Sprite generic)
        {
            Sprite original;
            return s_originals.TryGetValue(generic, out original) ? original : generic;
        }

        // Returns the generic replacement, or the same sprite if it is not a known diagram.
        public static Sprite Map(Sprite sprite)
        {
            if (sprite == null) return sprite;
            int id = sprite.GetInstanceID();
            Sprite cached;
            if (s_cache.TryGetValue(id, out cached)) return cached != null ? cached : sprite;

            Sprite result = null;
            string name = sprite.name;
            try
            {
                Spec spec;
                string alias;
                if (s_specs.TryGetValue(name, out spec))
                {
                    result = Build(sprite, spec);
                }
                else if (s_aliases.TryGetValue(name, out alias))
                {
                    Sprite xbox = FindLoadedSprite(alias);
                    if (xbox != null) result = Map(xbox);
                    if (result == xbox) result = null;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not generate generic diagram for " + name + ": " + ex.Message);
                result = null;
            }
            s_cache[id] = result;
            if (result != null) s_originals[result] = sprite;
            return result != null ? result : sprite;
        }

        private static Sprite FindLoadedSprite(string name)
        {
            Sprite[] all = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name) return all[i];
            }
            return null;
        }

        // --------------------------------------------------------------- generation

        private static Sprite Build(Sprite source, Spec spec)
        {
            if (source.packed && source.packingRotation != SpritePackingRotation.None) return null;

            Rect tr = source.textureRect;
            int w = Mathf.RoundToInt(tr.width), h = Mathf.RoundToInt(tr.height);
            Texture2D copy = ReadPixels(source.texture, tr, w, h);
            Color32[] px = copy.GetPixels32();

            float sx = w / spec.RefWidth, sy = h / spec.RefHeight;
            PixelGrid c = new PixelGrid(px, w, h);
            for (int i = 0; i < spec.Logos.Length; i++)
            {
                Logo l = spec.Logos[i];
                EraseLogo(c, l.X * sx, l.Y * sy, l.R * sx);
            }
            for (int i = 0; i < spec.Groups.Length; i++) DrawFaceGroup(c, spec.Groups[i], sx, sy);

            copy.SetPixels32(px);
            copy.Apply(true, true); // upload to GPU and free the CPU copy
            copy.hideFlags = HideFlags.HideAndDontSave;

            Vector2 pivot = new Vector2(source.pivot.x / source.rect.width, source.pivot.y / source.rect.height);
            Sprite sprite = Sprite.Create(copy, new Rect(0, 0, w, h), pivot, source.pixelsPerUnit, 0,
                                          SpriteMeshType.FullRect, source.border);
            sprite.name = source.name + "_OC2CI";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Plugin.Log.LogInfo("Generic diagram created: " + source.name + " (" + w + "x" + h + ")");
            return sprite;
        }

        // Game textures are not CPU-readable: copy them through the GPU.
        private static Texture2D ReadPixels(Texture2D tex, Rect rect, int w, int h)
        {
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, true);
                copy.ReadPixels(new Rect(rect.x, rect.y, w, h), 0, 0, false);
                copy.Apply(false, false);
                copy.wrapMode = TextureWrapMode.Clamp;
                copy.filterMode = tex.filterMode;
                return copy;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        // Pixel access using image coordinates (y grows downwards).
        private class PixelGrid
        {
            private readonly Color32[] m_px;
            public readonly int W, H;
            public PixelGrid(Color32[] px, int w, int h) { m_px = px; W = w; H = h; }

            public Color Get(int x, int y)
            {
                x = Mathf.Clamp(x, 0, W - 1); y = Mathf.Clamp(y, 0, H - 1);
                return m_px[(H - 1 - y) * W + x];
            }

            public void Blend(int x, int y, Color c, float a)
            {
                if (a <= 0f || x < 0 || y < 0 || x >= W || y >= H) return;
                int i = (H - 1 - y) * W + x;
                Color dst = m_px[i];
                Color outc = Color.Lerp(dst, c, Mathf.Clamp01(a));
                outc.a = Mathf.Max(dst.a, outc.a);
                m_px[i] = outc;
            }
        }

        private static float Coverage(float dist, float radius)
        {
            return Mathf.Clamp01(radius - dist + 0.5f);
        }

        private static float Luma(Color c)
        {
            return (c.r + c.g + c.b) / 3f;
        }

        // Fills the logo disc by interpolating the controller body colour horizontally.
        // If a light line (split-controller divider) crosses it, the line is rebuilt.
        private static void EraseLogo(PixelGrid c, float cx, float cy, float r)
        {
            float R = r + 3f;
            int top = Mathf.FloorToInt(cy - R - 3), bot = Mathf.FloorToInt(cy + R + 3);
            int lx = Mathf.FloorToInt(cx - R - 3), rx = Mathf.FloorToInt(cx + R + 3);

            float tc = 0, tw = 0, bc = 0, bw = 0;
            Color ct = Color.white, cb = Color.white;
            bool stripe = FindSingleLightRun(c, top, lx, rx, out tc, out tw) && FindSingleLightRun(c, bot, lx, rx, out bc, out bw);
            if (stripe)
            {
                ct = c.Get(Mathf.RoundToInt(tc), top);
                cb = c.Get(Mathf.RoundToInt(bc), bot);
            }

            for (int y = top + 1; y < bot; y++)
            {
                Color cl = c.Get(lx, y), cr = c.Get(rx, y);
                float ty = (y - top) / (float)(bot - top);
                for (int x = lx + 1; x < rx; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Coverage(d, R);
                    if (a <= 0f) continue;
                    Color col = Color.Lerp(cl, cr, (x - lx) / (float)(rx - lx));
                    if (stripe)
                    {
                        float sc = Mathf.Lerp(tc, bc, ty), sw = Mathf.Lerp(tw, bw, ty);
                        float wgt = Mathf.Clamp01(sw - Mathf.Abs(x - sc) + 0.5f);
                        if (wgt > 0f) col = Color.Lerp(col, Color.Lerp(ct, cb, ty), wgt);
                    }
                    c.Blend(x, y, col, a);
                }
            }
        }

        private static bool FindSingleLightRun(PixelGrid c, int y, int lx, int rx, out float center, out float halfWidth)
        {
            center = 0; halfWidth = 0;
            int runs = 0, start = -1;
            for (int x = lx; x <= rx + 1; x++)
            {
                bool light = x <= rx && Luma(c.Get(x, y)) > 140f / 255f;
                if (light && start < 0) start = x;
                if (!light && start >= 0)
                {
                    runs++;
                    center = (start + x - 1) / 2f;
                    halfWidth = (x - start) / 2f;
                    start = -1;
                }
            }
            return runs == 1;
        }

        private static void DrawFaceGroup(PixelGrid c, FaceGroup g, float sx, float sy)
        {
            Vector2[] pts = { Scale(g.Y, sx, sy), Scale(g.B, sx, sy), Scale(g.A, sx, sy), Scale(g.X, sx, sy) };
            string[] keys = { "Y", "B", "A", "X" };
            Vector2 center = (pts[0] + pts[1] + pts[2] + pts[3]) / 4f;
            float off = 0f;
            for (int i = 0; i < 4; i++) off += Vector2.Distance(pts[i], center);
            off /= 4f;
            float r = 0.56f * off;
            float ringR = 0.58f * r, stroke = 0.17f * r;
            Color white = new Color(240f / 255f, 240f / 255f, 240f / 255f, 1f);

            for (int i = 0; i < 4; i++)
            {
                float bx = pts[i].x, by = pts[i].y;
                Color disc = SampleDisc(c, bx, by, r);
                bool filled = g.Filled.IndexOf(keys[i], StringComparison.Ordinal) >= 0;
                int x0 = Mathf.FloorToInt(bx - r - 2), x1 = Mathf.CeilToInt(bx + r + 2);
                int y0 = Mathf.FloorToInt(by - r - 2), y1 = Mathf.CeilToInt(by + r + 2);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        float d = Mathf.Sqrt((x - bx) * (x - bx) + (y - by) * (y - by));
                        c.Blend(x, y, disc, Coverage(d, r - 0.5f));
                        float wgt = filled
                            ? Coverage(d, ringR)
                            : Mathf.Min(Coverage(d, ringR + stroke / 2f), 1f - Coverage(d, ringR - stroke / 2f));
                        c.Blend(x, y, white, wgt);
                    }
                }
            }
        }

        private static Vector2 Scale(Vector2 v, float sx, float sy)
        {
            return new Vector2(v.x * sx, v.y * sy);
        }

        // Button colour: average of the darkest samples on the outer ring (avoids the letter).
        private static Color SampleDisc(PixelGrid c, float bx, float by, float r)
        {
            List<Color> samples = new List<Color>();
            for (int i = 0; i < 12; i++)
            {
                float ang = i * Mathf.PI * 2f / 12f;
                samples.Add(c.Get(Mathf.RoundToInt(bx + Mathf.Cos(ang) * r * 0.8f), Mathf.RoundToInt(by + Mathf.Sin(ang) * r * 0.8f)));
            }
            samples.Sort(delegate(Color a, Color b) { return Luma(a).CompareTo(Luma(b)); });
            Color sum = Color.clear;
            for (int i = 0; i < 4; i++) sum += samples[i];
            Color result = sum / 4f;
            result.a = 1f;
            return result;
        }
    }
}
