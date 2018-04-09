using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Linq;

namespace exsdk {
  public partial class Exporter {

    // -----------------------------------------
    // DumpTexture
    // -----------------------------------------

    JSON_SpriteTexture DumpSpriteTexture(Texture _texture) {
      JSON_SpriteTexture result = new JSON_SpriteTexture();

      result.anisotropy = _texture.anisoLevel;
      result.type = "sprite";

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

      string assetPath = AssetDatabase.GetAssetPath(_texture);
      var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToList();

      foreach (var sprite in sprites) {
        JSON_Sprite spriteJson = new JSON_Sprite();
        int height = sprite.texture.height;
        spriteJson.texture = Utils.AssetID(_texture);
        spriteJson.rotated = false;
        spriteJson.x = sprite.rect.x;
        spriteJson.y = height - sprite.rect.height - sprite.rect.y;
        spriteJson.width = sprite.rect.width;
        spriteJson.height = sprite.rect.height;
        spriteJson.left = sprite.border.x;
        spriteJson.right = sprite.border.z;
        spriteJson.bottom = sprite.border.y;
        spriteJson.top = sprite.border.w;

        result.sprites.Add(sprite.name, spriteJson);
      }

      return result;
    }
  }
}