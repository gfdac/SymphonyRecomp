#version 330 core

in vec2 vUv;
out vec4 oColor;

uniform sampler2D uTex;
uniform vec2 uTexSize;
uniform vec2 uOutputSize;
uniform float uTime;
uniform int uFrame;

uniform float MASK_INTENSITY;
uniform float GLOW_AMOUNT;
uniform float SATURATION;
uniform float GAMMA_IN;

#define PI 3.14159265358979323846

void main()
{
    vec4 baseColor = texture(uTex, vUv);

    // Subtle 4-tap blur for phosphor bloom/glow on highlights
    vec2 pixelOffset = 1.0 / uTexSize;
    vec3 glowSample = (
        texture(uTex, vUv + vec2(pixelOffset.x, 0.0)).rgb +
        texture(uTex, vUv - vec2(pixelOffset.x, 0.0)).rgb +
        texture(uTex, vUv + vec2(0.0, pixelOffset.y)).rgb +
        texture(uTex, vUv - vec2(0.0, pixelOffset.y)).rgb
    ) * 0.25;

    // Isolate bright pixels (luminance)
    float luma = dot(glowSample, vec3(0.299, 0.587, 0.114));
    vec3 bloom = glowSample * smoothstep(0.5, 1.0, luma) * GLOW_AMOUNT;

    vec3 color = baseColor.rgb + bloom;

    // Aperture Grille vertical RGB phosphor triad mask
    int screenPixelX = int(gl_FragCoord.x);
    int modX = screenPixelX % 3;
    vec3 mask = vec3(1.0 - MASK_INTENSITY);
    if (modX == 0) mask.r = 1.0;
    else if (modX == 1) mask.g = 1.0;
    else mask.b = 1.0;

    // Scanlines
    float scanline = sin(vUv.y * uTexSize.y * 2.0 * PI);
    float scanWeight = 1.0 - (0.25 * (1.0 - abs(scanline)));

    // Saturation adjustment
    float gray = dot(color, vec3(0.299, 0.587, 0.114));
    vec3 saturated = mix(vec3(gray), color, SATURATION);

    // Combine
    vec3 finalRgb = saturated * mask * scanWeight * 1.15;

    oColor = vec4(clamp(finalRgb, 0.0, 1.0), baseColor.a);
}
