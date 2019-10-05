Shader "Wires/Tunnel"
{
    Properties
    {
		_Color ("Color", Color) = (1, 1, 1, 1)
		_Alpha ("Alpha", Range(0, 1)) = 0.2
        _Tex1 ("Tex 1", 2D) = "white" {}
		_Tex2 ("Tex 2", 2D) = "white" {}
		_TexScalar ("Texture Scalar", Float) = 1
		_PanningSpeed ("Panning Speed (X, Y)", Vector) = (1, 0, 0, 0)
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

			half4 _Color; // _Color.a contains interpolation between Tex 1 and Tex 2
			half _Alpha;
			sampler2D _Tex1;
			sampler2D _Tex2;
			half _TexScalar;
			half2 _PanningSpeed;

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

				// Draw behind everything
				o.vertex.z = 0.f;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// Sum up weights to one
				float3 blend = abs(i.worldNormal);
				blend /= dot(blend, 1.f);

				// Move texture (we sample from different location)
				float2 offset = _PanningSpeed * _Time.x;

				fixed4 x1 = tex2D(_Tex1, (i.worldPos.yz + offset) * _TexScalar);
				fixed4 y1 = tex2D(_Tex1, (i.worldPos.xz + offset) * _TexScalar);
				fixed4 z1 = tex2D(_Tex1, (i.worldPos.xy + offset) * _TexScalar);

				fixed4 x2 = tex2D(_Tex2, (i.worldPos.yz + offset) * _TexScalar);
				fixed4 y2 = tex2D(_Tex2, (i.worldPos.xz + offset) * _TexScalar);
				fixed4 z2 = tex2D(_Tex2, (i.worldPos.xy + offset) * _TexScalar);

				fixed4 x = lerp(x1, x2, _Color.a);
				fixed4 y = lerp(y1, y2, _Color.a);
				fixed4 z = lerp(z1, z2, _Color.a);

				// How close pixel is to center of plane
				fixed centerRatio = 1.f - abs(pow((i.uv.y * 2.f) - 1.f, 2.f));

				fixed4 col = _Color * (x * blend.x + y * blend.y + z * blend.z);
				col.a = _Alpha * ease(centerRatio);

				return col;
			}

			ENDCG
		}	
	}
	Fallback "Diffuse"
}
