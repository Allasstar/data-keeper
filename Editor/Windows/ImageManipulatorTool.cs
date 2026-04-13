using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  ImageManipulatorTool  –  Tools/Image Manipulator
//  Tabs: Single | Batch | Color Adjust | Channel Extract | Channel Import (ORM)
// ─────────────────────────────────────────────────────────────────────────────
namespace DataKeeper.Editor.Windows
{
    public class ImageManipulatorTool : EditorWindow
    {
        // ── Tab ──────────────────────────────────────────────────────────────────
        private enum Tab
        {
            Single,
            Batch,
            ColorAdjust,
            ChannelExtract,
            ChannelImport
        }

        private Tab activeTab = Tab.Single;

        private readonly string[] tabLabels =
            { "Single", "Batch", "Color Adjust", "Ch. Extract", "Ch. Import (ORM)" };

        // ── Single-image state ────────────────────────────────────────────────────
        private Texture2D sourceTexture;
        private Texture2D previewTexture;
        private string assetPath;

        private int resizeWidth = 512;
        private int resizeHeight = 512;
        private bool maintainAspectRatio = true;
        private float rotationAngle = 0f;
        private bool flipHorizontal = false;
        private bool flipVertical = false;
        private readonly float[] rotationPresets = { 90f, 180f, 270f };

        // ── Color-adjust state ────────────────────────────────────────────────────
        private float brightness = 0f; // -1 … +1
        private float contrast = 0f; // -1 … +1
        private float saturation = 1f; //  0 … 2  (1 = neutral)
        private bool grayscale = false;
        private bool tintEnabled = false;
        private Color tintColor = Color.white;

        // ── Batch state ───────────────────────────────────────────────────────────
        private List<Texture2D> batchTextures = new List<Texture2D>();
        private bool batchFlipH = false;
        private bool batchFlipV = false;
        private float batchRotation = 0f;
        private bool batchResize = false;
        private int batchResizeW = 512;
        private int batchResizeH = 512;
        private bool batchMaintainAspect = true;
        private bool batchApplyColor = false;
        private bool batchOverwrite = false;
        private string batchSuffix = "_edited";
        private Vector2 batchScroll;

        // ── Channel-extract state ─────────────────────────────────────────────────
        private Texture2D extractSource;
        private string extractAssetPath;
        private bool extractR = true, extractG = true, extractB = true, extractA = false;
        private Texture2D prevR, prevG, prevB, prevA;

        // ── Channel-import (ORM) state ────────────────────────────────────────────
        private Texture2D ormR, ormG, ormB;
        private string ormRPath = "", ormGPath = "", ormBPath = "";
        private bool ormInvertR, ormInvertG, ormInvertB;
        private Texture2D ormPreview;
        private string ormOutputPath = "Assets";
        private string ormOutputName = "ORM_Packed";

        // ── isReadable restore tracking ───────────────────────────────────────────
        // Maps asset path → original isReadable value before the tool changed it.
        private readonly Dictionary<string, bool> _originalReadability = new Dictionary<string, bool>();

        // ── UI ────────────────────────────────────────────────────────────────────
        private Vector2 scrollPos;
        private const float PREVIEW_MAX = 280f;
        private const float SMALL_PREV = 110f;
        private GUIStyle headerStyle;
        private GUIStyle sectionBox;

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Tools/Windows/Image Manipulator", priority = 14)]
        public static void Open()
        {
            var w = GetWindow<ImageManipulatorTool>("Image Manipulator");
            w.minSize = new Vector2(580f, 680f);
        }

        private void InitStyles()
        {
            if (headerStyle != null) return;
            headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, margin = new RectOffset(0, 0, 6, 6) };
            sectionBox = new GUIStyle(GUI.skin.box)
                { padding = new RectOffset(10, 10, 8, 8), margin = new RectOffset(0, 0, 4, 4) };
        }

        private void OnGUI()
        {
            InitStyles();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space(6);
            GUILayout.Label("🖼  Image Manipulator", headerStyle);

            if (_originalReadability.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "One or more textures have \"Read/Write Enabled\" turned ON by this tool so pixels can be read.\n" +
                    "This increases memory usage at runtime. Click \"Finish Edit\" to restore original settings.",
                    MessageType.Warning);
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.75f, 0.3f);
                GUIContent finishContent = new GUIContent(
                    "✔  Finish Edit  —  Restore isReadable",
                    "Restores the Read/Write Enabled (isReadable) flag on all textures that were modified " +
                    "by this tool back to their original value. Run this when you are done editing.");
                if (GUILayout.Button(finishContent, GUILayout.Height(28)))
                    RestoreReadability();
                GUI.backgroundColor = prevBg;
                EditorGUILayout.Space(4);
            }

            activeTab = (Tab)GUILayout.Toolbar((int)activeTab, tabLabels);
            EditorGUILayout.Space(4);

            switch (activeTab)
            {
                case Tab.Single: DrawSingleTab(); break;
                case Tab.Batch: DrawBatchTab(); break;
                case Tab.ColorAdjust: DrawColorAdjustTab(); break;
                case Tab.ChannelExtract: DrawChannelExtractTab(); break;
                case Tab.ChannelImport: DrawChannelImportTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  TAB: SINGLE
        // ═════════════════════════════════════════════════════════════════════════
        private void DrawSingleTab()
        {
            // Source picker
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Source Image", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var newTex = (Texture2D)EditorGUILayout.ObjectField("Texture", sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && newTex != sourceTexture)
            {
                sourceTexture = newTex;
                assetPath = sourceTexture ? AssetDatabase.GetAssetPath(sourceTexture) : "";
                resizeWidth = sourceTexture ? sourceTexture.width : 512;
                resizeHeight = sourceTexture ? sourceTexture.height : 512;
                RefreshPreview();
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Path", assetPath);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.LabelField("Original Size",
                    $"{sourceTexture.width} × {sourceTexture.height} px", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();

            DrawPreviewWidget(previewTexture, PREVIEW_MAX);

            if (sourceTexture == null) return;

            // Flip
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Flip", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            bool nH = GUILayout.Toggle(flipHorizontal, "⟺  Horizontal", "Button");
            bool nV = GUILayout.Toggle(flipVertical, "⟷  Vertical", "Button");
            if (nH != flipHorizontal || nV != flipVertical)
            {
                flipHorizontal = nH;
                flipVertical = nV;
                RefreshPreview();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Rotate
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Rotate", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            rotationAngle = EditorGUILayout.Slider("Angle", rotationAngle, 0f, 360f);
            if (EditorGUI.EndChangeCheck()) RefreshPreview();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Presets:", GUILayout.Width(52));
            foreach (float p in rotationPresets)
                if (GUILayout.Button($"{p}°"))
                {
                    rotationAngle = p;
                    RefreshPreview();
                }

            if (GUILayout.Button("Reset"))
            {
                rotationAngle = 0f;
                RefreshPreview();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            DrawResizeSection(ref resizeWidth, ref resizeHeight, ref maintainAspectRatio,
                sourceTexture.width, sourceTexture.height);

            DrawSaveButtons();
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  TAB: BATCH
        // ═════════════════════════════════════════════════════════════════════════
        private void DrawBatchTab()
        {
            // Texture list
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Textures", EditorStyles.boldLabel);

            Rect dropRect = GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "▼  Drop textures here or use the + button");
            HandleDragDrop(dropRect);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add", GUILayout.Width(60))) batchTextures.Add(null);
            if (GUILayout.Button("Clear", GUILayout.Width(60))) batchTextures.Clear();
            EditorGUILayout.LabelField($"{batchTextures.Count} texture(s)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            batchScroll = EditorGUILayout.BeginScrollView(batchScroll, GUILayout.MaxHeight(140));
            for (int i = 0; i < batchTextures.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                batchTextures[i] = (Texture2D)EditorGUILayout.ObjectField(batchTextures[i], typeof(Texture2D), false);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    batchTextures.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Operations
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Operations", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            batchFlipH = GUILayout.Toggle(batchFlipH, "Flip H", "Button");
            batchFlipV = GUILayout.Toggle(batchFlipV, "Flip V", "Button");
            EditorGUILayout.EndHorizontal();

            batchRotation = EditorGUILayout.Slider("Rotate", batchRotation, 0f, 360f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Presets:", GUILayout.Width(52));
            foreach (float p in rotationPresets)
                if (GUILayout.Button($"{p}°"))
                    batchRotation = p;
            if (GUILayout.Button("Reset")) batchRotation = 0f;
            EditorGUILayout.EndHorizontal();

            batchResize = EditorGUILayout.Toggle("Resize", batchResize);
            if (batchResize)
            {
                EditorGUI.indentLevel++;
                DrawResizeSection(ref batchResizeW, ref batchResizeH,
                    ref batchMaintainAspect, batchResizeW, batchResizeH);
                EditorGUI.indentLevel--;
            }

            batchApplyColor = EditorGUILayout.Toggle("Apply Color Adjustments", batchApplyColor);
            if (batchApplyColor)
                EditorGUILayout.HelpBox("Uses the values from the Color Adjust tab.", MessageType.Info);

            EditorGUILayout.EndVertical();

            // Output
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Output", EditorStyles.boldLabel);
            batchOverwrite = EditorGUILayout.Toggle("Overwrite Originals", batchOverwrite);
            if (!batchOverwrite)
                batchSuffix = EditorGUILayout.TextField("Suffix", batchSuffix);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.75f, 1f);
            if (GUILayout.Button("▶  Process All", GUILayout.Height(32)))
            {
                if (batchTextures.Count == 0)
                {
                    EditorUtility.DisplayDialog("Batch", "No textures in the list.", "OK");
                    return;
                }

                if (batchOverwrite &&
                    !EditorUtility.DisplayDialog("Batch Overwrite",
                        $"This will overwrite {batchTextures.Count} original file(s). Continue?",
                        "Yes, Overwrite", "Cancel"))
                    return;
                RunBatch();
            }

            GUI.backgroundColor = prevBg;
            EditorGUILayout.Space(6);
        }

        private void HandleDragDrop(Rect zone)
        {
            Event e = Event.current;
            if (!zone.Contains(e.mousePosition)) return;
            if (e.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();
            }
            else if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                    if (obj is Texture2D t)
                        batchTextures.Add(t);
                e.Use();
            }
        }

        private void RunBatch()
        {
            int done = 0, skipped = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < batchTextures.Count; i++)
                {
                    Texture2D src = batchTextures[i];
                    if (src == null)
                    {
                        skipped++;
                        continue;
                    }

                    string srcPath = AssetDatabase.GetAssetPath(src);
                    EditorUtility.DisplayProgressBar("Batch Processing",
                        Path.GetFileName(srcPath), (float)i / batchTextures.Count);

                    EnsureReadable(srcPath);
                    Texture2D result = GetReadableCopy(src);

                    if (batchFlipH || batchFlipV)
                        result = FlipTexture(result, batchFlipH, batchFlipV);
                    if (batchRotation != 0f)
                        result = RotateTexture(result, batchRotation);
                    if (batchApplyColor)
                        result = ApplyColorAdjustments(result);
                    if (batchResize && (result.width != batchResizeW || result.height != batchResizeH))
                        result = ResizeTexture(result, batchResizeW, batchResizeH);

                    string outPath = batchOverwrite
                        ? srcPath
                        : AssetDatabase.GenerateUniqueAssetPath(
                            $"{Path.GetDirectoryName(srcPath)}" +
                            $"/{Path.GetFileNameWithoutExtension(srcPath)}{batchSuffix}" +
                            $"{Path.GetExtension(srcPath)}");

                    WriteTexture(result, outPath);

                    if (!batchOverwrite)
                    {
                        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
                        var si = AssetImporter.GetAtPath(srcPath) as TextureImporter;
                        var di = AssetImporter.GetAtPath(outPath) as TextureImporter;
                        if (si != null && di != null) CopyTextureImporterSettings(si, di);
                    }

                    TrackAndSetReadable(AssetImporter.GetAtPath(outPath) as TextureImporter, outPath);

                    done++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Batch Complete",
                $"Processed: {done}\nSkipped (null): {skipped}", "OK");
            Debug.Log($"[ImageManipulator] Batch done — {done} saved, {skipped} skipped.");
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  TAB: COLOR ADJUST
        // ═════════════════════════════════════════════════════════════════════════
        private void DrawColorAdjustTab()
        {
            // Source (shared with Single tab)
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Source Image", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var newTex = (Texture2D)EditorGUILayout.ObjectField("Texture", sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && newTex != sourceTexture)
            {
                sourceTexture = newTex;
                assetPath = sourceTexture ? AssetDatabase.GetAssetPath(sourceTexture) : "";
                RefreshPreview();
            }

            EditorGUILayout.EndVertical();

            // Sliders
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Adjustments", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            brightness = EditorGUILayout.Slider("Brightness", brightness, -1f, 1f);
            contrast = EditorGUILayout.Slider("Contrast", contrast, -1f, 1f);
            saturation = EditorGUILayout.Slider("Saturation", saturation, 0f, 2f);

            EditorGUILayout.Space(4);
            bool newGray = EditorGUILayout.Toggle("Grayscale", grayscale);
            if (newGray != grayscale)
            {
                grayscale = newGray;
                if (grayscale) saturation = 0f;
            }

            EditorGUILayout.Space(4);
            tintEnabled = EditorGUILayout.Toggle("Tint", tintEnabled);
            if (tintEnabled)
                tintColor = EditorGUILayout.ColorField("Tint Color", tintColor);

            if (EditorGUI.EndChangeCheck()) RefreshPreview();

            if (GUILayout.Button("Reset All"))
            {
                brightness = 0f;
                contrast = 0f;
                saturation = 1f;
                grayscale = false;
                tintEnabled = false;
                tintColor = Color.white;
                RefreshPreview();
            }

            EditorGUILayout.EndVertical();

            DrawPreviewWidget(previewTexture, PREVIEW_MAX);

            if (sourceTexture != null) DrawSaveButtons();
            EditorGUILayout.Space(6);
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  TAB: CHANNEL EXTRACT
        // ═════════════════════════════════════════════════════════════════════════
        private void DrawChannelExtractTab()
        {
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Source Texture  (ORM / packed)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var newTex = (Texture2D)EditorGUILayout.ObjectField("Texture", extractSource, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && newTex != extractSource)
            {
                extractSource = newTex;
                extractAssetPath = extractSource ? AssetDatabase.GetAssetPath(extractSource) : "";
                prevR = prevG = prevB = prevA = null;
            }

            if (!string.IsNullOrEmpty(extractAssetPath))
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Path", extractAssetPath);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.LabelField(
                    $"{extractSource.width} × {extractSource.height} px", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();

            // Channel toggles
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Channels to Extract", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            extractR = GUILayout.Toggle(extractR, "R  (Occlusion)", "Button");
            extractG = GUILayout.Toggle(extractG, "G  (Roughness)", "Button");
            extractB = GUILayout.Toggle(extractB, "B  (Metallic)", "Button");
            extractA = GUILayout.Toggle(extractA, "A", "Button");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(
                "Each selected channel is saved as a separate greyscale PNG (linear) next to the source.",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            // Previews
            if (prevR != null || prevG != null || prevB != null || prevA != null)
            {
                EditorGUILayout.BeginVertical(sectionBox);
                GUILayout.Label("Channel Previews", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                DrawChannelPreview(prevR, "R – Occlusion");
                DrawChannelPreview(prevG, "G – Roughness");
                DrawChannelPreview(prevB, "B – Metallic");
                DrawChannelPreview(prevA, "A");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview", GUILayout.Height(28))) PreviewChannels();
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("💾  Extract & Save", GUILayout.Height(28))) ExtractAndSave();
            GUI.backgroundColor = prev;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
        }

        private void DrawChannelPreview(Texture2D tex, string label)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(SMALL_PREV));
            GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(SMALL_PREV));
            if (tex != null)
            {
                Rect r = GUILayoutUtility.GetRect(SMALL_PREV, SMALL_PREV,
                    GUILayout.Width(SMALL_PREV), GUILayout.Height(SMALL_PREV));
                EditorGUI.DrawTextureTransparent(r, tex);
            }
            else
            {
                GUILayout.Box("—", GUILayout.Width(SMALL_PREV), GUILayout.Height(SMALL_PREV));
            }

            EditorGUILayout.EndVertical();
        }

        private void PreviewChannels()
        {
            if (extractSource == null) return;
            EnsureReadable(extractAssetPath);
            Texture2D src = GetReadableCopy(extractSource);
            prevR = extractR ? ExtractChannel(src, 0) : null;
            prevG = extractG ? ExtractChannel(src, 1) : null;
            prevB = extractB ? ExtractChannel(src, 2) : null;
            prevA = extractA ? ExtractChannel(src, 3) : null;
            Repaint();
        }

        private void ExtractAndSave()
        {
            if (extractSource == null) return;
            EnsureReadable(extractAssetPath);
            Texture2D src = GetReadableCopy(extractSource);

            string dir = Path.GetDirectoryName(extractAssetPath);
            string name = Path.GetFileNameWithoutExtension(extractAssetPath);

            string[] suffixes = { "_R_Occlusion", "_G_Roughness", "_B_Metallic", "_A" };
            bool[] flags = { extractR, extractG, extractB, extractA };

            var saved = new List<string>();
            for (int ch = 0; ch < 4; ch++)
            {
                if (!flags[ch]) continue;
                Texture2D channelTex = ExtractChannel(src, ch);
                string outPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{name}{suffixes[ch]}.png");
                WriteTexture(channelTex, outPath);
                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);
                var imp = AssetImporter.GetAtPath(outPath) as TextureImporter;
                if (imp != null)
                {
                    imp.sRGBTexture = false;
                    imp.SaveAndReimport();
                }

                TrackAndSetReadable(AssetImporter.GetAtPath(outPath) as TextureImporter, outPath);
                saved.Add(outPath);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Extracted",
                $"Saved {saved.Count} channel(s):\n{string.Join("\n", saved)}", "OK");
            Debug.Log($"[ImageManipulator] Extracted → {string.Join(", ", saved)}");
        }

        // channel: 0=R, 1=G, 2=B, 3=A  →  greyscale RGBA32 texture
        private Texture2D ExtractChannel(Texture2D src, int channel)
        {
            Color32[] sp = src.GetPixels32();
            Color32[] dp = new Color32[sp.Length];
            for (int i = 0; i < sp.Length; i++)
            {
                byte v = channel == 0 ? sp[i].r :
                    channel == 1 ? sp[i].g :
                    channel == 2 ? sp[i].b : sp[i].a;
                dp[i] = new Color32(v, v, v, 255);
            }

            var result = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            result.SetPixels32(dp);
            result.Apply();
            return result;
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  TAB: CHANNEL IMPORT (ORM packer)
        // ═════════════════════════════════════════════════════════════════════════
        private void DrawChannelImportTab()
        {
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Input Channels  (assign greyscale textures)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "R = Ambient Occlusion    G = Roughness    B = Metallic\n" +
                "Leave a slot empty to fill that channel with black (0).\n" +
                "The red channel of each input texture is used as the grey value.",
                MessageType.Info);

            DrawOrmSlot("R — Occlusion", ref ormR, ref ormRPath, ref ormInvertR);
            DrawOrmSlot("G — Roughness", ref ormG, ref ormGPath, ref ormInvertG);
            DrawOrmSlot("B — Metallic", ref ormB, ref ormBPath, ref ormInvertB);
            EditorGUILayout.EndVertical();

            // Output
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Output", EditorStyles.boldLabel);
            ormOutputPath = EditorGUILayout.TextField("Folder  (Assets/…)", ormOutputPath);
            ormOutputName = EditorGUILayout.TextField("File Name", ormOutputName);
            EditorGUILayout.LabelField("Saved as PNG, linear colour space (sRGB off).", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // ORM preview
            if (ormPreview != null)
            {
                DrawPreviewWidget(ormPreview, PREVIEW_MAX);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview ORM", GUILayout.Height(28)))
            {
                ormPreview = BuildORM();
                Repaint();
            }

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("💾  Pack & Save", GUILayout.Height(28))) SaveORM();
            GUI.backgroundColor = prev;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
        }

        private void DrawOrmSlot(string label, ref Texture2D tex, ref string path, ref bool invert)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            tex = (Texture2D)EditorGUILayout.ObjectField(label, tex, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck()) path = tex ? AssetDatabase.GetAssetPath(tex) : "";
            invert = GUILayout.Toggle(invert, "Invert", GUILayout.Width(54));
            EditorGUILayout.EndHorizontal();
        }

        private Texture2D BuildORM()
        {
            int w = 4, h = 4;
            if (ormR != null)
            {
                w = Mathf.Max(w, ormR.width);
                h = Mathf.Max(h, ormR.height);
            }

            if (ormG != null)
            {
                w = Mathf.Max(w, ormG.width);
                h = Mathf.Max(h, ormG.height);
            }

            if (ormB != null)
            {
                w = Mathf.Max(w, ormB.width);
                h = Mathf.Max(h, ormB.height);
            }

            byte[] rCh = SampleGray(ormR, w, h, ormInvertR);
            byte[] gCh = SampleGray(ormG, w, h, ormInvertG);
            byte[] bCh = SampleGray(ormB, w, h, ormInvertB);

            Color32[] dp = new Color32[w * h];
            for (int i = 0; i < dp.Length; i++)
                dp[i] = new Color32(rCh[i], gCh[i], bCh[i], 255);

            var result = new Texture2D(w, h, TextureFormat.RGBA32, false);
            result.SetPixels32(dp);
            result.Apply();
            return result;
        }

        // Returns the red channel of tex resampled to (tw,th).
        // Returns black array if tex is null.
        private byte[] SampleGray(Texture2D tex, int tw, int th, bool invert)
        {
            byte[] ch = new byte[tw * th];
            if (tex == null) return ch;

            EnsureReadable(AssetDatabase.GetAssetPath(tex));
            Texture2D readable = GetReadableCopy(tex);
            if (readable.width != tw || readable.height != th)
                readable = ResizeTexture(readable, tw, th);

            Color32[] px = readable.GetPixels32();
            for (int i = 0; i < px.Length; i++)
                ch[i] = invert ? (byte)(255 - px[i].r) : px[i].r;
            return ch;
        }

        private void SaveORM()
        {
            Texture2D packed = BuildORM();
            if (packed == null)
            {
                EditorUtility.DisplayDialog("Error", "Nothing to pack.", "OK");
                return;
            }

            string folder = ormOutputPath.TrimEnd('/', '\\');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("Error", $"Folder '{folder}' not found in project.", "OK");
                return;
            }

            string outPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{ormOutputName}.png");
            WriteTexture(packed, outPath);
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

            var imp = AssetImporter.GetAtPath(outPath) as TextureImporter;
            if (imp != null)
            {
                imp.sRGBTexture = false;
                imp.SaveAndReimport();
            }

            TrackAndSetReadable(AssetImporter.GetAtPath(outPath) as TextureImporter, outPath);
            ormPreview = packed;
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Saved", $"ORM texture saved to:\n{outPath}", "OK");
            Debug.Log($"[ImageManipulator] ORM saved → {outPath}");
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  SHARED UI HELPERS
        // ═════════════════════════════════════════════════════════════════════════
        private void DrawPreviewWidget(Texture2D tex, float maxSize)
        {
            if (tex == null) return;
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            float scale = Mathf.Min(maxSize / tex.width, maxSize / tex.height, 1f);
            float pw = tex.width * scale, ph = tex.height * scale;
            Rect r = GUILayoutUtility.GetRect(pw, ph, GUILayout.ExpandWidth(false));
            r.x = (EditorGUIUtility.currentViewWidth - pw) * 0.5f;
            r.width = pw;
            r.height = ph;
            EditorGUI.DrawTextureTransparent(r, tex);
            EditorGUILayout.LabelField($"{tex.width} × {tex.height} px",
                EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawResizeSection(ref int rW, ref int rH, ref bool lockAspect, int origW, int origH)
        {
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Resize", EditorStyles.boldLabel);
            lockAspect = EditorGUILayout.Toggle("Lock Aspect Ratio", lockAspect);

            EditorGUI.BeginChangeCheck();
            int nW = EditorGUILayout.IntField("Width", rW);
            if (EditorGUI.EndChangeCheck() && nW > 0)
            {
                if (lockAspect && origH > 0) rH = Mathf.RoundToInt(nW * (float)origH / origW);
                rW = nW;
            }

            EditorGUI.BeginChangeCheck();
            int nH = EditorGUILayout.IntField("Height", rH);
            if (EditorGUI.EndChangeCheck() && nH > 0)
            {
                if (lockAspect && origW > 0) rW = Mathf.RoundToInt(nH * (float)origW / origH);
                rH = nH;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Presets:", GUILayout.Width(52));
            foreach (int p in new[] { 128, 256, 512, 1024, 2048 })
                if (GUILayout.Button(p.ToString(), GUILayout.Width(44)))
                {
                    rW = p;
                    rH = lockAspect && origW > 0 ? Mathf.RoundToInt(p * (float)origH / origW) : p;
                }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawSaveButtons()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Preview", GUILayout.Height(28))) RefreshPreview();
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("💾  Save as New", GUILayout.Height(28))) SaveSingle(overwrite: false);
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
            if (GUILayout.Button("⚠  Overwrite", GUILayout.Height(28)))
                if (EditorUtility.DisplayDialog("Overwrite", "Replace the original file?", "Overwrite", "Cancel"))
                    SaveSingle(overwrite: true);
            GUI.backgroundColor = prev;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  SINGLE PIPELINE
        // ═════════════════════════════════════════════════════════════════════════
        private void RefreshPreview()
        {
            if (sourceTexture == null)
            {
                previewTexture = null;
                return;
            }

            EnsureReadable(assetPath);
            Texture2D result = GetReadableCopy(sourceTexture);
            if (flipHorizontal || flipVertical) result = FlipTexture(result, flipHorizontal, flipVertical);
            if (rotationAngle != 0f) result = RotateTexture(result, rotationAngle);
            result = ApplyColorAdjustments(result);
            previewTexture = result;
            Repaint();
        }

        private void SaveSingle(bool overwrite)
        {
            if (sourceTexture == null || previewTexture == null) return;

            Texture2D finalTex = previewTexture;
            if (resizeWidth != previewTexture.width || resizeHeight != previewTexture.height)
                finalTex = ResizeTexture(previewTexture, resizeWidth, resizeHeight);

            string savedPath = overwrite
                ? assetPath
                : AssetDatabase.GenerateUniqueAssetPath(
                    $"{Path.GetDirectoryName(assetPath)}" +
                    $"/{Path.GetFileNameWithoutExtension(assetPath)}_edited" +
                    $"{Path.GetExtension(assetPath)}");

            WriteTexture(finalTex, savedPath);
            AssetDatabase.ImportAsset(savedPath, ImportAssetOptions.ForceUpdate);

            var srcImp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            var dstImp = AssetImporter.GetAtPath(savedPath) as TextureImporter;
            if (srcImp != null && dstImp != null && !overwrite) CopyTextureImporterSettings(srcImp, dstImp);
            TrackAndSetReadable(dstImp, savedPath);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Saved", $"Saved to:\n{savedPath}", "OK");
            Debug.Log($"[ImageManipulator] Saved → {savedPath}");
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  IMAGE PROCESSING  (all Color32, no colour-space conversion)
        // ═════════════════════════════════════════════════════════════════════════

        private Texture2D GetReadableCopy(Texture2D src)
        {
            var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            copy.SetPixels32(src.GetPixels32());
            copy.Apply();
            return copy;
        }

        private Texture2D FlipTexture(Texture2D src, bool h, bool v)
        {
            int w = src.width, ht = src.height;
            Color32[] sp = src.GetPixels32(), dp = new Color32[sp.Length];
            for (int y = 0; y < ht; y++)
            for (int x = 0; x < w; x++)
                dp[y * w + x] = sp[(v ? ht - 1 - y : y) * w + (h ? w - 1 - x : x)];
            var r = new Texture2D(w, ht, TextureFormat.RGBA32, false);
            r.SetPixels32(dp);
            r.Apply();
            return r;
        }

        private Texture2D RotateTexture(Texture2D src, float deg)
        {
            int a = Mathf.RoundToInt(deg) % 360;
            if (a == 90) return RotateExact90(src, false);
            if (a == 270) return RotateExact90(src, true);
            if (a == 180) return RotateExact180(src);
            return RotateBilinear(src, deg);
        }

        private Texture2D RotateExact90(Texture2D src, bool ccw)
        {
            int sw = src.width, sh = src.height;
            Color32[] sp = src.GetPixels32(), dp = new Color32[sw * sh];
            for (int y = 0; y < sh; y++)
            for (int x = 0; x < sw; x++)
                dp[ccw ? x * sh + (sh - 1 - y) : (sw - 1 - x) * sh + y] = sp[y * sw + x];
            var r = new Texture2D(sh, sw, TextureFormat.RGBA32, false);
            r.SetPixels32(dp);
            r.Apply();
            return r;
        }

        private Texture2D RotateExact180(Texture2D src)
        {
            Color32[] sp = src.GetPixels32(), dp = new Color32[sp.Length];
            for (int i = 0; i < sp.Length; i++) dp[sp.Length - 1 - i] = sp[i];
            var r = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            r.SetPixels32(dp);
            r.Apply();
            return r;
        }

        private Texture2D RotateBilinear(Texture2D src, float deg)
        {
            int w = src.width, h = src.height;
            float rad = -deg * Mathf.Deg2Rad, cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            float cx = w * 0.5f, cy = h * 0.5f;
            Color32[] sp = src.GetPixels32(), dp = new Color32[w * h];
            Color32 clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float ddx = x - cx, ddy = y - cy;
                float sx = cos * ddx - sin * ddy + cx, sy = sin * ddx + cos * ddy + cy;
                if (sx < 0 || sx >= w - 1 || sy < 0 || sy >= h - 1)
                {
                    dp[y * w + x] = clear;
                    continue;
                }

                int x0 = (int)sx, y0 = (int)sy;
                float tx = sx - x0, ty = sy - y0;
                dp[y * w + x] = LC(LC(sp[y0 * w + x0], sp[y0 * w + x0 + 1], tx),
                    LC(sp[(y0 + 1) * w + x0], sp[(y0 + 1) * w + x0 + 1], tx), ty);
            }

            var res = new Texture2D(w, h, TextureFormat.RGBA32, false);
            res.SetPixels32(dp);
            res.Apply();
            return res;
        }

        private Texture2D ResizeTexture(Texture2D src, int tw, int th)
        {
            Color32[] sp = src.GetPixels32(), dp = new Color32[tw * th];
            int sw = src.width, sh = src.height;
            for (int dy = 0; dy < th; dy++)
            {
                float fy = (dy + 0.5f) * sh / th - 0.5f;
                int y0 = Mathf.Clamp((int)fy, 0, sh - 1), y1 = Mathf.Clamp(y0 + 1, 0, sh - 1);
                float tyf = fy - y0;
                for (int dx = 0; dx < tw; dx++)
                {
                    float fx = (dx + 0.5f) * sw / tw - 0.5f;
                    int x0 = Mathf.Clamp((int)fx, 0, sw - 1), x1 = Mathf.Clamp(x0 + 1, 0, sw - 1);
                    float txf = fx - x0;
                    dp[dy * tw + dx] = LC(LC(sp[y0 * sw + x0], sp[y0 * sw + x1], txf),
                        LC(sp[y1 * sw + x0], sp[y1 * sw + x1], txf), tyf);
                }
            }

            var r = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            r.SetPixels32(dp);
            r.Apply();
            return r;
        }

        private Texture2D ApplyColorAdjustments(Texture2D src)
        {
            bool isDefault = Mathf.Approximately(brightness, 0f)
                             && Mathf.Approximately(contrast, 0f)
                             && Mathf.Approximately(saturation, 1f)
                             && !grayscale && !tintEnabled;
            if (isDefault) return src;

            Color32[] sp = src.GetPixels32(), dp = new Color32[sp.Length];
            float tr = tintEnabled ? tintColor.r : 1f;
            float tg = tintEnabled ? tintColor.g : 1f;
            float tb = tintEnabled ? tintColor.b : 1f;
            float cf = contrast >= 0f ? 1f + contrast * 3f : 1f + contrast;

            for (int i = 0; i < sp.Length; i++)
            {
                float r = sp[i].r / 255f, g = sp[i].g / 255f, b = sp[i].b / 255f, a = sp[i].a / 255f;

                // Brightness
                r += brightness;
                g += brightness;
                b += brightness;
                // Contrast (pivot 0.5)
                r = (r - 0.5f) * cf + 0.5f;
                g = (g - 0.5f) * cf + 0.5f;
                b = (b - 0.5f) * cf + 0.5f;
                // Saturation / greyscale
                float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                r = lum + (r - lum) * saturation;
                g = lum + (g - lum) * saturation;
                b = lum + (b - lum) * saturation;
                // Tint
                r *= tr;
                g *= tg;
                b *= tb;

                dp[i] = new Color32(
                    (byte)(Mathf.Clamp01(r) * 255), (byte)(Mathf.Clamp01(g) * 255),
                    (byte)(Mathf.Clamp01(b) * 255), (byte)(Mathf.Clamp01(a) * 255));
            }

            var result = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            result.SetPixels32(dp);
            result.Apply();
            return result;
        }

        private static Color32 LC(Color32 a, Color32 b, float t) => new Color32(
            (byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t),
            (byte)(a.b + (b.b - a.b) * t), (byte)(a.a + (b.a - a.a) * t));

        // ═════════════════════════════════════════════════════════════════════════
        //  ASSET HELPERS
        // ═════════════════════════════════════════════════════════════════════════
        private void EnsureReadable(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && !imp.isReadable)
            {
                if (!_originalReadability.ContainsKey(path))
                    _originalReadability[path] = false;
                imp.isReadable = true;
                imp.SaveAndReimport();
            }
        }

        private void TrackAndSetReadable(TextureImporter imp, string path)
        {
            if (imp == null) return;
            if (!_originalReadability.ContainsKey(path))
                _originalReadability[path] = imp.isReadable;
            imp.isReadable = true;
            imp.SaveAndReimport();
        }

        private void RestoreReadability()
        {
            if (_originalReadability.Count == 0) return;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var kv in _originalReadability)
                {
                    var imp = AssetImporter.GetAtPath(kv.Key) as TextureImporter;
                    if (imp == null) continue;
                    imp.isReadable = kv.Value;
                    imp.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[ImageManipulator] Restored isReadable on {_originalReadability.Count} texture(s).");
            _originalReadability.Clear();
        }

        private void WriteTexture(Texture2D tex, string assetDstPath)
        {
            string full = Application.dataPath.Replace("Assets", "") + assetDstPath;
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            string ext = Path.GetExtension(assetDstPath).ToLower();
            File.WriteAllBytes(full, (ext == ".jpg" || ext == ".jpeg")
                ? tex.EncodeToJPG(95)
                : tex.EncodeToPNG());
        }

        private void CopyTextureImporterSettings(TextureImporter src, TextureImporter dst)
        {
            dst.textureType = src.textureType;
            dst.textureShape = src.textureShape;
            dst.sRGBTexture = src.sRGBTexture;
            dst.alphaSource = src.alphaSource;
            dst.alphaIsTransparency = src.alphaIsTransparency;
            dst.ignorePngGamma = src.ignorePngGamma;
            dst.npotScale = src.npotScale;
            dst.isReadable = src.isReadable;
            dst.streamingMipmaps = src.streamingMipmaps;
            dst.streamingMipmapsPriority = src.streamingMipmapsPriority;
            dst.mipmapEnabled = src.mipmapEnabled;
            dst.mipmapFilter = src.mipmapFilter;
            dst.mipMapsPreserveCoverage = src.mipMapsPreserveCoverage;
            dst.fadeout = src.fadeout;
            dst.mipmapFadeDistanceStart = src.mipmapFadeDistanceStart;
            dst.mipmapFadeDistanceEnd = src.mipmapFadeDistanceEnd;
            dst.wrapMode = src.wrapMode;
            dst.wrapModeU = src.wrapModeU;
            dst.wrapModeV = src.wrapModeV;
            dst.wrapModeW = src.wrapModeW;
            dst.filterMode = src.filterMode;
            dst.anisoLevel = src.anisoLevel;
            dst.maxTextureSize = src.maxTextureSize;
            dst.textureCompression = src.textureCompression;
            dst.compressionQuality = src.compressionQuality;
            dst.crunchedCompression = src.crunchedCompression;
            dst.allowAlphaSplitting = src.allowAlphaSplitting;

            if (src.textureType == TextureImporterType.Sprite)
            {
                dst.spriteImportMode = src.spriteImportMode;
                dst.spritePackingTag = src.spritePackingTag;
                dst.spritePixelsPerUnit = src.spritePixelsPerUnit;
                dst.spritePivot = src.spritePivot;
            }

            string[] platforms = { "Standalone", "iPhone", "Android", "WebGL", "Windows Store Apps", "tvOS" };
            foreach (string p in platforms)
            {
                var ps = src.GetPlatformTextureSettings(p);
                if (ps.overridden) dst.SetPlatformTextureSettings(ps);
            }

            dst.SetPlatformTextureSettings(src.GetDefaultPlatformTextureSettings());
        }
    }
}