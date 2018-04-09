using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using System.IO;

namespace exsdk {
  public partial class Exporter {
    // -----------------------------------------
    // DumpSkinningModel
    // -----------------------------------------

    void DumpSkinningModel(GameObject _prefab, GLTF _gltf, BufferInfo _bufInfo) {
      // get joints
      List<GameObject> joints = new List<GameObject>();
      Utils.RecurseNode(_prefab, _go => {
        // this is not a joint
        if (_go.GetComponent<SkinnedMeshRenderer>() != null) {
          return false;
        }

        joints.Add(_go);
        return true;
      });

      // get nodes
      List<GameObject> nodes = new List<GameObject>();
      Utils.RecurseNode(_prefab, _go => {
        // this is a joint, skip it.
        if (_go.GetComponents<Component>().Length == 1) {
          return false;
        }

        nodes.Add(_go);
        return true;
      });

      // get skins & meshes
      List<Mesh> meshes = new List<Mesh>();
      List<SkinnedMeshRenderer> smrList = new List<SkinnedMeshRenderer>();
      Utils.RecurseNode(_prefab, _go => {
        SkinnedMeshRenderer smr = _go.GetComponent<SkinnedMeshRenderer>();
        if (smr != null) {
          meshes.Add(smr.sharedMesh);
          smrList.Add(smr);
        }

        return true;
      });

      // dump nodes
      foreach (GameObject go in nodes) {
        GLTF_Node gltfNode = DumpGltfNode(go, nodes);

        SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
        if (smr != null) {
          gltfNode.mesh = meshes.IndexOf(smr.sharedMesh);
          gltfNode.skin = smrList.IndexOf(smr);
        }

        _gltf.nodes.Add(gltfNode);
      }

      // dump joints
      foreach (GameObject go in joints) {
        GLTF_Node gltfNode = DumpGltfNode(go, joints);
        _gltf.joints.Add(gltfNode);
      }

      // dump meshes & skin
      int accOffset = 0;
      foreach (SkinnedMeshRenderer smr in smrList) {
        // dump mesh
        accOffset = _bufInfo.GetAccessorCount();
        DumpMesh(smr.sharedMesh, _gltf, _bufInfo, accOffset);

        // dump skin
        int accBindposesIdx = _bufInfo.GetAccessorCount() - 1;
        GameObject rootBone = Utils.GetRootBone(smr, _prefab).gameObject;

        GLTF_Skin gltfSkin = DumpGtlfSkin(smr, joints, rootBone, accBindposesIdx);
        if (gltfSkin != null) {
          _gltf.skins.Add(gltfSkin);
        }
      }
    }

    // -----------------------------------------
    // DumpGltfSkin
    // -----------------------------------------

    GLTF_Skin DumpGtlfSkin(SkinnedMeshRenderer _smr, List<GameObject> _joints, GameObject _rootBone, int _accBindposes) {
      Mesh mesh = _smr.sharedMesh;
      if (mesh.bindposes.Length != _smr.bones.Length) {
        Debug.LogWarning("Failed to dump gltf-skin from " + _smr.name + ", please turn off \"Optimize Game Objects\" in the \"Rig\".");
        return null;
      }

      GLTF_Skin gltfSkin = new GLTF_Skin();

      gltfSkin.name = _smr.name;
      gltfSkin.inverseBindMatrices = _accBindposes;
      gltfSkin.skeleton = _joints.IndexOf(_rootBone);
      gltfSkin.joints = new int[_smr.bones.Length];

      for (int i = 0; i < _smr.bones.Length; ++i) {
        gltfSkin.joints[i] = _joints.IndexOf(_smr.bones[i].gameObject);
      }

      return gltfSkin;
    }
  }
}