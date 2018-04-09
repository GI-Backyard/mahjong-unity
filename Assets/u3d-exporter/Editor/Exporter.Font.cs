using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using System.IO;

namespace exsdk {
  public partial class Exporter {
    // -----------------------------------------
    // DumpBitmapFont
    // -----------------------------------------
    JSON_BitmapFont DumpBitmapFont(Font font) {
      JSON_BitmapFont result = new JSON_BitmapFont();
      SerializedObject so = new SerializedObject(font);
      so.Update();

      // JSON_Font_Chars
      CharacterInfo[] infos = font.characterInfo;
      Texture tex = font.material.mainTexture;
      for (int i = 0; i < infos.Length; i++) {
        JSON_Font_Chars jsonInfo = new JSON_Font_Chars();
        CharacterInfo info = infos[i];

        jsonInfo.id = info.index;
        jsonInfo.x = info.uvBottomLeft.x * tex.width;
        jsonInfo.y = (1 - info.uvTopRight.y) * tex.height;
        jsonInfo.width = info.maxX - info.minX;
        jsonInfo.height = info.maxY - info.minY;
        jsonInfo.xoffset = info.minX;
        jsonInfo.yoffset = -info.maxY + font.ascent;
        jsonInfo.xadvance = info.advance;

        result.chars.Add(info.index.ToString(), jsonInfo);
      }

      // JSON_Font_Kerning
      SerializedProperty kernings = so.FindProperty("m_KerningValues");
      int len = kernings.arraySize;
      for (int i = 0; i < len; i++) {
        JSON_Font_Kerning jsonKerning = new JSON_Font_Kerning();
        SerializedProperty kerning = kernings.GetArrayElementAtIndex(i);
        SerializedProperty pairProp = kerning.FindPropertyRelative("first");
        pairProp.Next(true);
        jsonKerning.first = pairProp.intValue;
        pairProp.Next(false);
        jsonKerning.second = pairProp.intValue;
        jsonKerning.amount = (int)kerning.FindPropertyRelative("second").floatValue;

        result.kernings.Add(jsonKerning);
      }

      // referenced texture
      result.texture = Utils.AssetID(font.material.mainTexture);

      // common
      result.face = font.name;
      result.size = font.fontSize;
      result.lineHeight = font.lineHeight;
      result.lineBaseHeight = font.ascent;
      result.scaleW = tex.width;
      result.scaleH = tex.height;

      return result;
    }

    // -----------------------------------------
    // DumpOpenTypeFont
    // -----------------------------------------
    JSON_OpenTypeFont DumpOpenTypeFont(Font font) {
      JSON_OpenTypeFont result = new JSON_OpenTypeFont();
      SerializedObject so = new SerializedObject(font);
      so.Update();

      Texture tex = font.material.mainTexture;

      // referenced font
      result.font = Utils.AssetID(font);

      // common
      result.size = font.fontSize;
      result.lineHeight = font.lineHeight;
      result.width = tex.width;
      result.height = tex.height;

      return result;
    }
  }
}
