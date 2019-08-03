Shader "Wires/Laser"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1 ,1)
		_Alpha ("Alpha", Range(0, 1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

		Lighting Off
		ZWrite On
		Blend SrcAlpha OneMinusSrcAlpha

		// TODO: Stencil Ref should be a property
		Stencil
		{
			Ref 128
			ReadMask 255
			WriteMask 128
			Comp NotEqual
			Pass Replace
		    Fail Keep
		}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
			fixed _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {             
				return fixed4(_Color.rgb, _Alpha);
            }
            ENDCG
        }
    }
}
