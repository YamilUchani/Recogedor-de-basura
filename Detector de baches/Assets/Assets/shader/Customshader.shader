Shader "Custom/HeightShader" {
    Properties {
        _AboveTexture ("Above Texture", 2D) = "white" {}
        _BelowTexture ("Below Texture", 2D) = "white" {}
        _HeightThreshold ("Height Threshold", Range(-1,0)) = 0.05
        _AboveColor ("Above Color", Color) = (1, 1, 1, 1)
    }
 
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
 
        CGPROGRAM
        #pragma surface surf Lambert
 
        sampler2D _AboveTexture;
        sampler2D _BelowTexture;
        float _HeightThreshold;
        fixed4 _AboveColor;
 
        struct Input {
            float2 uv_AboveTexture;
            float2 uv_BelowTexture;
            float3 worldPos;
        };
 
        void surf (Input IN, inout SurfaceOutput o) {
            float height = IN.worldPos.y;
            
            // Compara la altura con el umbral
            if (height >= _HeightThreshold) {
                // Si la altura está por encima del umbral, utiliza la textura de arriba y el color de arriba
                o.Albedo = _AboveColor.rgb;
                o.Albedo *= tex2D(_AboveTexture, IN.uv_AboveTexture).rgb;
            } else {
                // Si la altura está por debajo del umbral, utiliza la textura de abajo
                o.Albedo = tex2D(_BelowTexture, IN.uv_BelowTexture).rgb;
            }
        }
        ENDCG
    }
 
    FallBack "Diffuse"
} 
