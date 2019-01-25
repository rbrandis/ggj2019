#ifndef _WEATHER_MAKER_SIMPLE_NOISE_SHADER_INCLUDE_
#define _WEATHER_MAKER_SIMPLE_NOISE_SHADER_INCLUDE_

//1/7
#define K 0.142857142857
//3/7
#define Ko 0.428571428571

float2 mod(float2 x, float y) { return x - y * floor(x / y); }
float3 mod(float3 x, float y) { return x - y * floor(x / y); }

// Permutation polynomial: (34x^2 + x) mod 289
float3 permutation(float3 x) { return mod((34.0 * x + 1.0) * x, 289.0); }

float mod289(float x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float4 perm289(float4 x) { return mod289(((x * 34.0) + 1.0) * x); }

float generic_noise_3d(float3 p)
{
	float3 a = floor(p);
	float3 d = p - a;
	d = d * d * (3.0 - 2.0 * d);

	float4 b = a.xxyy + float4(0.0, 1.0, 0.0, 1.0);
	float4 k1 = perm289(b.xyxy);
	float4 k2 = perm289(k1.xyxy + b.zzww);

	float4 c = k2 + a.zzzz;
	float4 k3 = perm289(c);
	float4 k4 = perm289(c + 1.0);

	float4 o1 = frac(k3 * (1.0 / 41.0));
	float4 o2 = frac(k4 * (1.0 / 41.0));

	float4 o3 = o2 * d.z + o1 * (1.0 - d.z);
	float2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);

	return o4.y * d.y + o4.x * (1.0 - d.y);
}

float simplex_hash(float n)
{
	return frac(sin(n) * 43758.5453);
}

float simplex_noise_3d(float3 x)
{
	// The noise function returns a value in the range 0 to 1

	float3 p = floor(x);
	float3 f = frac(x);

	f = f * f * (3.0 - 2.0 * f);
	float n = p.x + p.y * 57.0 + 113.0 * p.z;

	return lerp
	(
		lerp
		(
			lerp(simplex_hash(n + 0.0), simplex_hash(n + 1.0), f.x),
			lerp(simplex_hash(n + 57.0), simplex_hash(n + 58.0), f.x),
			f.y
		),
		lerp
		(
			lerp(simplex_hash(n + 113.0), simplex_hash(n + 114.0), f.x),
			lerp(simplex_hash(n + 170.0), simplex_hash(n + 171.0), f.x),
			f.y
		),
		f.z
	);
}

#endif // _WEATHER_MAKER_NOISE_SHADER_INCLUDE_