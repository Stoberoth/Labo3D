#ifndef LABO3D_DISSOLVE_INPUT_INCLUDED
#define LABO3D_DISSOLVE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "DissolveNoise.hlsl"

// Même bloc dans chaque pass : c'est ce que le SRP Batcher exige.
// L'ordre et les types suivent le bloc Properties du shader.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float _Dissolve;
    float _EdgeWidth;
    float4 _EdgeColor;
    float _NoiseScale;
CBUFFER_END

// 0 garde tous les pixels, 1 les retire tous. Le liseré est la bande
// de bruit juste au-dessus du seuil.
float Labo3D_DissolveCut(float3 positionOS)
{
    float noise = Labo3D_Noise(positionOS * _NoiseScale);
    return noise + 0.001 - _Dissolve * 1.01;
}

#endif
