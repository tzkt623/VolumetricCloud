Shader "Tezcat/WeatherNoiseView"
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
		Cull Off

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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _WeatherTex2D;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// sample the texture
				float3 noise = tex2D(_WeatherTex2D, i.uv).rgb;
				//noise *= noise;
				float4 col = float4(noise, 1.0f);
				return col;
			}
			ENDCG
		}
	}
}
