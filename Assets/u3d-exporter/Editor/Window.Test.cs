using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Sprites;

using System.Collections;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

namespace exsdk {
  public class TestWindow : EditorWindow {
    public Object target;

    [MenuItem("Window/test")]
    static void Open() {
      TestWindow window = (TestWindow)EditorWindow.GetWindow(typeof(TestWindow));
      window.titleContent = new GUIContent("test");
      window.minSize = new Vector2(200, 200);
      window.Show();
    }

    void OnEnable() {
      this.Repaint();
    }

    void OnGUI() {
      EditorGUIUtility.labelWidth = 100.0f;

      // =========================
      // Options
      // =========================

      GUILayout.Label("Options", EditorStyles.boldLabel);

      // #########################
      // Start
      // #########################

      GUIStyle style = EditorStyles.inspectorDefaultMargins;
      EditorGUILayout.BeginVertical(style, new GUILayoutOption[0]);

      // =========================
      // Test Button
      // =========================

      this.target = EditorGUILayout.ObjectField("Target", this.target, typeof(Object), true);

      EditorGUILayout.Space();
      EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
      EditorGUILayout.Space();
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("Test", "LargeButton", GUILayout.MaxWidth(200))) {
        if (this.target) {
          var sprite = this.target as Sprite;
          // var path = AssetDatabase.GetAssetPath(this.target);
          // var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);

          Debug.Log("is sub asset: " + AssetDatabase.IsSubAsset(this.target));
          Debug.Log(sprite.rect);

          // var packedTexture = SpriteUtility.GetSpriteTexture(sprite, true);
          // Debug.Log(packedTexture);
          // Debug.Log(AssetDatabase.GetAssetPath(packedTexture));
          // Debug.Log(sprite.packed);
        }
      }
      GUILayout.FlexibleSpace();
      EditorGUILayout.Space();
      EditorGUILayout.EndHorizontal();

      // #########################
      // End
      // #########################

      EditorGUILayout.EndVertical();
    }
  }
}