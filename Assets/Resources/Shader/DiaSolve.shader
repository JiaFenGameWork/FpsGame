Shader "Custom/DiaSolve"
{
    Properties
    {
        _MainTex ("主纹理", 2D) = "white" {}
        _NoiseTex ("噪声纹理", 2D) = "white" {}
        _DissolveAmount ("溶解进度", Range(0, 1)) = 0
        _EdgeWidth ("边缘宽度", Range(0, 0.2)) = 0.05
        _EdgeColor ("边缘颜色", Color) = (1, 0.5, 0, 1)
        _EdgeEmission ("边缘发光强度", Range(0, 5)) = 2
        [HDR] _EdgeEmissionColor ("边缘发光颜色", Color) = (1, 0.3, 0, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100
        Cull Off  // 双面渲染

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 noiseUV : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            
            float _DissolveAmount;
            float _EdgeWidth;
            fixed4 _EdgeColor;
            float _EdgeEmission;
            fixed4 _EdgeEmissionColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.noiseUV = TRANSFORM_TEX(v.uv, _NoiseTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 采样主纹理
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 采样噪声纹理
                float noise = tex2D(_NoiseTex, i.noiseUV).r;
                
                // 计算溶解阈值
                float dissolveThreshold = _DissolveAmount;
                
                // 裁剪溶解区域
                clip(noise - dissolveThreshold);
                
                // 计算边缘发光
                float edge = 1 - smoothstep(0, _EdgeWidth, noise - dissolveThreshold);
                
                // 混合边缘颜色和发光
                fixed4 edgeGlow = _EdgeEmissionColor * _EdgeEmission * edge;
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edge);
                col.rgb += edgeGlow.rgb;
                
                // 应用雾效
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDHLSL
        }
    }
    
    // 备用 Shader
    FallBack "Transparent/Cutout/Diffuse"
}
