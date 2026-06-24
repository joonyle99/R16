Shader "Cooptd_UI/VFX/BaseAdd01"
{
    Properties
    {
        _Color ("Color" , Vector) = (1,1,1,1)
        _ColorPow("ColorPow", Float) = 1
        _MainTex ("Texture", 2D) = "white" {}
        _Stencil ("Stencil Ref", Float) = 0
        _StencilReadMask ("ReadMask [0;255]", Int) = 255
        _StencilWriteMask ("WriteMask [0;255]", Int) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Int) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Int) = 0
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        LOD 100

        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Stencil
            {
                Ref [_Stencil]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilOp]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0 

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                half4  color  : COLOR; 
                half2  uv     : TEXCOORD0;
            };

            struct v2f
            {
                half2  uv      : TEXCOORD0;
                half4  color   : COLOR;
                float4 vertex  : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                sampler2D _MainTex;
                float4    _MainTex_ST;
                half4     _Color;
                half      _ColorPow;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.color  = v.color * _Color; 
                o.vertex = UnityObjectToClipPos(v.vertex); 
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 t = tex2D(_MainTex, i.uv) * _ColorPow;
                half4 c = saturate(t) * i.color * i.color.a; 
                return c;
            }
            ENDCG
        }
    }
}
