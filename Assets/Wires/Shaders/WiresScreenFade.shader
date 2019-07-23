﻿Shader "Wires/ScreenFade"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "RenderType" = "Transparent" "Queue" = "Overlay" "IgnoreProjector" = "True"}
		LOD 100

		Pass
		{
			Cull Off
			ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

			fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
				o.vertex = v.vertex;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 color = _Color;
				return color;
            }
            ENDCG
        }
    }
}
