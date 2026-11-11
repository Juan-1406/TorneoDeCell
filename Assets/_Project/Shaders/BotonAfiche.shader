Shader "Torneo/UI/BotonAfiche"
{
    Properties
    {
        [PerRendererData] _MainTex ("Afiche", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Seleccion ("Selección dorada", Range(0,1)) = 0
        _Pulsado ("Confirmación naranja", Range(0,1)) = 0
        _Brillo ("Luz", Range(0,1)) = 0
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect;
            float _Seleccion, _Pulsado, _Brillo;
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=(tex2D(_MainTex,i.uv)+_TextureSampleAdd);
                float l=dot(c.rgb,float3(.299,.587,.114));
                float m=smoothstep(.28,.63,max(c.r,max(c.g,c.b)));
                float3 gris=float3(l*.94,l*.97,l*1.02);
                float3 oro=float3(saturate(l*1.48+.07),saturate(l*1.12),saturate(l*.42));
                float3 luz=lerp(gris,oro,_Seleccion);
                luz=lerp(luz,float3(1,.48,.10)*max(l,.4),_Pulsado*.7);
                c.rgb=lerp(c.rgb*.94,luz,m*.98)+_Brillo*_Seleccion*m*.065;
                c*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
