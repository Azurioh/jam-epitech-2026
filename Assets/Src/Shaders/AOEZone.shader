Shader "Custom/AOEZone"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.3, 0, 1)
        _EdgeColor ("Edge Color", Color) = (1, 0.6, 0, 1)
        _Radius ("Radius", Range(0, 1)) = 1
        _EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseIntensity ("Pulse Intensity", Range(0, 0.5)) = 0.1
        _WaveCount ("Wave Count", Int) = 2
        _WaveSpeed ("Wave Speed", Float) = 1.5
        _WaveWidth ("Wave Width", Range(0, 0.1)) = 0.03
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.15
        _NoiseScale ("Noise Scale", Float) = 5
        _NoiseSpeed ("Noise Speed", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            float4 _Color;
            float4 _EdgeColor;
            float _Radius;
            float _EdgeWidth;
            float _PulseSpeed;
            float _PulseIntensity;
            int _WaveCount;
            float _WaveSpeed;
            float _WaveWidth;
            float _FillAlpha;
            float _NoiseScale;
            float _NoiseSpeed;

            // Simple hash noise
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Centre le UV autour de (0.5, 0.5) -> (-0.5, 0.5)
                float2 centeredUV = i.uv - 0.5;
                float dist = length(centeredUV) * 2.0; // 0 au centre, 1 au bord

                // Pulse du rayon
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseIntensity;
                float currentRadius = _Radius + pulse;

                // En dehors du cercle = transparent
                if (dist > currentRadius + _EdgeWidth)
                    discard;

                float alpha = 0;
                float3 col = _Color.rgb;

                // --- Remplissage intérieur avec noise ---
                float n = noise(centeredUV * _NoiseScale + _Time.y * _NoiseSpeed);
                float fillAlpha = _FillAlpha * (0.5 + 0.5 * n);

                // Gradient: plus intense vers le bord
                float gradientFactor = smoothstep(0, currentRadius, dist);
                fillAlpha *= (0.3 + 0.7 * gradientFactor);
                alpha = fillAlpha;

                // --- Bord principal ---
                float edgeDist = abs(dist - currentRadius);
                float edge = 1.0 - smoothstep(0, _EdgeWidth, edgeDist);
                col = lerp(col, _EdgeColor.rgb, edge);
                alpha = max(alpha, edge * _EdgeColor.a);

                // --- Ondes concentriques qui se propagent vers l'extérieur ---
                for (int w = 0; w < _WaveCount; w++)
                {
                    float waveOffset = (float)w / (float)_WaveCount;
                    float wavePos = frac(_Time.y * _WaveSpeed + waveOffset) * currentRadius;
                    float waveDist = abs(dist - wavePos);
                    float wave = 1.0 - smoothstep(0, _WaveWidth, waveDist);
                    // Fade out vers le bord
                    wave *= (1.0 - wavePos / currentRadius) * 0.6;
                    alpha = max(alpha, wave);
                    col = lerp(col, _EdgeColor.rgb, wave * 0.5);
                }

                // --- Fade doux au bord extérieur ---
                float outerFade = 1.0 - smoothstep(currentRadius - _EdgeWidth, currentRadius + _EdgeWidth, dist);
                alpha *= outerFade;

                return fixed4(col, alpha * _Color.a);
            }
            ENDCG
        }
    }
}
