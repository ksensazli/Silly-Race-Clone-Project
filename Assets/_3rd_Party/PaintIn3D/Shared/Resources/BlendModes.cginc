sampler2D _Buffer;
float2    _BufferSize;

// REPLACE_ORIGINAL + REPLACE_CUSTOM
float4    _ReplaceColor;
sampler2D _ReplaceTexture;

float4 Blend(float4 current, float4 color, float strength, float2 coord)
{
#if BLEND_MODE_INDEX == 0 // ALPHA_BLEND
	color.a *= strength;
	float str = 1.0f - color.a;
	float div = color.a + current.a * str;

	current.rgb = (color.rgb * color.a + current.rgb * current.a * str) / div;
	current.a   = div;
#elif BLEND_MODE_INDEX == 1 // ALPHA_BLEND_INVERSE
	color.a *= strength;
	float str = 1.0f - current.a;
	float div = current.a + color.a * str;

	current.rgb = (current.rgb * current.a + color.rgb * color.a * str) / div;
	current.a   = div;
#elif BLEND_MODE_INDEX == 2 // ALPHA_BLEND_RGB
	color.a *= strength;
	float str = 1.0f - color.a;
	float div = color.a + current.a * str;

	current.rgb = (color.rgb * color.a + current.rgb * current.a * str) / div;
#elif BLEND_MODE_INDEX == 3 // ADDITIVE
	current += color * strength;
#elif BLEND_MODE_INDEX == 4 // ADDITIVE_SOFT
	current += color * strength * (1.0f - current);
#elif BLEND_MODE_INDEX == 5 // SUBTRACTIVE
	current -= color * strength;
#elif BLEND_MODE_INDEX == 6 // SUBTRACTIVE_SOFT
	current -= color * strength * current;
#elif BLEND_MODE_INDEX == 7 // REPLACE
	current += (color - current) * strength;
#elif BLEND_MODE_INDEX == 8 // REPLACE_ORIGINAL
	float4 rep = tex2D(_ReplaceTexture, coord) * _ReplaceColor;
	current += (rep - current) * strength;
#elif BLEND_MODE_INDEX == 9 // REPLACE_CUSTOM
	float4 rep = tex2D(_ReplaceTexture, coord) * _ReplaceColor;
	current += (rep - current) * strength;
#elif BLEND_MODE_INDEX == 10 // MULTIPLY_INVERSE_RGB
	//current.rgb *= 1.0f - (1.0f - color.rgb) * strength;
	color.rgb = lerp(color.rgb, float3(1, 1, 1), 1 - color.a * strength);
	current.rgb *= color.rgb;
#elif BLEND_MODE_INDEX == 11 // BLUR
	float2 k = 1.0f / _BufferSize;
	float4 a = tex2D(_Buffer, coord + float2(-k.x, 0.0f));
	float4 b = tex2D(_Buffer, coord + float2(+k.x, 0.0f));
	float4 c = tex2D(_Buffer, coord + float2(0.0f, -k.y));
	float4 d = tex2D(_Buffer, coord + float2(0.0f, +k.y));
	current += ((a + b + c + d) * 0.25f - current) * strength;
#endif
	return current;
}

float4 Blend(float4 color, float strength, float2 coord)
{
	float4 current = tex2D(_Buffer, coord);

	return Blend(current, color, strength, coord);
}

float4 BlendMinimum(float4 color, float strength, float2 coord, float4 step)
{
	float4 current = tex2D(_Buffer, coord);
	float4 result  = Blend(current, color, 1.0f, coord);
	float4 change  = result - current;
	float4 maximum = abs(change);

	return current + sign(change) * clamp(strength, step, maximum);
}