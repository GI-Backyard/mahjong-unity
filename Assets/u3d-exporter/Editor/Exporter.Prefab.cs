using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System.IO;

namespace exsdk {
  public partial class Exporter {

    // -----------------------------------------
    // DumpPrefab
    // -----------------------------------------

    public JSON_Prefab DumpPrefab(GameObject _prefab) {
      JSON_Prefab result = new JSON_Prefab();
      List<GameObject> nodes = new List<GameObject>();
      bool isAnimPrefab = Utils.IsAnimPrefab(_prefab);

      // collect nodes
      Utils.Walk(new List<GameObject> { _prefab }, _go => {
        if (isAnimPrefab) {
          // this is a joint, skip it.
          if (_go.GetComponents<Component>().Length == 1) {
            return false;
          }
        }

        nodes.Add(_go);
        return true;
      });

      // dump entities
      foreach (GameObject go in nodes) {
        JSON_Entity ent = DumpEntity(go, nodes);
        result.entities.Add(ent);
      }

      return result;
    }
  }
}