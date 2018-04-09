using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using System.IO;

namespace exsdk {
  public partial class Exporter {

    // -----------------------------------------
    // DumpBuffer
    // -----------------------------------------

    void DumpBuffer(BufferInfo _bufInfo, GLTF _gltf) {
      // buffers
      GLTF_Buffer gltfBuffer = new GLTF_Buffer();

      gltfBuffer.name = _bufInfo.name;
      gltfBuffer.uri = _bufInfo.id + ".bin";
      gltfBuffer.byteLength = _bufInfo.data.Length;

      int bufViewIdx = _gltf.bufferViews.Count;
      var bufferViews = new List<GLTF_BufferView>();
      var accessors = new List<GLTF_Accessor>();

      // bufferViews
      foreach (BufferViewInfo bufView in _bufInfo.bufferViews) {
        GLTF_BufferView gltfBufferView = new GLTF_BufferView();

        gltfBufferView.name = bufView.name;
        gltfBufferView.buffer = _gltf.buffers.Count;
        gltfBufferView.byteOffset = bufView.offset;
        gltfBufferView.byteLength = bufView.length;
        gltfBufferView.byteStride = bufView.stride;
        gltfBufferView.target = (int)bufView.type;

        bufferViews.Add(gltfBufferView);

        // accessors
        foreach (AccessorInfo acc in bufView.accessors) {
          GLTF_Accessor gltfAccessor = new GLTF_Accessor();

          gltfAccessor.name = acc.name;
          gltfAccessor.bufferView = bufViewIdx;
          gltfAccessor.byteOffset = acc.offset;
          gltfAccessor.componentType = (int)acc.compType;
          gltfAccessor.count = acc.count;
          gltfAccessor.type = acc.attrType.ToString();
          gltfAccessor.min = acc.min;
          gltfAccessor.max = acc.max;

          accessors.Add(gltfAccessor);
        }

        ++bufViewIdx;
      }

      _gltf.accessors.AddRange(accessors);
      _gltf.bufferViews.AddRange(bufferViews);
      _gltf.buffers.Add(gltfBuffer);
    }
  }
}