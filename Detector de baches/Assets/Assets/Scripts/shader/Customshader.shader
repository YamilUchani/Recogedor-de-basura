Shader "Custom/HeightShader" {
    Properties {
        _Threshold1 ("Threshold 1", Range(-10,10)) = 0.0
        _Threshold2 ("Threshold 2", Range(-10,10)) = 5.0
        _TransitionRange ("Transition Range", Range(0,2)) = 0.5
        _Texture0 ("Texture 0", 2D) = "white" {}
        _Scale0 ("Scale 0", Range(0,20)) = 1
        _Color0 ("Color 0", Color) = (1,1,1,1)
        _Texture1 ("Texture 1", 2D) = "white" {}
        _Scale1 ("Scale 1", Range(0,20)) = 1
        _Color1 ("Color 1", Color) = (1,1,1,1)
        _Texture2 ("Texture 2", 2D) = "white" {}
        _Scale2 ("Scale 2", Range(0,20)) = 1
        _Color2 ("Color 2", Color) = (1,1,1,1)
    }
 
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
 
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert
 
        sampler2D _Texture0;
        sampler2D _Texture1;
        sampler2D _Texture2;
        float _Scale0;
        float _Scale1;
        float _Scale2;
        float _Threshold1;
        float _Threshold2;
        float _TransitionRange;
        fixed4 _Color0;
        fixed4 _Color1;
        fixed4 _Color2;
 
        struct Input {
            float3 objectPos;
        };
 
        void vert (inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.objectPos = v.vertex.xyz;
        }
 
        void surf (Input IN, inout SurfaceOutput o) {
            float height = IN.objectPos.y;
            float2 uv = IN.objectPos.xz;
            
            // Calcular las texturas escaladas
            float2 uv0 = uv * _Scale0;
            float2 uv1 = uv * _Scale1;
            float2 uv2 = uv * _Scale2;
            
            fixed4 tex0 = tex2D(_Texture0, uv0) * _Color0;
            fixed4 tex1 = tex2D(_Texture1, uv1) * _Color1;
            fixed4 tex2 = tex2D(_Texture2, uv2) * _Color2;
            
            if (height < _Threshold1 - _TransitionRange) {
                o.Albedo = tex0.rgb;
            } else if (height < _Threshold1 + _TransitionRange) {
                // Transición suave entre tex0 y tex1
                float blend = (height - (_Threshold1 - _TransitionRange)) / (2 * _TransitionRange);
                o.Albedo = lerp(tex0, tex1, blend).rgb;
            } else if (height < _Threshold2 - _TransitionRange) {
                o.Albedo = tex1.rgb;
            } else if (height < _Threshold2 + _TransitionRange) {
                // Transición suave entre tex1 y tex2
                float blend = (height - (_Threshold2 - _TransitionRange)) / (2 * _TransitionRange);
                o.Albedo = lerp(tex1, tex2, blend).rgb;
            } else {
                o.Albedo = tex2.rgb;
            }
        }
        ENDCG
    }
 
    FallBack "Diffuse"
}