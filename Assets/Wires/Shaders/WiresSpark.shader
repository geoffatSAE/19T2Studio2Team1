Shader "Wires/Spark"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
		_Speed ("Speed", Range(-10, 10)) = -5
        _Frequency ("Frequency", Range(0, 1000)) = 10
		_Amplitude ("Amplitude", Range(0, 5)) = 0.1
		_FallOff("Fall Off", Range(1, 8)) = 4
		_Distortion ("Distortion", Range(0.1, 2)) = 1
		_InfluenceSpeed ("Influence Speed", Range(0, 10)) = 0.5
		_InfluenceVector ("Influence Vector", Vector) = (5, 0, 0, 0)		
    }
    SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			fixed4 _Color;
			fixed _Speed;
			fixed _Frequency;
			fixed _Amplitude;
			fixed _FallOff;
			fixed _Distortion;
			fixed _InfluenceSpeed;
			fixed4 _InfluenceVector;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				// Models world position
				fixed4 center = unity_WorldToObject[3];
				fixed4 worldInf = center + _InfluenceVector;

				fixed4 pos = mul(unity_ObjectToWorld, v.vertex);
				fixed4 dir = normalize(center - _InfluenceVector);
				fixed4 origin = _InfluenceVector + dir;// *(_Time.x * _InfluenceSpeed);
				
				fixed dis = distance(pos, origin);
				dis = pow(dis, _FallOff);
				dis = max(dis, _Distortion);

				
				fixed4 worldDir = mul(unity_ObjectToWorld, dir);

				fixed axis = worldInf + dot((v.vertex - worldInf), worldDir);

				fixed3 objectPos = v.vertex.xyz + v.normal * sin((axis * _Frequency) /*+ (_Time.x * _Speed)*/) * _Amplitude * (1 / dis);

				v2f o;
				o.vertex = UnityObjectToClipPos(fixed4(objectPos, 1));
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col;
				col.rgb = _Color.rgb;
				col.a = 1.f;
				return col;
			}
			ENDCG
		}
    }
}
