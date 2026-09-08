// 로비 캐러셀용 원형 클리핑 UI 셰이더. UI/Default 기반.
//
// 하는 일 두 가지:
//   1) 사각형 스프라이트를 원형으로 잘라낸다(가장자리 안티에일리어싱 포함).
//   2) 정사각이 아닌 원본(예: 1600x1024)을 늘려 채우지 않고 중앙을 정사각으로 크롭한다.
//      비율은 _SpriteAspect(가로/세로)로 받는다. LobbyController 가 곡마다 설정한다.
//
// 전제 조건
//   - 이 셰이더는 정점 UV(texcoord)를 0~1 쿼드 좌표로 사용한다. 따라서 앨범아트는
//     Sprite Atlas 로 묶지 말고 개별 스프라이트여야 한다(아틀라스면 UV가 텍스처의
//     부분 영역이 되어 원과 크롭이 어긋난다).
//   - 원이 정원으로 보이려면 RectTransform 의 화면상 가로/세로가 같아야 한다.
//     LobbyController.BuildRing() 이 기준 스케일을 정사각으로 강제한다.
Shader "RhythmTherapy/UI/CircleClip"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 원본 가로/세로 비율. 1 이 아니면 중앙을 정사각으로 크롭해 왜곡을 막는다.
        _SpriteAspect ("Sprite Aspect (w/h)", Float) = 1

        // 테두리. 두께는 UV 단위(반지름 0.5 기준)라 원이 작아지면 같은 비율로 얇아진다.
        // 0 이면 테두리 없음. 색의 알파로 진하기를 조절한다.
        _BorderWidth ("Border Width (UV)", Range(0, 0.25)) = 0.02
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0        // fwidth(가장자리 안티에일리어싱)에 미분 명령이 필요

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
                float2 local         : TEXCOORD2;   // 클리핑용 0~1 쿼드 좌표
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _SpriteAspect;
            float _BorderWidth;
            fixed4 _BorderColor;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.local = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 비정사각 아트는 중앙을 정사각으로 크롭한다(늘려 채우면 왜곡되므로).
                float2 s = _SpriteAspect > 1.0
                    ? float2(1.0 / _SpriteAspect, 1.0)
                    : float2(1.0, _SpriteAspect);
                float2 uv = 0.5 + (IN.texcoord - 0.5) * s;

                half4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * IN.color;

                // 원형 클리핑. 정사각 프레임이므로 UV 거리 0.5 가 곧 원의 반지름.
                float d = length(IN.local - 0.5);
                float aa = fwidth(d);

                // 테두리는 원 안쪽으로 그린다. 바깥으로 그리면 실루엣이 커져 배치가 흔들린다.
                float inner = 0.5 - _BorderWidth;
                float border = smoothstep(inner - aa, inner, d);
                color.rgb = lerp(color.rgb, _BorderColor.rgb, border * _BorderColor.a);

                color.a *= 1.0 - smoothstep(0.5 - aa, 0.5, d);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
