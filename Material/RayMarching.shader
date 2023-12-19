Shader "Tezcat/RayMarching"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

#define MAX_STEPS 100
#define MAX_DIST 100.0
#define SURF_DIST 0.001

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 ro : TEXCOORD1;
				float3 hitPoint : TEXCOORD2;
				float3 viewDir : TEXCOORD3;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _CameraDepthTexture;

			float cloudRayMarching(float3 startPoint, float3 direction)
			{
				float3 testPoint = startPoint;
				float sum = 0.0;
				direction *= 0.5;//每次步进间隔
				for (int i = 0; i < 256; i++)//步进总长度
				{
					testPoint += direction;
					if (testPoint.x < 10.0 && testPoint.x > -10.0 &&
						testPoint.z < 10.0 && testPoint.z > -10.0 &&
						testPoint.y < 10.0 && testPoint.y > -10.0)
						sum += 0.01;
				}
				return sum;
			}

			float getDist(float3 p)
			{
				float d = length(p) - 0.5f;

				return d;
			}

			float getDist(float3 p, float3 wPos)
			{
				float d = length(p - wPos);

				return d;
			}

			float3 getNormal(float3 p)
			{
				float2 e = float2(1e-2, 0);
				float3 n = getDist(p) - float3(
					getDist(p - e.xyy),
					getDist(p - e.yxy),
					getDist(p - e.yyx));

				return normalize(n);
			}

			float rayMarch(float3 ro, float3 rd)
			{
				float dO = 0;
				float dS;

				for (int i = 0; i < MAX_STEPS; i++)
				{
					float3 p = ro + dO * rd;
					dS = getDist(p);
					dO += dS;

					if (dS < SURF_DIST || dO > MAX_DIST)
					{
						break;
					}
				}

				return dO;
			}

			float rayMarch(float3 ro, float3 rd, float3 worldPosition)
			{
				float dO = 0;
				float dS;

				for (int i = 0; i < MAX_STEPS; i++)
				{
					float3 p = ro + dO * rd;
					dS = getDist(p, worldPosition);
					dO += dS;

					if (dS < SURF_DIST || dO > MAX_DIST)
					{
						break;
					}
				}

				return dO;
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//o.ro = _WorldSpaceCameraPos;
				o.ro = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0f));
				o.hitPoint = v.vertex;

				float4 screenPos = ComputeScreenPos(o.vertex);
				//float4 ndcPos = (screenPos / screenPos.w) * 2.0 - 1.0f;
				float4 ndcPos = float4(o.uv * 2.0 - 1.0f, 0, -1);
				float far = _ProjectionParams.z;
				float4 clipPos = float4(ndcPos.x * far, ndcPos.y * far, far, far);
				float3 viewPos = mul(unity_CameraInvProjection, clipPos).xyz;

				o.viewDir = mul(unity_CameraToWorld, float4(viewPos, 0.0f)).xyz;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 finalColor = 1.0f;
				finalColor.rgb = 1.0f - tex2D(_MainTex, i.uv).rgb;


				return finalColor;
			}
			ENDCG
		}
	}
}
