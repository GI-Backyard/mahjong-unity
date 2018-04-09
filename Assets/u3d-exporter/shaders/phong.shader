Shader "u3d-exporter/phong" {
  Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    [Toggle(USE_DIFFUSE_TEXTURE)] _USE_DIFFUSE_TEXTURE("Use Diffuse Texture", Int) = 0
    _MainTex ("Base (RGB)", 2D) = "white" {}
    [Toggle(USE_SPECULAR)] _USE_SPECULAR("Use Specular", Int) = 0
    _SpecularColor ("Specular Color", Color) = (0,0,0,1)
    [Toggle(USE_SPECULAR_TEXTURE)] _USE_SPECULAR_TEXTURE("Use Specular Texture", Int) = 0
    _SpecularMap ("Specular Texture", 2D) = "white" {}
    [Toggle(USE_EMISSIVE)] _USE_EMISSIVE("Use Emissive", Int) = 0
    _Emission ("Emissive Color", Color) = (0,0,0,1)
    [Toggle(USE_EMISSIVE_TEXTURE)] _USE_EMISSIVE_TEXTURE("Use Emissive Texture", Int) = 0
    _EmissionMap ("Emissive Texture", 2D) = "black" {}
    _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
    [Toggle(USE_NORMAL_TEXTURE)] _USE_NORMAL_TEXTURE("Use Normal Texture", Int) = 0
    _BumpMap ("Normal Texture", 2D) = "bump" {}
  }

  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200

    CGPROGRAM
      #pragma surface surf CustomPhong

      #pragma shader_feature USE_DIFFUSE_TEXTURE
      #pragma shader_feature USE_SPECULAR
      #pragma shader_feature USE_SPECULAR_TEXTURE
      #pragma shader_feature USE_EMISSIVE
      #pragma shader_feature USE_EMISSIVE_TEXTURE
      #pragma shader_feature USE_NORMAL_TEXTURE

      half4 LightingCustomPhong(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
        half3 h = normalize(lightDir + viewDir);
        half ndl = max(0.0, dot(s.Normal, lightDir));
        half ndh = max(0.0, dot(s.Normal, h));
        ndh = (ndl == 0.0) ? 0.0: ndh;
        ndh = pow(ndh, max(1.0, s.Gloss * 128.0));
        half4 c;
        c.rgb = (s.Albedo * _LightColor0.rgb * ndl + _LightColor0.rgb * s.Specular * ndh) * atten + s.Emission;
        c.a = s.Alpha;
        return c;
      }
#if USE_DIFFUSE_TEXTURE
      sampler2D _MainTex;
#endif
      fixed4 _Color;

#if USE_SPECULAR
      fixed4 _SpecularColor;
      #if USE_SPECULAR_TEXTURE
            sampler2D _SpecularMap;
      #endif
#endif

#if USE_EMISSIVE
      fixed4 _Emission;
      #if USE_EMISSIVE_TEXTURE
            sampler2D _EmissionMap;
      #endif
#endif

#if USE_NORMAL_TEXTURE
      sampler2D _BumpMap;
#endif
      float _Shininess;

      struct Input {
        float2 uv_MainTex;
        float2 uv_SpecularMap;
        float2 uv_EmissionMap;
        float2 uv_BumpMap;
      };

      void surf (Input IN, inout SurfaceOutput o) {
#if USE_DIFFUSE_TEXTURE
        fixed4 c = _Color * tex2D(_MainTex, IN.uv_MainTex);
#else
        fixed4 c = _Color;
#endif
        o.Albedo = c.rgb;

#if USE_SPECULAR
      o.Specular = _SpecularColor.r;
      #if USE_SPECULAR_TEXTURE
            o.Specular = tex2D(_SpecularMap, IN.uv_SpecularMap).r;
      #endif
#endif

#if USE_EMISSIVE
      o.Emission = _Emission;
      #if USE_EMISSIVE_TEXTURE
            o.Emission = tex2D(_EmissionMap, IN.uv_EmissionMap);
      #endif
#endif

#if USE_NORMAL_TEXTURE
        o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
#endif
        o.Gloss = _Shininess;
        o.Alpha = c.a;
      }
    ENDCG
  }

  FallBack "u3d-exporter/unlit"
}