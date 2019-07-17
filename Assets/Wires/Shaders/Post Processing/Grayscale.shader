Shader "Wires/PostProcess/Grayscale"
{
	SubShader
	{
	Pass
	{
		Stencil
			{
				Ref 2
				Comp NotEqual
			}

	CGPROGRAM
		 #pragma vertex vert
		 #pragma fragment frag
		 struct appdata {
			 float4 vertex : POSITION;
		 };
		 struct v2f {
			 float4 pos : SV_POSITION;
		 };
		 v2f vert(appdata v) {
			 v2f o;
			 o.pos = UnityObjectToClipPos(v.vertex);
			 return o;
		 }
		 half4 frag(v2f i) : COLOR{
			 float4 color = float4(1, 0, 0, 1);
				return color;
		 }
		 ENDCG
	}
	}
}
