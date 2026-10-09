using UnityEditor;
using UnityEngine;
using System.Reflection;
using GamePause.SalahEvents.PrayerTimeAccuracy;

namespace GamePause.SalahEvents.PrayerTimeAccuracy.EditorTools
{
    /// <summary>Inspection drawer for [Button] methods: label + clickable button.</summary>
    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    public class ButtonDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var button = (ButtonAttribute)attribute;
            float buttonWidth = 140f;
            var fieldRect = position;
            fieldRect.width -= buttonWidth;
            EditorGUI.PropertyField(fieldRect, property, label);
            var btnRect = position;
            btnRect.x += btnRect.width - buttonWidth;
            btnRect.width = buttonWidth;
            if (GUI.Button(btnRect, button.text))
            {
                object obj = property.serializedObject.targetObject;
                MethodInfo mi = obj.GetType().GetMethod(
                    property.name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (mi != null && mi.ReturnType == typeof(void))
                {
                    mi.Invoke(obj, null);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUI.EndProperty();
        }
    }
}
