using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace DataKeeper.Editor.Windows
{
    enum TargetMode
    {
        Shader,
        MaterialTemplate
    }

    public class MaterialShaderConverter : EditorWindow
    {
        Object sourceObject;

        enum TargetMode
        {
            Shader,
            MaterialTemplate
        }

        TargetMode targetMode = TargetMode.Shader;

        Shader targetShader;
        Material targetMaterial;

        DefaultAsset outputFolder;

        const string NONE = "<None>";

        Vector2 scroll;

        class ShaderGroup
        {
            public Shader shader;
            public bool foldout = true;

            public List<Material> materials = new List<Material>();
            public List<string> sourceSlots = new List<string>();

            public Dictionary<string, string> mapping = new Dictionary<string, string>();
        }

        List<ShaderGroup> groups = new List<ShaderGroup>();
        List<string> targetSlots = new List<string>();


        [MenuItem("Tools/Windows/Material Shader Converter", priority = 12)]
        static void Open()
        {
            GetWindow<MaterialShaderConverter>("Material Converter");
        }

        void OnGUI()
        {
            GUILayout.Label("Source", EditorStyles.boldLabel);

            sourceObject = EditorGUILayout.ObjectField(
                "Prefab / Mesh",
                sourceObject,
                typeof(Object),
                true);

            GUILayout.Space(5);

            GUILayout.Label("Target", EditorStyles.boldLabel);

            targetMode = (TargetMode)EditorGUILayout.EnumPopup(
                "Target Type",
                targetMode);

            if (targetMode == TargetMode.Shader)
            {
                targetShader = (Shader)EditorGUILayout.ObjectField(
                    "Target Shader",
                    targetShader,
                    typeof(Shader),
                    false);
            }
            else
            {
                targetMaterial = (Material)EditorGUILayout.ObjectField(
                    "Template Material",
                    targetMaterial,
                    typeof(Material),
                    false);

                if (targetMaterial != null)
                    targetShader = targetMaterial.shader;
            }

            outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Output Folder",
                outputFolder,
                typeof(DefaultAsset),
                false);

            GUILayout.Space(5);

            if (GUILayout.Button("Scan Materials"))
            {
                Scan();
            }

            if (groups.Count == 0)
                return;

            GUILayout.Space(10);

            GUILayout.Label("Texture Mapping", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var group in groups)
            {
                DrawShaderGroup(group);
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            bool targetValid =
                (targetMode == TargetMode.Shader && targetShader != null) ||
                (targetMode == TargetMode.MaterialTemplate && targetMaterial != null);

            GUI.enabled = targetValid && outputFolder != null;

            if (GUILayout.Button("Process"))
            {
                Process();
            }

            GUI.enabled = true;
        }

        void DrawShaderGroup(ShaderGroup group)
        {
            EditorGUILayout.BeginVertical("box");

            group.foldout = EditorGUILayout.Foldout(
                group.foldout,
                $"{group.shader.name} ({group.materials.Count} materials)",
                true,
                EditorStyles.foldoutHeader);

            if (group.foldout)
            {
                foreach (var src in group.sourceSlots)
                {
                    if (!group.mapping.ContainsKey(src))
                        group.mapping[src] = NONE;

                    EditorGUILayout.BeginHorizontal();

                    Texture preview = GetPreviewTexture(group, src);

                    GUILayout.Label(
                        preview,
                        GUILayout.Width(40),
                        GUILayout.Height(40));

                    EditorGUILayout.BeginVertical();

                    GUILayout.Label(src, EditorStyles.boldLabel);

                    int index = Mathf.Max(0, targetSlots.IndexOf(group.mapping[src]));

                    index = EditorGUILayout.Popup(
                        "Target Slot",
                        index,
                        targetSlots.ToArray());

                    group.mapping[src] = targetSlots[index];

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(4);
                }
            }

            EditorGUILayout.EndVertical();
        }

        Texture GetPreviewTexture(ShaderGroup group, string slot)
        {
            foreach (var mat in group.materials)
            {
                if (mat != null && mat.HasProperty(slot))
                {
                    var tex = mat.GetTexture(slot);

                    if (tex != null)
                        return tex;
                }
            }

            return Texture2D.grayTexture;
        }

        void Scan()
        {
            groups.Clear();
            targetSlots.Clear();

            if (sourceObject == null)
                return;

            GameObject go = sourceObject as GameObject;

            if (go == null)
            {
                var path = AssetDatabase.GetAssetPath(sourceObject);
                go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            if (go == null)
                return;

            var renderers = go.GetComponentsInChildren<Renderer>(true);

            var materials = renderers
                .SelectMany(r => r.sharedMaterials)
                .Where(m => m != null)
                .Distinct();

            foreach (var mat in materials)
            {
                var group = groups.FirstOrDefault(g => g.shader == mat.shader);

                if (group == null)
                {
                    group = new ShaderGroup();
                    group.shader = mat.shader;
                    groups.Add(group);
                }

                group.materials.Add(mat);

                foreach (var tex in mat.GetTexturePropertyNames())
                {
                    if (!group.sourceSlots.Contains(tex))
                        group.sourceSlots.Add(tex);
                }
            }

            if (targetShader != null)
            {
                targetSlots.Add(NONE);

                int count = ShaderUtil.GetPropertyCount(targetShader);

                for (int i = 0; i < count; i++)
                {
                    if (ShaderUtil.GetPropertyType(targetShader, i) ==
                        ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        targetSlots.Add(
                            ShaderUtil.GetPropertyName(targetShader, i));
                    }
                }
            }

            AutoMatchSlots();
        }

        void AutoMatchSlots()
        {
            foreach (var group in groups)
            {
                foreach (var src in group.sourceSlots)
                {
                    if (group.mapping.ContainsKey(src))
                        continue;

                    string match = targetSlots
                        .FirstOrDefault(t =>
                            t != NONE &&
                            t.ToLower().Contains(src.ToLower()));

                    group.mapping[src] = match ?? NONE;
                }
            }
        }

        Material CreateTargetMaterial()
        {
            if (targetMode == TargetMode.MaterialTemplate)
                return new Material(targetMaterial);

            return new Material(targetShader);
        }

        void Process()
        {
            string folder = AssetDatabase.GetAssetPath(outputFolder);

            if (string.IsNullOrEmpty(folder))
            {
                Debug.LogError("Invalid output folder");
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Material Conversion");

            Dictionary<Material, Material> converted =
                new Dictionary<Material, Material>();

            foreach (var group in groups)
            {
                foreach (var mat in group.materials)
                {
                    Material newMat = CreateTargetMaterial();

                    foreach (var map in group.mapping)
                    {
                        if (map.Value == NONE)
                            continue;

                        if (!mat.HasProperty(map.Key))
                            continue;

                        Texture tex = mat.GetTexture(map.Key);

                        if (tex != null)
                            newMat.SetTexture(map.Value, tex);
                    }

                    string path = folder + "/" + mat.name + "_converted.mat";
                    path = AssetDatabase.GenerateUniqueAssetPath(path);

                    AssetDatabase.CreateAsset(newMat, path);

                    Undo.RegisterCreatedObjectUndo(newMat, "Create Converted Material");

                    converted[mat] = newMat;
                }
            }

            GameObject go = sourceObject as GameObject;

            if (go != null)
            {
                var renderers = go.GetComponentsInChildren<Renderer>(true);

                foreach (var r in renderers)
                {
                    Undo.RecordObject(r, "Assign Converted Materials");

                    var mats = r.sharedMaterials;

                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (converted.ContainsKey(mats[i]))
                            mats[i] = converted[mats[i]];
                    }

                    r.sharedMaterials = mats;

                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    EditorUtility.SetDirty(r);
                }
            }

            AssetDatabase.SaveAssets();

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("Material conversion finished");
        }
    }
}