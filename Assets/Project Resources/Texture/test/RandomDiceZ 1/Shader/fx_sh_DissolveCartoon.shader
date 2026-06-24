Shader "RandomDice2/fx_sh_DissolveCartoon"
{
    Properties
    {
        [HDR]_MainCol("Color",color) = (1,1,1,1)
        _MainTex ("MainTex", 2D) = "white" {}
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"  }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Zwrite Off

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;

                float3 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;

                float2 uv : TEXCOORD0;

                float CustomData1 : TEXCOORD1;
            };

            sampler2D _MainTex;

            float4 _MainTex_ST;
            float4 _ExpTex_ST;
            float4 _MainCol;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv.xy = TRANSFORM_TEX(v.uv.xy, _MainTex);
                o.CustomData1 = v.uv.z;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float fCustomData1 = i.CustomData1;

                float4 col;
                float4 expcol;
                float4 MTex = tex2D(_MainTex, i.uv);

                col.rgb = MTex.rgb * _MainCol.rgb * i.color.rgb;
                col.a = saturate(ceil(MTex.a - fCustomData1) * _MainCol.a * i.color.a);


                return col;
            }
            ENDCG
        }
    }
}
