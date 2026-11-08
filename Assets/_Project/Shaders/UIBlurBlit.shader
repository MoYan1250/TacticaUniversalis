Shader "Hidden/Tactica/UIBlurBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset ("Offset", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "KawaseBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _Offset;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy * _Offset;
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * 0.25h;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texel.x, texel.y)) * 0.25h;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-texel.x, texel.y)) * 0.25h;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texel.x, -texel.y)) * 0.25h;
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
