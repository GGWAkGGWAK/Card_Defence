Shader "CardDefense/MonsterChromaKey"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _KeyColor ("Key Color", Color) = (0.94,0.045,0.89,1)
        _Tolerance ("Tolerance", Range(0,1)) = 0.22
        _Softness ("Softness", Range(0.001,0.5)) = 0.10
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _KeyColor;
            float _Tolerance;
            float _Softness;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv) * input.color;
                float keyDistance = distance(color.rgb, _KeyColor.rgb);
                float pinkDominance = min(color.r, color.b) - color.g;
                float chromaMask = smoothstep(0.48, 0.68, pinkDominance);
                float distanceMask = 1.0 - smoothstep(_Tolerance, _Tolerance + _Softness, keyDistance);
                color.a *= 1.0 - max(chromaMask, distanceMask);
                return color;
            }
            ENDCG
        }
    }
}
