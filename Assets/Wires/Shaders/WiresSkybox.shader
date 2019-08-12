Shader "Wires/Skybox"
{
	// Mimics on Unity's default Cubemap Skybox shader.
	// We just needed to add some additional functionality to it

    Properties
    {
        _Color("Color", Color) = (1, 0, 0, 0)
		_Intensity("Intensity", Float) = 1
		_Reach("Reach", Float) = 0.5
		_DropScale("Drop Scale", Float) = 0.5

		_Cubemap("Cubemap", Cube) = "white" {}
		_Speed("Rotation Speed", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" }

		Cull Off
		Lighting Off
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
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {   
                float4 vertex : SV_POSITION;
				float3 uv : TEXCOORD0;
            };

			half4 _Color;
			half _Intensity;
			half _Reach;
			half _DropScale;

			samplerCUBE _Cubemap;
			half _Speed;

			float3 rotateVertex(float3 vertex, float rad)
			{
				float sina = sin(rad);
				float cosa = cos(rad);

				// Rotatin matrix around Z axis
				float2x2 rotationMat = float2x2(cosa, -sina, sina, cosa);

				return float3(mul(rotationMat, vertex.xz), vertex.y).xzy;
			}

			half ease(half t)
			{
				// OutCubic easing function
				// See https://easings.net/en
				return (--t) * t * t + 1;
			}

            v2f vert (appdata v)
            {
				float3 vertex = rotateVertex(v.vertex, _Time.x * _Speed);

                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
				o.uv = normalize(v.uv);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				half d = dot(i.uv, float3(0, 1, 0));
				d += _Reach;
				d *= _DropScale;

				half a = ease(d);

				// Tint of skybox (tint gradually disappears
				half3 tint = (a * _Color.rgb) * _Intensity;

				// Color of skybox
				half4 tex = texCUBE(_Cubemap, i.uv);
				half3 color = DecodeHDR(tex, fixed4(1, 1, 1, 1));

				return fixed4(color * tint, 1);
            }
            ENDCG
        }
    }
}
