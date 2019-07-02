Shader "Wires/Skybox"
{
    Properties
    {
        _Color("Color", Color) = (1, 0, 0, 0)
		_Intensity("Intensity", Float) = 1
		_Reach("Reach", Float) = 0.5
		_DropScale("Drop Scale", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" }
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
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			half4 _Color;
			half _Intensity;
			half _Reach;
			half _DropScale;

			half ease(half t)
			{
				// OutCubic easing function
				// See https://easings.net/en
				return (--t) * t * t + 1;
			}

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				// If uv is for upper area of skybox
				half d = dot(normalize(i.uv), float3(0, 1, 0));

				// Half of sphere will be under 0, this will help the fade reach higher
				d += _Reach;

				// Scale the by amount (this could incease/decrease fade)
				d *= _DropScale;

				// Ease out alpha smoothly
				half a = ease(d);

				// Calculate color with additional intensity
				return (a * _Color) * _Intensity;
            }
            ENDCG
        }
    }
}
