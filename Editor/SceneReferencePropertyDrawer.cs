using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sceneAssetProp = property.FindPropertyRelative("_sceneAsset");
            SerializedProperty sceneNameProp = property.FindPropertyRelative("_sceneName");

            Rect sceneAssetRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();

            EditorGUI.PropertyField(sceneAssetRect, sceneAssetProp, label);

            if (EditorGUI.EndChangeCheck())
            {
                if (sceneAssetProp.objectReferenceValue != null)
                {
                    SceneAsset sceneAsset = sceneAssetProp.objectReferenceValue as SceneAsset;
                    sceneNameProp.stringValue = sceneAsset.name;
                }
                else
                {
                    sceneNameProp.stringValue = string.Empty;
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}