using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ValueField {
  public bool boolField;
  public int intField;
  public float floatField;
  public string stringField;
  public Object objectField;
}

[System.Serializable]
public struct ScriptProperty {
  public string name;
  public ValueField value;
}

public class ScriptComponent : MonoBehaviour {

  public ScriptCompDesc desc;
  public List<ScriptProperty> properties;

  public void resetProperties() {
    if (desc == null || desc.properties.Count == 0) {
      return;
    }

    if (properties == null) {
      properties = new List<ScriptProperty>();

      foreach (var item in desc.properties) {
        properties.Add(new ScriptProperty {
          name = item.name,
          value = newValue()
        });
      }

      return;
    }

    Dictionary<string, ValueField> oldproperties = new Dictionary<string, ValueField>();
    foreach (var item in properties) {
      oldproperties[item.name] = item.value;
    }

    properties.Clear();

    for (int i = 0; i < desc.properties.Count; i++) {
      var propDesc = desc.properties[i];
      ValueField value;

      if (oldproperties.ContainsKey(propDesc.name)) {
        value = oldproperties[propDesc.name];
      } else {
        value = newValue();
      }

      properties.Add(new ScriptProperty {
        name = propDesc.name,
        value = value,
      });
    }
  }

  public static ValueField newValue() {
    return new ValueField {
      objectField = null,
      intField = 0,
      floatField = 0.0f,
      boolField = false,
      stringField = "",
    };
  }
}
