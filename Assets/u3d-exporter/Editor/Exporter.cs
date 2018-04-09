using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using UnityEditor;
using UnityEditor.SceneManagement;

using System.Collections;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

namespace exsdk {
  public enum FileMode {
    Text, // all in one json file
    Mixed, // json file + bin file(s)
    Binary, // packed json + bin in one file
  }

  public delegate bool WalkCallback(GameObject _go);

  public partial class Exporter {
    public string outputPath;
    public string name;
    public FileMode mode;
    public List<SceneAsset> scenes;
    public List<Object> dirs;

    public void Exec() {
      if (!Directory.Exists(this.outputPath)) {
        Debug.LogError("u3d-exporter Failed: Can not find the path \"" + this.outputPath + "\"");
        return;
      }

      // create dest folder
      string name = string.IsNullOrEmpty(this.name) ? "exports" : this.name;
      string dest = Path.Combine(this.outputPath, name);

      if (Directory.Exists(dest)) {
        System.IO.DirectoryInfo di = new DirectoryInfo(dest);
        di.Delete(true);
      }

      if (scenes == null || scenes.Count <= 0) {
        Debug.LogWarning("There is no scene can export");
        return;
      }

      // create dest directory
      if (!Directory.Exists(dest)) {
        Directory.CreateDirectory(dest);
      }

      string currentScenePath = SceneManager.GetActiveScene().path;
      if (SceneManager.GetActiveScene().isDirty) {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
          return;
        }
      }

      // get data from scene
      var prefabs = new List<Object>();
      var modelPrefabs = new List<Object>();
      var materials = new List<Material>();
      var textures = new List<Texture>();
      var spriteTextures = new List<Texture>();
      var fonts = new List<Font>();
      var meshes = new List<Mesh>();

      // walk to each scene to get assets referenced in it.
      for (int i = 0; i < scenes.Count; i++) {
        var sceneAsset = scenes[i];

        if (sceneAsset == null) {
          continue;
        }

        List<GameObject> nodes = new List<GameObject>();

        string assetPath = AssetDatabase.GetAssetPath(sceneAsset);
        Scene scene = EditorSceneManager.GetSceneByPath(assetPath);

        if (SceneManager.GetActiveScene().name != sceneAsset.name) {
          EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
          scene = EditorSceneManager.GetActiveScene();
        }

        WalkScene(
          scene,
          nodes,
          prefabs,
          modelPrefabs,
          meshes,
          materials,
          textures,
          spriteTextures,
          fonts
        );

        // ========================================
        // save scene
        // ========================================

        var sceneJson = DumpScene(nodes);
        string path;
        string sceneName = string.IsNullOrEmpty(scene.name) ? "scene" : scene.name;
        string json = JsonConvert.SerializeObject(sceneJson, Formatting.Indented);

        path = Path.Combine(dest, sceneName + ".json");
        StreamWriter writer = new StreamWriter(path);
        writer.Write(json);
        writer.Close();

        Debug.Log(Path.GetFileName(path) + " saved.");
      }

      saveAssets(
        dest,
        prefabs,
        modelPrefabs,
        meshes,
        textures,
        spriteTextures,
        materials,
        fonts
      );

      EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);

      dest = Path.Combine(dest, "resources");
      SaveDirs(dest);
    }

    Dictionary<string, JSON_Asset> saveAssets(
      string dest,
      List<Object> prefabs,
      List<Object> modelPrefabs,
      List<Mesh> meshes,
      List<Texture> textures,
      List<Texture> spriteTextures,
      List<Material> materials,
      List<Font> fonts
      ) {
      Dictionary<string, JSON_Asset> assetsJson = new Dictionary<string, JSON_Asset>();

      // DELME {
      // // save meshes
      // var destMeshes = Path.Combine(dest, "meshes");
      // foreach (Mesh mesh in meshes) {
      //   string id = Utils.AssetID(mesh);
      //   GLTF gltf = new GLTF();
      //   gltf.asset = new GLTF_Asset
      //   {
      //     version = "1.0.0",
      //     generator = "u3d-exporter"
      //   };
      //   BufferInfo bufInfo = new BufferInfo
      //   {
      //     id = id,
      //     name = mesh.name
      //   };

      //   DumpMesh(mesh, gltf, bufInfo, 0);
      //   DumpBuffer(bufInfo, gltf);

      //   Save(
      //     destMeshes,
      //     id,
      //     gltf,
      //     new List<BufferInfo> { bufInfo }
      //   );

      //   // add asset to table
      //   assetsJson.Add(id, new JSON_Asset {
      //     type = "mesh",
      //     urls = new Dictionary<string, string> {
      //       { "gltf", "meshes/" + id + ".gltf" },
      //       { "bin", "meshes/" + id + ".bin" }
      //     }
      //   });
      // }
      // } DELME

      // ========================================
      // save animations
      // ========================================

      var destAnims = Path.Combine(dest, "anims");
      foreach (GameObject prefab in prefabs) {
        // skip ModelPrefab
        if (PrefabUtility.GetPrefabType(prefab) == PrefabType.ModelPrefab) {
          Debug.LogWarning("Can not export model prefab " + prefab.name + " in the scene");
          continue;
        }

        // skip non-animation prefab
        bool isAnimPrefab = Utils.IsAnimPrefab(prefab);
        if (isAnimPrefab == false) {
          continue;
        }

        // get animations
        GameObject prefabInst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        List<AnimationClip> clips = Utils.GetAnimationClips(prefabInst);

        // get joints
        List<GameObject> joints = new List<GameObject>();
        Utils.RecurseNode(prefabInst, _go => {
          // this is not a joint
          if (_go.GetComponent<SkinnedMeshRenderer>() != null) {
            return false;
          }

          joints.Add(_go);
          return true;
        });

        // dump animation clips
        if (clips != null) {
          // process AnimationClip(s)
          foreach (AnimationClip clip in clips) {
            string id = Utils.AssetID(clip);
            GLTF gltf = new GLTF();
            gltf.asset = new GLTF_Asset {
              version = "1.0.0",
              generator = "u3d-exporter"
            };
            BufferInfo bufInfo = new BufferInfo {
              id = id,
              name = prefab.name
            };

            AnimData animData = DumpAnimData(prefabInst, clip);
            DumpBufferInfoFromAnimData(animData, bufInfo);

            GLTF_AnimationEx gltfAnim = DumpGltfAnimationEx(animData, joints, 0);
            gltf.animations.Add(gltfAnim);

            DumpBuffer(bufInfo, gltf);

            Save(
              destAnims,
              id + ".anim",
              gltf,
              new List<BufferInfo> { bufInfo }
            );

            // add asset to table
            try {
              if (!assetsJson.ContainsKey(id)) {
                assetsJson.Add(id, new JSON_Asset {
                  type = "animation",
                  urls = new Dictionary<string, string> {
                  { "anim", "anims/" + id + ".anim" },
                  { "bin", "anims/" + id + ".bin" }
                }
                });
              }
            } catch (System.SystemException e) {
              Debug.LogError("Failed to add " + id + " to assets: " + e);
            }
          }
        }

        Object.DestroyImmediate(prefabInst);
      }

      // ========================================
      // save prefabs
      // ========================================

      var destMeshes = Path.Combine(dest, "meshes");
      var destPrefabs = Path.Combine(dest, "prefabs");

      // create dest directory
      if (!Directory.Exists(destMeshes)) {
        Directory.CreateDirectory(destMeshes);
      }
      if (!Directory.Exists(destPrefabs)) {
        Directory.CreateDirectory(destPrefabs);
      }

      foreach (GameObject prefab in prefabs) {
        string id = Utils.AssetID(prefab);

        // save prefabs
        if (PrefabUtility.GetPrefabType(prefab) == PrefabType.ModelPrefab) {
          Debug.LogWarning("Can not export model prefab " + prefab.name + " in the scene");
          continue;
        }
        var prefabJson = DumpPrefab(prefab);
        string path;
        string json = JsonConvert.SerializeObject(prefabJson, Formatting.Indented);

        path = Path.Combine(destPrefabs, id + ".json");
        StreamWriter writer = new StreamWriter(path);
        writer.Write(json);
        writer.Close();

        // Debug.Log(Path.GetFileName(path) + " saved.");

        // add asset to table
        if (!assetsJson.ContainsKey(id)) {
          assetsJson.Add(id, new JSON_Asset {
            type = "prefab",
            urls = new Dictionary<string, string> {
            { "json", "prefabs/" + id + ".json" },
          }
          });
        }
      }

      // save model prefab (as gltf)
      foreach (GameObject modelPrefab in modelPrefabs) {
        string id = Utils.AssetID(modelPrefab);
        // save model prefabs
        GLTF gltf = new GLTF();
        gltf.asset = new GLTF_Asset {
          version = "1.0.0",
          generator = "u3d-exporter"
        };
        BufferInfo bufInfo = new BufferInfo {
          id = id,
          name = modelPrefab.name
        };

        bool isAnimPrefab = Utils.IsAnimPrefab(modelPrefab);
        if (isAnimPrefab) {
          DumpSkinningModel(modelPrefab, gltf, bufInfo);
          DumpBuffer(bufInfo, gltf);
        } else {
          DumpModel(modelPrefab, gltf, bufInfo);
          DumpBuffer(bufInfo, gltf);
        }

        Save(
          destMeshes,
          id + ".gltf",
          gltf,
          new List<BufferInfo> { bufInfo }
        );

        // add asset to table
        if (!assetsJson.ContainsKey(id)) {
          assetsJson.Add(id, new JSON_Asset {
            type = "gltf",
            urls = new Dictionary<string, string> {
            { "gltf", "meshes/" + id + ".gltf" },
            { "bin", "meshes/" + id + ".bin" }
          }
          });
        }
      }

      // save meshes (as gltf)
      foreach (Mesh mesh in meshes) {
        string id = Utils.AssetID(mesh);
        // save model prefabs
        GLTF gltf = new GLTF();
        gltf.asset = new GLTF_Asset {
          version = "1.0.0",
          generator = "u3d-exporter"
        };
        BufferInfo bufInfo = new BufferInfo {
          id = id,
          name = mesh.name
        };

        DumpMesh(mesh, gltf, bufInfo, 0);
        DumpBuffer(bufInfo, gltf);

        Save(
          destMeshes,
          id + ".mesh",
          gltf,
          new List<BufferInfo> { bufInfo }
        );

        // add asset to table
        if (!assetsJson.ContainsKey(id)) {
          assetsJson.Add(id, new JSON_Asset {
            type = "mesh",
            urls = new Dictionary<string, string> {
            { "mesh", "meshes/" + id + ".mesh" },
            { "bin", "meshes/" + id + ".bin" }
          }
          });
        }
      }

      // ========================================
      // save textures
      // ========================================

      var destTextures = Path.Combine(dest, "textures");
      // create dest directory
      if (!Directory.Exists(destTextures)) {
        Directory.CreateDirectory(destTextures);
      }
      foreach (Texture tex in textures) {
        var textureJson = DumpTexture(tex);
        string path;
        string json = JsonConvert.SerializeObject(textureJson, Formatting.Indented);
        string id = Utils.AssetID(tex);

        // json
        path = Path.Combine(destTextures, id + ".json");
        StreamWriter writer = new StreamWriter(path);
        writer.Write(json);
        writer.Close();

        // image
        string assetPath = AssetDatabase.GetAssetPath(tex);
        path = Path.Combine(destTextures, id + Utils.AssetExt(tex));
        File.Copy(assetPath, path, true);

        // Debug.Log(Path.GetFileName(path) + " saved.");

        // add asset to table
        if (!assetsJson.ContainsKey(id)) {
          assetsJson.Add(id, new JSON_Asset {
            type = "texture",
            urls = new Dictionary<string, string> {
            { "json", "textures/" + id + ".json" },
            { "image", "textures/" + id + Utils.AssetExt(tex) },
          }
          });
        }
      }

      // ========================================
      // save sprite textures
      // ========================================

      var destSprites = Path.Combine(dest, "sprites");
      // create dest directory
      if (!Directory.Exists(destSprites)) {
        Directory.CreateDirectory(destSprites);
      }
      foreach (Texture spriteTex in spriteTextures) {
        var spriteTextureJson = DumpSpriteTexture(spriteTex);
        string path;
        string json = JsonConvert.SerializeObject(spriteTextureJson, Formatting.Indented);
        string id = Utils.AssetID(spriteTex);

        // json
        path = Path.Combine(destSprites, id + ".json");
        StreamWriter writer = new StreamWriter(path);
        writer.Write(json);
        writer.Close();

        // image
        string assetPath = AssetDatabase.GetAssetPath(spriteTex);
        path = Path.Combine(destSprites, id + Utils.AssetExt(spriteTex));
        File.Copy(assetPath, path);

        // add asset to table
        if (!assetsJson.ContainsKey(id)) {
          assetsJson.Add(id, new JSON_Asset {
            type = "texture",
            urls = new Dictionary<string, string> {
            { "json", "sprites/" + id + ".json" },
            { "image", "sprites/" + id + Utils.AssetExt(spriteTex) },
          }
          });
        }
      }

      // ========================================
      // save fonts
      // ========================================

      var destFonts = Path.Combine(dest, "fonts");
      if (!Directory.Exists(destFonts)) {
        Directory.CreateDirectory(destFonts);
      }

      foreach (Font font in fonts) {
        if (font.dynamic) {
          if (font.fontNames.Length > 1) { // system font
            // do nothing
          } else { // opentype font
            JSON_OpenTypeFont fontJson = DumpOpenTypeFont(font);

            // save font json
            string path;
            string json = JsonConvert.SerializeObject(fontJson, Formatting.Indented);
            string id = Utils.AssetID(font);

            path = Path.Combine(destFonts, id + ".json");
            StreamWriter writer = new StreamWriter(path);
            writer.Write(json);
            writer.Close();

            // save font file (ttf)
            Texture tex = font.material.mainTexture;
            string assetPath = AssetDatabase.GetAssetPath(font);
            path = Path.Combine(destFonts, id + Utils.AssetExt(font));
            File.Copy(assetPath, path, true);

            if (!assetsJson.ContainsKey(id)) {
              assetsJson.Add(id, new JSON_Asset {
                type = "otfont",
                urls = new Dictionary<string, string> {
                  { "json", "fonts/" + id + ".json" },
                  { "bin", "fonts/" + id + Utils.AssetExt(font) },
                }
              });
            }
          }
        } else { // bitmapFont
          JSON_BitmapFont fontJson = DumpBitmapFont(font);

          string path;
          string json = JsonConvert.SerializeObject(fontJson, Formatting.Indented);
          string id = Utils.AssetID(font);

          path = Path.Combine(destFonts, id + ".json");
          StreamWriter writer = new StreamWriter(path);
          writer.Write(json);
          writer.Close();

          if (!assetsJson.ContainsKey(id)) {
            assetsJson.Add(id, new JSON_Asset {
              type = "bmfont",
              urls = new Dictionary<string, string> {
              { "json", "fonts/" + id + ".json" },
            }
            });
          }
        }
      }

      // ========================================
      // save materials
      // ========================================

      var destMaterials = Path.Combine(dest, "materials");
      // create dest directory
      if (!Directory.Exists(destMaterials)) {
        Directory.CreateDirectory(destMaterials);
      }
      foreach (Material mat in materials) {
        var materialJson = DumpMaterial(mat);
        if (materialJson == null) {
          continue;
        }

        string path;
        string json = JsonConvert.SerializeObject(materialJson, Formatting.Indented);
        string id = Utils.AssetID(mat);

        // json
        path = Path.Combine(destMaterials, id + ".json");
        StreamWriter writer = new StreamWriter(path);
        writer.Write(json);
        writer.Close();

        // Debug.Log(Path.GetFileName(path) + " saved.");

        // add asset to table
        if (!assetsJson.ContainsKey(id)) {
          assetsJson.Add(id, new JSON_Asset {
            type = "material",
            urls = new Dictionary<string, string> {
            { "json", "materials/" + id + ".json" },
          }
          });
        }
      }

      // ========================================
      // save assets
      // ========================================

      {
        string path = Path.Combine(dest, "assets.json");
        string json = JsonConvert.SerializeObject(assetsJson, Formatting.Indented);

        StreamWriter writer = new StreamWriter(path);
        writer.Write(json);
        writer.Close();
      }

      return assetsJson;
    }

    void Save(string _dest, string _file, GLTF _gltf, List<BufferInfo> _bufferInfos) {
      // create dest directory
      if (!Directory.Exists(_dest)) {
        Directory.CreateDirectory(_dest);
      }

      string path;

      // =========================
      // gltf
      // =========================

      string json = JsonConvert.SerializeObject(_gltf, Formatting.Indented);
      path = Path.Combine(_dest, _file);
      StreamWriter writer = new StreamWriter(path);
      writer.Write(json);
      writer.Close();

      // Debug.Log(Path.GetFileName(path) + " saved.");

      // =========================
      // buffers (.bin)
      // =========================

      foreach (BufferInfo buf in _bufferInfos) {
        path = Path.Combine(_dest, buf.id + ".bin");
        BinaryWriter bwriter = new BinaryWriter(new FileStream(path, System.IO.FileMode.Create));
        bwriter.Write(buf.data);
        bwriter.Close();

        // Debug.Log(Path.GetFileName(path) + " saved.");
      }

      // =========================
      // finalize
      // =========================

      // make sure our saved file will show up in Project panel
      AssetDatabase.Refresh();
    }

    void WalkScene(
      Scene _scene,
      List<GameObject> _nodes,
      List<Object> _prefabs,
      List<Object> _modelPrefabs,
      List<Mesh> _meshes,
      List<Material> _materials,
      List<Texture> _textures,
      List<Texture> _spriteTextures,
      List<Font> _fonts
    ) {
      List<GameObject> rootObjects = new List<GameObject>();
      // get root objects in scene
      _scene.GetRootGameObjects(rootObjects);

      // collect meshes, skins, materials, textures and animation-clips
      Utils.Walk(rootObjects, _go => {
        // =========================
        // get material & textures
        // =========================

        Renderer renderer = _go.GetComponent<Renderer>();
        if (renderer) {
          foreach (Material mat in renderer.sharedMaterials) {
            if (mat == null) {
              Debug.LogWarning("Null material in " + _go.name);
              continue;
            }
            Material foundedMaterial = _materials.Find(m => {
              return m == mat;
            });
            if (foundedMaterial == null) {
              _materials.Add(mat);

              // handle textures
              List<Texture> textures = Utils.GetTextures(mat);
              foreach (Texture tex in textures) {
                Texture foundedTexture = _textures.Find(t => {
                  return t == tex;
                });
                if (foundedTexture == null) {
                  _textures.Add(tex);
                }
              }
            }
          }
        }

        // =========================
        // get sprite texture
        // =========================

        Sprite sprite = null;
        SpriteRenderer spriteRenderer = _go.GetComponent<SpriteRenderer>();
        if (spriteRenderer) {
          sprite = spriteRenderer.sprite;
        }

        Image image = _go.GetComponent<Image>();
        if (image) {
          sprite = image.sprite;
        }

        if (sprite != null) {
          Texture foundedTexture = _spriteTextures.Find(t => {
            return t == sprite.texture;
          });
          if (foundedTexture == null) {
            _spriteTextures.Add(sprite.texture);
          }
        }

        // =========================
        // get font
        // =========================

        Text text = _go.GetComponent<Text>();
        if (text) {
          var fontTexture = text.font.material.mainTexture;
          if (text.font == null) {
            Debug.LogWarning("Font not found in " + _go.name);
          } else if (fontTexture == null) {
            Debug.LogWarning("No texture for font " + fontTexture.name);
          }

          if (Utils.IsBuiltinAsset(text.font)) {
            Debug.LogWarning("Can't export the texture because of it's default font's texture");
          } else {
            if (!text.font.dynamic) {
              Texture foundedTexture = _textures.Find(t => t == fontTexture);
              if (foundedTexture == null) {
                _textures.Add(fontTexture);
              }
            }
            Font font = _fonts.Find(t => t == text.font);
            if (font == null) {
              _fonts.Add(text.font);
            }
          }
        }

        // =========================
        // get model prefab from mesh
        // =========================

        Mesh mesh = null;
        MeshFilter meshFilter = _go.GetComponent<MeshFilter>();
        if (meshFilter) {
          mesh = meshFilter.sharedMesh;
        }
        SkinnedMeshRenderer skinnedMeshRenderer = _go.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer) {
          mesh = skinnedMeshRenderer.sharedMesh;
        }

        if (mesh != null && Utils.IsBuiltinAsset(mesh) == false) {
          var path = AssetDatabase.GetAssetPath(mesh);
          var prefab = AssetDatabase.LoadMainAssetAtPath(path);
          var type = PrefabUtility.GetPrefabType(prefab);

          // if this is a model-prefab
          if (type == PrefabType.ModelPrefab) {
            // check if prefab already exists
            var founded = _modelPrefabs.Find(item => {
              return item == prefab;
            });
            if (founded == null) {
              _modelPrefabs.Add(prefab);
            }
          } else {
            // check if mesh already exists
            var founded = _meshes.Find(item => {
              return item == mesh;
            });
            if (founded == null) {
              _meshes.Add(mesh);
            }
          }
        }

        // =========================
        // get comp from script  component
        // =========================

        ScriptComponent comp = _go.GetComponent<ScriptComponent>();
        if (comp != null) {
          if (comp.properties.Count > 0) {
            for (int i = 0; i < comp.properties.Count; i++) {
              var prop = comp.properties[i].value.objectField;
              if (prop == null) {
                continue;
              }

              if (prop is Mesh) {
                var founded = _meshes.Find(item => {
                  return item == prop;
                });
                if (founded == null) {
                  _meshes.Add(prop as Mesh);
                }
              } else if (prop is Material) {
                Material mat = prop as Material;
                Material foundedMaterial = _materials.Find(m => {
                  return m == mat;
                });
                if (foundedMaterial == null) {
                  _materials.Add(mat);

                  // handle textures
                  List<Texture> textures = Utils.GetTextures(mat);
                  foreach (Texture tex in textures) {
                    Texture foundedTexture = _textures.Find(t => {
                      return t == tex;
                    });
                    if (foundedTexture == null) {
                      _textures.Add(tex);
                    }
                  }
                }
              } else if (prop is Texture) {
                Texture foundedTexture = _textures.Find(t => {
                  return t == prop;
                });
                if (foundedTexture == null) {
                  _textures.Add(prop as Texture);
                }
              } else if (prop is Sprite) {
                Sprite spriteTemp = prop as Sprite;
                Texture foundedTexture = _spriteTextures.Find(t => {
                  return t == spriteTemp.texture;
                });
                if (foundedTexture == null) {
                  _spriteTextures.Add(spriteTemp.texture);
                }
              } else if (prop is Font) {
                var font = prop as Font;
                var fontTexture = font.material.mainTexture;
                if (fontTexture == null) {
                  Debug.LogWarning("No texture for font " + fontTexture.name);
                }

                if (!font.dynamic) {
                  Texture foundedTexture = _textures.Find(t => t == fontTexture);
                  if (foundedTexture == null) {
                    _textures.Add(fontTexture);
                  }
                }
                Font foundedFont = _fonts.Find(t => t == font);
                if (foundedFont == null) {
                  _fonts.Add(font);
                }
              } else if (prop is GameObject) {
                string assetID = Utils.AssetID(prop);
                if (!string.IsNullOrEmpty(assetID)) {
                  var type = PrefabUtility.GetPrefabType((prop as GameObject));
                  if (type == PrefabType.ModelPrefab) {
                    var founded = _modelPrefabs.Find(item => {
                      return item == prop;
                    });
                    if (founded == null) {
                      _modelPrefabs.Add(prop as GameObject);
                    }
                  } else {
                    var founded = _prefabs.Find(item => {
                      return item == prop;
                    });
                    if (founded == null) {
                      _prefabs.Add(prop as GameObject);
                    }
                  }
                }
              }
            }
          }
        }

        // continue children or not
        return true;
      });
      // collect prefabs & nodes
      Utils.Walk(rootObjects, _go => {

        // =========================
        // get prefabs
        // =========================

        var type = PrefabUtility.GetPrefabType(_go);
        if (type != PrefabType.None) {
          var prefab = Utils.GetPrefabAsset(_go);

          // check if prefab already exists
          var founded = _prefabs.Find(item => {
            return item == prefab;
          });
          if (founded == null) {
            _prefabs.Add(prefab);
          }

          // add nodes & skip prefab children
          _nodes.Add(_go);

          // recurse prefab child see if any nested prefab in it.
          Utils.RecurseNode(_go, _childGO => {
            var childRoot = PrefabUtility.FindPrefabRoot(_childGO);
            if (childRoot == _go) {
              return false;
            }

            _nodes.Add(_childGO);

            var childType = PrefabUtility.GetPrefabType(_childGO);
            if (childType != PrefabType.None) {
              var childPrefab = Utils.GetPrefabAsset(_childGO);

              // check if prefab already exists
              founded = _prefabs.Find(item => {
                return item == childPrefab;
              });
              if (founded == null) {
                _prefabs.Add(childPrefab);
              }
            }

            return true;
          }, true);

          return false;
        }

        // =========================
        // add nodes
        // =========================

        _nodes.Add(_go);

        return true;
      });
    }
  }
}