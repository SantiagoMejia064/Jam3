Shader "UI/CORE_CinematicaFrameFX"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _UnscaledTime ("Unscaled Time", Float) = 0
        _GrainIntensity ("Film Grain", Range(0, 0.4)) = 0.065
        _ChromaticOffset ("Chromatic Aberration", Range(0, 0.05)) = 0.0028
        _ScanlineIntensity ("Scanline Intensity", Range(0, 0.4)) = 0.06
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _VignetteDarkness ("Frame Vignette", Range(0, 1)) = 0.26

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _UnscaledTime;
            float _GrainIntensity;
            float _ChromaticOffset;
            float _ScanlineIntensity;
            float _GlitchIntensity;
            float _DissolveAmount;
            float _VignetteDarkness;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float t = _UnscaledTime;

                // 1. Glitch fuerte (barrido digital, píxeles rotos y salto de bandas)
                float glitch = saturate(_GlitchIntensity);
                float rgbSplit = _ChromaticOffset;

                if (glitch > 0.001)
                {
                    float timeStep = floor(t * 19.0);
                    float bandY = floor(uv.y * 28.0);
                    float bandRand = Hash21(float2(bandY, timeStep));

                    // Desplazamiento horizontal brusco de franjas digitales
                    if (bandRand > (1.0 - glitch * 0.55))
                    {
                        float shift = (Hash21(float2(timeStep, bandY * 1.7)) - 0.5) * 0.16 * glitch;
                        uv.x = saturate(uv.x + shift);
                    }

                    // Bloques de píxeles rotos / cuantizados
                    float2 blockCoord = floor(uv * float2(36.0, 22.0));
                    float blockNoise = Hash21(blockCoord + floor(t * 14.0));
                    if (blockNoise > (1.0 - glitch * 0.34))
                    {
                        float pixelScale = lerp(160.0, 32.0, glitch);
                        uv = floor(uv * pixelScale) / pixelScale;
                    }

                    rgbSplit += glitch * 0.026 * (0.4 + 0.6 * Hash21(float2(timeStep, 9.1)));
                }

                // 2. Aberración cromática (radial suave + refuerzo horizontal de glitch)
                float2 centered = uv - 0.5;
                float radial = 0.45 + dot(centered, centered) * 1.6;
                float2 caShift = float2(rgbSplit * radial, rgbSplit * centered.y * 0.35);

                fixed4 colR = tex2D(_MainTex, saturate(uv + caShift)) + _TextureSampleAdd;
                fixed4 colG = tex2D(_MainTex, saturate(uv)) + _TextureSampleAdd;
                fixed4 colB = tex2D(_MainTex, saturate(uv - caShift)) + _TextureSampleAdd;

                fixed4 col = fixed4(colR.r, colG.g, colB.b, colG.a) * IN.color;

                // Barrido digital luminoso durante el glitch
                if (glitch > 0.01)
                {
                    float sweepPos = frac(t * 0.65);
                    float sweepBand = smoothstep(0.06, 0.0, abs(uv.y - sweepPos));
                    col.rgb += float3(0.45, 0.16, 0.62) * sweepBand * glitch * 0.48;

                    // Bloques corruptos puntuales cian/violeta
                    float2 corruptCell = floor(uv * float2(48.0, 30.0));
                    float corruptHit = Hash21(corruptCell + floor(t * 22.0));
                    if (corruptHit > (1.0 - glitch * 0.11))
                    {
                        col.rgb = lerp(col.rgb, float3(0.58, 0.24, 0.85), glitch * 0.75);
                    }
                }

                // 3. Disolución digital de memoria (Cuadro 7: recuerdo desintegrándose)
                if (_DissolveAmount > 0.001)
                {
                    float2 memGrid = floor(uv * float2(96.0, 56.0));
                    float cellVal = Hash21(memGrid);
                    // Se disuelve primero desde los bordes hacia el núcleo y luego completo
                    float radialBias = length(centered) * 0.35;
                    float threshold = saturate(_DissolveAmount * 1.18 + radialBias * _DissolveAmount);

                    if (cellVal < threshold)
                    {
                        col.rgb *= 0.03;
                        col.a *= 0.04;
                    }
                    else if (cellVal < threshold + 0.085)
                    {
                        // Borde incandescente violeta/cian del recuerdo desintegrándose en código
                        col.rgb = lerp(col.rgb, float3(0.72, 0.32, 0.96), 0.82);
                    }
                }

                // 4. Líneas de escaneo finas y grano de película
                float scan = sin(uv.y * 540.0 + t * 5.0) * 0.5 + 0.5;
                col.rgb *= (1.0 - _ScanlineIntensity * scan);

                float grain = Hash21(uv * float2(640.0, 360.0) + float2(frac(t * 37.13), frac(t * 19.71))) - 0.5;
                col.rgb += grain * _GrainIntensity;

                // 5. Viñeta suave interna del cuadro
                float vig = saturate(1.0 - dot(centered, centered) * 1.55 * _VignetteDarkness);
                col.rgb *= vig;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
