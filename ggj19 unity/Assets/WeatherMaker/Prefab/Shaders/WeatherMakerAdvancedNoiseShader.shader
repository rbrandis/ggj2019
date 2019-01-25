//
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

Shader "WeatherMaker/WeatherMakerAdvancedNoiseShader"
{
	Properties
	{
		_Type("Type", Int) = 1

		[Header(Tiling)]
		_Seamless_Tiling("Seamless Tiling", Int) = 1
		_Frame("Frame", Float) = 0.0
		_EndFrame("End Frame", Float) = 1.0

		[Header(Worley Noise)]
		_Worley_Octaves("Octaves", Range(1, 64)) = 4.0
		_Worley_Frequency("Frequency", Range(0.0, 256.0)) = 16.0
		_Worley_Lacunarity("Lacunarity", Range(0.0, 64.0)) = 2.0
		_Worley_Start_Weight("Start Weight", Range(0.0, 1.0)) = 07
		_Worley_Decay("Decay", Range(0.0, 1.0)) = 0.45
		_Worley_Amp("Amp", Range(0.0, 1.0)) = 0.0
		_Worley_Power("Power", Range(0.0, 256.0)) = 1.5
		_Worley_Inverter("Inverter", Range(0, 1)) = 1

		[Header(Perlin Noise)]
		_Perlin_Octaves("Octaves", Range(1, 10)) = 6.0
		_Perlin_Frequency("Frequency", Range(0.0, 256.0)) = 4.0
		_Perlin_Lacunarity("Lacunarity", Range(0.0, 16.0)) = 2.0
		_Perlin_Start_Weight("Start Weight", Range(0.0, 1.0)) = 0.5
		_Perlin_Decay("Decay", Range(0.0, 1.0)) = 0.5
		_Perlin_Amp("Amp", Range(0.0, 1.0)) = 0.0
		_Perlin_Power("Power", Range(0.0, 256.0)) = 2.0

		[Header(Combined)]
		_Worley_Perlin_Factor("Worley/Perlin Factor", Range(0, 1)) = 0.4
		_Worley_Perlin_Factor2("Worley/Perlin Factor2", Range(0, 1)) = 0.6
	}

	Subshader
	{
		CGINCLUDE

		#pragma target 3.5
		#pragma exclude_renderers gles
		#pragma exclude_renderers d3d9
		

		ENDCG

		Blend Off

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "WeatherMakerAdvancedNoiseShaderInclude.cginc"

			uniform int _Type;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			// note this takes a LONG time to compile, only enable if generating noise, put back to #if 0 when done
#if 0

			inline float MixPerlinWorley(float p, float w, float x)
			{
				static const float curve = 0.75;
				static const float invCurve = 1.334;

				if (x < 0.5)
				{
					x = x / 0.5;
					float n = p + w * x;
					return n * lerp(1, 0.5, pow(x, curve));
				}
				else
				{
					x = (x - 0.5) / 0.5;
					float n = w + p * (1.0 - x);
					float z = pow(saturate(x), invCurve);
					return n * lerp(0.5, 1.0, z);
				}
			}

			inline float Noise_Perlin_Worley(float3 coord)
			{
				float perlinNoise = Noise_Perlin_Octave(coord, 7, 6);
				float worleyNoise = Noise_Worley_Octave(coord, 3, 8);
				float perlinWorleyNoise = MixPerlinWorley(perlinNoise, worleyNoise, _Worley_Perlin_Factor);
				//float perlinWorleyNoise = saturate(Remap(perlinNoise, 0.0, 1.0, 0.0, worleyNoise));
				//float perlinWorleyNoise = saturate(Remap(perlinNoise, 0.0, 1.0, worleyNoise, 1.0));
				return perlinWorleyNoise;
			}

			inline float Noise_Worley_Shape_G(float3 coord)
			{
				return Noise_Worley_Octave(coord, 2, 6);
			}

			inline float Noise_Worley_Shape_B(float3 coord)
			{
				return Noise_Worley_Octave(coord, 2, 12);
			}

			inline float Noise_Worley_Shape_A(float3 coord)
			{
				return Noise_Worley_Octave(coord, 2, 24);
				//float perlinNoise = Noise_Perlin_Octave(coord, 6, 12);
				//float worleyNoise = Noise_Worley_Octave(coord, 3, 16);
				//float perlinWorleyNoise = MixPerlinWorley(perlinNoise, worleyNoise, _Worley_Perlin_Factor2);
				//return perlinWorleyNoise;
			}

			float4 Noise_Perlin_Worley_FBM(float3 coord)
			{
				// worley: start weight = 0.575, amp = 0.1, power = 0.7
				// perlin: start weight = 0.527, power = 2
				// https://github.com/sebh/TileableVolumeNoise/blob/master/main.cpp
				float perlinWorleyNoise = Noise_Perlin_Worley(coord);
				float worleyNoise2 = Noise_Worley_Shape_G(coord);
				float worleyNoise3 = Noise_Worley_Shape_B(coord);
				float worleyNoise4 = Noise_Worley_Shape_A(coord);
				//float lowFreqFBM = (worleyNoise2 * 0.625) + (worleyNoise3 * 0.25) + (worleyNoise4 * 0.125);
				//return saturate(Remap(perlinWorleyNoise, -(1.0 - lowFreqFBM), 1.0, 0.0, 1.0));
				return float4(perlinWorleyNoise, worleyNoise2, worleyNoise3, worleyNoise4);
			}

			float Noise_Worley_Details(float3 coord)
			{
				float worleyNoise1 = Noise_Worley_Octave(coord, 3, 4);
				float worleyNoise2 = Noise_Worley_Octave(coord, 3, 8);
				float worleyNoise3 = Noise_Worley_Octave(coord, 2, 16);
				float worleyNoise4 = Noise_Worley_Octave(coord, 2, 32);
				return (worleyNoise1 * 0.6) + (worleyNoise2 * 0.2) + (worleyNoise3 * 0.125) + (worleyNoise4 * 0.075);
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 h = float4Zero;
				float3 pos = float3(i.uv, _Frame);

				UNITY_BRANCH
				switch (_Type)
				{
					case 2:
						h = Noise_Perlin_Octave(pos, _Perlin_Octaves, _Perlin_Frequency);
						h = float4(h.xxx, 1.0);
						break;

					case 3:
						h = Noise_Perlin_Worley_FBM(pos);
						break;

					case 4:
						h = Noise_Worley_Details(pos);
						h = float4(h.xxx, 1.0);
						break;

					case 5:
						h = Noise_Simplex_Worley_Octave_5(pos, 0.0, 0.0, 1.0, 4.0, 12.0);
						h = float4(h.xxx, 1.0);
						break;

					case 6:
						h = Noise_Perlin_Worley(pos);
						h = float4(h.xxx, 1.0);
						break;

					case 7:
						h = Noise_Worley_Shape_G(pos);
						h = float4(h.xxx, 1.0);
						break;

					case 8:
						h = Noise_Worley_Shape_B(pos);
						h = float4(h.xxx, 1.0);
						break;

					case 9:
						h = Noise_Worley_Shape_A(pos);
						h = float4(h.xxx, 1.0);
						break;

					default:
						h = Noise_Worley_Octave(float3(i.uv, _Frame), _Worley_Octaves, _Worley_Frequency);
						h.a = 1.0;
						break;
				}
				return h;
			}

#else

			float4 frag(v2f i) : SV_Target
			{
				return float4Zero;
			}

#endif

			ENDCG
		}
	}
}
