Shader "Wires/Companion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
		_MinAlpha ("Min Alpha", Range(0, 1)) = 0.2
		_T("T", Float) = 0.3
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

		Blend SrcAlpha OneMinusSrcAlpha
		//ZWrite Off

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
				float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
			fixed _MinAlpha;
			fixed _T;

            v2f vert (appdata v)
            {
				fixed3 displacement = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
				fixed3 viewDir = WorldSpaceViewDir(v.vertex);

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = v.uv;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				
				fixed2 t = mul(UNITY_MATRIX_V, mul(unity_ObjectToWorld, v.vertex));
				fixed l = ceil(length(t) - _T);
				o.uv.z = max(l, _MinAlpha);
				

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = fixed4(1, 0, 0, i.uv.z);
                return col;
            }
            ENDCG
        }
    }
}
