using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  ImageManipulatorTool  –  Tools/Image Manipulator
//  Tabs: Single | Batch | Color Adjust | Recolor | Channel Extract | Channel Import (ORM) | Gradient
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
            Recolor,
            ChannelExtract,
            ChannelImport,
            Gradient
        }

        private Tab activeTab = Tab.Single;

        private readonly string[] tabLabels =
            { "Single", "Batch", "Color Adjust", "Recolor", "Ch. Extract", "Ch. Import (ORM)", "Gradient" };

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
        private float saturation = 1f; //  0 … 2  (0 = greyscale, 1 = neutral)
        private bool tintEnabled = false;
        private Color tintColor = Color.white;

        // ── Recolor state (OKLab palette-match) ───────────────────────────────────
        private enum RecolorMode
        {
            Recolor,  // map the whole image onto the picked colour (keeps texture, becomes "much this colour")
            HueShift, // rotate the source's dominant hue onto the picked colour (keeps colour variety)
            Layering  // composite a colour / second image on top with a Photoshop-style blend mode
        }

        private RecolorMode recolorMode = RecolorMode.Recolor;

        // The palette the textures should be matched to. Editable swatch grid.
        private readonly List<Color> palette = new List<Color>
        {
            new Color(0.86f, 0.22f, 0.27f), // red
            new Color(0.95f, 0.61f, 0.19f), // orange
            new Color(0.98f, 0.85f, 0.30f), // yellow
            new Color(0.36f, 0.72f, 0.36f), // green
            new Color(0.26f, 0.55f, 0.87f), // blue
            new Color(0.55f, 0.35f, 0.78f), // violet
        };

        private int selectedSwatch = 0;
        private float recolorStrength = 1f; //  0 … 1  (how far toward the target colour)
        private float recolorChroma = 1f; //  0 … 2  (colour intensity multiplier)
        private float recolorLightness = 0f; // -0.5 … +0.5 (perceptual value shift)
        private bool recolorNaturalShading = true; // desaturate highlights→white, shadows→black
        private Texture2D recolorPreview;

        // ── Layering state (Photoshop-style blend compositing) ────────────────────
        private enum LayerSourceType
        {
            Color, // flat colour from the field below (or pulled off the palette)
            Image  // a second texture, fitted onto the base
        }

        private enum LayerFit
        {
            Stretch, // scale to the base size, aspect ignored
            Fit,     // scale to fit inside the base, aspect kept, rest transparent
            Fill,    // scale to cover the base, aspect kept, overflow cropped
            Tile,    // repeat, driven by Tile Scale
            Center   // 1:1 pixels, centred, rest transparent
        }

        // Photoshop's blend list. Everything up to Divide is separable (per-channel);
        // Hue…Luminosity are the non-separable W3C compositing modes.
        private enum LayerBlendMode
        {
            Normal,
            Darken, Multiply, ColorBurn, LinearBurn,
            Lighten, Screen, ColorDodge, LinearDodge,
            Overlay, SoftLight, HardLight, VividLight, LinearLight, PinLight, HardMix,
            Difference, Exclusion, Subtract, Divide,
            Hue, Saturation, Color, Luminosity
        }

        // Which alpha decides *how much* of the blend lands on each pixel.
        private enum BlendMaskSource
        {
            None,          // blend everywhere at full Opacity
            LayerAlpha,    // the layer's own alpha
            BaseAlpha,     // the source image's alpha
            MultiplyBoth,  // layerA × baseA
            MinBoth,
            MaxBoth,
            AverageBoth,
            LayerLuminance,
            LayerR, LayerG, LayerB
        }

        // Which alpha the produced texture carries.
        private enum ResultAlpha
        {
            Base,     // keep the source's alpha (safest for sprites)
            Layer,
            Multiply,
            Min,
            Max,
            Mix,      // lerp(baseA, layerA, layerAlphaMix)
            Union,    // standard "over": aB + aEff − aB·aEff
            Opaque
        }

        private LayerSourceType layerSourceType = LayerSourceType.Color;
        private Color layerColor = new Color(0.26f, 0.55f, 0.87f, 1f);
        private Texture2D layerTexture;
        private string layerTexturePath = "";
        private LayerFit layerFit = LayerFit.Stretch;
        private float layerTileScale = 1f;          // 0.1 … 8   (Tile only)
        private Vector2 layerOffset = Vector2.zero; //  -1 … +1  in fractions of the base size

        private LayerBlendMode layerBlendMode = LayerBlendMode.Multiply;
        private float layerOpacity = 1f;    //  0 … 1
        private float layerAlphaScale = 1f; //  0 … 2  multiplies the layer's own alpha

        private BlendMaskSource layerMask = BlendMaskSource.LayerAlpha;
        private bool layerMaskInvert = false;
        private float layerMaskContrast = 1f; // gamma on the mask, 0.1 … 4

        private ResultAlpha layerResultAlpha = ResultAlpha.Base;
        private float layerAlphaMix = 0.5f; //  0 … 1  (ResultAlpha.Mix only)

        private bool layerPreserveLuma = false; // re-apply the base's OKLab lightness after blending
        private bool layerClipToBase = false;   // never paint where the base is transparent

        // Live = recompute preview on every change; off = only when "Apply" is pressed.
        // Shared by the Recolor and Color Adjust tabs.
        private bool livePreview = false;

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

        // ── Gradient-generator state ──────────────────────────────────────────────
        private enum GradientShape
        {
            Linear, // straight ramp along Angle
            Radial  // ramp outward from Position
        }

        // What happens outside the 0…1 range of the ramp.
        private enum GradientWrapMode
        {
            Clamp,
            Repeat,
            PingPong
        }

        // Serialized so a hand-authored ramp survives domain reloads.
        [SerializeField] private Gradient gradientRamp = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.white, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        };

        private GradientShape gradientShape = GradientShape.Linear;
        private int gradientWidth = 512;
        private int gradientHeight = 512;
        private float gradientAngle = 0f;                            // 0° = left → right, CCW
        private Vector2 gradientCenter = new Vector2(0.5f, 0.5f);    // normalised, (0,0) = bottom-left
        private float gradientSpread = 1f;                           // ramp length (linear) / radius (radial)
        private GradientWrapMode gradientWrap = GradientWrapMode.Clamp;
        private bool gradientCircular = true;                        // radial: compensate for non-square images
        private bool gradientDither = true;                          // ordered dither → kills 8-bit banding
        private bool gradientSRGB = true;
        private bool gradientAsSprite = false;
        private string gradientOutputPath = "Assets";
        private string gradientOutputName = "Gradient";
        private Texture2D gradientPreview;

        private const int GRADIENT_LUT = 4096;
        private const float GRADIENT_PREVIEW_MAX = 512f;

        // 4×4 Bayer matrix, normalised to −0.5…+0.5 (applied in 0…255 units before rounding).
        private static readonly float[] Bayer4 = BuildBayer4();

        // ── isReadable restore tracking ───────────────────────────────────────────
        // Maps asset path → original isReadable value before the tool changed it.
        private readonly Dictionary<string, bool> _originalReadability = new Dictionary<string, bool>();

        // ── UI ────────────────────────────────────────────────────────────────────
        private Vector2 scrollPos;
        private const float PREVIEW_MAX = 280f;
        private const float SMALL_PREV = 110f;
        private GUIStyle sectionBox;

        // ─────────────────────────────────────────────────────────────────────────
        [MenuItem("Tools/Windows/Image Manipulator", priority = 14)]
        public static void Open()
        {
            var w = GetWindow<ImageManipulatorTool>();
            w.titleContent = new GUIContent("Image Manipulator",
                EditorGUIUtility.IconContent("d_Texture Icon").image);
            w.minSize = new Vector2(580f, 680f);
        }

        private void OnEnable() => LoadPalette();

        private void InitStyles()
        {
            if (sectionBox != null) return;
            sectionBox = new GUIStyle(GUI.skin.box)
                { padding = new RectOffset(10, 10, 8, 8), margin = new RectOffset(0, 0, 4, 4) };
        }

        private void OnGUI()
        {
            InitStyles();

            // Tabs as a fixed toolbar strip (outside the scroll view → always visible)
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                activeTab = (Tab)GUILayout.Toolbar((int)activeTab, tabLabels,
                    EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space(4);

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

            switch (activeTab)
            {
                case Tab.Single: DrawSingleTab(); break;
                case Tab.Batch: DrawBatchTab(); break;
                case Tab.ColorAdjust: DrawColorAdjustTab(); break;
                case Tab.Recolor: DrawRecolorTab(); break;
                case Tab.ChannelExtract: DrawChannelExtractTab(); break;
                case Tab.ChannelImport: DrawChannelImportTab(); break;
                case Tab.Gradient: DrawGradientTab(); break;
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

            DrawBeforeAfterPreview(sourceTexture, previewTexture);

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

            // Summary of what "Process All" will do
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Summary", EditorStyles.boldLabel);

            int validCount = 0;
            foreach (var t in batchTextures)
                if (t != null) validCount++;

            var ops = new List<string>();
            if (batchFlipH) ops.Add("Flip H");
            if (batchFlipV) ops.Add("Flip V");
            if (batchRotation != 0f) ops.Add($"Rotate {batchRotation:0.#}°");
            if (batchResize) ops.Add($"Resize → {batchResizeW}×{batchResizeH}");
            if (batchApplyColor) ops.Add("Color Adjust");

            string opText = ops.Count > 0 ? string.Join("  ·  ", ops) : "None";
            string outText = batchOverwrite ? "overwrite originals" : $"new files (suffix \"{batchSuffix}\")";

            if (validCount == 0)
                EditorGUILayout.HelpBox("No textures assigned — add some above.", MessageType.Warning);
            else if (ops.Count == 0)
                EditorGUILayout.HelpBox(
                    $"{validCount} texture(s) selected, but no operations enabled — nothing would change.",
                    MessageType.Warning);
            else
                EditorGUILayout.HelpBox(
                    $"{validCount} texture(s)   →   {opText}\nOutput: {outText}",
                    MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.75f, 1f);
            EditorGUI.BeginDisabledGroup(validCount == 0 || ops.Count == 0);
            if (GUILayout.Button($"▶  Process All  ({validCount})", GUILayout.Height(32)))
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

            EditorGUI.EndDisabledGroup();
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
                previewTexture = null; // result cleared until "Apply" is pressed
            }

            EditorGUILayout.EndVertical();

            // Sliders
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Adjustments", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            brightness = EditorGUILayout.Slider("Brightness", brightness, -1f, 1f);
            contrast = EditorGUILayout.Slider("Contrast", contrast, -1f, 1f);
            saturation = EditorGUILayout.Slider(
                new GUIContent("Saturation", "0 = greyscale · 1 = original · 2 = double"),
                saturation, 0f, 2f);

            EditorGUILayout.Space(4);
            tintEnabled = EditorGUILayout.Toggle("Tint", tintEnabled);
            if (tintEnabled)
                tintColor = EditorGUILayout.ColorField("Tint Color", tintColor);

            bool changed = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            livePreview = EditorGUILayout.ToggleLeft(
                new GUIContent("Live", "Recompute the preview on every change instead of on Apply."),
                livePreview, GUILayout.Width(52));
            if (EditorGUI.EndChangeCheck() && livePreview) changed = true;

            if (GUILayout.Button("Reset All", GUILayout.Width(80)))
            {
                brightness = 0f;
                contrast = 0f;
                saturation = 1f;
                tintEnabled = false;
                tintColor = Color.white;
                changed = true;
            }

            EditorGUI.BeginDisabledGroup(livePreview);
            Color applyBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.75f, 1f);
            if (GUILayout.Button("↻  Apply", GUILayout.Height(22))) RefreshPreview();
            GUI.backgroundColor = applyBg;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (changed && livePreview) RefreshPreview();

            EditorGUILayout.EndVertical();

            DrawBeforeAfterPreview(sourceTexture, previewTexture);

            if (sourceTexture != null) DrawSaveButtons();
            EditorGUILayout.Space(6);
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  TAB: RECOLOR  (OKLab palette matching – keeps texture, changes colour)
        // ═════════════════════════════════════════════════════════════════════════
        private void DrawRecolorTab()
        {
            // Source (shared with Single / Color Adjust tabs)
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Source Image", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var newTex = (Texture2D)EditorGUILayout.ObjectField("Texture", sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && newTex != sourceTexture)
            {
                sourceTexture = newTex;
                assetPath = sourceTexture ? AssetDatabase.GetAssetPath(sourceTexture) : "";
                recolorPreview = null; // result cleared until "Apply" is pressed
            }

            EditorGUILayout.EndVertical();

            // Mode selector — picks which parameter block is shown below.
            EditorGUILayout.BeginVertical(sectionBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Mode", GUILayout.Width(48));
            EditorGUI.BeginChangeCheck();
            recolorMode = (RecolorMode)GUILayout.Toolbar((int)recolorMode,
                new[] { "Recolor", "Hue Shift", "Layering" });
            bool changed = EditorGUI.EndChangeCheck();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            DrawPalette();

            // Adjustments
            EditorGUILayout.BeginVertical(sectionBox);

            if (recolorMode == RecolorMode.Layering)
            {
                changed |= DrawLayeringSection();
            }
            else
            {
                GUILayout.Label("Recolor", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.HelpBox(
                    recolorMode == RecolorMode.Recolor
                        ? "Recolor: every pixel is pushed onto the selected palette colour while its perceptual " +
                          "lightness (the texture detail / shading) is preserved. Strength 1 = fully that colour."
                        : "Hue Shift: the image's dominant hue is rotated onto the palette colour. Keeps the " +
                          "original colour variety — good for textures that already contain several colours.",
                    MessageType.Info);

                recolorStrength = EditorGUILayout.Slider("Strength", recolorStrength, 0f, 1f);
                recolorChroma = EditorGUILayout.Slider("Chroma", recolorChroma, 0f, 2f);
                recolorLightness = EditorGUILayout.Slider("Lightness", recolorLightness, -0.5f, 0.5f);
                recolorNaturalShading = EditorGUILayout.Toggle(
                    new GUIContent("Natural Shading",
                        "Colour theory: highlights fade toward white and deep shadows toward black, " +
                        "instead of every tone carrying full chroma. Recommended for physically-plausible results."),
                    recolorNaturalShading);

                changed |= EditorGUI.EndChangeCheck();
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            livePreview = EditorGUILayout.ToggleLeft(
                new GUIContent("Live", "Recompute the preview on every change instead of on Apply."),
                livePreview, GUILayout.Width(52));
            if (EditorGUI.EndChangeCheck() && livePreview) changed = true; // enabling Live updates now

            if (GUILayout.Button("Reset", GUILayout.Width(70)))
            {
                // Reset the active mode's parameters, not the mode itself.
                if (recolorMode == RecolorMode.Layering)
                {
                    ResetLayering();
                }
                else
                {
                    recolorStrength = 1f;
                    recolorChroma = 1f;
                    recolorLightness = 0f;
                    recolorNaturalShading = true;
                }

                changed = true;
            }

            EditorGUI.BeginDisabledGroup(livePreview);
            Color applyBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.75f, 1f);
            if (GUILayout.Button("↻  Apply", GUILayout.Height(22)))
                RefreshRecolorPreview();
            GUI.backgroundColor = applyBg;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (changed && livePreview) RefreshRecolorPreview();

            EditorGUILayout.EndVertical();

            DrawBeforeAfterPreview(sourceTexture, recolorPreview);

            if (sourceTexture == null) return;

            // Save buttons (own pipeline – no resize, keeps original dimensions)
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("💾  Save as New", GUILayout.Height(28))) SaveRecolor(overwrite: false);
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.3f);
            if (GUILayout.Button("⚠  Overwrite", GUILayout.Height(28)))
                if (EditorUtility.DisplayDialog("Overwrite", "Replace the original file?", "Overwrite", "Cancel"))
                    SaveRecolor(overwrite: true);
            GUI.backgroundColor = prevBg;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
        }

        private void DrawPalette()
        {
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Color Palette", EditorStyles.boldLabel);
            GUILayout.Label("Click a swatch to pick the colour your texture is matched to.",
                EditorStyles.miniLabel);

            const int perRow = 8;
            const float sw = 30f;
            for (int i = 0; i < palette.Count; i++)
            {
                if (i % perRow == 0) EditorGUILayout.BeginHorizontal();

                Rect r = GUILayoutUtility.GetRect(sw, sw, GUILayout.Width(sw), GUILayout.Height(sw));
                bool sel = i == selectedSwatch;
                EditorGUI.DrawRect(r, sel ? Color.white : new Color(0f, 0f, 0f, 0.5f));
                EditorGUI.DrawRect(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), palette[i]);

                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    selectedSwatch = i;
                    Event.current.Use();
                    if (livePreview) RefreshRecolorPreview();
                    else Repaint();
                }

                if ((i + 1) % perRow == 0 || i == palette.Count - 1) EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add", GUILayout.Width(60)))
            {
                palette.Add(palette.Count > 0 ? palette[selectedSwatch] : Color.white);
                selectedSwatch = palette.Count - 1;
                SavePalette();
                if (livePreview) RefreshRecolorPreview();
            }

            EditorGUI.BeginDisabledGroup(palette.Count == 0);
            if (GUILayout.Button("− Remove", GUILayout.Width(70)))
            {
                palette.RemoveAt(selectedSwatch);
                selectedSwatch = Mathf.Clamp(selectedSwatch, 0, palette.Count - 1);
                SavePalette();
                if (livePreview) RefreshRecolorPreview();
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (palette.Count > 0)
            {
                selectedSwatch = Mathf.Clamp(selectedSwatch, 0, palette.Count - 1);
                EditorGUI.BeginChangeCheck();
                palette[selectedSwatch] =
                    EditorGUILayout.ColorField("Selected", palette[selectedSwatch]);
                if (EditorGUI.EndChangeCheck())
                {
                    SavePalette();
                    if (livePreview) RefreshRecolorPreview();
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ── Layering parameters. Returns true when anything changed. ──────────────
        private bool DrawLayeringSection()
        {
            GUILayout.Label("Layering", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Layering: a flat colour or a second image is composited on top of the source using a " +
                "Photoshop blend mode. The Alpha / Mask block decides where the blend lands and which " +
                "alpha the result keeps.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();

            // ── 1. What gets layered on top ───────────────────────────────────────
            GUILayout.Label("Layer Source", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Type", GUILayout.Width(48));
            layerSourceType = (LayerSourceType)GUILayout.Toolbar((int)layerSourceType,
                new[] { "Color", "Image" });
            EditorGUILayout.EndHorizontal();

            if (layerSourceType == LayerSourceType.Color)
            {
                EditorGUILayout.BeginHorizontal();
                layerColor = EditorGUILayout.ColorField(
                    new GUIContent("Color", "The colour blended on top. Its alpha feeds the Layer Alpha mask."),
                    layerColor);
                if (GUILayout.Button(
                        new GUIContent("← Swatch", "Copy the selected palette swatch into the layer colour."),
                        GUILayout.Width(74)))
                {
                    Color c = ActiveTarget();
                    layerColor = new Color(c.r, c.g, c.b, layerColor.a);
                    GUI.changed = true;
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                var newLayer = (Texture2D)EditorGUILayout.ObjectField(
                    new GUIContent("Image", "The texture blended on top of the source."),
                    layerTexture, typeof(Texture2D), false);
                if (newLayer != layerTexture)
                {
                    layerTexture = newLayer;
                    layerTexturePath = layerTexture ? AssetDatabase.GetAssetPath(layerTexture) : "";
                    GUI.changed = true;
                }

                if (layerTexture == null)
                    EditorGUILayout.HelpBox("Pick a texture to layer, or switch back to Color.",
                        MessageType.Warning);

                layerFit = (LayerFit)EditorGUILayout.EnumPopup(
                    new GUIContent("Fit", "How the layer image is mapped onto the base resolution."),
                    layerFit);

                if (layerFit == LayerFit.Tile)
                    layerTileScale = EditorGUILayout.Slider(
                        new GUIContent("Tile Scale", "Repeats across the base width / height."),
                        layerTileScale, 0.1f, 8f);

                if (layerFit != LayerFit.Stretch)
                    layerOffset = EditorGUILayout.Vector2Field(
                        new GUIContent("Offset", "Shift the layer, in fractions of the base size."),
                        layerOffset);
            }

            EditorGUILayout.Space(4);

            // ── 2. Blend mode + strength ──────────────────────────────────────────
            GUILayout.Label("Blend", EditorStyles.miniBoldLabel);
            layerBlendMode = (LayerBlendMode)EditorGUILayout.EnumPopup(
                new GUIContent("Blend Mode", "The same maths as the Photoshop layer blend modes."),
                layerBlendMode);
            EditorGUILayout.LabelField(" ", BlendModeHint(layerBlendMode), EditorStyles.wordWrappedMiniLabel);

            layerOpacity = EditorGUILayout.Slider(
                new GUIContent("Opacity", "Global strength of the layer. 0 = untouched source."),
                layerOpacity, 0f, 1f);

            layerPreserveLuma = EditorGUILayout.Toggle(
                new GUIContent("Preserve Luminosity",
                    "Re-apply the source's perceptual lightness (OKLab L) after blending — the colour " +
                    "changes but the original shading and contrast survive intact."),
                layerPreserveLuma);

            EditorGUILayout.Space(4);

            // ── 3. Which alpha gates the blend, and which alpha survives ──────────
            GUILayout.Label("Alpha / Mask", EditorStyles.miniBoldLabel);
            layerMask = (BlendMaskSource)EditorGUILayout.EnumPopup(
                new GUIContent("Blend Mask",
                    "Which alpha (or channel) decides how much of the blend lands per pixel. " +
                    "The \"…Both\" options mix the layer's alpha with the base's."),
                layerMask);

            using (new EditorGUI.DisabledGroupScope(layerMask == BlendMaskSource.None))
            {
                layerMaskInvert = EditorGUILayout.Toggle("Invert Mask", layerMaskInvert);
                layerMaskContrast = EditorGUILayout.Slider(
                    new GUIContent("Mask Contrast", "Gamma on the mask. <1 widens the blend, >1 tightens it."),
                    layerMaskContrast, 0.1f, 4f);
            }

            layerAlphaScale = EditorGUILayout.Slider(
                new GUIContent("Layer Alpha ×", "Multiplies the layer's own alpha before it is used."),
                layerAlphaScale, 0f, 2f);

            layerClipToBase = EditorGUILayout.Toggle(
                new GUIContent("Clip To Base Alpha",
                    "Never paint where the source is transparent — keeps sprite silhouettes clean."),
                layerClipToBase);

            EditorGUILayout.Space(2);
            layerResultAlpha = (ResultAlpha)EditorGUILayout.EnumPopup(
                new GUIContent("Result Alpha", "Which alpha the produced texture carries."),
                layerResultAlpha);

            using (new EditorGUI.DisabledGroupScope(layerResultAlpha != ResultAlpha.Mix))
                layerAlphaMix = EditorGUILayout.Slider(
                    new GUIContent("Alpha Mix", "0 = base alpha, 1 = layer alpha."),
                    layerAlphaMix, 0f, 1f);

            return EditorGUI.EndChangeCheck();
        }

        private void ResetLayering()
        {
            layerSourceType = LayerSourceType.Color;
            layerColor = new Color(0.26f, 0.55f, 0.87f, 1f);
            layerFit = LayerFit.Stretch;
            layerTileScale = 1f;
            layerOffset = Vector2.zero;
            layerBlendMode = LayerBlendMode.Multiply;
            layerOpacity = 1f;
            layerAlphaScale = 1f;
            layerMask = BlendMaskSource.LayerAlpha;
            layerMaskInvert = false;
            layerMaskContrast = 1f;
            layerResultAlpha = ResultAlpha.Base;
            layerAlphaMix = 0.5f;
            layerPreserveLuma = false;
            layerClipToBase = false;
        }

        private static string BlendModeHint(LayerBlendMode m)
        {
            switch (m)
            {
                case LayerBlendMode.Normal: return "Plain overlay — the layer simply replaces the base.";
                case LayerBlendMode.Darken: return "Keeps the darker of the two, per channel.";
                case LayerBlendMode.Multiply: return "Darkens; white in the layer is a no-op. Good for shadows and dirt.";
                case LayerBlendMode.ColorBurn: return "Strong darkening with boosted contrast.";
                case LayerBlendMode.LinearBurn: return "Darkens by subtracting brightness. Flatter than Color Burn.";
                case LayerBlendMode.Lighten: return "Keeps the lighter of the two, per channel.";
                case LayerBlendMode.Screen: return "Brightens; black in the layer is a no-op. Good for glow and light.";
                case LayerBlendMode.ColorDodge: return "Strong brightening with boosted contrast.";
                case LayerBlendMode.LinearDodge: return "Additive light — the classic \"Add\" mode.";
                case LayerBlendMode.Overlay: return "Multiply in the shadows, Screen in the highlights. Contrast boost.";
                case LayerBlendMode.SoftLight: return "A gentle Overlay — subtle tinting and shading.";
                case LayerBlendMode.HardLight: return "Overlay driven by the layer instead of the base.";
                case LayerBlendMode.VividLight: return "Burn / Dodge combo. Very strong contrast.";
                case LayerBlendMode.LinearLight: return "Linear Burn / Dodge combo. Strong, and clips easily.";
                case LayerBlendMode.PinLight: return "Replaces tones based on the layer's brightness.";
                case LayerBlendMode.HardMix: return "Posterises to the eight primary corners.";
                case LayerBlendMode.Difference: return "Absolute difference — inverts where the layer is bright.";
                case LayerBlendMode.Exclusion: return "A softer Difference; mid-tones go grey.";
                case LayerBlendMode.Subtract: return "Subtracts the layer from the base.";
                case LayerBlendMode.Divide: return "Divides the base by the layer. Aggressive brightening.";
                case LayerBlendMode.Hue: return "The layer's hue, the base's saturation and brightness.";
                case LayerBlendMode.Saturation: return "The layer's saturation, the base's hue and brightness.";
                case LayerBlendMode.Color: return "The layer's hue + saturation, the base's brightness. Classic tinting.";
                case LayerBlendMode.Luminosity: return "The layer's brightness, the base's colour.";
                default: return "";
            }
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

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                extractR = extractG = extractB = extractA = true;
            if (GUILayout.Button("None", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                extractR = extractG = extractB = extractA = false;
            GUILayout.FlexibleSpace();
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
            Parallel.For(0, sp.Length, i =>
            {
                byte v = channel == 0 ? sp[i].r :
                    channel == 1 ? sp[i].g :
                    channel == 2 ? sp[i].b : sp[i].a;
                dp[i] = new Color32(v, v, v, 255);
            });

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
            Parallel.For(0, dp.Length, i =>
                dp[i] = new Color32(rCh[i], gCh[i], bCh[i], 255));

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
            Parallel.For(0, px.Length, i =>
                ch[i] = invert ? (byte)(255 - px[i].r) : px[i].r);
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
        //  TAB: GRADIENT
        // ═════════════════════════════════════════════════════════════════════════
        private void DrawGradientTab()
        {
            if (gradientPreview == null) RefreshGradientPreview();

            // Ramp + shape
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Gradient", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            gradientRamp = EditorGUILayout.GradientField("Ramp", gradientRamp);
            gradientShape = (GradientShape)EditorGUILayout.EnumPopup(
                new GUIContent("Shape", "Linear — straight ramp along Angle.\nRadial — ramp outward from Position."),
                gradientShape);
            if (EditorGUI.EndChangeCheck()) RefreshGradientPreview();
            EditorGUILayout.EndVertical();

            // Size
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Image Size", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            gradientWidth = Mathf.Clamp(EditorGUILayout.IntField("Width", gradientWidth), 1, 8192);
            gradientHeight = Mathf.Clamp(EditorGUILayout.IntField("Height", gradientHeight), 1, 8192);
            if (EditorGUI.EndChangeCheck()) RefreshGradientPreview();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Presets:", GUILayout.Width(52));
            foreach (int p in new[] { 64, 128, 256, 512, 1024, 2048 })
                if (GUILayout.Button(p.ToString(), GUILayout.Width(44)))
                {
                    gradientWidth = gradientHeight = p;
                    RefreshGradientPreview();
                }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Ramps:", GUILayout.Width(52));
            if (GUILayout.Button(new GUIContent("256 × 1", "Horizontal 1-pixel lookup ramp")))
            {
                gradientWidth = 256;
                gradientHeight = 1;
                RefreshGradientPreview();
            }

            if (GUILayout.Button(new GUIContent("1 × 256", "Vertical 1-pixel lookup ramp")))
            {
                gradientWidth = 1;
                gradientHeight = 256;
                RefreshGradientPreview();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            bool linear = gradientShape == GradientShape.Linear;

            // Direction
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label(linear ? "Direction" : "Direction  (ellipse rotation)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            gradientAngle = EditorGUILayout.Slider(
                new GUIContent("Angle", "0° = left → right, counter-clockwise. 90° = bottom → top."),
                gradientAngle, 0f, 360f);
            if (EditorGUI.EndChangeCheck()) RefreshGradientPreview();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Presets:", GUILayout.Width(52));
            DrawAngleButton("→", 0f);
            DrawAngleButton("↗", 45f);
            DrawAngleButton("↑", 90f);
            DrawAngleButton("↖", 135f);
            DrawAngleButton("←", 180f);
            DrawAngleButton("↙", 225f);
            DrawAngleButton("↓", 270f);
            DrawAngleButton("↘", 315f);
            if (GUILayout.Button(new GUIContent("Flip", "Rotate 180°"), GUILayout.Width(40)))
            {
                gradientAngle = Mathf.Repeat(gradientAngle + 180f, 360f);
                RefreshGradientPreview();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Position
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label(linear ? "Position  (ramp midpoint)" : "Position  (center)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            gradientCenter.x = EditorGUILayout.Slider("X", gradientCenter.x, -1f, 2f);
            gradientCenter.y = EditorGUILayout.Slider("Y", gradientCenter.y, -1f, 2f);
            gradientSpread = EditorGUILayout.Slider(
                new GUIContent(linear ? "Length" : "Radius",
                    linear
                        ? "1 = the ramp spans the whole image along Angle."
                        : "1 = the ramp ends at the image edge."),
                gradientSpread, 0.01f, 4f);
            gradientWrap = (GradientWrapMode)EditorGUILayout.EnumPopup(
                new GUIContent("Beyond Ends", "How the area outside the ramp is filled."), gradientWrap);
            if (!linear)
                gradientCircular = EditorGUILayout.Toggle(
                    new GUIContent("Keep Circular",
                        "Compensate for non-square images so the gradient stays a circle."),
                    gradientCircular);
            if (EditorGUI.EndChangeCheck()) RefreshGradientPreview();

            // 3 × 3 position presets, laid out like the image (top row = y 1)
            for (int row = 0; row < 3; row++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(row == 1 ? "Presets:" : " ", GUILayout.Width(52));
                for (int col = 0; col < 3; col++)
                {
                    float px = col * 0.5f, py = 1f - row * 0.5f;
                    bool active = Mathf.Approximately(gradientCenter.x, px) &&
                                  Mathf.Approximately(gradientCenter.y, py);
                    if (GUILayout.Toggle(active, new GUIContent("●", $"({px:0.#}, {py:0.#})"),
                            "Button", GUILayout.Width(26)) && !active)
                    {
                        gradientCenter = new Vector2(px, py);
                        RefreshGradientPreview();
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // Output
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Output", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            gradientOutputPath = EditorGUILayout.TextField("Folder  (Assets/…)", gradientOutputPath);
            if (GUILayout.Button("…", GUILayout.Width(26)))
            {
                string picked = EditorUtility.OpenFolderPanel("Output Folder", gradientOutputPath, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    string root = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length)
                        .Replace('\\', '/');
                    picked = picked.Replace('\\', '/');
                    if (picked.StartsWith(root)) gradientOutputPath = picked.Substring(root.Length).TrimEnd('/');
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndHorizontal();
            gradientOutputName = EditorGUILayout.TextField("File Name", gradientOutputName);

            EditorGUI.BeginChangeCheck();
            gradientDither = EditorGUILayout.Toggle(
                new GUIContent("Dither", "Ordered dithering — removes 8-bit banding on long, subtle ramps."),
                gradientDither);
            if (EditorGUI.EndChangeCheck()) RefreshGradientPreview();

            gradientSRGB = EditorGUILayout.Toggle(
                new GUIContent("sRGB", "Off for data ramps (masks, lookup tables)."), gradientSRGB);
            gradientAsSprite = EditorGUILayout.Toggle(
                new GUIContent("Import as Sprite", "Sets the texture type to Sprite (2D and UI)."), gradientAsSprite);
            EditorGUILayout.LabelField("Saved as PNG, uncompressed, no mipmaps.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Preview
            DrawPreviewWidget(gradientPreview, PREVIEW_MAX);
            if (gradientPreview != null &&
                (gradientPreview.width != gradientWidth || gradientPreview.height != gradientHeight))
                EditorGUILayout.LabelField(
                    $"Preview downscaled — output will be {gradientWidth} × {gradientHeight} px",
                    EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(
                    new GUIContent("↺  Reset",
                        "Restores direction, position, wrap and output options to their defaults.\n" +
                        "The ramp and the image size are left alone."),
                    GUILayout.Height(28), GUILayout.Width(90)))
                ResetGradientSettings();

            if (GUILayout.Button("Refresh Preview", GUILayout.Height(28)))
            {
                RefreshGradientPreview();
                Repaint();
            }

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("💾  Generate & Save", GUILayout.Height(28))) SaveGradient();
            GUI.backgroundColor = prev;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
        }

        // Resets everything except the ramp itself and the image size.
        private void ResetGradientSettings()
        {
            gradientShape = GradientShape.Linear;
            gradientAngle = 0f;
            gradientCenter = new Vector2(0.5f, 0.5f);
            gradientSpread = 1f;
            gradientWrap = GradientWrapMode.Clamp;
            gradientCircular = true;
            gradientDither = true;
            gradientSRGB = true;
            gradientAsSprite = false;
            gradientOutputPath = "Assets";
            gradientOutputName = "Gradient";
            GUI.FocusControl(null);
            RefreshGradientPreview();
            Repaint();
        }

        private void DrawAngleButton(string label, float deg)
        {
            bool active = Mathf.Approximately(gradientAngle, deg);
            if (GUILayout.Toggle(active, new GUIContent(label, $"{deg}°"), "Button", GUILayout.Width(28)) && !active)
            {
                gradientAngle = deg;
                RefreshGradientPreview();
            }
        }

        private void RefreshGradientPreview()
        {
            // Preview is capped so dragging sliders with a 4K output stays interactive.
            float scale = Mathf.Min(1f, GRADIENT_PREVIEW_MAX / Mathf.Max(gradientWidth, gradientHeight));
            int pw = Mathf.Max(1, Mathf.RoundToInt(gradientWidth * scale));
            int ph = Mathf.Max(1, Mathf.RoundToInt(gradientHeight * scale));

            Texture2D next = BuildGradient(pw, ph);
            if (gradientPreview != null) DestroyImmediate(gradientPreview);
            gradientPreview = next;
        }

        private void SaveGradient()
        {
            string folder = gradientOutputPath.TrimEnd('/', '\\');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("Error", $"Folder '{folder}' not found in project.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(gradientOutputName))
            {
                EditorUtility.DisplayDialog("Error", "File name is empty.", "OK");
                return;
            }

            Texture2D tex = BuildGradient(gradientWidth, gradientHeight);
            string outPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{gradientOutputName}.png");
            WriteTexture(tex, outPath);
            DestroyImmediate(tex);
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

            var imp = AssetImporter.GetAtPath(outPath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = gradientAsSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
                if (gradientAsSprite) imp.spriteImportMode = SpriteImportMode.Single;
                imp.sRGBTexture = gradientSRGB;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = gradientWrap == GradientWrapMode.Clamp
                    ? TextureWrapMode.Clamp
                    : TextureWrapMode.Repeat;
                // Block compression bands badly on smooth ramps.
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }

            AssetDatabase.Refresh();
            var saved = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
            if (saved != null) EditorGUIUtility.PingObject(saved);
            RefreshGradientPreview();
            Debug.Log($"[ImageManipulator] Gradient saved → {outPath}");
        }

        // Renders the gradient at an arbitrary resolution.
        // Instance fields are copied to locals before the parallel fill.
        private Texture2D BuildGradient(int w, int h)
        {
            w = Mathf.Clamp(w, 1, 8192);
            h = Mathf.Clamp(h, 1, 8192);

            // Gradient.Evaluate is main-thread only → bake it into a LUT first.
            Color[] lut = new Color[GRADIENT_LUT];
            for (int i = 0; i < GRADIENT_LUT; i++)
                lut[i] = gradientRamp.Evaluate(i / (float)(GRADIENT_LUT - 1));

            float rad = gradientAngle * Mathf.Deg2Rad;
            float dx = Mathf.Cos(rad), dy = Mathf.Sin(rad);
            // Normalising by |dx|+|dy| makes Length = 1 span the image at any angle.
            float linLen = Mathf.Max(0.0001f, (Mathf.Abs(dx) + Mathf.Abs(dy)) * gradientSpread);
            float radius = Mathf.Max(0.0001f, gradientSpread * 0.5f);
            float aspect = w / (float)h;

            Vector2 c = gradientCenter;
            GradientShape shape = gradientShape;
            GradientWrapMode wrap = gradientWrap;
            bool circular = gradientCircular;
            bool dither = gradientDither;

            Color32[] px = new Color32[w * h];
            Parallel.For(0, h, y =>
            {
                float v = (y + 0.5f) / h - c.y;
                int row = y * w;
                int bayerRow = (y & 3) << 2;
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w - c.x;

                    float t;
                    if (shape == GradientShape.Linear)
                    {
                        t = (u * dx + v * dy) / linLen + 0.5f;
                    }
                    else
                    {
                        // Rotate into the ellipse's own space, then un-squash the short axis.
                        float ex = u * dx + v * dy;
                        float ey = -u * dy + v * dx;
                        if (circular)
                        {
                            if (aspect > 1f) ex *= aspect;
                            else ey /= aspect;
                        }

                        t = Mathf.Sqrt(ex * ex + ey * ey) / radius;
                    }

                    Color col = lut[(int)(WrapGradientT(t, wrap) * (GRADIENT_LUT - 1) + 0.5f)];
                    float d = dither ? Bayer4[bayerRow | (x & 3)] : 0f;
                    px[row + x] = new Color32(
                        QuantizeDithered(col.r, d), QuantizeDithered(col.g, d),
                        QuantizeDithered(col.b, d), QuantizeDithered(col.a, d));
                }
            });

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // Always returns a value inside 0…1.
        private static float WrapGradientT(float t, GradientWrapMode mode)
        {
            switch (mode)
            {
                case GradientWrapMode.Repeat:
                    t -= Mathf.Floor(t);
                    return t >= 1f ? 0.9999f : t;
                case GradientWrapMode.PingPong:
                    float p = Mathf.Abs(t) % 2f;
                    return p > 1f ? 2f - p : p;
                default:
                    return t < 0f ? 0f : (t > 1f ? 1f : t);
            }
        }

        private static byte QuantizeDithered(float v, float dither)
        {
            int i = (int)(v * 255f + dither + 0.5f);
            return (byte)(i < 0 ? 0 : (i > 255 ? 255 : i));
        }

        private static float[] BuildBayer4()
        {
            int[] m = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
            float[] o = new float[16];
            for (int i = 0; i < 16; i++) o[i] = (m[i] + 0.5f) / 16f - 0.5f;
            return o;
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

        // Side-by-side "Original vs Result". `after` may be null until Apply is pressed.
        private void DrawBeforeAfterPreview(Texture2D before, Texture2D after)
        {
            if (before == null) return;
            EditorGUILayout.BeginVertical(sectionBox);
            GUILayout.Label("Preview", EditorStyles.boldLabel);

            float box = Mathf.Clamp((EditorGUIUtility.currentViewWidth - 60f) * 0.5f, 80f, PREVIEW_MAX);
            EditorGUILayout.BeginHorizontal();
            DrawLabeledThumb("Original", before, box);
            GUILayout.Space(8);
            DrawLabeledThumb(after != null ? "Result" : "Result  (press Apply)", after, box);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawLabeledThumb(string label, Texture2D tex, float box)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(box));
            GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(box));
            if (tex != null)
            {
                float scale = Mathf.Min(box / tex.width, box / tex.height, 1f);
                float pw = tex.width * scale, ph = tex.height * scale;
                Rect r = GUILayoutUtility.GetRect(box, ph, GUILayout.Width(box), GUILayout.Height(ph));
                EditorGUI.DrawTextureTransparent(new Rect(r.x + (box - pw) * 0.5f, r.y, pw, ph), tex);
            }
            else
            {
                GUILayout.Box("—", GUILayout.Width(box), GUILayout.Height(box));
            }

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
            Parallel.For(0, ht, y =>
            {
                for (int x = 0; x < w; x++)
                    dp[y * w + x] = sp[(v ? ht - 1 - y : y) * w + (h ? w - 1 - x : x)];
            });
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
            Parallel.For(0, sh, y =>
            {
                for (int x = 0; x < sw; x++)
                    dp[ccw ? x * sh + (sh - 1 - y) : (sw - 1 - x) * sh + y] = sp[y * sw + x];
            });
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
            Parallel.For(0, h, y =>
            {
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
            });

            var res = new Texture2D(w, h, TextureFormat.RGBA32, false);
            res.SetPixels32(dp);
            res.Apply();
            return res;
        }

        private Texture2D ResizeTexture(Texture2D src, int tw, int th)
        {
            Color32[] sp = src.GetPixels32(), dp = new Color32[tw * th];
            int sw = src.width, sh = src.height;
            Parallel.For(0, th, dy =>
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
            });

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
                             && !tintEnabled;
            if (isDefault) return src;

            Color32[] sp = src.GetPixels32(), dp = new Color32[sp.Length];
            float tr = tintEnabled ? tintColor.r : 1f;
            float tg = tintEnabled ? tintColor.g : 1f;
            float tb = tintEnabled ? tintColor.b : 1f;
            float cf = contrast >= 0f ? 1f + contrast * 3f : 1f + contrast;
            float bright = brightness, sat = saturation; // locals for the parallel closure

            // Per-pixel work is independent → run it across all cores.
            Parallel.For(0, sp.Length, i =>
            {
                float r = sp[i].r / 255f, g = sp[i].g / 255f, b = sp[i].b / 255f, a = sp[i].a / 255f;

                // Brightness
                r += bright;
                g += bright;
                b += bright;
                // Contrast (pivot 0.5)
                r = (r - 0.5f) * cf + 0.5f;
                g = (g - 0.5f) * cf + 0.5f;
                b = (b - 0.5f) * cf + 0.5f;
                // Saturation / greyscale
                float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                r = lum + (r - lum) * sat;
                g = lum + (g - lum) * sat;
                b = lum + (b - lum) * sat;
                // Tint
                r *= tr;
                g *= tg;
                b *= tb;

                dp[i] = new Color32(
                    (byte)(Mathf.Clamp01(r) * 255), (byte)(Mathf.Clamp01(g) * 255),
                    (byte)(Mathf.Clamp01(b) * 255), (byte)(Mathf.Clamp01(a) * 255));
            });

            var result = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            result.SetPixels32(dp);
            result.Apply();
            return result;
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  RECOLOR PIPELINE  (OKLab – perceptual, texture-preserving)
        // ═════════════════════════════════════════════════════════════════════════
        //
        //  Why OKLab and not HSV?
        //  In HSV a hue change keeps "Value" constant, but perceptually yellow is far
        //  brighter than blue at the same Value → naive hue shifts look wrong.
        //  OKLab separates perceptual Lightness (L) from colour (a,b). We keep L (that
        //  is the texture / shading detail) and only move a,b, so every palette colour
        //  automatically lands at its correct perceived brightness.
        //
        private Color ActiveTarget() =>
            palette.Count > 0 && selectedSwatch >= 0 && selectedSwatch < palette.Count
                ? palette[selectedSwatch]
                : Color.gray;

        // ── Palette persistence (EditorPrefs – survives restarts & domain reloads) ─
        private const string PalettePrefsKey = "DataKeeper.ImageManipulator.Palette";

        private void LoadPalette()
        {
            string s = EditorPrefs.GetString(PalettePrefsKey, "");
            if (string.IsNullOrEmpty(s)) return; // no saved palette → keep the defaults

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            const System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Float;
            var parsed = new List<Color>();
            foreach (string entry in s.Split(';'))
            {
                string[] p = entry.Split(',');
                if (p.Length == 4 &&
                    float.TryParse(p[0], ns, inv, out float r) &&
                    float.TryParse(p[1], ns, inv, out float g) &&
                    float.TryParse(p[2], ns, inv, out float b) &&
                    float.TryParse(p[3], ns, inv, out float a))
                    parsed.Add(new Color(r, g, b, a));
            }

            if (parsed.Count == 0) return;
            palette.Clear();
            palette.AddRange(parsed);
            selectedSwatch = Mathf.Clamp(selectedSwatch, 0, palette.Count - 1);
        }

        private void SavePalette()
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < palette.Count; i++)
            {
                Color c = palette[i];
                if (i > 0) sb.Append(';');
                sb.Append(c.r.ToString(inv)).Append(',')
                    .Append(c.g.ToString(inv)).Append(',')
                    .Append(c.b.ToString(inv)).Append(',')
                    .Append(c.a.ToString(inv));
            }

            EditorPrefs.SetString(PalettePrefsKey, sb.ToString());
        }

        private void RefreshRecolorPreview()
        {
            if (sourceTexture == null)
            {
                recolorPreview = null;
                return;
            }

            EnsureReadable(assetPath);

            // Preview runs on a downscaled copy so "Apply" stays snappy even on 2K/4K
            // textures. Saving (SaveRecolor) always processes the full-resolution source.
            Texture2D previewSrc = GetReadableCopy(sourceTexture);
            const int cap = 512;
            if (previewSrc.width > cap || previewSrc.height > cap)
            {
                float s = Mathf.Min((float)cap / previewSrc.width, (float)cap / previewSrc.height);
                previewSrc = ResizeTexture(previewSrc,
                    Mathf.Max(1, Mathf.RoundToInt(previewSrc.width * s)),
                    Mathf.Max(1, Mathf.RoundToInt(previewSrc.height * s)));
            }

            recolorPreview = ApplyRecolor(previewSrc);
            Repaint();
        }

        private void SaveRecolor(bool overwrite)
        {
            if (sourceTexture == null) return;

            EnsureReadable(assetPath);
            Texture2D finalTex = ApplyRecolor(GetReadableCopy(sourceTexture));

            string suffix = recolorMode == RecolorMode.Layering ? "_layered" : "_recolored";
            string savedPath = overwrite
                ? assetPath
                : AssetDatabase.GenerateUniqueAssetPath(
                    $"{Path.GetDirectoryName(assetPath)}" +
                    $"/{Path.GetFileNameWithoutExtension(assetPath)}{suffix}" +
                    $"{Path.GetExtension(assetPath)}");

            WriteTexture(finalTex, savedPath);
            AssetDatabase.ImportAsset(savedPath, ImportAssetOptions.ForceUpdate);

            var srcImp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            var dstImp = AssetImporter.GetAtPath(savedPath) as TextureImporter;
            if (srcImp != null && dstImp != null && !overwrite) CopyTextureImporterSettings(srcImp, dstImp);
            TrackAndSetReadable(dstImp, savedPath);

            AssetDatabase.Refresh();
            string verb = recolorMode == RecolorMode.Layering ? "Layered" : "Recolored";
            EditorUtility.DisplayDialog("Saved", $"{verb} texture saved to:\n{savedPath}", "OK");
            Debug.Log($"[ImageManipulator] {verb} → {savedPath}");
        }

        private Texture2D ApplyRecolor(Texture2D src)
        {
            if (recolorMode == RecolorMode.Layering) return ApplyLayering(src);

            RgbToOklab(ActiveTarget(), out _, out float ta, out float tb);
            ta *= recolorChroma;
            tb *= recolorChroma;

            Color32[] sp = src.GetPixels32();
            Color32[] dp = new Color32[sp.Length];

            // Hue-shift needs the source's dominant hue → precompute the mean (a,b).
            float cosD = 1f, sinD = 0f;
            if (recolorMode == RecolorMode.HueShift)
            {
                double sumA = 0, sumB = 0;
                for (int i = 0; i < sp.Length; i++)
                {
                    RgbToOklab(sp[i], out _, out float pa, out float pb);
                    sumA += pa;
                    sumB += pb;
                }

                float meanHue = Mathf.Atan2((float)sumB, (float)sumA);
                float targetHue = Mathf.Atan2(tb, ta);
                float delta = targetHue - meanHue;
                while (delta > Mathf.PI) delta -= 2f * Mathf.PI;
                while (delta < -Mathf.PI) delta += 2f * Mathf.PI;
                delta *= recolorStrength;
                cosD = Mathf.Cos(delta);
                sinD = Mathf.Sin(delta);
            }

            // Locals for the parallel closure (avoid touching instance fields per-pixel).
            bool recolorModeIsRecolor = recolorMode == RecolorMode.Recolor;
            float strength = recolorStrength, chroma = recolorChroma, lightShift = recolorLightness;
            bool naturalShading = recolorNaturalShading;

            // Per-pixel work is independent → run it across all cores.
            Parallel.For(0, sp.Length, i =>
            {
                RgbToOklab(sp[i], out float pl, out float pa, out float pb);

                float oa, ob;
                if (recolorModeIsRecolor)
                {
                    oa = Mathf.Lerp(pa, ta, strength);
                    ob = Mathf.Lerp(pb, tb, strength);
                }
                else // HueShift – rotate the pixel's chroma around the grey axis
                {
                    oa = (pa * cosD - pb * sinD) * chroma;
                    ob = (pa * sinD + pb * cosD) * chroma;
                }

                float ol = Mathf.Clamp01(pl + lightShift);

                // Colour theory: highlights desaturate toward white, shadows toward
                // black. Faded in by strength so Strength 0 stays a true identity.
                if (naturalShading)
                {
                    float env = 4f * ol * (1f - ol); // 1 at mid-grey, 0 at the extremes
                    float envEff = Mathf.Lerp(1f, env, strength);
                    oa *= envEff;
                    ob *= envEff;
                }

                OklabToRgb(ol, oa, ob, out float r, out float g, out float b);
                dp[i] = new Color32(
                    (byte)(Mathf.Clamp01(r) * 255f),
                    (byte)(Mathf.Clamp01(g) * 255f),
                    (byte)(Mathf.Clamp01(b) * 255f),
                    sp[i].a);
            });

            var res = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            res.SetPixels32(dp);
            res.Apply();
            return res;
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  LAYERING PIPELINE  (Photoshop-style blend compositing)
        // ═════════════════════════════════════════════════════════════════════════
        //
        //  Blending runs on sRGB-encoded 0…1 values, not linear light — that is what
        //  Photoshop does by default, so Multiply/Screen/Overlay land on the numbers
        //  an artist expects from the same modes in Photoshop.
        //
        //  Compositing follows the W3C formula so a transparent base behaves:
        //      Cs' = (1 − αb)·Cs + αb·B(Cb, Cs)      blend fades out over holes
        //      Cr  = (1 − αEff)·Cb + αEff·Cs'        αEff = opacity × mask
        //
        private Texture2D ApplyLayering(Texture2D src)
        {
            int w = src.width, h = src.height;
            Color32[] sp = src.GetPixels32();
            Color32[] dp = new Color32[sp.Length];
            Color32[] lp = BuildLayerBuffer(w, h);

            // Locals for the parallel closure (never touch instance fields per-pixel).
            LayerBlendMode mode = layerBlendMode;
            BlendMaskSource maskSrc = layerMask;
            ResultAlpha resAlpha = layerResultAlpha;
            float opacity = layerOpacity, alphaScale = layerAlphaScale;
            float maskGamma = layerMaskContrast, alphaMix = layerAlphaMix;
            bool maskInvert = layerMaskInvert, preserveLuma = layerPreserveLuma, clipToBase = layerClipToBase;

            Parallel.For(0, sp.Length, i =>
            {
                Color32 bc = sp[i], lc = lp[i];

                float br = bc.r / 255f, bg = bc.g / 255f, bb = bc.b / 255f, ba = bc.a / 255f;
                float sr = lc.r / 255f, sg = lc.g / 255f, sb = lc.b / 255f;
                float sa = Mathf.Clamp01(lc.a / 255f * alphaScale);

                // ── mask: how much of the blend lands here ────────────────────────
                float mask;
                switch (maskSrc)
                {
                    case BlendMaskSource.None: mask = 1f; break;
                    case BlendMaskSource.LayerAlpha: mask = sa; break;
                    case BlendMaskSource.BaseAlpha: mask = ba; break;
                    case BlendMaskSource.MultiplyBoth: mask = sa * ba; break;
                    case BlendMaskSource.MinBoth: mask = Mathf.Min(sa, ba); break;
                    case BlendMaskSource.MaxBoth: mask = Mathf.Max(sa, ba); break;
                    case BlendMaskSource.AverageBoth: mask = (sa + ba) * 0.5f; break;
                    case BlendMaskSource.LayerLuminance: mask = Lum(sr, sg, sb); break;
                    case BlendMaskSource.LayerR: mask = sr; break;
                    case BlendMaskSource.LayerG: mask = sg; break;
                    case BlendMaskSource.LayerB: mask = sb; break;
                    default: mask = 1f; break;
                }

                if (maskInvert) mask = 1f - mask;
                if (maskSrc != BlendMaskSource.None && !Mathf.Approximately(maskGamma, 1f))
                    mask = Mathf.Pow(Mathf.Clamp01(mask), maskGamma);

                float eff = Mathf.Clamp01(opacity * Mathf.Clamp01(mask));
                if (clipToBase) eff *= ba;

                // ── blend ─────────────────────────────────────────────────────────
                float rr, rg, rb;
                if (eff <= 0f)
                {
                    rr = br; rg = bg; rb = bb;
                }
                else
                {
                    Blend(mode, br, bg, bb, sr, sg, sb, out float nr, out float ng, out float nb);

                    // Fade the blend back toward the raw layer colour over holes in
                    // the base, so blending onto transparency stays well-defined.
                    nr = (1f - ba) * sr + ba * nr;
                    ng = (1f - ba) * sg + ba * ng;
                    nb = (1f - ba) * sb + ba * nb;

                    rr = Mathf.Lerp(br, nr, eff);
                    rg = Mathf.Lerp(bg, ng, eff);
                    rb = Mathf.Lerp(bb, nb, eff);

                    if (preserveLuma)
                    {
                        // Swap the blended colour's OKLab lightness for the base's.
                        RgbToOklab(new Color(br, bg, bb), out float baseL, out _, out _);
                        RgbToOklab(new Color(Mathf.Clamp01(rr), Mathf.Clamp01(rg), Mathf.Clamp01(rb)),
                            out _, out float oa, out float ob);
                        OklabToRgb(baseL, oa, ob, out rr, out rg, out rb);
                    }
                }

                // ── result alpha ──────────────────────────────────────────────────
                float outA;
                switch (resAlpha)
                {
                    case ResultAlpha.Base: outA = ba; break;
                    case ResultAlpha.Layer: outA = sa; break;
                    case ResultAlpha.Multiply: outA = ba * sa; break;
                    case ResultAlpha.Min: outA = Mathf.Min(ba, sa); break;
                    case ResultAlpha.Max: outA = Mathf.Max(ba, sa); break;
                    case ResultAlpha.Mix: outA = Mathf.Lerp(ba, sa, alphaMix); break;
                    case ResultAlpha.Union: outA = ba + eff - ba * eff; break;
                    case ResultAlpha.Opaque: outA = 1f; break;
                    default: outA = ba; break;
                }

                dp[i] = new Color32(
                    (byte)(Mathf.Clamp01(rr) * 255f),
                    (byte)(Mathf.Clamp01(rg) * 255f),
                    (byte)(Mathf.Clamp01(rb) * 255f),
                    (byte)(Mathf.Clamp01(outA) * 255f));
            });

            var res = new Texture2D(w, h, TextureFormat.RGBA32, false);
            res.SetPixels32(dp);
            res.Apply();
            return res;
        }

        // Builds the top layer at the base resolution: either a flat colour, or the
        // picked texture mapped in with the selected fit mode.
        private Color32[] BuildLayerBuffer(int w, int h)
        {
            var buf = new Color32[w * h];

            if (layerSourceType == LayerSourceType.Color || layerTexture == null)
            {
                Color32 c = layerColor;
                for (int i = 0; i < buf.Length; i++) buf[i] = c;
                return buf;
            }

            // The path can be stale after a domain reload — re-resolve before relying on it.
            if (string.IsNullOrEmpty(layerTexturePath))
                layerTexturePath = AssetDatabase.GetAssetPath(layerTexture);
            EnsureReadable(layerTexturePath);
            Texture2D lay = GetReadableCopy(layerTexture);

            // Stretch is just a resample; the rest place a scaled copy on a
            // transparent canvas (or repeat it), so do the scaling up front.
            int lw = lay.width, lh = lay.height;
            switch (layerFit)
            {
                case LayerFit.Stretch:
                    lay = ResizeTexture(lay, w, h);
                    lw = w;
                    lh = h;
                    break;
                case LayerFit.Fit:
                case LayerFit.Fill:
                {
                    float s = layerFit == LayerFit.Fit
                        ? Mathf.Min((float)w / lw, (float)h / lh)
                        : Mathf.Max((float)w / lw, (float)h / lh);
                    lw = Mathf.Max(1, Mathf.RoundToInt(lay.width * s));
                    lh = Mathf.Max(1, Mathf.RoundToInt(lay.height * s));
                    lay = ResizeTexture(lay, lw, lh);
                    break;
                }
                case LayerFit.Tile:
                {
                    float s = Mathf.Max(0.01f, layerTileScale);
                    lw = Mathf.Max(1, Mathf.RoundToInt(w / s));
                    lh = Mathf.Max(1, Mathf.RoundToInt(h / s));
                    lay = ResizeTexture(lay, lw, lh);
                    break;
                }
                // Center keeps the layer's native pixels.
            }

            Color32[] src = lay.GetPixels32();
            bool tile = layerFit == LayerFit.Tile;
            int offX = Mathf.RoundToInt(layerOffset.x * w) + (tile ? 0 : (w - lw) / 2);
            int offY = Mathf.RoundToInt(layerOffset.y * h) + (tile ? 0 : (h - lh) / 2);
            Color32 clear = new Color32(0, 0, 0, 0);

            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    int sx = x - offX, sy = y - offY;
                    if (tile)
                    {
                        sx = ((sx % lw) + lw) % lw;
                        sy = ((sy % lh) + lh) % lh;
                    }
                    else if (sx < 0 || sx >= lw || sy < 0 || sy >= lh)
                    {
                        buf[y * w + x] = clear;
                        continue;
                    }

                    buf[y * w + x] = src[sy * lw + sx];
                }
            });

            return buf;
        }

        // ── Blend dispatch. Separable modes run per channel; the last four need all
        //    three channels at once, so they are handled before the split. ──────────
        private static void Blend(LayerBlendMode m,
            float br, float bg, float bb, float sr, float sg, float sb,
            out float rr, out float rg, out float rb)
        {
            switch (m)
            {
                case LayerBlendMode.Hue:
                    SetLum(SetSat(sr, sg, sb, Sat(br, bg, bb)), Lum(br, bg, bb), out rr, out rg, out rb);
                    return;
                case LayerBlendMode.Saturation:
                    SetLum(SetSat(br, bg, bb, Sat(sr, sg, sb)), Lum(br, bg, bb), out rr, out rg, out rb);
                    return;
                case LayerBlendMode.Color:
                    SetLum(new Vector3(sr, sg, sb), Lum(br, bg, bb), out rr, out rg, out rb);
                    return;
                case LayerBlendMode.Luminosity:
                    SetLum(new Vector3(br, bg, bb), Lum(sr, sg, sb), out rr, out rg, out rb);
                    return;
                default:
                    rr = BlendChannel(m, br, sr);
                    rg = BlendChannel(m, bg, sg);
                    rb = BlendChannel(m, bb, sb);
                    return;
            }
        }

        // b = base (backdrop), s = layer (source). Both sRGB-encoded 0…1.
        private static float BlendChannel(LayerBlendMode m, float b, float s)
        {
            switch (m)
            {
                case LayerBlendMode.Normal: return s;
                case LayerBlendMode.Darken: return Mathf.Min(b, s);
                case LayerBlendMode.Multiply: return b * s;
                case LayerBlendMode.ColorBurn: return s <= 0f ? 0f : 1f - Mathf.Min(1f, (1f - b) / s);
                case LayerBlendMode.LinearBurn: return b + s - 1f;
                case LayerBlendMode.Lighten: return Mathf.Max(b, s);
                case LayerBlendMode.Screen: return b + s - b * s;
                case LayerBlendMode.ColorDodge: return s >= 1f ? 1f : Mathf.Min(1f, b / (1f - s));
                case LayerBlendMode.LinearDodge: return b + s;
                case LayerBlendMode.Overlay: return HardLight(s, b); // Hard Light with the roles swapped
                case LayerBlendMode.HardLight: return HardLight(b, s);
                case LayerBlendMode.SoftLight: return SoftLight(b, s);
                case LayerBlendMode.VividLight:
                    return s <= 0.5f
                        ? BlendChannel(LayerBlendMode.ColorBurn, b, 2f * s)
                        : BlendChannel(LayerBlendMode.ColorDodge, b, 2f * (s - 0.5f));
                case LayerBlendMode.LinearLight: return b + 2f * s - 1f;
                case LayerBlendMode.PinLight:
                    return s <= 0.5f ? Mathf.Min(b, 2f * s) : Mathf.Max(b, 2f * s - 1f);
                case LayerBlendMode.HardMix: return b + 2f * s - 1f >= 1f ? 1f : 0f;
                case LayerBlendMode.Difference: return Mathf.Abs(b - s);
                case LayerBlendMode.Exclusion: return b + s - 2f * b * s;
                case LayerBlendMode.Subtract: return b - s;
                case LayerBlendMode.Divide: return s <= 0f ? 1f : Mathf.Min(1f, b / s);
                default: return s;
            }
        }

        private static float HardLight(float b, float s) =>
            s <= 0.5f ? b * (2f * s) : 1f - (1f - b) * (2f - 2f * s);

        private static float SoftLight(float b, float s)
        {
            if (s <= 0.5f) return b - (1f - 2f * s) * b * (1f - b);
            float d = b <= 0.25f ? ((16f * b - 12f) * b + 4f) * b : Mathf.Sqrt(b);
            return b + (2f * s - 1f) * (d - b);
        }

        // ── Non-separable helpers (W3C compositing spec) ──────────────────────────
        private static float Lum(float r, float g, float b) => 0.3f * r + 0.59f * g + 0.11f * b;

        private static float Sat(float r, float g, float b) =>
            Mathf.Max(r, Mathf.Max(g, b)) - Mathf.Min(r, Mathf.Min(g, b));

        // Shift a colour to the target luminosity, then pull it back into gamut.
        private static void SetLum(Vector3 c, float l, out float rr, out float rg, out float rb)
        {
            float d = l - Lum(c.x, c.y, c.z);
            float r = c.x + d, g = c.y + d, b = c.z + d;

            float lum = Lum(r, g, b);
            float n = Mathf.Min(r, Mathf.Min(g, b));
            float x = Mathf.Max(r, Mathf.Max(g, b));

            if (n < 0f && lum - n > 1e-6f)
            {
                float k = lum / (lum - n);
                r = lum + (r - lum) * k;
                g = lum + (g - lum) * k;
                b = lum + (b - lum) * k;
            }

            if (x > 1f && x - lum > 1e-6f)
            {
                float k = (1f - lum) / (x - lum);
                r = lum + (r - lum) * k;
                g = lum + (g - lum) * k;
                b = lum + (b - lum) * k;
            }

            rr = r;
            rg = g;
            rb = b;
        }

        // Rescale a colour so its saturation becomes `s`: the darkest channel goes to
        // 0, the brightest to s, and the middle one keeps its relative position.
        // Indices are used rather than value comparisons so duplicate channel values
        // (grey, or any two-equal colour) can't land on the wrong branch.
        private static Vector3 SetSat(float r, float g, float b, float s)
        {
            int iMin = 0, iMax = 0;
            float mn = r, mx = r;
            if (g < mn) { mn = g; iMin = 1; }
            if (b < mn) { mn = b; iMin = 2; }
            if (g > mx) { mx = g; iMax = 1; }
            if (b > mx) { mx = b; iMax = 2; }

            if (iMin == iMax || mx - mn <= 1e-6f)
                return Vector3.zero; // flat colour → nothing to rescale

            int iMid = 3 - iMin - iMax;
            float mid = iMid == 0 ? r : iMid == 1 ? g : b;

            var outc = Vector3.zero;
            outc[iMax] = s;
            outc[iMid] = (mid - mn) * s / (mx - mn);
            outc[iMin] = 0f;
            return outc;
        }

        // ── OKLab conversions (Björn Ottosson) – done in linear light ──────────────
        private static float SrgbToLinear(float c) =>
            c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);

        private static float LinearToSrgb(float c) =>
            c <= 0.0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1f / 2.4f) - 0.055f;

        // sign-safe cube root (Mathf.Pow throws on negatives, which out-of-gamut hits)
        private static float Cbrt(float x) =>
            x < 0f ? -Mathf.Pow(-x, 1f / 3f) : Mathf.Pow(x, 1f / 3f);

        private static void RgbToOklab(Color32 c, out float L, out float a, out float b) =>
            LinearRgbToOklab(
                SrgbToLinear(c.r / 255f), SrgbToLinear(c.g / 255f), SrgbToLinear(c.b / 255f),
                out L, out a, out b);

        private static void RgbToOklab(Color c, out float L, out float a, out float b) =>
            LinearRgbToOklab(
                SrgbToLinear(c.r), SrgbToLinear(c.g), SrgbToLinear(c.b),
                out L, out a, out b);

        private static void LinearRgbToOklab(float r, float g, float bl,
            out float L, out float a, out float b)
        {
            float l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * bl;
            float m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * bl;
            float s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * bl;

            float l_ = Cbrt(l), m_ = Cbrt(m), s_ = Cbrt(s);

            L = 0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_;
            a = 1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_;
            b = 0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_;
        }

        private static void OklabToRgb(float L, float a, float b,
            out float r, out float g, out float bl)
        {
            float l_ = L + 0.3963377774f * a + 0.2158037573f * b;
            float m_ = L - 0.1055613458f * a - 0.0638541728f * b;
            float s_ = L - 0.0894841775f * a - 1.2914855480f * b;

            float l = l_ * l_ * l_, m = m_ * m_ * m_, s = s_ * s_ * s_;

            float lr = 4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s;
            float lg = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s;
            float lb = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s;

            r = LinearToSrgb(lr);
            g = LinearToSrgb(lg);
            bl = LinearToSrgb(lb);
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