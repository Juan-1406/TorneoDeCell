Shader "Torneo/UI/TituloAfiche"
{
    Properties
    {
        [PerRendererData] _MainTex ("Título transparente", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Reloj ("Tiempo", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
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
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; };
            sampler2D _MainTex; fixed4 _Color, _TextureSampleAdd; float _Reloj; float4 _ClipRect;
            v2f vert(appdata v) { v2f o; o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)+_TextureSampleAdd;
                float fase=fmod(_Reloj+2,10);
                float x=(fase-7.8)*1.5;
                float banda=pow(saturate(1-abs(i.uv.x-i.uv.y*.25-x)*14),3);
                float tinta=smoothstep(.24,.65,max(c.r,max(c.g,c.b)));
                c.rgb+=float3(.17,.12,.04)*banda*tinta;
                c*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
