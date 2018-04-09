using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;

namespace exsdk {
  [CustomEditor(typeof(ScriptCompDesc))]
  public class ScriptCompDescEditor : Editor {

    ReorderableList list;

    private void OnEnable() {
      var prop = serializedObject.FindProperty("properties");
      // If you want to design your own preview, the draggable set true
      list = new ReorderableList(serializedObject, prop, false, true, true, true);

      // header
      list.drawHeaderCallback = (Rect rect) => {
        EditorGUI.LabelField(rect, "properties");
      };

      // It's called when a list item is drawn
      list.drawElementCallback = (Rect rect, int index, bool selected, bool focused) => {
        SerializedProperty itemData = list.serializedProperty.GetArrayElementAtIndex(index);
        rect.y += 2;
        rect.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.LabelField(new Rect(rect.x, rect.y, 50, EditorGUIUtility.singleLineHeight), "name");
        // find own properties and drawn
        EditorGUI.PropertyField(new Rect(rect.x + 50, rect.y, 100, EditorGUIUtility.singleLineHeight),
          itemData.FindPropertyRelative("name"), GUIContent.none);
        EditorGUI.LabelField(new Rect(rect.x + 160, rect.y, 50, EditorGUIUtility.singleLineHeight), "type");
        EditorGUI.PropertyField(new Rect(rect.x + 210, rect.y, 100, EditorGUIUtility.singleLineHeight),
          itemData.FindPropertyRelative("type"), GUIContent.none);
      };
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();
      list.DoLayoutList();
      serializedObject.ApplyModifiedProperties();
    }
  }
}
