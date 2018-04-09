Shader "u3d-exporter/pbr" {
  Properties {
    _Color ("Albedo Color", Color) = (1,1,1,1)
    [Toggle(USE_ALBEDO_TEXTURE)] _USE_ALBEDO_TEXTURE("Use Albedo Texture", Int) = 0
    _MainTex ("Albedo Texture", 2D) = "white" {}
    _Metallic ("Metallic", Float) = 1
    [Toggle(USE_METALLIC_TEXTURE)] _USE_METALLIC_TEXTURE("Use Metallic Texture", Int) = 0
    _MetallicTexture ("Metallic Texture", 2D) = "white" {}
    _Roughness ("Roughness", Float) = 0.5
    [Toggle(USE_ROUGHNESS_TEXTURE)] _USE_ROUGHNESS_TEXTURE("Use Roughness Texture", Int) = 0
    _RoughnessTexture ("Roughness Texture", 2D) = "white" {}
    _OcclusionStrength("Ambient Occlusion", Range(0.0, 1.0)) = 1.0
    [Toggle(USE_AO_TEXTURE)] _USE_AO_TEXTURE("Use AO Texture", Int) = 0
    _OcclusionMap("Ambient Occlusion Texture", 2D) = "white" {}
    [Toggle(USE_NORMAL_TEXTURE)] _USE_NORMAL_TEXTURE("Use Normal Texture", Int) = 0
    _BumpMap ("Normal Texture", 2D) = "bump" {}
  }
  SubShader{
    Tags { "RenderType" = "Opaque" }

    LOD 200
    CGPROGRAM
    // Physically based Standard lighting model, and enable shadows on all light types
    #pragma surface surf Standard fullforwardshadows nometa

    #pragma shader_feature USE_ALBEDO_TEXTURE
    #pragma shader_feature USE_METALLIC_TEXTURE
    #pragma shader_feature USE_ROUGHNESS_TEXTURE
    #pragma shader_feature USE_AO_TEXTURE
    #pragma shader_feature USE_NORMAL_TEXTURE


    // Use shader model 3.0 target, to get nicer looking lighting
    #pragma target 3.0

#if USE_ALBEDO_TEXTURE
    sampler2D _MainTex;
#endif
    fixed4 _Color;

#if USE_METALLIC_TEXTURE
    sampler2D _MetallicTexture;
#else
    float _Metallic;
#endif
#if USE_ROUGHNESS_TEXTURE
    sampler2D _RoughnessTexture;
#else
    float _Roughness;
#endif
#if USE_AO_TEXTURE
    sampler2D _OcclusionMap;
#else
    float _OcclusionStrength;
#endif
#if USE_NORMAL_TEXTURE
    sampler2D _BumpMap;
#endif

    struct Input {
      float2 uv_MainTex;
      float2 uv_MetallicTexture;
      float2 uv_RoughnessTexture;
      float2 uv_OcclusionMap;
      float2 uv_BumpMap;
    };

    void surf (Input IN, inout SurfaceOutputStandard o) {
#if USE_ALBEDO_TEXTURE
      fixed4 c = _Color * tex2D(_MainTex, IN.uv_MainTex);
#else
      fixed4 c = _Color;
#endif
      o.Albedo = c.rgb;
#if USE_NORMAL_TEXTURE
      o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
#endif
#if USE_METALLIC_TEXTURE
      o.Metallic = tex2D(_MetallicTexture, IN.uv_MetallicTexture).r;
#else
      o.Metallic = _Metallic;
#endif
#if USE_ROUGHNESS_TEXTURE
      o.Smoothness = 1.0 - tex2D(_RoughnessTexture, IN.uv_RoughnessTexture).r;
#else
      o.Smoothness = 1.0 - _Roughness;
#endif
#if USE_AO_TEXTURE
      o.Occlusion = tex2D(_OcclusionMap, IN.uv_OcclusionMap).r;
#else
      o.Occlusion = _OcclusionStrength;
#endif
      o.Alpha = c.a;
    }
    ENDCG
  }
  FallBack "u3d-exporter/vertex-lit"
}
