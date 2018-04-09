using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Linq;

namespace exsdk {
  public partial class Exporter {
    // -----------------------------------------
    // DumpModel
    // -----------------------------------------

    void DumpModel(GameObject _prefab, GLTF _gltf, BufferInfo _bufInfo) {
      // get nodes
      List<GameObject> nodes = new List<GameObject>();
      Utils.RecurseNode(_prefab, _go => {
        nodes.Add(_go);
        return true;
      });

      string assetPath = AssetDatabase.GetAssetPath(_prefab);
      var meshes = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Mesh>().ToList();

      // get meshes
      // List<Mesh> meshes = new List<Mesh>();
      // Utils.RecurseNode(_prefab, _go => {
      //   MeshFilter meshFilter = _go.GetComponent<MeshFilter>();
      //   if (meshFilter && meshes.IndexOf(meshFilter.sharedMesh) == -1 ) {
      //     meshes.Add(meshFilter.sharedMesh);
      //   }

      //   return true;
      // });

      // dump nodes
      foreach (GameObject go in nodes) {
        GLTF_Node gltfNode = DumpGltfNode(go, nodes);

        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter != null) {
          gltfNode.mesh = meshes.IndexOf(meshFilter.sharedMesh);
        }

        _gltf.nodes.Add(gltfNode);
      }

      // dump meshes
      int accOffset = 0;
      foreach (Mesh mesh in meshes) {
        // dump mesh
        accOffset = _bufInfo.GetAccessorCount();
        DumpMesh(mesh, _gltf, _bufInfo, accOffset);
      }
    }

    // -----------------------------------------
    // DumpMesh
    // -----------------------------------------

    void DumpMesh(Mesh _mesh, GLTF _gltf, BufferInfo _bufInfo, int _accOffset) {
      // dump buffer info
      DumpBufferInfoFromMesh(_mesh, _bufInfo);

      // dump mesh
      GLTF_Mesh gltfMesh = DumpGltfMesh(_mesh, _accOffset);
      _gltf.meshes.Add(gltfMesh);
    }

    // -----------------------------------------
    // DumpGltfMesh
    // -----------------------------------------

    GLTF_Mesh DumpGltfMesh(Mesh _mesh, int _accOffset) {
      GLTF_Mesh result = new GLTF_Mesh();
      Dictionary<string, int> attributes = new Dictionary<string, int>();
      int idx = 0;

      // name
      result.name = _mesh.name;

      // primitives
      result.primitives = new List<GLTF_Primitive>();

      // attributes
      if (_mesh.vertices.Length > 0) {
        attributes.Add("POSITION", _accOffset + idx);
        ++idx;
      }

      if (_mesh.normals.Length > 0) {
        attributes.Add("NORMAL", _accOffset + idx);
        ++idx;
      }

      if (_mesh.tangents.Length > 0) {
        attributes.Add("TANGENT", _accOffset + idx);
        ++idx;
      }

      if (_mesh.colors.Length > 0) {
        attributes.Add("COLOR_0", _accOffset + idx);
        ++idx;
      }

      if (_mesh.uv.Length > 0) {
        attributes.Add("TEXCOORD_0", _accOffset + idx);
        ++idx;
      }

      if (_mesh.uv2.Length > 0) {
        attributes.Add("TEXCOORD_1", _accOffset + idx);
        ++idx;
      }

      if (_mesh.uv3.Length > 0) {
        attributes.Add("TEXCOORD_2", _accOffset + idx);
        ++idx;
      }

      if (_mesh.uv4.Length > 0) {
        attributes.Add("TEXCOORD_3", _accOffset + idx);
        ++idx;
      }

      if (_mesh.boneWeights.Length > 0) {
        attributes.Add("JOINTS_0", _accOffset + idx);
        ++idx;

        attributes.Add("WEIGHTS_0", _accOffset + idx);
        ++idx;
      }

      // primitives
      if (_mesh.triangles.Length > 0) {
        int cnt = _mesh.subMeshCount;

        for (int i = 0; i < cnt; ++i) {
          GLTF_Primitive primitive = new GLTF_Primitive();
          primitive.attributes = attributes;
          primitive.indices = _accOffset + idx;
          ++idx;

          result.primitives.Add(primitive);
        }
      } else {
        GLTF_Primitive primitive = new GLTF_Primitive();
        primitive.attributes = attributes;

        result.primitives.Add(primitive);
      }

      return result;
    }

    // -----------------------------------------
    // DumpBufferInfoFromMesh
    // -----------------------------------------

    void DumpBufferInfoFromMesh(Mesh _mesh, BufferInfo _bufInfo) {
      int vertexBytes = 0;
      byte[] vertexData;
      List<byte[]> indexDataList = new List<byte[]>();
      byte[] bindposesData;
      Vector3[] vertices = _mesh.vertices;
      Vector3[] normals = _mesh.normals;
      Vector4[] tangents = _mesh.tangents;
      Color[] colors = _mesh.colors;
      Vector2[] uv = _mesh.uv;
      Vector2[] uv2 = _mesh.uv2;
      Vector2[] uv3 = _mesh.uv3;
      Vector2[] uv4 = _mesh.uv4;
      BoneWeight[] boneWeights = _mesh.boneWeights;
      int offsetNormal = 0;
      int offsetTangent = 0;
      int offsetColor = 0;
      int offsetUV = 0;
      int offsetUV2 = 0;
      int offsetUV3 = 0;
      int offsetUV4 = 0;
      int offsetJoint = 0;
      int offsetWeight = 0;
      int offsetBuffer = _bufInfo.data.Length;
      Vector3 minPos = Vector3.zero;
      Vector3 maxPos = Vector3.zero;
      Vector3 minNormal = Vector3.zero;
      Vector3 maxNormal = Vector3.zero;
      Vector4 minTangent = Vector4.zero;
      Vector4 maxTangent = Vector4.zero;

      if (vertices.Length > 0) {
        minPos = maxPos = vertices[0];
        vertexBytes += 12; // float32 * 3
      }

      if (normals.Length > 0) {
        offsetNormal = vertexBytes;
        vertexBytes += 12; // float32 * 3
      }

      if (tangents.Length > 0) {
        offsetTangent = vertexBytes;
        vertexBytes += 16; // float32 * 4
      }

      if (colors.Length > 0) {
        offsetColor = vertexBytes;
        vertexBytes += 16; // float32 * 4
      }

      if (uv.Length > 0) {
        offsetUV = vertexBytes;
        vertexBytes += 8; // float32 * 2
      }

      if (uv2.Length > 0) {
        offsetUV2 = vertexBytes;
        vertexBytes += 8; // float32 * 2
      }

      if (uv3.Length > 0) {
        offsetUV3 = vertexBytes;
        vertexBytes += 8; // float32 * 2
      }

      if (uv4.Length > 0) {
        offsetUV4 = vertexBytes;
        vertexBytes += 8; // float32 * 2
      }

      if (boneWeights.Length > 0) {
        offsetJoint = vertexBytes;
        vertexBytes += 8; // uint16 * 4

        offsetWeight = vertexBytes;
        vertexBytes += 16; // float32 * 4
      }

      // vertexData
      using (MemoryStream stream = new MemoryStream(vertexBytes * _mesh.vertexCount)) {
        using (BinaryWriter writer = new BinaryWriter(stream)) {
          for (int i = 0; i < _mesh.vertexCount; ++i) {
            if (vertices.Length > 0) {
              Vector3 vert = vertices[i];
              // NOTE: convert LH to RH
              vert.z = -vert.z;

              writer.Write(vert.x);
              writer.Write(vert.y);
              writer.Write(vert.z);

              if (vert.x < minPos.x) {
                minPos.x = vert.x;
              }
              if (vert.y < minPos.y) {
                minPos.y = vert.y;
              }
              if (vert.z < minPos.z) {
                minPos.z = vert.z;
              }
              if (vert.x > maxPos.x) {
                maxPos.x = vert.x;
              }
              if (vert.y > maxPos.y) {
                maxPos.y = vert.y;
              }
              if (vert.z > maxPos.z) {
                maxPos.z = vert.z;
              }
            }

            if (normals.Length > 0) {
              Vector3 normal = normals[i];
              // NOTE: convert LH to RH
               normal.z = -normal.z;

              writer.Write(normal.x);
              writer.Write(normal.y);
              writer.Write(normal.z);

              if (normal.x < minNormal.x) {
                minNormal.x = normal.x;
              }
              if (normal.y < minNormal.y) {
                minNormal.y = normal.y;
              }
              if (normal.z < minNormal.z) {
                minNormal.z = normal.z;
              }
              if (normal.x > maxNormal.x) {
                maxNormal.x = normal.x;
              }
              if (normal.y > maxNormal.y) {
                maxNormal.y = normal.y;
              }
              if (normal.z > maxNormal.z) {
                maxNormal.z = normal.z;
              }
            }

            if (tangents.Length > 0) {
              Vector4 tangent = tangents[i];
              // NOTE: convert LH to RH
              tangent.z = -tangent.z;

              writer.Write(tangent.x);
              writer.Write(tangent.y);
              writer.Write(tangent.z);
              writer.Write(tangent.w);

              if (tangent.x < minTangent.x) {
                minTangent.x = tangent.x;
              }
              if (tangent.y < minTangent.y) {
                minTangent.y = tangent.y;
              }
              if (tangent.z < minTangent.z) {
                minTangent.z = tangent.z;
              }
              if (tangent.w < minTangent.w) {
                minTangent.w = tangent.w;
              }
              if (tangent.x > maxTangent.x) {
                maxTangent.x = tangent.x;
              }
              if (tangent.y > maxTangent.y) {
                maxTangent.y = tangent.y;
              }
              if (tangent.z > maxTangent.z) {
                maxTangent.z = tangent.z;
              }
              if (tangent.w > maxTangent.w) {
                maxTangent.w = tangent.w;
              }
            }

            if (colors.Length > 0) {
              writer.Write(colors[i].r);
              writer.Write(colors[i].g);
              writer.Write(colors[i].b);
              writer.Write(colors[i].a);
            }

            if (uv.Length > 0) {
              writer.Write(uv[i].x);
              writer.Write(uv[i].y);
            }

            if (uv2.Length > 0) {
              writer.Write(uv2[i].x);
              writer.Write(uv2[i].y);
            }

            if (uv3.Length > 0) {
              writer.Write(uv3[i].x);
              writer.Write(uv3[i].y);
            }

            if (uv4.Length > 0) {
              writer.Write(uv4[i].x);
              writer.Write(uv4[i].y);
            }

            if (boneWeights.Length > 0) {
              writer.Write((ushort)boneWeights[i].boneIndex0);
              writer.Write((ushort)boneWeights[i].boneIndex1);
              writer.Write((ushort)boneWeights[i].boneIndex2);
              writer.Write((ushort)boneWeights[i].boneIndex3);

              writer.Write(boneWeights[i].weight0);
              writer.Write(boneWeights[i].weight1);
              writer.Write(boneWeights[i].weight2);
              writer.Write(boneWeights[i].weight3);
            }
          }
        }
        vertexData = stream.ToArray();
      }

      // indexDataList
      for (int i = 0; i < _mesh.subMeshCount; ++i) {
        int[] subTriangles = _mesh.GetTriangles(i);

        if (subTriangles.Length > 0) {
          using (MemoryStream stream = new MemoryStream(2 * subTriangles.Length)) {
            using (BinaryWriter writer = new BinaryWriter(stream)) {
              // DISABLE
              // for ( int ii = 0; ii < subTriangles.Length; ++ii ) {
              //   writer.Write((ushort)subTriangles[ii]);
              // }

              // NOTE: convert mesh winding order from CW (Unity3D's) to CCW (most webgl programs)
              for (int ii = 0; ii < subTriangles.Length / 3; ++ii) {
                writer.Write((ushort)subTriangles[3 * ii]);
                writer.Write((ushort)subTriangles[3 * ii + 2]);
                writer.Write((ushort)subTriangles[3 * ii + 1]);
              }
            }

            indexDataList.Add(stream.ToArray());
          }
        }
      }

      // bindposesData
      using (MemoryStream stream = new MemoryStream(4 * 16 * _mesh.bindposes.Length)) {
        using (BinaryWriter writer = new BinaryWriter(stream)) {
          for (int i = 0; i < _mesh.bindposes.Length; ++i) {
            Matrix4x4 bindpose = _mesh.bindposes[i];

            // NOTE: convert LH to RH
            writer.Write(bindpose[0]);
            writer.Write(bindpose[1]);
            writer.Write(-bindpose[2]); // a02
            writer.Write(bindpose[3]);
            writer.Write(bindpose[4]);
            writer.Write(bindpose[5]);
            writer.Write(-bindpose[6]); // a12
            writer.Write(bindpose[7]);
            writer.Write(-bindpose[8]); // a20
            writer.Write(-bindpose[9]); // a21
            writer.Write(bindpose[10]);
            writer.Write(bindpose[11]);
            writer.Write(bindpose[12]);
            writer.Write(bindpose[13]);
            writer.Write(-bindpose[14]); // b2
            writer.Write(bindpose[15]);
          }
        }
        bindposesData = stream.ToArray();
      }

      // bufferViews
      List<BufferViewInfo> bufferViews = new List<BufferViewInfo>();

      // vbView
      BufferViewInfo vbView = new BufferViewInfo
      {
        name = "vb@" + _mesh.name,
        offset = offsetBuffer,
        length = vertexData.Length,
        stride = vertexBytes,
        type = BufferType.VERTEX,
        accessors = new List<AccessorInfo>(),
      };

      if (vertices.Length > 0) {
        AccessorInfo acc = new AccessorInfo
        {
          name = "position@" + _mesh.name,
          offset = 0,
          count = _mesh.vertexCount,
          compType = ComponentType.FLOAT32,
          attrType = AttrType.VEC3,
          min = new object[3] { minPos.x, minPos.y, minPos.z },
          max = new object[3] { maxPos.x, maxPos.y, maxPos.z },
        };

        vbView.accessors.Add(acc);
      }

      if (normals.Length > 0) {
        AccessorInfo acc = new AccessorInfo
        {
          name = "normal@" + _mesh.name,
          offset = offsetNormal,
          count = _mesh.vertexCount,
          compType = ComponentType.FLOAT32,
          attrType = AttrType.VEC3,
          min = new object[3] { minNormal.x, minNormal.y, minNormal.z },
          max = new object[3] { maxNormal.x, maxNormal.y, maxNormal.z },
        };

        vbView.accessors.Add(acc);
      }

      if (tangents.Length > 0) {
        AccessorInfo acc = new AccessorInfo
        {
          name = "tangent@" + _mesh.name,
          offset = offsetTangent,
          count = _mesh.vertexCount,
          compType = ComponentType.FLOAT32,
          attrType = AttrType.VEC4,
          min = new object[4] { minTangent.x, minTangent.y, minTangent.z, minTangent.w },
          max = new object[4] { maxTangent.x, maxTangent.y, maxTangent.z, maxTangent.w },
        };

        vbView.accessors.Add(acc);
      }

      if (colors.Length > 0) {
        AccessorInfo acc = new AccessorInfo
        {
          name = "color@" + _mesh.name,
          offset = offsetColor,
          count = _mesh.vertexCount,
          compType = ComponentType.FLOAT32,
          attrType = AttrType.VEC4,
          // TODO: min, max
        };

        vbView.accessors.Add(acc);
      }

      if (uv.Length > 0) {
        AccessorInfo acc = new AccessorInfo
        {
          name = "uv0@" + _mesh.name,
          offset = offsetUV,
          count = _mesh.vertexCount,
          compType = ComponentType.FLOAT32,
          attrType = AttrType.VEC2,
          // TODO: min, max
        };

        vbView.accessors.Add(acc);
      }

      if (uv2.Length > 0) {
        AccessorInfo acc = new AccessorInfo
        {
          name = "uv1@" + _mesh.name,
          offset = offsetUV2,
          count = _mesh.vertexCount,
          compType = ComponentType.FLOAT32,
          attrType = AttrType.VEC2,
          // TODO: min, max
        };

        vbView.accessors.Add(acc);
      }

      if (uv3.Length > 0) {
        AccessorInfo acc = new AccessorInfo
        {
          name = "uv2@" + _mesh.name,
          offset = offsetUV3,
          count = _mesh.vertexCount,
          compType = ComponentType.FLOAT32,
          attrType = AttrType.VEC2,
          // TODO: min, max
        };

        vbView.accessors.Add(acc);
      }

      if (uv4.Length > 0) {
        AccessorInfo acc = new AccessorInfo
        {
          name = "uv3@" + _mesh.name,
          offset = offsetUV4,
          count = _mesh.vertexCount,
          compType = ComponentType.FLOAT32,
          attrType = AttrType.VEC2,
          // TODO: min, max
        };

        vbView.accessors.Add(acc);
      }

      if (boneWeights.Length > 0) {
        AccessorInfo acc = new AccessorInfo
        {
          name = "joints@" + _mesh.name,
          offset = offsetJoint,
          count = _mesh.vertexCount,
          compType = ComponentType.UINT16,
          attrType = AttrType.VEC4,
          // TODO: min, max
        };

        vbView.accessors.Add(acc);

        acc = new AccessorInfo
        {
          name = "weights@" + _mesh.name,
          offset = offsetWeight,
          count = _mesh.vertexCount,
          compType = ComponentType.FLOAT32,
          attrType = AttrType.VEC4,
          // TODO: min, max
        };

        vbView.accessors.Add(acc);
      }

      //
      bufferViews.Add(vbView);
      offsetBuffer += Utils.Align(vbView.length, 4);

      // ibView
      for (int i = 0; i < indexDataList.Count; ++i) {
        byte[] indexData = indexDataList[i];

        BufferViewInfo ibView = new BufferViewInfo
        {
          name = "ib" + i + "@" + _mesh.name,
          offset = offsetBuffer,
          length = indexData.Length,
          type = BufferType.INDEX,
          accessors = new List<AccessorInfo>(),
        };

        AccessorInfo acc = new AccessorInfo
        {
          name = "indices" + i + "@" + _mesh.name,
          offset = 0,
          count = indexData.Length / 2,
          compType = ComponentType.UINT16,
          attrType = AttrType.SCALAR,
        };

        ibView.accessors.Add(acc);

        bufferViews.Add(ibView);
        offsetBuffer += Utils.Align(ibView.length, 4);
      }

      // bpView
      if (bindposesData.Length > 0) {
        BufferViewInfo bpView = new BufferViewInfo
        {
          name = "bp@" + _mesh.name,
          offset = offsetBuffer,
          length = bindposesData.Length,
          type = BufferType.NONE,
          accessors = new List<AccessorInfo>(),
        };

        AccessorInfo acc = new AccessorInfo
        {
          name = "bindposes@" + _mesh.name,
          offset = 0,
          count = bindposesData.Length / (4 * 16),
          compType = ComponentType.FLOAT32,
          attrType = AttrType.MAT4,
        };

        bpView.accessors.Add(acc);

        bufferViews.Add(bpView);
        offsetBuffer += Utils.Align(bpView.length, 4);
      }

      // data
      byte[] data = new byte[offsetBuffer];
      int offset = 0;

      System.Buffer.BlockCopy(_bufInfo.data, 0, data, offset, _bufInfo.data.Length);
      offset += _bufInfo.data.Length;

      System.Buffer.BlockCopy(vertexData, 0, data, offset, vertexData.Length);
      offset += Utils.Align(vertexData.Length, 4);

      for (int i = 0; i < indexDataList.Count; ++i) {
        System.Buffer.BlockCopy(indexDataList[i], 0, data, offset, indexDataList[i].Length);
        offset += Utils.Align(indexDataList[i].Length, 4);
      }

      System.Buffer.BlockCopy(bindposesData, 0, data, offset, bindposesData.Length);
      offset += Utils.Align(bindposesData.Length, 4);

      //
      _bufInfo.data = data;
      _bufInfo.bufferViews.AddRange(bufferViews);
    }
  }
}