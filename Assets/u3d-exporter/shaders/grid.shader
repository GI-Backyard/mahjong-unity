Shader "u3d-exporter/grid" {
  Properties {
    [Toggle(WPOS_ON)] _WPOS("Enable World Position UV", Int) = 0
    _TilingX ("Global Tiling X", Float) = 1
    _TilingY ("Global Tiling Y", Float) = 1

    _BaseColorWhite ("Base Color White", Color) = (0.1, 0.1, 0.1, 1)
    _BaseColorBlack ("Base Color Black", Color) = (0.3, 0.3, 0.3, 1)
    _BasePattern ("Base Patter", 2D) = "black" {}

    _SubPatternColor ("Sub Pattern Color 01", Color) = (1, 1, 1, 1)
    _SubPattern ("Sub Pattern 01", 2D) = "black" {}

    _SubPatternColor2 ("Sub Pattern Color 02", Color) = (1, 1, 1, 1)
    _SubPattern2 ("Sub Pattern 02", 2D) = "black" {}

  }
  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200

    CGPROGRAM
    // Physically based Standard lighting model, and enable shadows on all light types
    #pragma surface surf Standard fullforwardshadows
    // #pragma surface surf NoLighting noambient
    #pragma shader_feature WPOS_ON
    // #pragma target 4.0

    fixed4 _BaseColorWhite;
    fixed4 _BaseColorBlack;
    sampler2D _BasePattern;

    fixed4 _SubPatternColor;
    sampler2D _SubPattern;

    fixed4 _SubPatternColor2;
    sampler2D _SubPattern2;

#if WPOS_ON
    float4 _BasePattern_ST;
    float4 _SubPattern_ST;
    float4 _SubPattern2_ST;
#endif

    struct Input {
      float2 uv_BasePattern;
      float2 uv_SubPattern;
      float2 uv_SubPattern2;

      float3 worldNormal;
      float3 worldPos;
    };

    half _TilingX;
    half _TilingY;

    fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) {
      fixed4 c;
      // c.rgb = LinearToGammaSpace(s.Albedo);
      c.rgb = s.Albedo;
      c.a = s.Alpha;
      return c;
    }

    void surf (Input IN, inout SurfaceOutputStandard o) {
    // void surf (Input IN, inout SurfaceOutput o) {

      float2 overT = float2(_TilingX,_TilingY);

      float2 UV_Base = IN.uv_BasePattern * overT;
      float2 UV_Sub = IN.uv_SubPattern * overT;
      float2 UV_Sub2 = IN.uv_SubPattern2 * overT;

#if WPOS_ON
      if (abs(IN.worldNormal.x)>0.5) { // side
        UV_Base = (IN.worldPos.zy * overT * _BasePattern_ST.xy) + _BasePattern_ST.zw;
        UV_Sub = (IN.worldPos.zy * overT * _SubPattern_ST.xy) + _SubPattern_ST.zw;
        UV_Sub2 = (IN.worldPos.zy * overT * _SubPattern2_ST.xy) + _SubPattern2_ST.zw;
      } else if (abs(IN.worldNormal.z)>0.5) { // front
        UV_Base = (IN.worldPos.xy * overT * _BasePattern_ST.xy) + _BasePattern_ST.zw;
        UV_Sub = (IN.worldPos.xy * overT * _SubPattern_ST.xy) + _SubPattern_ST.zw;
        UV_Sub2 = (IN.worldPos.xy * overT * _SubPattern2_ST.xy) + _SubPattern2_ST.zw;
      } else { // top
        UV_Base = (IN.worldPos.xz * overT * _BasePattern_ST.xy) + _BasePattern_ST.zw;
        UV_Sub = (IN.worldPos.xz * overT * _SubPattern_ST.xy) + _SubPattern_ST.zw;
        UV_Sub2 = (IN.worldPos.xz * overT * _SubPattern2_ST.xy) + _SubPattern2_ST.zw;
      }
#endif

      fixed4 texColBase = tex2D (_BasePattern, UV_Base);
      fixed4 texColSub = tex2D (_SubPattern, UV_Sub);
      fixed4 texColSub2 = tex2D (_SubPattern2, UV_Sub2);

      fixed4 colBase = (_BaseColorWhite * texColBase + _BaseColorBlack * (1 - texColBase));
      fixed4 colFinal =
        colBase * (1 - texColSub) +
        (_SubPatternColor * _SubPatternColor.a + colBase * (1-_SubPatternColor.a)) * texColSub
        ;
      colFinal =
        colFinal * (1 - texColSub2) +
        (_SubPatternColor2 * _SubPatternColor2.a + colFinal * (1-_SubPatternColor2.a)) * texColSub2
        ;

      o.Albedo = colFinal.rgb;
      o.Alpha = 1;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
