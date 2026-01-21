Shader "NekoFX/EnergyBall"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutTex ("Out Texture", 2D) = "white" {}
        _FresnelPower ("Fresnel Power", Range(1, 5)) = 2
        [HDR]_InnerEnergyColor("Inner Energy Color", Color) = (1,1,1,1)
        [HDR]_OuterEnergyColor("Outer Energy Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha One  // 加法混合，适合能量球发光效果
        // 其他选择：
        // Blend SrcAlpha OneMinusSrcAlpha  // 标准透明混合
        // Blend One One                     // 纯加法（更亮）
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS :NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _DeltaTime;
            float _FresnelPower;
            float4 _InnerEnergyColor;
            float4 _OuterEnergyColor;
            CBUFFER_END



            Texture2D _MainTex;
            Texture2D _OutTex;
            SAMPLER(sampler_MainTex);
            SAMPLER(sampler_OutTex);
            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.vertex.xyz);
                o.positionWS = float4(TransformObjectToWorld(v.vertex.xyz), 1);
                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                o.uv+= float2(_Time.y*0.1f,0);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float fresnel = pow(1- dot(i.normalWS, viewDir), _FresnelPower);
                float innerball = 1-smoothstep(0.0f, 0.2f, fresnel);
                fresnel = smoothstep(0.0f, 0.3f, fresnel);
                half4 OutColor = SAMPLE_TEXTURE2D(_OutTex, sampler_OutTex, i.uv);
                // sample the texture
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 OutLayer = smoothstep(0.0f, 0.3f, fresnel*OutColor.r);
                color.rgba *= _InnerEnergyColor;
                color.a -= fresnel;
                color.rgba += OutLayer.rgba*_OuterEnergyColor.rgba+innerball*_InnerEnergyColor.rgba;
                return color;
            }
            ENDHLSL
        }
    }
}
