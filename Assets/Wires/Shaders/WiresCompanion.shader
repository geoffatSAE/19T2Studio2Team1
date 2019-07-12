Shader "Wires/Companion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
		_Tint ("Tint", COLOR) = (1, 1, 1, 1)
		_MinAlpha ("Min Alpha", Range(0, 1)) = 0.2
		_Encapsulation ("Encapsulation", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 objectPos : TEXCOORD1;
            };

            sampler2D _MainTex;
			fixed4 _Tint;
			fixed _MinAlpha;
			fixed _Encapsulation;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = v.uv;
				o.objectPos = v.vertex;
			
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = tex2D(_MainTex, i.uv);

				// Direction from vertex world position to camera
				fixed3 posDir = normalize(WorldSpaceViewDir(fixed4(i.objectPos, 1)));

				// Cameras forward vector
				fixed3 camDir = UNITY_MATRIX_V[2].xyz;

				fixed d = dot(camDir, posDir);
				col.a = max(clamp(((1 - d) * 2) / _Encapsulation, 0, 1), _MinAlpha);

				return col *_Tint;
            }
            ENDCG
        }
    }
}
