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
            float3 worldPos;
        };
 
        void surf (Input IN, inout SurfaceOutput o) {
            float height = IN.worldPos.y;
            
            // Normalizar la altura en el rango [0, 1]
            float normalizedHeight = (height - _HeightThreshold) / (1 - _HeightThreshold);
            
            // Mapear las coordenadas UV de la textura en función de la altura normalizada
            float2 uv_AboveTexture = IN.worldPos.xz;
            float2 uv_BelowTexture = IN.worldPos.xz;
            
            // Si la altura está por encima del umbral, utiliza la textura de arriba y el color de arriba
            if (normalizedHeight >= 0) {
                o.Albedo = _AboveColor.rgb;
                o.Albedo *= tex2D(_AboveTexture, uv_AboveTexture).rgb;
            } else {
                // Si la altura está por debajo del umbral, utiliza la textura de abajo
                o.Albedo = tex2D(_BelowTexture, uv_BelowTexture).rgb;
            }
        }
        ENDCG
    }
 
    FallBack "Diffuse"
}
