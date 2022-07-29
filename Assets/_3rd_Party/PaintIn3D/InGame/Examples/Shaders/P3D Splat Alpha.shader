Shader "Paint in 3D/P3D Splat Alpha"
{
	Properties
	{
		[NoScaleOffset]_MainTex("Albedo (RGB) Alpha (A)", 2D) = "white" {}
		_MainTex_Transform("Scale (XY) Offset (ZW)", Vector) = (1.0, 1.0, 0.0, 0.0)
		[NoScaleOffset][Normal]_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpMap_Transform("Scale (XY) Offset (ZW)", Vector) = (1.0, 1.0, 0.0, 0.0)
		[NoScaleOffset]_MetallicGlossMap("Metallic (R) Ambient Occlusion (G) Smoothness(B)", 2D) = "white" {}
		_MetallicGlossMap_Transform("Scale (XY) Offset (ZW)", Vector) = (1.0, 1.0, 0.0, 0.0)
		[NoScaleOffset]_ParallaxMap("Height Map (A)", 2D) = "black" {}
		_ParallaxMap_Transform("Scale (XY) Offset (ZW)", Vector) = (1.0, 1.0, 0.0, 0.0)

		_Color("Color", Color) = (1,1,1,1)
		_Channel("Channel (UV0, UV1, UV2, UV3)", Vector) = (1.0, 0.0, 0.0, 0.0)
		_BumpScale("Normal Map Strength", Range(0,5)) = 1
		_Metallic("Metallic", Range(0,1)) = 0
		_GlossMapScale("Smoothness", Range(0,1)) = 1
		_Parallax("Parallax", Range(0,0.1)) = 0.0
		_Offset("Offset", Float) = 0.0

		[NoScaleOffset]_SplatMap("Splat (R)", 2D) = "gray" {}
		//_SplatDetail("Splat Detail (A)", 2D) = "black" {}
		_SplatChannel("Splat Channel (UV0, UV1, UV2, UV3)", Vector) = (1.0, 0.0, 0.0, 0.0)
		_SplatOffset("Splat Offset", Range(-1,1)) = 0.0
		_SplatScale("Splat Scale", Range(0.01,2)) = 1.0
		_SplatContrast("Splat Contrast", Range(1,100)) = 2.4
		//_SplatDetailStrength("Splat Detail Strength", Range(0, 5)) = 1.66

		[HideInInspector]_BaseChannel("Base Channel (UV0, UV1, UV2, UV3)", Vector) = (1.0, 0.0, 0.0, 0.0)
		[HideInInspector]_BaseParallaxMap("Base Height Map (A)", 2D) = "black" {}
		[HideInInspector]_BaseParallaxMap_Transform("Scale (XY) Offset (ZW)", Vector) = (1.0, 1.0, 0.0, 0.0)
	}
	SubShader
	{
		Blend SrcAlpha OneMinusSrcAlpha
		Tags{ "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 400
		CGPROGRAM
			#pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade
			#pragma target 3.0
			#include "UnityCG.cginc"
			#include "UnityPBSLighting.cginc"

			#define P3D_DECLARE(NAME) sampler2D NAME; float4 NAME##_Transform
			#define P3D_SAMPLE(NAME, UV) tex2D( NAME , ( (UV) * NAME##_Transform.xy + NAME##_Transform.zw ) )
			#define P3D_SAMPLE_PARALLAX(NAME, UV, SHIFT) P3D_SAMPLE( NAME , (UV + SHIFT / NAME##_Transform.xy) )

			P3D_DECLARE(_MainTex);
			P3D_DECLARE(_BumpMap);
			P3D_DECLARE(_MetallicGlossMap);
			P3D_DECLARE(_ParallaxMap);

			float4 _Color;
			float4 _Channel;
			float  _BumpScale;
			float  _Metallic;
			float  _GlossMapScale;
			float  _Parallax;
			float  _Offset;

			sampler2D _SplatMap;
			float4    _SplatChannel;
			float     _SplatOffset;
			float     _SplatScale;
			float     _SplatContrast;

			P3D_DECLARE(_BaseParallaxMap);
			
			float4 _BaseChannel;

			struct Input
			{
				float2 coord;
				float2 splatCoord;
				float2 baseCoord;
				float3 viewDir;
			};

			void vert(inout appdata_full v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input, o);

				o.coord = v.texcoord.xy * _Channel.x + v.texcoord1.xy * _Channel.y + v.texcoord2.xy * _Channel.z + v.texcoord3.xy * _Channel.w;

				o.splatCoord = v.texcoord.xy * _SplatChannel.x + v.texcoord1.xy * _SplatChannel.y + v.texcoord2.xy * _SplatChannel.z + v.texcoord3.xy * _SplatChannel.w;

				o.baseCoord = v.texcoord.xy * _BaseChannel.x + v.texcoord1.xy * _BaseChannel.y + v.texcoord2.xy * _BaseChannel.z + v.texcoord3.xy * _BaseChannel.w;
			}

			void surf(Input i, inout SurfaceOutputStandard o)
			{
				float height     = P3D_SAMPLE(_ParallaxMap, i.coord).a;
				float baseHeight = P3D_SAMPLE(_BaseParallaxMap, i.baseCoord).a;

				float  shift   = ParallaxOffset(height, _Parallax, i.viewDir);
				float4 texMain = P3D_SAMPLE_PARALLAX(_MainTex, i.coord, shift);
				float4 gloss   = P3D_SAMPLE_PARALLAX(_MetallicGlossMap, i.coord, shift);
				float4 normal  = P3D_SAMPLE_PARALLAX(_BumpMap, i.coord, shift);
				float  splat   = tex2D(_SplatMap, i.splatCoord).r;

				height = (height + _SplatOffset) * splat * _SplatScale;

				o.Albedo     = texMain.rgb * _Color.rgb;
				o.Normal     = UnpackScaleNormal(normal, _BumpScale);
				o.Metallic   = gloss.r * _Metallic;
				o.Occlusion  = gloss.g;
				o.Smoothness = gloss.b * _GlossMapScale;
				o.Alpha      = saturate((height - baseHeight) * _SplatContrast);
			}
		ENDCG
	}
	FallBack "Standard"
}