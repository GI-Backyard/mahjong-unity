Shader "u3d-exporter/matcap" {
  Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _ColorFactor ("Color Factor", Range (0, 1)) = 0.5
    _MainTex ("Main Texture (RGB)", 2D) = "white" {}
    _MatcapTex("Matcap Texture (RGB)", 2D) = "white" {}
  }

  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200

    Pass {

      CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        struct v2f {
          float4 pos : POSITION;
          float2 main_uv : TEXCOORD0;
          float2 matcap_uv : TEXCOORD1;
        };

        v2f vert(appdata_base v) {
          v2f o;
          o.pos = UnityObjectToClipPos(v.vertex);
          o.main_uv = v.texcoord;
          float3 normal_w = UnityObjectToWorldNormal(v.normal);
          o.matcap_uv = normal_w.xy * 0.5 + 0.5;
          return o;
        }

        uniform float4 _Color;
        uniform float _ColorFactor;
        uniform sampler2D _MainTex;
        uniform sampler2D _MatcapTex;

        float4 frag(v2f i) : COLOR {
          float4 mainColor = tex2D(_MainTex, i.main_uv) * _Color;
          float4 matcapColor = tex2D(_MatcapTex, i.matcap_uv);
          return mainColor * _ColorFactor + matcapColor * (1 - _ColorFactor);
        }
      ENDCG
    }
  }

  Fallback "u3d-exporter/vertex-lit"
}
