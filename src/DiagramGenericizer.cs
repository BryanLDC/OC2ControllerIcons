using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OC2ControllerIcons
{
    // Controls diagrams (Controls screen, controller settings panels...) are shown with
    // an unbranded controller, or with the game's own Xbox / PlayStation diagram when every
    // joined player uses that layout.
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
        private static readonly Dictionary<string, string> s_psVersion = new Dictionary<string, string>(); // xbox -> ps
        private static readonly Dictionary<string, string> s_xboxOfPs = new Dictionary<string, string>();  // ps -> xbox

        private const string GeneratedSuffix = "_OC2CI";
        private static readonly Dictionary<int, string> s_familyById = new Dictionary<int, string>(); // sprite id -> xbox name (or null)
        private static readonly Dictionary<string, Sprite> s_byName = new Dictionary<string, Sprite>();
        private static readonly Dictionary<int, Sprite> s_generic = new Dictionary<int, Sprite>();  // xbox id -> generic (or null)
        private static readonly Dictionary<int, Sprite> s_nintendo = new Dictionary<int, Sprite>(); // xbox id -> Nintendo (or null)

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

            // PlayStation version of each Xbox diagram (shipped in the PC build).
            s_psVersion["Controls_XB1_split"] = "Controls_PS4_split";
            s_psVersion["UI_ControllerSettingsPanel_02"] = "UI_ControllerSettingsPanel_01";
            s_psVersion["UI_ControllerSettingsPanel_06"] = "UI_ControllerSettingsPanel_05";
            foreach (KeyValuePair<string, string> kv in s_psVersion) s_xboxOfPs[kv.Value] = kv.Key;
        }

        // Picks the diagram to show for the current settings, or returns the same sprite if it
        // is not a known controller diagram:
        //  - mod disabled, or every joined player on Xbox -> the game's Xbox diagram;
        //  - every joined player on PlayStation           -> the game's PlayStation diagram;
        //  - every joined player on Nintendo -> unbranded diagram with the game's Nintendo button
        //    icons on the face buttons (the game ships no Nintendo diagram);
        //  - mixed layouts or Generic -> unbranded diagram.
        public static Sprite Resolve(Sprite sprite)
        {
            if (sprite == null) return sprite;
            string xboxName = FamilyOf(sprite);
            if (xboxName == null) return sprite;
            try
            {
                Sprite xbox = GetSprite(xboxName);
                if (xbox == null) return sprite;
                if (!ModSettings.Enabled) return xbox;

                PadLayout uniform;
                if (ModSettings.TryGetUniformLayout(out uniform))
                {
                    if (uniform == PadLayout.Xbox) return xbox;
                    if (uniform == PadLayout.PlayStation)
                    {
                        Sprite ps = GetSprite(s_psVersion[xboxName]);
                        if (ps != null) return ps;
                    }
                    if (uniform == PadLayout.Nintendo)
                    {
                        Sprite nx = Nintendo(xbox);
                        if (nx != null) return nx;
                    }
                }
                Sprite generic = Generic(xbox);
                return generic != null ? generic : xbox;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Diagram selection failed for " + sprite.name + ": " + ex.Message);
                return sprite;
            }
        }

        // Xbox diagram name of the family this sprite belongs to (original, PS or generated), or null.
        private static string FamilyOf(Sprite sprite)
        {
            int id = sprite.GetInstanceID();
            string family;
            if (s_familyById.TryGetValue(id, out family)) return family;

            string name = sprite.name;
            int cut = name.IndexOf(GeneratedSuffix, StringComparison.Ordinal);
            bool generated = cut >= 0;
            if (generated) name = name.Substring(0, cut);
            if (s_specs.ContainsKey(name)) family = name;
            else if (!s_xboxOfPs.TryGetValue(name, out family)) family = null;

            if (family != null && !generated) s_byName[name] = sprite;
            s_familyById[id] = family;
            return family;
        }

        private static Sprite GetSprite(string name)
        {
            Sprite sprite;
            if (s_byName.TryGetValue(name, out sprite) && sprite != null) return sprite;
            sprite = FindLoadedSprite(name);
            if (sprite == null) sprite = LoadFromGameBundles(name);
            if (sprite != null)
            {
                s_byName[name] = sprite;
                s_familyById[sprite.GetInstanceID()] = s_specs.ContainsKey(name) ? name : s_xboxOfPs[name];
            }
            return sprite;
        }

        private static Sprite Generic(Sprite xbox)
        {
            int id = xbox.GetInstanceID();
            Sprite generic;
            if (s_generic.TryGetValue(id, out generic)) return generic;
            generic = null;
            try
            {
                generic = Build(xbox, s_specs[xbox.name], null, "");
                if (generic != null) s_familyById[generic.GetInstanceID()] = xbox.name;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not generate generic diagram for " + xbox.name + ": " + ex.Message);
            }
            s_generic[id] = generic;
            return generic;
        }

        private static Sprite Nintendo(Sprite xbox)
        {
            int id = xbox.GetInstanceID();
            Sprite nx;
            if (s_nintendo.TryGetValue(id, out nx)) return nx;
            nx = null;
            try
            {
                // Face buttons in diagram order: north, east, south, west (Xbox Y, B, A, X).
                ControllerIconLookup lookup = GameUtils.RequestManager<ControllerIconLookup>();
                ControlPadInput.Button[] order = { ControlPadInput.Button.Y, ControlPadInput.Button.B, ControlPadInput.Button.A, ControlPadInput.Button.X };
                IconImage[] icons = new IconImage[4];
                for (int i = 0; i < 4; i++)
                {
                    Sprite icon = IconLibrary.GetForLayout(lookup, order[i], ControllerIconLookup.IconContext.Borderless, PadLayout.Nintendo);
                    if (icon == null) throw new Exception("Nintendo icon missing for " + order[i]);
                    icons[i] = IconImage.From(icon);
                }
                nx = Build(xbox, s_specs[xbox.name], icons, "_NX");
                if (nx != null) s_familyById[nx.GetInstanceID()] = xbox.name;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not generate Nintendo diagram for " + xbox.name + ": " + ex.Message);
            }
            s_nintendo[id] = nx;
            return nx;
        }

        // CPU copy of a (GPU-only) sprite, used to stamp button icons onto diagrams.
        private class IconImage
        {
            public Color32[] Px;
            public int W, H;

            public static IconImage From(Sprite sprite)
            {
                Rect r = sprite.textureRect;
                int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
                Texture2D copy = ReadPixels(sprite.texture, r, w, h);
                IconImage img = new IconImage();
                img.Px = copy.GetPixels32();
                img.W = w;
                img.H = h;
                UnityEngine.Object.Destroy(copy);
                return img;
            }

            // Bilinear sample with u, v in [0,1], v growing downwards.
            public Color Sample(float u, float v)
            {
                float fx = Mathf.Clamp(u * W - 0.5f, 0, W - 1), fy = Mathf.Clamp((1f - v) * H - 0.5f, 0, H - 1);
                int x0 = (int)fx, y0 = (int)fy, x1 = Mathf.Min(x0 + 1, W - 1), y1 = Mathf.Min(y0 + 1, H - 1);
                float tx = fx - x0, ty = fy - y0;
                Color a = Color.Lerp(Px[y0 * W + x0], Px[y0 * W + x1], tx);
                Color b = Color.Lerp(Px[y1 * W + x0], Px[y1 * W + x1], tx);
                return Color.Lerp(a, b, ty);
            }
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

        // The PlayStation diagrams are not used by the PC build, so they may not be loaded yet:
        // look them up in the asset bundles the game already has open.
        private static Sprite LoadFromGameBundles(string name)
        {
            FieldInfo field = typeof(AssetBundles.AssetBundleManager).GetField("m_LoadedAssetBundles",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            IDictionary bundles = field != null ? field.GetValue(null) as IDictionary : null;
            if (bundles == null) return null;
            foreach (object value in bundles.Values)
            {
                AssetBundles.LoadedAssetBundle loaded = value as AssetBundles.LoadedAssetBundle;
                if (loaded == null || loaded.m_AssetBundle == null) continue;
                try
                {
                    Sprite sprite = loaded.m_AssetBundle.LoadAsset<Sprite>(name);
                    if (sprite != null) return sprite;
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        // --------------------------------------------------------------- generation

        private static Sprite Build(Sprite source, Spec spec, IconImage[] faceIcons, string variant)
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
            for (int i = 0; i < spec.Groups.Length; i++) DrawFaceGroup(c, spec.Groups[i], sx, sy, faceIcons);

            copy.SetPixels32(px);
            copy.Apply(true, true); // upload to GPU and free the CPU copy
            copy.hideFlags = HideFlags.HideAndDontSave;

            Vector2 pivot = new Vector2(source.pivot.x / source.rect.width, source.pivot.y / source.rect.height);
            Sprite sprite = Sprite.Create(copy, new Rect(0, 0, w, h), pivot, source.pixelsPerUnit, 0,
                                          SpriteMeshType.FullRect, source.border);
            sprite.name = source.name + GeneratedSuffix + variant;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Plugin.Log.LogInfo("Diagram created: " + source.name + (variant.Length > 0 ? " (" + variant.Substring(1) + ")" : " (generic)") + " " + w + "x" + h);
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

        // faceIcons == null: empty / filled circles. Otherwise the icon for each face button
        // (north, east, south, west) is stamped over the erased letter.
        private static void DrawFaceGroup(PixelGrid c, FaceGroup g, float sx, float sy, IconImage[] faceIcons)
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
                        if (faceIcons != null)
                        {
                            float size = r * 2.3f;
                            float u = (x - bx) / size + 0.5f, v = (y - by) / size + 0.5f;
                            if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                            Color ic = faceIcons[i].Sample(u, v);
                            c.Blend(x, y, ic, ic.a);
                            continue;
                        }
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
