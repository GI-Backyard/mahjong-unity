using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace exsdk {
  public enum ShaderType {
    FRAGMENT_SHADER = 35632,
    VERTEX_SHADER = 35633,
  }

  // mapping to WebGL enums
  public enum EnableStateType {
    BLEND = 3042,
    CULL_FACE = 2884,
    DEPTH_TEST = 2929,
    POLYGON_OFFSET_FILL = 32823,
    SAMPLE_ALPHA_TO_COVERAGE = 32926,
    SCISSOR_TEST = 3089,
  }

  public enum BlendEquation {
    FUNC_ADD = 32774,
    FUNC_SUBTRACT = 32778,
    FUNC_REVERSE_SUBTRACT = 32779,
  }

  public enum BlendMode {
    ZERO = 0,
    ONE = 1,
    SRC_COLOR = 768,
    ONE_MINUS_SRC_COLOR = 769,
    DST_COLOR = 774,
    ONE_MINUS_DST_COLOR = 775,
    SRC_ALPHA = 770,
    ONE_MINUS_SRC_ALPHA = 771,
    DST_ALPHA = 772,
    ONE_MINUS_DST_ALPHA = 773,
    CONSTANT_COLOR = 32769,
    ONE_MINUS_CONSTANT_COLOR = 32770,
    CONSTANT_ALPHA = 32771,
    ONE_MINUS_CONSTANT_ALPHA = 32772,
    SRC_ALPHA_SATURATE = 776,
  }

  public enum CullFace {
    FRONT = 1028,
    BACK = 1029,
    FRONT_AND_BACK = 1032,
  }

  public enum DepthFunc {
    NEVER = 512,
    LESS = 513,
    LEQUAL = 515,
    EQUAL = 514,
    GREATER = 516,
    NOTEQUAL = 517,
    GEQUAL = 518,
    ALWAYS = 519,
  }

  public enum FrontFace {
    CW = 2304,
    CCW = 2305,
  }

  // mapping to WebGL enums
  public enum PrimitiveType {
    POINT = 0,
    LINES = 1,
    LINE_LOOP = 2,
    LINE_STRIP = 3,
    TRIANGLES = 4,
    TRIANGLE_STRIP = 5,
    TRIANGLE_FAN = 6,
  }

  // mapping to WebGL enums
  public enum ComponentType {
    INT8 = 5120, // BYTE
    UINT8 = 5121, // UNSIGNED_BYTE
    INT16 = 5122, // SHORT
    UINT16 = 5123, // UNSIGNED_SHORT
    UINT32 = 5125, // UNSIGNED_INT
    FLOAT32 = 5126, // FLOAT
  }

  public enum AttrType {
    SCALAR,
    VEC2,
    VEC3,
    VEC4,
    MAT2,
    MAT3,
    MAT4,
  }

  public enum BufferType {
    NONE = -1,
    VERTEX = 34962, // ARRAY_BUFFER
    INDEX = 34963,  // ELEMENT_ARRAY_BUFFER
  }

  public enum ParameterType {
    BYTE = 5120,
    UNSIGNED_BYTE = 5121,
    SHORT = 5122,
    UNSIGNED_SHORT = 5123,
    INT = 5124,
    UNSIGNED_INT = 5125,
    FLOAT = 5126,
    FLOAT_VEC2 = 35664,
    FLOAT_VEC3 = 35665,
    FLOAT_VEC4 = 35666,
    INT_VEC2 = 35667,
    INT_VEC3 = 35668,
    INT_VEC4 = 35669,
    BOOL = 35670,
    BOOL_VEC2 = 35671,
    BOOL_VEC3 = 35672,
    BOOL_VEC4 = 35673,
    FLOAT_MAT2 = 35674,
    FLOAT_MAT3 = 35675,
    FLOAT_MAT4 = 35676,
    SAMPLER_2D = 35678,
  }

  public enum TextureFormat {
    ALPHA = 6406,
    RGB = 6407,
    RGBA = 6408,
    LUMINANCE = 6409,
    LUMINANCE_ALPHA = 6410,
  }

  public enum TextureTarget {
    TEXTURE_2D = 3553,
  }

  public enum TexturePixelType {
    UNSIGNED_BYTE = 5121,
    UNSIGNED_SHORT_5_6_5 = 33635,
    UNSIGNED_SHORT_4_4_4_4 = 32819,
    UNSIGNED_SHORT_5_5_5_1 = 32820,
  }

  public enum FilterMode {
    NEAREST = 9728,
    LINEAR = 9729,
    NEAREST_MIPMAP_NEAREST = 9984,
    LINEAR_MIPMAP_NEAREST = 9985,
    NEAREST_MIPMAP_LINEAR = 9986,
    LINEAR_MIPMAP_LINEAR = 9987,
  }

  public enum WrapMode {
    CLAMP_TO_EDGE = 33071,
    MIRRORED_REPEAT = 33648,
    REPEAT = 10497,
  }

  public class AccessorInfo {
    public string name;
    public int offset;
    public int count;
    public ComponentType compType;
    public AttrType attrType;
    public object[] min;
    public object[] max;
  }

  public class BufferViewInfo {
    public string name;
    public int offset;
    public int length;
    public int stride;
    public BufferType type;
    public List<AccessorInfo> accessors;
  }

  public class BufferInfo {
    public string id;
    public string name;
    public byte[] data = new byte[0];
    public List<BufferViewInfo> bufferViews = new List<BufferViewInfo>();

    public int GetAccessorCount() {
      int cnt = 0;
      foreach (BufferViewInfo bufView in bufferViews) {
        cnt += bufView.accessors.Count;
      }
      return cnt;
    }
  }

  public class NodeFrames {
    public GameObject node;
    public List<Vector3> tlist = new List<Vector3>();
    public List<Vector3> slist = new List<Vector3>();
    public List<Quaternion> rlist = new List<Quaternion>();
  }

  public class AnimData {
    public string name;
    public List<float> times = new List<float>();
    public Dictionary<string, NodeFrames> nameToFrames = new Dictionary<string, NodeFrames>();
  }

  public class ShaderProperty {
    public string name;
    public string type;
    public string mapping;
    public string mappingTiling;
    public string mappingOffset;
  }

  public class ShaderInfo {
    public string type = "phong";
    public List<ShaderProperty> properties;
  }

  public class ModProperty {
    public string name;
    public string mapping;
    public ConvertFn fn = null;

    public delegate object ConvertFn (object val);
  }

  public class ComponentModInfo {
    public string type = "unknown";
    public List<ModProperty> properties;

    public string MapProperty(string propertyPath) {
      foreach (ModProperty prop in properties) {
        if (propertyPath.IndexOf(prop.name) == 0 ) {
          string suffix = "";
          if (propertyPath.Length > prop.name.Length) {
            suffix = propertyPath.Substring(prop.name.Length);
          }

          return prop.mapping + suffix;
        }
      }

      return null;
    }
  }

  public class Utils {

    public static Object GetPrefabAsset(GameObject _go) {
      var root = PrefabUtility.FindPrefabRoot(_go);
      return PrefabUtility.GetPrefabParent(root);
    }

    public static string GetJointsID(GameObject _go) {
      var skinnedMeshRenderer = _go.GetComponentInChildren<SkinnedMeshRenderer>();
      var mesh = skinnedMeshRenderer.sharedMesh;
      var path = AssetDatabase.GetAssetPath(mesh);
      var prefab = AssetDatabase.LoadMainAssetAtPath(path);

      return "joints@" + AssetID(prefab);
    }

    public static ShaderInfo GetShaderInfo(Material _mat) {
      ShaderInfo shaderInfo;
      if (Registry.shaderInfos.TryGetValue(_mat.shader.name, out shaderInfo) == false) {
        return null;
      }

      return shaderInfo;
    }

    public static ComponentModInfo GetComponentModInfo(string typename) {
      ComponentModInfo compModInfo;
      if (Registry.componentModInfos.TryGetValue(typename, out compModInfo) == false) {
        return null;
      }

      return compModInfo;
    }

    public static List<Texture> GetTextures(Material _mat) {
      List<Texture> results = new List<Texture>();

      // get shader info
      ShaderInfo shaderInfo;
      if (Registry.shaderInfos.TryGetValue(_mat.shader.name, out shaderInfo) == false) {
        Debug.LogWarning("Unregisterred Shader: " + _mat.shader.name + " for material: " + _mat.name);
        return results;
      }

      //
      if (shaderInfo.properties != null) {
        foreach (ShaderProperty prop in shaderInfo.properties) {
          if (prop.type == "tex2d") {
            var texture = _mat.GetTexture(prop.name);

            // NOTE: it is possible the material don't have texture
            if (!texture) {
              continue;
            }

            results.Add(_mat.GetTexture(prop.name));
          }
        }
      }

      return results;
    }

    public static int Align(int _size, int _align) {
      return (int)Mathf.Ceil((float)_size / _align) * _align;
    }

    public static string AssetExt(Object _obj) {
      string assetPath = AssetDatabase.GetAssetPath(_obj);
      if (string.IsNullOrEmpty(assetPath)) {
        return null;
      }

      return Path.GetExtension(assetPath);
    }

    public static bool IsBuiltinAsset(Object _obj) {
      string assetPath = AssetDatabase.GetAssetPath(_obj);
      if (assetPath == "Library/unity default resources") {
        return true;
      }

      return false;
    }

    public static string AssetID(Object _obj) {
      string assetPath = AssetDatabase.GetAssetPath(_obj);
      if (string.IsNullOrEmpty(assetPath)) {
        return null;
      }

      // if the asset saved in the disk
      if (assetPath.IndexOf("Assets/") == 0) {
        // if this is a sub asset
        if (AssetDatabase.IsSubAsset(_obj)) {
          if (_obj is Mesh) {
            var meshes = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Mesh>().ToList();
            var localID = "m" + meshes.IndexOf(_obj as Mesh);
            return localID + "@" + AssetDatabase.AssetPathToGUID(assetPath);
          } else if (_obj is Sprite) {
            return _obj.name + "@" + AssetDatabase.AssetPathToGUID(assetPath);
          } else if (_obj is AnimationClip) {
            // NOTE: it is possible AnimationClip inside a model-prefab, and that prefab will be export, so they will have the same uuid.
            var anims = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>().ToList();
            var localID = anims.IndexOf(_obj as AnimationClip);
            return AssetDatabase.AssetPathToGUID(assetPath) + "-" + localID;
          }
        }

        return AssetDatabase.AssetPathToGUID(assetPath);
      }

      // if this is a internal asset
      if (assetPath == "Library/unity default resources") {
        return "builtin-" + _obj.name.ToLower();
      }

      return _obj.GetInstanceID().ToString();
    }

    public static int FilterModeIndex(FilterMode _mode) {
      if (_mode == FilterMode.NEAREST) {
        return 0;
      } else if (_mode == FilterMode.LINEAR) {
        return 1;
      } else if (_mode == FilterMode.NEAREST_MIPMAP_NEAREST) {
        return 2;
      } else if (_mode == FilterMode.LINEAR_MIPMAP_NEAREST) {
        return 3;
      } else if (_mode == FilterMode.NEAREST_MIPMAP_LINEAR) {
        return 4;
      } else if (_mode == FilterMode.LINEAR_MIPMAP_LINEAR) {
        return 5;
      }

      return -1;
    }

    public static int WrapModeIndex(WrapMode _mode) {
      if (_mode == WrapMode.CLAMP_TO_EDGE) {
        return 0;
      } else if (_mode == WrapMode.MIRRORED_REPEAT) {
        return 1;
      } else if (_mode == WrapMode.REPEAT) {
        return 2;
      }

      return -1;
    }

    public static void SetSamplerProperties(Texture _texture, ref GLTF_Sampler _sampler) {
      // filter mode
      if (_texture.filterMode == UnityEngine.FilterMode.Point) {
        _sampler.magFilter = (int)FilterMode.NEAREST;
        _sampler.minFilter = (int)FilterMode.NEAREST;
      } else if (_texture.filterMode == UnityEngine.FilterMode.Bilinear) {
        _sampler.magFilter = (int)FilterMode.LINEAR;
        _sampler.minFilter = (int)FilterMode.LINEAR_MIPMAP_NEAREST;
      } else if (_texture.filterMode == UnityEngine.FilterMode.Trilinear) {
        _sampler.magFilter = (int)FilterMode.LINEAR;
        _sampler.minFilter = (int)FilterMode.LINEAR_MIPMAP_LINEAR;
      }

      // wrap mode
      if (_texture.wrapMode == TextureWrapMode.Repeat) {
        _sampler.wrapS = (int)WrapMode.REPEAT;
        _sampler.wrapT = (int)WrapMode.REPEAT;
      } else if (_texture.wrapMode == TextureWrapMode.Clamp) {
        _sampler.wrapS = (int)WrapMode.CLAMP_TO_EDGE;
        _sampler.wrapT = (int)WrapMode.CLAMP_TO_EDGE;
      }
    }

    public static string SamplerID(Texture _texture) {
      FilterMode magFilter = FilterMode.LINEAR;
      FilterMode minFilter = FilterMode.LINEAR;
      WrapMode wrapS = WrapMode.REPEAT;
      WrapMode wrapT = WrapMode.REPEAT;

      // filter mode
      if (_texture.filterMode == UnityEngine.FilterMode.Point) {
        magFilter = FilterMode.NEAREST;
        minFilter = FilterMode.NEAREST;
      } else if (_texture.filterMode == UnityEngine.FilterMode.Bilinear) {
        magFilter = FilterMode.LINEAR;
        minFilter = FilterMode.LINEAR;
      } else if (_texture.filterMode == UnityEngine.FilterMode.Trilinear) {
        magFilter = FilterMode.LINEAR;
        minFilter = FilterMode.LINEAR_MIPMAP_LINEAR;
      }

      // wrap mode
      if (_texture.wrapMode == TextureWrapMode.Repeat) {
        wrapS = WrapMode.REPEAT;
        wrapT = WrapMode.REPEAT;
      } else if (_texture.wrapMode == TextureWrapMode.Clamp) {
        wrapS = WrapMode.CLAMP_TO_EDGE;
        wrapT = WrapMode.CLAMP_TO_EDGE;
      }

      string id = "sampler_"
        + FilterModeIndex(magFilter)
        + FilterModeIndex(minFilter)
        + WrapModeIndex(wrapS)
        + WrapModeIndex(wrapT)
        ;

      return id;
    }

    public static Mesh GetMesh(GameObject _go) {
      // MeshFilter
      MeshFilter meshFilter = _go.GetComponent<MeshFilter>();
      if (meshFilter) {
        return meshFilter.sharedMesh;
      }

      // SkinnedMeshRenderer
      SkinnedMeshRenderer skinnedMeshRenderer = _go.GetComponent<SkinnedMeshRenderer>();
      if (skinnedMeshRenderer) {
        return skinnedMeshRenderer.sharedMesh;
      }

      return null;
    }

    public static List<AnimationClip> GetAnimationClips(GameObject _go) {
      // Animation
      Animation animation = _go.GetComponent<Animation>();
      if (animation) {
        List<AnimationClip> clips = new List<AnimationClip>();

        foreach (AnimationState state in animation) {
          clips.Add(state.clip);
        }

        return clips;
      }

      // Animator
      Animator animator = _go.GetComponent<Animator>();
      if (animator && animator.runtimeAnimatorController) {
        List<AnimationClip> clips = new List<AnimationClip>();

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips) {
          clips.Add(clip);
        }

        return clips;
      }

      return null;
    }

    public static bool IsAnimPrefab(GameObject _prefab) {
      if (_prefab.GetComponent<Animation>() == null &&
           _prefab.GetComponent<Animator>() == null) {
        return false;
      }

      // foreach (Transform child in _prefab.transform) {
      //   GameObject gameObject = child.gameObject;
      //   SkinnedMeshRenderer smr = gameObject.GetComponent<SkinnedMeshRenderer>();
      //   if (smr != null) {
      //     return true;
      //   }
      // }
      //
      // return false;

      bool hasSkinnedMeshRenderer = false;

      Utils.RecurseNode(_prefab, _go => {
        SkinnedMeshRenderer smr = _go.GetComponent<SkinnedMeshRenderer>();
        if (smr != null) {
          hasSkinnedMeshRenderer = true;
          return false;
        }

        return true;
      });

      return hasSkinnedMeshRenderer;
    }

    public static void Walk(List<GameObject> _roots, WalkCallback _fn) {
      for (int i = 0; i < _roots.Count; ++i) {
        GameObject gameObject = _roots[i];
        Utils.RecurseNode(gameObject, _fn);
      }
    }

    public static void RecurseNode(GameObject _go, WalkCallback _fn, bool excludeSelf = false) {
      if (!excludeSelf) {
        bool walkChildren = _fn(_go);
        if (!walkChildren) {
          return;
        }
      }

      foreach (Transform child in _go.transform) {
        RecurseNode(child.gameObject, _fn);
      }
    }

    public static Transform GetRootBone(SkinnedMeshRenderer _smr, GameObject _prefab) {
      int minLevel = -1;
      Transform rootBone = null;

      for (int i = 0; i < _smr.bones.Length; ++i) {
        Transform bone = _smr.bones[i];
        int level = 0;

        Transform parent = bone;
        while (true) {
          parent = parent.parent;
          ++level;

          if (parent == _prefab || parent == null) {
            break;
          }
        }

        if (minLevel == -1 || level < minLevel) {
          minLevel = level;
          rootBone = bone;
        }
      }

      return rootBone;
    }

    public static string textAlignment(TextAnchor _align) {
      switch (_align) {
        case TextAnchor.UpperLeft:
          return "top-left";

        case TextAnchor.UpperCenter:
          return "top-center";

        case TextAnchor.UpperRight:
          return "top-right";

        case TextAnchor.MiddleLeft:
          return "middle-left";

        case TextAnchor.MiddleCenter:
          return "middle-center";

        case TextAnchor.MiddleRight:
          return "middle-right";

        case TextAnchor.LowerLeft:
          return "bottom-left";

        case TextAnchor.LowerCenter:
          return "bottom-center";

        case TextAnchor.LowerRight:
          return "bottom-right";
      }

      return "top-left";
    }

    public static string refPathLookup(GameObject go, GameObject root) {
      string path = go.name;
      Transform parent = go.transform;

      while (true) {
        parent = parent.parent;

        if (root == null && parent == null) {
          break;
        } else if (parent.gameObject == root) {
          break;
        }

        path = parent.name + "/" + path;
      }

      return path;
    }
  }
}