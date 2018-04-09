
Shader "u3d-exporter/unlit" {
  Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB)", 2D) = "white" {}
  }

  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200

    CGPROGRAM
      #pragma surface surf NoLighting noambient

      fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) {
        fixed4 c;
        // c.rgb = LinearToGammaSpace(s.Albedo);
        c.rgb = s.Albedo;
        c.a = s.Alpha;
        return c;
      }

      sampler2D _MainTex;
      fixed4 _Color;

      struct Input {
        float2 uv_MainTex;
      };

      void surf (Input IN, inout SurfaceOutput o) {
        fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
        o.Albedo = c.rgb;
        o.Alpha = c.a;
      }
    ENDCG
  }
}
