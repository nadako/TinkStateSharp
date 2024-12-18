using UnityEditor;
using UnityEngine;

namespace TinkState
{
	[CustomPropertyDrawer(typeof(SerializableState<>))]
	class SerializableStateDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PropertyField(position, property.FindPropertyRelative("value"), label);
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("value"), label);
		}
	}
}
