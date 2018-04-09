using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using System.IO;

namespace exsdk {
  public partial class Exporter {

    // -----------------------------------------
    // DumpTexture
    // -----------------------------------------

    JSON_Texture DumpTexture(Texture _texture) {
      JSON_Texture result = new JSON_Texture();

      result.anisotropy = _texture.anisoLevel;

      if (_texture is Texture2D) {
        result.type = "2d";

        if (_texture.filterMode == UnityEngine.FilterMode.Point) {
          result.minFilter = "nearest";
          result.magFilter = "nearest";
          result.mipFilter = "nearest";
        } else if (_texture.filterMode == UnityEngine.FilterMode.Bilinear) {
          result.minFilter = "linear";
          result.magFilter = "linear";
          result.mipFilter = "nearest";
        } else if (_texture.filterMode == UnityEngine.FilterMode.Trilinear) {
          result.minFilter = "linear";
          result.magFilter = "linear";
          result.mipFilter = "linear";
        }

        if (_texture.wrapMode == TextureWrapMode.Repeat) {
          result.wrapS = "repeat";
          result.wrapT = "repeat";
        } else if (_texture.wrapMode == TextureWrapMode.Clamp) {
          result.wrapS = "clamp";
          result.wrapT = "clamp";
        }
      }

      return result;
    }
  }
}