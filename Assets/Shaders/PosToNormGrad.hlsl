// PosToNormGrad.hlsl

void PosToNormGrad_float(float3 pos, float min, float max, out float3 normGrad)
{
    float x = saturate((pos.y - min) / (max - min + 1e-5));
    float t = 1 - exp(-1.50 * x);
    normGrad = float3(t, t, t);
}