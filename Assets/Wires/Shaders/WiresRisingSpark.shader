Shader "Wires/RisingSpark"
{
    Properties
    {
		_LowColor ("Low Color", Color) = (0.5, 0.5, 0.5, 1)
		_HighColor ("High Color", Color) = (1, 1, 1, 1)
        _Noise ("Noise", 2D) = "white" {}
		_PanningSpeed ("Panning Speed", Vector) = (10, 5, 0, 0)
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
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
