using UnityEngine;
using UnityEngine.PostProcessing;
using PPSMinAttribute = UnityEngine.PostProcessing.MinAttribute; // <-- Añade esta línea

namespace UnityEditor.PostProcessing
{
    [CustomPropertyDrawer(typeof(PPSMinAttribute))]
    sealed class MinDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PPSMinAttribute attribute = (PPSMinAttribute)base.attribute;

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int v = EditorGUI.IntField(position, label, property.intValue);
                property.intValue = (int)Mathf.Max(v, attribute.min);
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                float v = EditorGUI.FloatField(position, label, property.floatValue);
                property.floatValue = Mathf.Max(v, attribute.min);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use Min with float or int.");
            }
        }
    }
}
