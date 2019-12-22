using UnityEngine;
using UnityEditor;

namespace TO5
{
    /// <summary>
    /// Simple attribute that allows assets to be search in the inspector
    /// but have the path to the asset be stored instead of a reference to it
    /// </summary>
    public class ObjectAssetPath : PropertyAttribute
    {
        public System.Type m_Type;      // Object type we find. Must be an unity object
        public string m_DisplayName;    // Override display name. If null, property name is used

        public ObjectAssetPath(System.Type type, string displayName = "")
        {
            m_Type = type;
            m_DisplayName = displayName;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// This class is responsible for drawing the properties with ObjectAssetPath attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(ObjectAssetPath))]
    public class ObjectAssetPathDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ObjectAssetPath objectAssetPath = attribute as ObjectAssetPath;

            if (property.propertyType == SerializedPropertyType.String)
            {
                // Override display name
                string displayName = property.displayName;
                if (objectAssetPath.m_DisplayName.Length > 0)
                    displayName = objectAssetPath.m_DisplayName;

                EditorGUI.BeginChangeCheck();

                Object curAsset = AssetDatabase.LoadAssetAtPath<Object>(property.stringValue);
                Object newAsset = EditorGUI.ObjectField(position, displayName, curAsset, objectAssetPath.m_Type, false);

                if (EditorGUI.EndChangeCheck())
                    property.stringValue = AssetDatabase.GetAssetPath(newAsset);
            }
        }
    }
#endif
}
