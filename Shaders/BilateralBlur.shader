Shader "Tezcat/BilateralBlur"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			//float _SpatialWeight;
			//float _TonalWeight;
			//float _BlurRadius;
			float3 _BlurParams;
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

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
#define PI 3.1415926
#define TAU (PI * 2.0)

			float gaussianWeight(float d, float sigma)
			{
				return 1.0 / (sigma * sqrt(TAU)) * exp(-(d * d) / (2.0 * sigma * sigma));
			}

			float4 gaussianWeight(float4 d, float sigma)
			{
				return 1.0 / (sigma * sqrt(TAU)) * exp(-(d * d) / (2.0 * sigma * sigma));
			}

			float4 bilateralWeight(float2 currentUV, float2 centerUV, float4 currentColor, float4 centerColor)
			{
				float spacialDifference = length(centerUV - currentUV);
				float4 tonalDifference = centerColor - currentColor;
				return gaussianWeight(spacialDifference, _BlurParams.x) * gaussianWeight(tonalDifference, _BlurParams.y);
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}


			float4 frag(v2f i) : SV_Target
			{
				float4 numerator = float4(0, 0, 0, 0);
				float4 denominator = float4(0, 0, 0, 0);

				float4 centerColor = tex2D(_MainTex, i.uv);

				for (int iii = -1; iii <= 1; iii++)
				{
					for (int jjj = -1; jjj <= 1; jjj++)
					{
						float2 offset = float2(iii, jjj) * _BlurParams.z;

						float2 currentUV = i.uv + offset * _MainTex_TexelSize.xy;
						float4 currentColor = tex2D(_MainTex, currentUV);

						float4 weight = bilateralWeight(currentUV, i.uv, currentColor, centerColor);
						numerator += currentColor * weight;
						denominator += weight;
					}
				}

				return numerator / denominator;

				//return float4(1.0, 0.0, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
