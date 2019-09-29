Shader "Wires/OuterWire"
{
    Properties
    {
		_Color ("Color", Color) = (1, 1, 1, 1)
		_Alpha ("Alpha", Range(0, 1)) = 0.2
        _MainTex ("Texture", 2D) = "white" {}
		_TexScalar ("Texture Scalar", Float) = 1
		_PanningSpeed ("Panning Speed (X, Y)", Vector) = (1, 0, 0, 0)
		_AlphaScale ("Alpha Scale", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }

		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag           

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;			
				float2 uv : TEXCOORD0;
				half3 objectPos : TEXCOORD1;
				half3 worldPos : TEXCOORD2;
				half3 worldNormal : TEXCOORD3;
			};

			half4 _Color;
			half _Alpha;
			sampler2D _MainTex;
			half _TexScalar;
			half2 _PanningSpeed;
			half _AlphaScale;

			float ease(float alpha)
			{
				// Ease Out Cubic
				// See https://easings.net/en
				return (--alpha) * alpha * alpha + 1;
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.objectPos = v.vertex;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal).xyz;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// Sum up weights to one
				float3 blend = abs(i.worldNormal);
				blend /= dot(blend, 1.f);

				// Move texture (we sample from different location)
				float2 offset = _PanningSpeed * _Time.x;

				fixed4 x = tex2D(_MainTex, (i.worldPos.yz + offset) * _TexScalar);
				fixed4 y = tex2D(_MainTex, (i.worldPos.xz + offset) * _TexScalar);
				fixed4 z = tex2D(_MainTex, (i.worldPos.xy + offset) * _TexScalar);

				// How close pixel is to center of outer border (we assume model being used 
				// is a cylinder facing upwards that vertices extend out to -0.5 and 0.5)
				fixed centerRatio = 1 - (abs(i.objectPos.y) + 0.5);

				fixed4 col = _Color * (x * blend.x + y * blend.y + z * blend.z);
				col.a = _Alpha * _AlphaScale * ease(centerRatio);

				return col;
			}

			ENDCG
		}	
	}
	Fallback "Diffuse"
}
