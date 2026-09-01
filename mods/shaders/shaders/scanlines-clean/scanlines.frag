#version 330 core

in vec2 vUv;
out vec4 oColor;

uniform sampler2D uTex;
uniform vec2 uTexSize;
uniform vec2 uOutputSize;
uniform float uTime;
uniform int uFrame;

uniform float SCANLINE_STRENGTH;
uniform float BRIGHT_BOOST;
uniform float BEAM_SHARPNESS;

#define PI 3.14159265358979323846

void main()
{
    vec4 color = texture(uTex, vUv);

    // Calculate vertical scanline wave based on texture height
    float scanlineCoord = vUv.y * uTexSize.y;
    float scanline = sin(scanlineCoord * 2.0 * PI);
    
    // Smooth cosine wave shaped by beam sharpness
    float scanWeight = 1.0 - (SCANLINE_STRENGTH * 0.5 * (1.0 - pow(abs(scanline), BEAM_SHARPNESS)));

    // Apply scanlines and brightness boost
    vec3 finalRgb = color.rgb * scanWeight * BRIGHT_BOOST;

    oColor = vec4(clamp(finalRgb, 0.0, 1.0), color.a);
}
