﻿Shader "Wires/Spark"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
		_Speed ("Speed", Float) = -10
        _Extent ("Extent", Float) = 0.1
    }
    SubShader
	{
		Tags { "RenderType" = "Opaque" }

		Lighting Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			fixed4 _Color;
			fixed _Speed;
			fixed _Extent;
			fixed3 _Position;

			// Global value (musics beat time)
			uniform float _BeatTime;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				fixed3 center = mul(unity_ObjectToWorld, fixed4(0, 0, 0, 1)).xyz;
				fixed3 vertex = mul(unity_ObjectToWorld, v.vertex).xyz;

				vertex += normalize(vertex - center) * (_Extent * _BeatTime);
				fixed4 object = mul(unity_WorldToObject, fixed4(vertex, 1.f));

				v2f o;
				o.vertex = UnityObjectToClipPos(object);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col;
				col.rgb = _Color.rgb * lerp(0.8f, 1.2f, abs(_CosTime.w));
				col.a = 1.f;
				return col;
			}
			ENDCG
		}
    }
}
