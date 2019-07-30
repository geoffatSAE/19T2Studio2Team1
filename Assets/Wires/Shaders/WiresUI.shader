Shader "Wires/UI"
{
	// Mimics on Unity's default UI shader.
	// We just needed to add some additional functionality to it

    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1, 1, 1, 1)
		_MinAlpha("Min Alpha", Range(0, 1)) = 0.2
		_Encapsulation("Encapsulation", Range(0, 1)) = 0.2

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float2 uv  : TEXCOORD0;
				float4 world : TEXCOORD1;
				
				UNITY_VERTEX_OUTPUT_STEREO
            };

			sampler2D _MainTex;
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			float4 _MainTex_ST;
			fixed _Encapsulation;
			fixed _MinAlpha;

			// Global values (controller position and forward vector)
			uniform fixed3 _WorldSpaceControllerPos;
			uniform fixed3 _WorldSpaceControllerDir;

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.world = v.vertex;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color * _Color;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;

				// Direction from controller position to vertex in world space
				fixed3 posDir = normalize(_WorldSpaceControllerPos - mul(unity_ObjectToWorld, i.world).xyz);

				// We scale alpha so when controller is facing object, it becomes more transparent
				fixed d = dot(posDir, -_WorldSpaceControllerDir);
				color.a *= max(clamp(((1 - d) * 2) / _Encapsulation, 0, 1), _MinAlpha);
				
				#ifdef UNITY_UI_CLIP_RECT
				color.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
				#endif
                
				#ifdef UNITY_UI_ALPHACLIP
				clip(color.a - 0.001)
				#endif 

                return color;
            }
            ENDCG
        }
    }
}
