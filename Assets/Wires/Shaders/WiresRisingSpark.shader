Shader "Wires/RisingSpark"
{
    Properties
    {
		_LowColor ("Low Color", Color) = (0.5, 0.5, 0.5, 1)
		_HighColor ("High Color", Color) = (1, 1, 1, 1)
        _Noise ("Noise", 2D) = "white" {}
		_PanningSpeed ("Panning Speed", Vector) = (10, 5, 0, 0)
		_Extent ("Extent", Float) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

		Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag     

			#pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {       
                float4 vertex : SV_POSITION;
				float4 grab : TEXCOORD1;
            };

			fixed4 _LowColor;
			fixed4 _HighColor;
			sampler2D _Noise;
			fixed4 _PanningSpeed;
			fixed _Extent;

			// Global value (musics beat time)
			uniform float _BeatTime;

            v2f vert (appdata v)
            {
				UNITY_SETUP_INSTANCE_ID(v);

				fixed3 center = mul(unity_ObjectToWorld, fixed4(0, 0, 0, 1)).xyz;
				fixed3 vertex = mul(unity_ObjectToWorld, v.vertex).xyz;

				vertex += normalize(vertex - center) * (_Extent * _BeatTime);
				fixed4 object = mul(unity_WorldToObject, fixed4(vertex, 1.f));

                v2f o;
                o.vertex = UnityObjectToClipPos(object);
				o.grab = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed2 uv = i.grab.xy / i.grab.w;
				uv += _PanningSpeed * _Time.x;

				fixed3 noise = tex2D(_Noise, uv).rgb;
				fixed3 color = lerp(_LowColor.rgb, _HighColor.rgb, noise);

				return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
