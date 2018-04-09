using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using System.IO;

namespace exsdk {
  public partial class Exporter {
    // -----------------------------------------
    // DumpGltfNode
    // -----------------------------------------

    GLTF_Node DumpGltfNode(GameObject _go, List<GameObject> _nodes) {
      GLTF_Node result = new GLTF_Node();

      // name
      result.name = _go.name;

      // NOTE: skinned mesh node will use identity matrix
      if (_go.GetComponent<SkinnedMeshRenderer>() != null) {
        // translation
        result.translation[0] = 0.0f;
        result.translation[1] = 0.0f;
        result.translation[2] = 0.0f;

        // rotation
        result.rotation[0] = 0.0f;
        result.rotation[1] = 0.0f;
        result.rotation[2] = 0.0f;
        result.rotation[3] = 1.0f;

        // scale
        result.scale[0] = 1.0f;
        result.scale[1] = 1.0f;
        result.scale[2] = 1.0f;
      } else {
        // translation
        // NOTE: convert LH to RH
        result.translation[0] = _go.transform.localPosition.x;
        result.translation[1] = _go.transform.localPosition.y;
        result.translation[2] = -_go.transform.localPosition.z;

        // rotation
        // NOTE: convert LH to RH
        result.rotation[0] = -_go.transform.localRotation.x;
        result.rotation[1] = -_go.transform.localRotation.y;
        result.rotation[2] = _go.transform.localRotation.z;
        result.rotation[3] = _go.transform.localRotation.w;

        // scale
        result.scale[0] = _go.transform.localScale.x;
        result.scale[1] = _go.transform.localScale.y;
        result.scale[2] = _go.transform.localScale.z;
      }

      // children
      result.children = new List<int>();
      foreach (Transform child in _go.transform) {
        int idx = _nodes.IndexOf(child.gameObject);
        if (idx != -1) {
          result.children.Add(idx);
        }
      }

      return result;
    }
  }
}