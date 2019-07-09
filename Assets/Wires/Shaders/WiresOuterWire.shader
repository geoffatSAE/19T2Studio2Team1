﻿// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Wires/OuterWire"
{
    Properties
    {
		_Color("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
		_PanningSpeed ("Panning Speed (X, Y)", Vector) = (1, 0, 0, 0)
		_AlphaScale ("Alpha Scale", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent"}

		CGPROGRAM

		#pragma surface surf Lambert alpha:blend nolighting noshadow

		struct Input 
		{
			float3 worldPos;
			float3 worldNormal;
		};

		half4 _Color;
		sampler2D _MainTex;
		half2 _PanningSpeed;
		half _AlphaScale;

		void surf(Input IN, inout SurfaceOutput o) 
		{
			float3 normal = abs(IN.worldNormal);
			float2 offset = _PanningSpeed * _Time.x;

			fixed4 x = tex2D(_MainTex, IN.worldPos.yz + offset);
			fixed4 y = tex2D(_MainTex, IN.worldPos.xz + offset);
			fixed4 z = tex2D(_MainTex, IN.worldPos.xy + offset);

			fixed4 c = _Color * (x * normal.x + y * normal.y + z * normal.z);
			o.Albedo = c.rgb;
			o.Alpha = 0.2f * _AlphaScale;
		}
		ENDCG
	}
	Fallback "Diffuse"
}
