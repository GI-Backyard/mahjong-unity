using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PropType {
  Bool,
  Int,
  Float,
  String,
  Reference,
}

[System.Serializable]
public class ScriptCompDescProperty {
  public string name;
  public PropType type;
}

[System.Serializable]
[CreateAssetMenu(menuName = "u3d-exporter/Script Desc")]
public class ScriptCompDesc : ScriptableObject {
  public List<ScriptCompDescProperty> properties = new List<ScriptCompDescProperty>();
}