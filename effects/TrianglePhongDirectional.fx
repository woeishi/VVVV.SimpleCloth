//@author: woei

// PARAMETERS ------------------------------------------------------------------

//transforms
float4x4 tW: WORLD;        //the models world matrix
float4x4 tV: VIEW;         //view matrix as set via Renderer (EX9)
float4x4 tWV: WORLDVIEW;
float4x4 tWVP: WORLDVIEWPROJECTION;
float4x4 tP: PROJECTION;   //projection matrix as set via Renderer (EX9)

#include <effects\PhongDirectional.fxh>

texture PTex <string uiname="Position";>;
sampler PSamp = sampler_state
{ 
    Texture   = (PTex);          
	FILTER = MIN_MAG_MIP_POINT;
	ADDRESSU = Clamp; ADDRESSV = Clamp;
};
texture NTex <string uiname="Normal";>;
sampler NSamp = sampler_state    
{ 
    Texture   = (NTex);        
    FILTER = MIN_MAG_MIP_POINT;
	ADDRESSU = Clamp; ADDRESSV = Clamp;
};
texture Tex <string uiname="Texture";>;
sampler Samp = sampler_state
{ 
    Texture   = (Tex);         
    FILTER = MIN_MAG_MIP_LINEAR;
};
float4x4 tTex: TEXTUREMATRIX <string uiname="Texture Transform";>;


struct vs2ps
{
    float4 PosWVP: POSITION;
    float4 TexCd : TEXCOORD0;
    float3 LightDirV: TEXCOORD1;
    float3 NormV: TEXCOORD2;
    float3 ViewDirV: TEXCOORD3;
};
// VERTEXSHADERS ---------------------------------------------------------------
vs2ps VS(
    float4 PosO: POSITION,
    float3 NormO: NORMAL,
    float4 TexCd : TEXCOORD0)
{
    vs2ps Out = (vs2ps)0;
    Out.LightDirV = normalize(-mul(lDir, tV));
    Out.NormV = normalize(mul(normalize(tex2Dlod(NSamp,TexCd).xyz), tWV));
	float4 pData = tex2Dlod(PSamp,TexCd);
    PosO = float4(pData.xyz*pData.w,PosO.w);
    Out.PosWVP  = mul(PosO, tWVP);
    Out.TexCd = mul(TexCd, tTex);
    Out.ViewDirV = -normalize(mul(PosO, tWV));
    return Out;
}
// PIXELSHADERS ----------------------------------------------------------------
float Alpha <float uimin=0.0; float uimax=1.0;> = 1;
float4 PS(vs2ps In): COLOR
{
    float4 col = tex2D(Samp, In.TexCd);

    col.rgb *= PhongDirectional(In.NormV, In.ViewDirV, In.LightDirV);
    col.a *= Alpha;

    return col;
}
// TECHNIQUES ------------------------------------------------------------------
technique TTrianglePhongDirectional
{
    pass P0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}