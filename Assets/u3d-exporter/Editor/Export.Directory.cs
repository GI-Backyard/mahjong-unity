using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

using Newtonsoft.Json;

using UnityEngine.UI;

namespace exsdk {
  public delegate bool WalkDirCallback(string path);

  public partial class Exporter {

    public void SaveDirs(string dest) {

      // ========================================
      // create directroy
      // ========================================

      if (Directory.Exists(dest)) {
        DirectoryInfo di = new DirectoryInfo(dest);
        di.Delete(true);
      }

      Directory.CreateDirectory(dest);

      var allDirs = new List<string>();
      var prefabs = new List<Object>();
      var modelPrefabs = new List<Object>();
      var meshes = new List<Mesh>();
      var textures = new List<Texture>();
      var spriteTextures = new List<Texture>();
      var materials = new List<Material>();
      var fonts = new List<Font>();

      // get all export directories
      for (int i = 0; i < dirs.Count; i++) {
        string assetPath = AssetDatabase.GetAssetPath(dirs[i]);

        string ext = Path.GetExtension(assetPath);
        if (!string.IsNullOrEmpty(ext)) {
          Debug.LogWarning("There is not directory in directory list of export");
          continue;
        }

        string directory = allDirs.Find(x => x == assetPath);
        if (!string.IsNullOrEmpty(directory)) {
          Debug.LogWarning("There are same directory in export directory list");
        }

        allDirs.Add(assetPath);

        WalkDir(assetPath, (walkPath) => {
          string dir = allDirs.Find(x => x == walkPath);
          if (!string.IsNullOrEmpty(dir)) {
            Debug.LogWarning("There are same directory in export directory list");
            return false;
          }

          allDirs.Add(walkPath);

          return true;
        });
      }

      // get all files in directory
      for (int i = 0; i < allDirs.Count; i++) {
        string[] allFilePath = Directory.GetFiles(allDirs[i]);
        for (int j = 0; j < allFilePath.Length; j++) {
          string ext = Path.GetExtension(allFilePath[j]);
          // it's own directory but the type is gameobject
          if (string.IsNullOrEmpty(ext)) {
            continue;
          }

          Object file = AssetDatabase.LoadAssetAtPath<Object>(allFilePath[j]);
          if (file == null) {
            continue;
          }

          if (file is GameObject) {
            Object gameObj = file;
            if (PrefabUtility.GetPrefabType(gameObj) == PrefabType.ModelPrefab) {
              GameObject gameTemp = (GameObject)modelPrefabs.Find(x => x == gameObj);
              if (gameTemp == null) {
                modelPrefabs.Add(gameObj);
              }
            } else {
              GameObject gameTemp = (GameObject)prefabs.Find(x => x == gameObj);
              if (gameTemp == null) {
                prefabs.Add(gameObj);
              }
            }
          } else if (file is Mesh) {
            Mesh mesh = file as Mesh;
            Mesh meshTemp = meshes.Find(x => x == mesh);
            if (meshTemp == null) {
              meshes.Add(mesh);
            }
          } else if (file is Texture) {
            Texture tex = file as Texture;
            string texAssetPath = AssetDatabase.GetAssetPath(tex);
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(texAssetPath);

            if (importer.textureType == TextureImporterType.Sprite) {
              Texture texTemp = spriteTextures.Find(x => x == tex);
              if (texTemp == null) {
                spriteTextures.Add(tex);
              }
            } else {
              Texture texTemp = textures.Find(x => x == tex);
              if (texTemp == null) {
                textures.Add(tex);
              }
            }
          } else if (file is Font) {
            Font font = file as Font;
            Font fontTemp = fonts.Find(x => x == font);
            if (fontTemp == null) {
              fonts.Add(font);
            }
          } else if (file is Material) {
            Material mat = file as Material;
            Material matTemp = materials.Find(x => x == mat);
            if (matTemp == null) {
              materials.Add(mat);
            }

            List<Texture> texList = Utils.GetTextures(mat);
            for (int a = 0; a < texList.Count; a++) {
              Texture texTemp = textures.Find(x => x == texList[a]);
              if (texTemp == null) {
                textures.Add(texList[a]);
              }
            }
          }
          // TODO:animationClip
        }
      }

      Dictionary<string, JSON_Asset> assetJson = saveAssets(
        dest, 
        prefabs,
        modelPrefabs,
        meshes,
        textures,
        spriteTextures,
        materials,
        fonts
      );

      saveUrls(dest, assetJson, allDirs);
    }

    void saveUrls(string dest,Dictionary<string, JSON_Asset> assetJson, List<string> allDirs) {
      Dictionary<string, string> assetUrls = new Dictionary<string, string>();
      foreach (string id in assetJson.Keys) {
        string assetPath = AssetDatabase.GUIDToAssetPath(id);

        int length = -1;
        string relativePath = null;
        string rootFileName = null;
        for (int i = 0; i < allDirs.Count; i++) {
          string dir = allDirs[i];
          if (assetPath.Contains(dir)) {
            string tempPath = assetPath.Substring(dir.Length + 1);
            int currLength = tempPath.Split('/').Length;
            if (length == -1 || currLength > length) {
              length = currLength;
              relativePath = tempPath;
              rootFileName = Path.GetFileName(dir);
            }
          }
        }

        assetUrls.Add(rootFileName + "/" + relativePath, id);
      }

      string path = Path.Combine(dest, "urls.json");
      string json = JsonConvert.SerializeObject(assetUrls,Formatting.Indented);
      StreamWriter write = new StreamWriter(path);
      write.Write(json);
      write.Close();
    }

    void WalkDir(string path, WalkDirCallback fn) {
      string[] dirs = Directory.GetDirectories(path);
      for (int i = 0; i < dirs.Length; i++) {
        RecurseDir(dirs[i], fn);
      }
    }

    void RecurseDir(string path, WalkDirCallback fn) {
      bool continueRecurse = fn(path);
      if (continueRecurse) {
        WalkDir(path, fn);
      }
    }
  }
}
