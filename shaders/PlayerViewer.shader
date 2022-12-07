HEADER
{
    Description = "Player Viewer"
    DevShader = true
}

MODES
{
    Default();
    VrForward();
}

FEATURES
{
}

COMMON
{
    #include "postprocess/shared.hlsl"
}

struct VertexInput
{
    float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
    float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
};

struct PixelInput
{
    float2 uv : TEXCOORD0;
    
    #if ( PROGRAM == VFX_PROGRAM_VS )
        float4 vPositionPs : SV_Position;
    #endif

    #if ( PROGRAM == VFX_PROGRAM_PS )
        float4 vPositionSs : SV_ScreenPosition;
    #endif
};

VS
{
    float g_flStep< Attribute( "Step" ); Default( 1.0f ); >;
    PixelInput MainVs( VertexInput i )
    {
        PixelInput o;
        o.vPositionPs = float4( i.vPositionOs.xy, 0.0f, 1.0f );
        o.uv = i.vTexCoord;
        return o;
    }
}

PS
{
    #include "postprocess/common.hlsl"
    #include "postprocess/functions.hlsl"
    #include "common/proceedural.hlsl"

    RenderState( DepthWriteEnable, false );
    RenderState( DepthEnable, false );

    CreateTexture2D( g_tPlayerBuffer ) < Attribute( "PlayerTexture" ); SrgbRead( false ); Filter( MIN_MAG_LINEAR_MIP_POINT ); AddressU( CLAMP ); AddressV( CLAMP ); >;
    CreateTexture2D( g_tColorBuffer ) < Attribute( "ColorBuffer" ); SrgbRead( true ); Filter( MIN_MAG_LINEAR_MIP_POINT ); AddressU( CLAMP ); AddressV( CLAMP ); >;
    float2 CursorUvs < Attribute( "CursorUvs" ); >;
    float2 CursorScale < Attribute( "CursorScale" ); >;

    float GetCircleSDF( float2 vUvs, float2 vCorrection, float flRadius, float2 vCenter )
    {
        return saturate( smoothstep( 0.0f, flRadius, length( (vUvs - vCenter) / vCorrection ) ) );
    }

    float4 MainPs( PixelInput i ) : SV_Target0
    {
        float3 vColor = 0.0f;
        
        float flAspect = g_vRenderTargetSize.x / g_vRenderTargetSize.y;
        float2 vCorrection = float2( 1.0f, flAspect );

        float2 vCenter = CursorUvs;//(CursorUvs + 500.0f) / g_vRenderTargetSize.xy;//0.5f;
        float2 vUvs = i.uv;

        float flRadius = 0.5f;

        float flPlayerMask = GetCircleSDF( vUvs, vCorrection, flRadius, 0.5f );// saturate(smoothstep(0.0f, flRadius, length((vUvs - vCenter) / vCorrection)));
        flPlayerMask = min( flPlayerMask, GetCircleSDF( vUvs, vCorrection, CursorScale, CursorUvs ) );
        flPlayerMask = saturate( flPlayerMask );

        flPlayerMask *= flPlayerMask * flPlayerMask;
        float flAreaMask = 1.0f - flPlayerMask;

        vColor = ( Tex2D( g_tColorBuffer, i.uv ).rgb ) * flPlayerMask;
        vColor += SrgbGammaToLinear( Tex2D( g_tPlayerBuffer, i.uv ).rgb  ) * flAreaMask;

        return float4( vColor, 1.0f );
    }
}
