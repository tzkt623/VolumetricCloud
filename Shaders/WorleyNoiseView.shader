Shader "Tezcat/WorleyNoiseView"
{
	Properties
	{
		_Mix("Mix", Float) = 0
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
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Function.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 uv3 : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 uv3 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex2D;
			sampler3D _ShapeTex3D;
			sampler3D _DetailTex3D;

			int _Dimension;
			int _Channel;
			float _Mix;

			float4 _MainTex2D_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex2D);
				o.uv3 = v.vertex.xyz + 0.5f;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// sample the texture
				float4 col = 1;
				if (_Dimension == 2)
				{
					col = tex2D(_MainTex2D, i.uv);
				}
				else if (_Dimension == 3)
				{
					//mipmap有bug
					float4 noise = tex3Dlod(_ShapeTex3D, float4(i.uv3, 0));

					switch (_Channel)
					{
					case 0:
						col.rgb = noise.rrr;
						break;
					case 1:
						col.rgb = noise.ggg;
						break;
					case 2:
						col.rgb = noise.bbb;
						break;
					case 3:
						col.rgb = noise.aaa;
						break;
					case 4:
						col.rgb = noise.gba;
						break;
					case 5:
						col.rgb = dot(noise.gba, float3(0.625, 0.25, 0.125));
						break;
					case 6:
						col.rgb = noise.r;
						break;
					case 7:
					{
						float worley_fbm = dot(noise.gba, float3(0.625, 0.25, 0.125));
						col.rgb = remap(noise.r, worley_fbm - 1, 1.0, 0.0, 1.0);
						break;
					}
					case 8:
					{
						float worley_fbm = dot(noise.gba, float3(0.625, 0.25, 0.125));
						float shape = remap(noise.r, worley_fbm - 1, 1.0, 0.0, 1.0);

						float4 detail = tex3Dlod(_DetailTex3D, float4(i.uv3, 0));
						float detail_fbm = dot(detail.rgb, float3(0.625, 0.25, 0.125));
						float modifier = mix(detail_fbm, 1 - detail_fbm, _Mix);
						col.rgb = remap(shape, (1 - detail_fbm) * i.uv3.y, 1.0, 0.0, 1.0);
						//col.rgb = 1 - detail_fbm;
						break;
					}
					default:
						col.rgb = float3(1, 0, 1);
						break;
					}
				}

				return col;
			}
			ENDCG
		}
	}
}
