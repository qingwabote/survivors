Shader "UI/GaussianBlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        
        [HideInInspector] _Stencil ("Stencil ID", Int) = 1
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane" 
            "CanUseSpriteAtlas"="True"
            
        }

        Stencil
        {
            Ref 1
            Comp Equal
            Pass Keep
            ReadMask 255
            WriteMask 255
        }

        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            fixed4 _BaseColor;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(0,0,0,0);
                float2 offset = _BlurSize * _MainTex_TexelSize.xy;

                col += tex2D(_MainTex, i.uv + float2(-offset.x, -offset.y)) * 1.0;
                col += tex2D(_MainTex, i.uv + float2( 0.0f   , -offset.y)) * 2.0;
                col += tex2D(_MainTex, i.uv + float2( offset.x, -offset.y)) * 1.0;

                col += tex2D(_MainTex, i.uv + float2(-offset.x,  0.0f )) * 2.0;
                col += tex2D(_MainTex, i.uv) * 4.0;
                col += tex2D(_MainTex, i.uv + float2( offset.x,  0.0f )) * 2.0;

                col += tex2D(_MainTex, i.uv + float2(-offset.x,  offset.y)) * 1.0;
                col += tex2D(_MainTex, i.uv + float2( 0.0f   ,  offset.y)) * 2.0;
                col += tex2D(_MainTex, i.uv + float2( offset.x,  offset.y)) * 1.0;

                col /= 16.0;

                col *= _BaseColor;

                return col;
            }
            ENDCG
        }
    } 
}
