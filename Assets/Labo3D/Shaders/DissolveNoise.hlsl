#ifndef LABO3D_DISSOLVE_NOISE_INCLUDED
#define LABO3D_DISSOLVE_NOISE_INCLUDED

// Bruit de valeur en [0, 1]. Il est calculé dans le shader pour que l'exemple
// n'ait pas besoin d'une texture de bruit.
float Labo3D_Hash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

float Labo3D_Noise(float3 p)
{
    float3 cell = floor(p);
    float3 blend = frac(p);
    blend = blend * blend * (3.0 - 2.0 * blend);

    float n000 = Labo3D_Hash(cell);
    float n100 = Labo3D_Hash(cell + float3(1, 0, 0));
    float n010 = Labo3D_Hash(cell + float3(0, 1, 0));
    float n110 = Labo3D_Hash(cell + float3(1, 1, 0));
    float n001 = Labo3D_Hash(cell + float3(0, 0, 1));
    float n101 = Labo3D_Hash(cell + float3(1, 0, 1));
    float n011 = Labo3D_Hash(cell + float3(0, 1, 1));
    float n111 = Labo3D_Hash(cell + float3(1, 1, 1));

    float x00 = lerp(n000, n100, blend.x);
    float x10 = lerp(n010, n110, blend.x);
    float x01 = lerp(n001, n101, blend.x);
    float x11 = lerp(n011, n111, blend.x);
    return lerp(lerp(x00, x10, blend.y), lerp(x01, x11, blend.y), blend.z);
}

#endif
