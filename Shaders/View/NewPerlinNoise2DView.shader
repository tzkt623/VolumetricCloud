Shader "Tezcat/NewPerlinNoise2DView"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		ZWrite On
		ZTest LEqual

		Pass
		{
			CGPROGRAM
			#pragma enable_d3d11_debug_symbols
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _PerlinNoiseTex2D;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.vertex.xy + 0.5f;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// sample the texture
				float noise = tex2D(_PerlinNoiseTex2D, i.uv).r;
				//noise *= noise;
				float4 col = float4(noise, noise, noise, 1.0f);
				return col;
			}
			ENDCG
		}
	}
}
