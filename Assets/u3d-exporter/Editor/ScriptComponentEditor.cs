using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEditor.SceneManagement;

namespace exsdk {
  [CustomEditor(typeof(ScriptComponent))]
  public class ScriptComponentEditor : Editor {
    private void OnEnable() {
      var scriptComp = target as ScriptComponent;
      scriptComp.resetProperties();
    }

    public override void OnInspectorGUI() {
      var scriptComp = target as ScriptComponent;
      serializedObject.Update();

	    EditorGUI.BeginChangeCheck();
      EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
      scriptComp.desc = (ScriptCompDesc)EditorGUILayout.ObjectField("desc", scriptComp.desc, typeof(ScriptCompDesc), false);
      EditorGUILayout.EndHorizontal();
      if (EditorGUI.EndChangeCheck()) {
        serializedObject.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        scriptComp.resetProperties();
        serializedObject.Update();
      }

      if (scriptComp.desc == null) {
        return;
      }

	    EditorGUI.BeginChangeCheck();
      var serializedProps = serializedObject.FindProperty("properties");

      foreach (var propDesc in scriptComp.desc.properties) {
        // find properties by name
        var index = -1;
        PropType type = PropType.String;
        for (int i = 0; i < scriptComp.properties.Count; ++i) {
          var prop = scriptComp.properties[i];

          if (prop.name == propDesc.name) {
            type = propDesc.type;
            index = i;
            break;
          }
        }

        // use founded index
        if (index != -1) {
          var prop = scriptComp.properties[index];
          var sprop = serializedProps.GetArrayElementAtIndex(index);
          var svalue = sprop.FindPropertyRelative("value");
          SerializedProperty sfield;

          if (type == PropType.Int) {
            sfield = svalue.FindPropertyRelative("intField");
          } else if (type == PropType.Float) {
            sfield = svalue.FindPropertyRelative("floatField");
          } else if (type == PropType.String) {
            sfield = svalue.FindPropertyRelative("stringField");
          } else if (type == PropType.Bool) {
            sfield = svalue.FindPropertyRelative("boolField");
          } else {
            sfield = svalue.FindPropertyRelative("objectField");
          }

          EditorGUILayout.PropertyField(sfield, new GUIContent(prop.name));
        }
      }

      if (EditorGUI.EndChangeCheck()) {
        serializedObject.ApplyModifiedProperties();
      }
    }
  }
}
