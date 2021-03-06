﻿//
// Weather Maker for Unity
// (c) 2016 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 
// *** A NOTE ABOUT PIRACY ***
// 
// If you got this asset off of leak forums or any other horrible evil pirate site, please consider buying it from the Unity asset store at https://www.assetstore.unity3d.com/en/#!/content/60955?aid=1011lGnL. This asset is only legally available from the Unity Asset Store.
// 
// I'm a single indie dev supporting my family by spending hundreds and thousands of hours on this and other assets. It's very offensive, rude and just plain evil to steal when I (and many others) put so much hard work into the software.
// 
// Thank you.
//
// *** END NOTE ABOUT PIRACY ***
//

Shader "WeatherMaker/WeatherMakerWeatherMapShader"
{
	Properties
	{
		[Header(Cloud Coverage)]
		_CloudCoverageFrequency("Frequency", Range(0.1, 64.0)) = 6.0
		_CloudCoverageRotation("Rotation", Vector) = (0.0, 0.0, 0.0, 0.0)
		_CloudCoverageVelocity("Velocity", Vector) = (0.01, 0.01, 0.01)
		_CloudCoverageOffset("Offset", Vector) = (0.0, 0.0, 0.0)
		_CloudCoverageMultiplier("Multiplier", Range(0.0, 100.0)) = 1.0
		_CloudCoverageAdder("Adder", Range(-1.0, 1.0)) = 0.0
		_CloudCoveragePower("Power", Range(0.0, 16.0)) = 1.0
		_CloudCoverageProfileInfluence("Profile influence", Range(0.0, 1.0)) = 1.0
		[NoScaleOffset] _CloudCoverageTexture("Coverage Texture", 2D) = "black" {} // additional cloud coverage
		_CloudCoverageTextureMultiplier("Coverage texture multiplier", Range(0.0, 1.0)) = 0.0
		_CloudCoverageTextureScale("Coverage texture scale", Range(0.0, 1.0)) = 1.0

		[Header(Cloud Type)]
		_CloudTypeFrequency("Frequency", Range(0.1, 64.0)) = 6.0
		_CloudTypeRotation("Rotation", Vector) = (0.0, 0.0, 0.0, 0.0)
		_CloudTypeVelocity("Velocity", Vector) = (0.01, 0.01, 0.01)
		_CloudTypeOffset("Offset", Vector) = (0.0, 0.0, 0.0)
		_CloudTypeMultiplier("Multiplier", Range(0.0, 100.0)) = 1.0
		_CloudTypeAdder("Adder", Range(-1.0, 1.0)) = 0.0
		_CloudTypePower("Power", Range(0.0, 16.0)) = 1.0
		_CloudTypeProfileInfluence("Profile influence", Range(0.0, 1.0)) = 1.0
		_CloudTypeCoveragePower("Coverage Power", Range(0.0, 1.0)) = 0.3
		[NoScaleOffset] _CloudTypeTexture("Type Texture", 2D) = "black" {} // additional cloud type
		_CloudTypeTextureMultiplier("Type texture multiplier", Range(0.0, 1.0)) = 0.0
		_CloudTypeTextureScale("Type texture scale", Range(0.0, 1.0)) = 1.0

		[Header(Other)]
		[NoScaleOffset] _MainTex("Mask Texture", 2D) = "white" {} // mask texture
		_MaskVelocity("Mask Velocity", Vector) = (0.0, 0.0, 0.0, 0.0)
		_MaskOffset("Mask Offset", Vector) = (0.0, 0.0, 0.0, 0.0)
	}
	SubShader
	{
		Tags { }
		LOD 100
		Blend One Zero
		Fog { Mode Off }
		ZWrite On
		ZTest Always

		CGINCLUDE

		#pragma target 3.5
		#pragma exclude_renderers gles
		#pragma exclude_renderers d3d9
		
			
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

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			return o;
		}

		ENDCG

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "WeatherMakerCloudVolumetricShaderInclude.cginc"

#if defined(WEATHER_MAKER_ENABLE_VOLUMETRIC_CLOUDS)

			#include "WeatherMakerAdvancedNoiseShaderInclude.cginc"

			uniform float _WeatherMakerWeatherMapSeed;

			uniform float _CloudCoverageFrequency;
			uniform float2 _CloudCoverageRotation;
			uniform float3 _CloudCoverageVelocity;
			uniform float3 _CloudCoverageOffset;
			uniform float _CloudCoverageMultiplier;
			uniform float _CloudCoverageAdder;
			uniform float _CloudCoveragePower;
			uniform float _CloudCoverageProfileInfluence;
			uniform sampler2D _CloudCoverageTexture;
			uniform float _CloudCoverageTextureMultiplier;
			uniform float _CloudCoverageTextureScale;

			uniform float _CloudTypeFrequency;
			uniform float2 _CloudTypeRotation;
			uniform float3 _CloudTypeVelocity;
			uniform float3 _CloudTypeOffset;
			uniform float _CloudTypeMultiplier;
			uniform float _CloudTypeAdder;
			uniform float _CloudTypePower;
			uniform float _CloudTypeProfileInfluence;
			uniform float _CloudTypeCoveragePower;
			uniform sampler2D _CloudTypeTexture;
			uniform float _CloudTypeTextureMultiplier;
			uniform float _CloudTypeTextureScale;

			uniform float2 _MaskOffset;

			static const float cloudDensity = min(1.0, _CloudDensityVolumetric);
			static const float cloudCoverageInfluence = _CloudCoverageProfileInfluence * _CloudCoverVolumetric * cloudDensity;
			static const float cloudCoverageInfluence2 = (1.0 + (_CloudCoverageProfileInfluence * _CloudCoverVolumetric * cloudDensity)) * _CloudCoverageMultiplier * min(1.0, _CloudCoverVolumetric * cloudDensity * 2.0);
			static const float3 cloudCoverageVelocity = (_CloudCoverageOffset + float3(0.0, 0.0, _WeatherMakerWeatherMapSeed) + _CloudCoverageVelocity);
            static const bool cloudCoverageIsMin = (cloudCoverageInfluence < 0.01 && _CloudCoverageAdder <= 0.0);
            static const bool cloudCoverageIsMax = (cloudCoverageInfluence > 0.999 && _CloudCoverageAdder >= 0.0);
			static const float cloudCoverageTextureMultiplier = (_CloudCoverSecondaryVolumetric + _CloudCoverageTextureMultiplier);
			static const float cloudCoverageTextureScale = (4.0 / max(_CloudCoverageTextureScale, _CloudCoverageFrequency));

			static const float cloudTypeInfluence = _CloudTypeProfileInfluence * _CloudTypeVolumetric;
			static const float cloudTypeInfluence2 = (1.0 + (_CloudTypeProfileInfluence * _CloudTypeVolumetric)) * _CloudTypeMultiplier * _CloudTypeVolumetric;
			static const float3 cloudTypeVelocity = (_CloudTypeOffset + float3(0.0, 0.0, _WeatherMakerWeatherMapSeed) + _CloudTypeVelocity);
            static const bool cloudTypeIsMin = (cloudTypeInfluence < 0.01 && _CloudTypeAdder <= 0.0);
            static const bool cloudTypeIsMax = (cloudTypeInfluence > 0.999 && _CloudTypeAdder >= 0.0);
			static const float cloudTypeTextureMultiplier = (_CloudTypeSecondaryVolumetric + _CloudTypeTextureMultiplier);
			static const float cloudTypeTextureScale = (4.0 / max(_CloudTypeTextureScale, _CloudTypeFrequency));

			static const float2 weatherMapCameraPos = _CloudCameraPosition.xz * _WeatherMakerWeatherMapScale;

			inline float3 GetSamplePos(float2 uv, float3 velocity, float freq, float2 rotation)
			{
				uv -= 0.5;
				uv = RotateUV(uv, rotation.x, rotation.y);
				float2 xyPos = uv + RotateUV(weatherMapCameraPos, rotation.x, rotation.y);
				return velocity + float3(xyPos * freq, 0.0);
			}

			inline float SampleTexture(sampler2D samp, float2 uv, float2 rotation, float scale, float multiplier)
			{
				UNITY_BRANCH
				if (scale == 0.0f || multiplier <= 0.0f)
				{
					return 0.0f;
				}
				else
				{
					uv = RotateUV(uv, rotation.x, rotation.y) * scale;
					return tex2Dlod(samp, float4(uv, 0.0, 0.0)).a * multiplier;
				}
			}

#endif

			
			fixed4 frag (v2f i) : SV_Target
			{
         
#if defined(WEATHER_MAKER_ENABLE_VOLUMETRIC_CLOUDS)

				float3 samp;
				float value;
				fixed cloudCoverage;
				fixed cloudType;

				samp = GetSamplePos(i.uv, cloudCoverageVelocity, _CloudCoverageFrequency, _CloudCoverageRotation);

				UNITY_BRANCH
                if (cloudCoverageIsMin)
                {
                    value = 0.0;
                }
				else if (cloudCoverageIsMax)
                {
                    value = 1.0;
                }
				else
				{
					float value2 = SampleTexture(_CloudCoverageTexture, i.uv, _CloudCoverageRotation, cloudCoverageTextureScale, cloudCoverageTextureMultiplier);
					value = saturate(((Noise_Simplex_Octave_5(samp, cloudCoverageInfluence + value2) + _CloudCoverageAdder) * cloudCoverageInfluence2));
				}
				cloudCoverage = pow(value, _CloudCoveragePower);

				samp = GetSamplePos(i.uv, cloudTypeVelocity, _CloudTypeFrequency, _CloudTypeRotation);
                UNITY_BRANCH
                if (cloudTypeIsMin)
                {
                    value = 0.0;
                }
                else if (cloudTypeIsMax)
                {
                    value = 1.0;
                }
                else
                {
					float value2 = SampleTexture(_CloudTypeTexture, i.uv, _CloudTypeRotation, cloudTypeTextureScale, cloudTypeTextureMultiplier);
				    value = saturate(((Noise_Simplex_Octave_5(samp, cloudTypeInfluence + value2) + _CloudTypeAdder) * cloudTypeInfluence2));
                }
				cloudType = pow(cloudCoverage, _CloudTypeCoveragePower) * pow(value, _CloudTypePower);

				fixed mask = tex2Dlod(_MainTex, float4(i.uv.xy + _MaskOffset, 0.0, 0.0)).a;

				// r = cloud coverage
				// g = precipitation (unused currently, 0)
				// b = cloud type
				// a = unused (1)
				return fixed4(cloudCoverage * mask * (cloudCoverage > MINIMUM_COVERAGE_FOR_CLOUD), 0.0, cloudType * mask, 1.0);

#else

				return fixed4Zero;

#endif

			}

			ENDCG
		}
	}

	Fallback Off
}
