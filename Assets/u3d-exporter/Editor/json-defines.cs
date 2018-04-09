using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace exsdk {
  // =========================
  // JSON_Project
  // =========================

  [System.Serializable]
  public class JSON_ExportSettings {
    public string outputPath = "";
    public string projectName = "";
    public FileMode mode = FileMode.Mixed;
    public List<string> scenes = new List<string>();
    public List<string> dirs = new List<string>();
  }

  // =========================
  // JSON_Scene
  // =========================

  [System.Serializable]
  public class JSON_Scene {
    public List<int> children = new List<int>();
    public List<JSON_Entity> entities = new List<JSON_Entity>();
  }

  // =========================
  // JSON_Prefab
  // =========================

  [System.Serializable]
  public class JSON_Prefab {
    public List<JSON_Entity> entities = new List<JSON_Entity>();
  }

  // =========================
  // JSON_Entity
  // =========================

  [System.Serializable]
  public class JSON_Entity {
    // basic
    public string name;
    public string prefab;
    public bool enabled = true;
    public float[] translation = new float[3] { 0, 0, 0 };
    public float[] rotation = new float[4] { 0, 0, 0, 1 };
    public float[] scale = new float[3] { 1, 1, 1 };
    public List<JSON_Component> components;
    public List<int> children;
    public List<JSON_Modification> modifications = new List<JSON_Modification>();

    public bool ShouldSerializename() {
      return string.IsNullOrEmpty(name) == false;
    }

    public bool ShouldSerializeprefab() {
      return prefab != null;
    }

    public bool ShouldSerializeenabled() {
      return enabled == false;
    }

    public bool ShouldSerializetranslation() {
      return translation[0] != 0 || translation[1] != 0 || translation[2] != 0;
    }

    public bool ShouldSerializerotation() {
      return rotation[0] != 0 || rotation[1] != 0 || rotation[2] != 0 || rotation[3] != 1;
    }

    public bool ShouldSerializescale() {
      return scale[0] != 1 || scale[1] != 1 || scale[2] != 1;
    }

    public bool ShouldSerializecomponents() {
      return components != null && components.Count != 0;
    }

    public bool ShouldSerializechildren() {
      return children != null && children.Count != 0;
    }

    public bool ShouldSerializemodifications() {
      return modifications != null && modifications.Count != 0;
    }
  }

  // =========================
  // JSON_Component
  // =========================

  [System.Serializable]
  public class JSON_Component {
    public string type;
    public bool enabled = true;
    public Dictionary<string, object> properties = new Dictionary<string, object>();

    public bool ShouldSerializeenabled() {
      return enabled == false;
    }

    public bool ShouldSerializeproperties() {
      return properties != null && properties.Count != 0;
    }
  }

  // =========================
  // JSON_Asset
  // =========================

  [System.Serializable]
  public class JSON_Asset {
    public string type;
    public Dictionary<string, string> urls = new Dictionary<string, string>();
  }

  // =========================
  // JSON_Texture
  // =========================

  [System.Serializable]
  public class JSON_Texture {
    public string type = "";
    public int anisotropy = -1;
    public string minFilter;
    public string magFilter;
    public string mipFilter;
    public string wrapS;
    public string wrapT;
    public string wrapR;
    public bool mipmap = true;

    public bool ShouldSerializeanisotropy() {
      return anisotropy != -1;
    }

    public bool ShouldSerializeminFilter() {
      return string.IsNullOrEmpty(minFilter) == false;
    }

    public bool ShouldSerializemagFilter() {
      return string.IsNullOrEmpty(magFilter) == false;
    }

    public bool ShouldSerializemipFilter() {
      return string.IsNullOrEmpty(mipFilter) == false;
    }

    public bool ShouldSerializewrapS() {
      return string.IsNullOrEmpty(wrapS) == false;
    }

    public bool ShouldSerializewrapT() {
      return string.IsNullOrEmpty(wrapT) == false;
    }

    public bool ShouldSerializewrapR() {
      return string.IsNullOrEmpty(wrapR) == false;
    }

    public bool ShouldSerializemipmap() {
      return mipmap == false;
    }
  }

  // =========================
  // JSON_Sprite
  // =========================

  [System.Serializable]
  public class JSON_Sprite {
    public string texture;
    public bool rotated;
    public float x;
    public float y;
    public float width;
    public float height;
    public float left;
    public float right;
    public float bottom;
    public float top;
  }

  // =========================
  // JSON_SpriteTexture
  // =========================

  [System.Serializable]
  public class JSON_SpriteTexture : JSON_Texture {
    public Dictionary<string, JSON_Sprite> sprites = new Dictionary<string, JSON_Sprite>();
  }

  // =========================
  // JSON_OpenTypeFont
  // =========================
  [System.Serializable]
  public class JSON_OpenTypeFont {
    // referenced texture;
    public string font;

    // common
    public int size;
    public int lineHeight;
    public float width;
    public float height;
  }
  // =========================
  // JSON_BitmapFont
  // =========================

  [System.Serializable]
  public class JSON_BitmapFont {
    // referenced texture;
    public string texture;

    // common
    public string face;
    public int size;
    public int lineHeight;
    public float lineBaseHeight;
    public float scaleW;
    public float scaleH;

    // public List<string> pages=new List<string>();
    public Dictionary<string, JSON_Font_Chars> chars = new Dictionary<string, JSON_Font_Chars>();
    public List<JSON_Font_Kerning> kernings = new List<JSON_Font_Kerning>();
  }

  [System.Serializable]
  public class JSON_Font_Chars {
    public int id;
    public float x;
    public float y;
    public float width;
    public float height;
    public float xoffset;
    public float yoffset;
    public float xadvance;
    public int page;
    public int chnl;
  }

  [System.Serializable]
  public class JSON_Font_Kerning {
    public int first;
    public int second;
    public int amount;
  }

  // =========================
  // JSON_Material
  // =========================

  [System.Serializable]
  public class JSON_Material {
    public string type = "phong";
    public string blendType = "none";
    public Dictionary<string, object> properties = new Dictionary<string, object>();

    public bool ShouldSerializeblendType() {
      return blendType != "none";
    }

    public bool ShouldSerializeproperties() {
      return properties.Count != 0;
    }
  }

  // =========================
  // JSON_Modification
  // =========================

  [System.Serializable]
  public class JSON_Modification {
    public string property;
    public int entity = -1;
    public object value;

    public bool ShouldSerializeentity() {
      return entity != -1;
    }

    public bool ShouldSerializeproperty() {
      return string.IsNullOrEmpty(property) == false;
    }

    public bool ShouldSerializevalue() {
      return value != null;
    }
  }
}