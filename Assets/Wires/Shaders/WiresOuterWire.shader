Shader "Wires/OuterWire"
{
    Properties
    {
		_Color ("Color", Color) = (1, 1, 1, 1)
		_Alpha ("Alpha", Range(0, 1)) = 0.2
		_MaxAlpha ("Max Alpha", Range(0, 1)) = 0.8
        _MainTex ("Texture", 2D) = "white" {}
		_PanningSpeed ("Panning Speed (X, Y)", Vector) = (1, 0, 0, 0)
		_AlphaScale ("Alpha Scale", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }

		CGPROGRAM

		#pragma surface surf Lambert alpha:blend nolighting noshadow

		struct Input 
		{
			float3 worldPos;
			float3 worldNormal;
		};

		half4 _Color;
		half _Alpha;
		half _MaxAlpha;
		sampler2D _MainTex;
		half2 _PanningSpeed;
		half _AlphaScale;

		float ease(float alpha)
		{
			// Ease Out Cubic
			// See https://easings.net/en
			return (--alpha) * alpha * alpha + 1;
		}

		void surf(Input IN, inout SurfaceOutput o) 
		{
			// Sum up weights to one
			float3 blend = abs(IN.worldNormal);
			blend /= dot(blend, 1.f);

			// Move texture (we sample from different location)
			float2 offset = _PanningSpeed * _Time.x;

			fixed4 x = tex2D(_MainTex, IN.worldPos.yz + offset);
			fixed4 y = tex2D(_MainTex, IN.worldPos.xz + offset);
			fixed4 z = tex2D(_MainTex, IN.worldPos.xy + offset);

			// How close pixel is to center of outer border (we assume model being used is a cylinder
			// facing upwards that vertices extend out to -0.5 and 0.5
			fixed3 objectPos = mul(unity_WorldToObject, fixed4(IN.worldPos, 1)).xyz;
			fixed centerRatio = 1 - (abs(objectPos.y) + 0.5);

			fixed4 c = _Color * (x * blend.x + y * blend.y + z * blend.z);
			o.Albedo = c.rgb;
			o.Alpha = _Alpha* _AlphaScale* ease(centerRatio);
		}
		ENDCG
	}
	Fallback "Diffuse"
}
