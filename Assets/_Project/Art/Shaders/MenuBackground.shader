// Процедурный фон главного меню: градиент фиолетовый -> красный, диагональные "разрезы",
// сетка-рабица, металлические панели с заклёпками, гранж, силуэты травы снизу и виньетка.
// Рисуется на RawImage/Image в Canvas, поэтому сделан как UI-шейдер.
Shader "N89/UI/MenuBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _ColorLeft ("Left Color", Color) = (0.17, 0.05, 0.25, 1)
        _ColorRight ("Right Color", Color) = (0.38, 0.03, 0.12, 1)
        _SlashColor ("Slash Color", Color) = (0.75, 0.06, 0.22, 1)
        _FenceColor ("Fence Color", Color) = (0.02, 0.01, 0.03, 1)

        _PixelSize ("Virtual Resolution (height)", Float) = 360
        _FenceScale ("Fence Cells (vertical)", Float) = 15
        _FenceWidth ("Fence Wire Width", Range(0.01, 0.2)) = 0.05
        _Grunge ("Grunge Amount", Range(0, 1)) = 0.35
        _Vignette ("Vignette", Range(0, 2)) = 1.1
        _GrassHeight ("Grass Height", Range(0, 0.5)) = 0.2
        _Flicker ("Flicker", Range(0, 0.3)) = 0.06
        _Aspect ("Aspect (w/h)", Float) = 1.7777

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _ColorLeft, _ColorRight, _SlashColor, _FenceColor;
            float _PixelSize, _FenceScale, _FenceWidth, _Grunge, _Vignette, _GrassHeight, _Flicker, _Aspect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * noise(p);
                    p *= 2.03;
                    a *= 0.5;
                }
                return v;
            }

            // Расстояние до проволоки ромбической сетки (рабица)
            float fence(float2 p, out float shade)
            {
                float2 r = float2(p.x + p.y, p.x - p.y) * 0.7071;
                float2 g = frac(r) - 0.5;
                float2 d = abs(g);
                shade = g.x * g.y;
                return min(0.5 - d.x, 0.5 - d.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Пикселизация под "пиксельный" вид Katana Zero
                float2 res = float2(_PixelSize * _Aspect, _PixelSize);
                float2 uv = (floor(i.uv * res) + 0.5) / res;
                float2 p = float2(uv.x * _Aspect, uv.y);
                float t = _Time.y;

                // Базовый градиент слева направо + лёгкая вертикаль
                float3 col = lerp(_ColorLeft.rgb, _ColorRight.rgb, smoothstep(0.15, 0.9, uv.x));
                col *= lerp(0.75, 1.1, uv.y);

                // Металлические панели: горизонтальные швы и заклёпки
                float panelY = frac(uv.y * 3.0);
                float seam = smoothstep(0.0, 0.012, panelY) * smoothstep(0.0, 0.012, 1.0 - panelY);
                col *= lerp(0.55, 1.0, seam);
                float2 rivetGrid = float2(p.x * 3.0, uv.y * 3.0);
                float2 rc = frac(rivetGrid + float2(0.5, 0.06)) - 0.5;
                float rivet = 1.0 - smoothstep(0.018, 0.03, length(rc * float2(1.0, 1.0)));
                col += rivet * 0.12 * float3(0.8, 0.6, 1.0);

                // Диагональные "разрезы" (красные полосы как когти)
                float diag = p.x * 0.85 + uv.y * 0.55;
                float jag = (noise(float2(uv.y * 40.0, 2.0)) - 0.5) * 0.02;
                float slashes = 0.0;
                slashes += step(abs(diag - 1.05 + jag), 0.09) * smoothstep(0.1, 0.5, uv.y);
                slashes += step(abs(diag - 1.42 + jag), 0.035);
                slashes += step(abs(diag - 1.62 - jag), 0.14) * smoothstep(0.85, 0.35, uv.y);
                slashes += step(abs(diag - 2.05 + jag), 0.02);
                slashes = saturate(slashes);
                col = lerp(col, _SlashColor.rgb * (0.8 + 0.4 * uv.y), slashes * 0.6);
                // Тёмная кромка у разрезов
                float edge = step(abs(diag - 1.42 + jag), 0.05) - step(abs(diag - 1.42 + jag), 0.035);
                col *= 1.0 - edge * 0.5;

                // Гранж и пятна
                float g = fbm(p * 6.0);
                float stains = fbm(p * 1.7 + 3.1);
                col *= lerp(1.0, 0.55 + g * 0.8, _Grunge);
                col *= lerp(0.7, 1.15, stains);

                // Рабица поверх
                float shade;
                float dist = fence(p * _FenceScale, shade);
                float wire = smoothstep(_FenceWidth, _FenceWidth * 0.4, dist);
                float highlight = smoothstep(_FenceWidth * 0.8, 0.0, dist) * saturate(shade * 6.0 + 0.5);
                float3 wireCol = _FenceColor.rgb + highlight * float3(0.05, 0.02, 0.06);
                col = lerp(col, wireCol, wire * 0.92);
                // Тень от сетки на стене
                float shadowDist = fence(p * _FenceScale + float2(0.06, -0.08), shade);
                col *= lerp(1.0, 0.6, smoothstep(_FenceWidth, 0.0, shadowDist) * (1.0 - wire));

                // Силуэты травы/шипов снизу
                // Два слоя травинок разной частоты, у каждой своя высота и наклон
                float grass = 0.0;
                for (int layer = 0; layer < 2; layer++)
                {
                    float freq = layer == 0 ? 14.0 : 29.0;
                    float x = p.x * freq + layer * 0.37;
                    float id = floor(x);
                    float lean = (hash21(float2(id, 11.0 + layer)) - 0.5) * 0.6;
                    float sway = sin(t * 0.9 + id * 0.7) * 0.05;
                    float bladeX = frac(x) - 0.5 + (lean + sway) * (uv.y / max(_GrassHeight, 0.01));
                    float blade = saturate(abs(bladeX) * 2.0);
                    float bladeH = hash21(float2(id, 3.0 + layer)) * 0.85 + 0.15;
                    float height = _GrassHeight * (layer == 0 ? 1.0 : 0.6);
                    float grassTop = height * (0.25 + 0.75 * bladeH * pow(1.0 - blade, 1.5)) + fbm(float2(p.x * 4.0, 1.0)) * 0.04;
                    grass = max(grass, step(uv.y, grassTop));
                }
                // Крупные листья по краям
                float leftLeaf = step(uv.y, (0.55 - uv.x * 2.2) * (0.6 + 0.4 * noise(float2(uv.y * 25.0, 1.0))));
                float rightLeaf = step(uv.y, (0.5 - (1.0 - uv.x) * 2.0) * (0.6 + 0.4 * noise(float2(uv.y * 22.0, 7.0))));
                col *= 1.0 - saturate(grass + leftLeaf + rightLeaf) * 0.93;

                // Виньетка
                float2 vc = i.uv - 0.5;
                float vig = 1.0 - dot(vc, vc) * _Vignette * 1.6;
                col *= saturate(vig);

                // Лёгкое мерцание неона
                float flick = 1.0 - _Flicker * step(0.93, hash21(float2(floor(t * 12.0), 1.0)));
                col *= flick;

                return fixed4(col, 1.0) * i.color;
            }
            ENDCG
        }
    }
}
