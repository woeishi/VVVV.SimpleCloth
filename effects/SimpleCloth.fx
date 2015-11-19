//@author: woei
// PARAMETERS ------------------------------------------------------------------
//transforms
float4x4 tW: WORLD;        //the models world matrix
float4x4 tV: VIEW;         //view matrix as set via Renderer (EX9)
float4x4 tWV: WORLDVIEW;
float4x4 tWVP: WORLDVIEWPROJECTION;
float4x4 tP: PROJECTION;   //projection matrix as set via Renderer (EX9)

#include <effects\PhongDirectional.fxh>

//texture
texture PTex <string uiname="Position";>;
sampler PSamp = sampler_state
{ 
    Texture   = (PTex);          
	FILTER = MIN_MAG_MIP_LINEAR;
	ADDRESSU = Clamp; ADDRESSV = Clamp;
};
texture NTex <string uiname="Normal";>;
sampler NSamp = sampler_state    
{ 
    Texture   = (NTex);        
    FILTER = MIN_MAG_MIP_LINEAR;
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
    float3 ViewDirV: TEXCOORD3;
};
// VERTEXSHADERS ---------------------------------------------------------------
vs2ps VS(
    float4 PosO: POSITION,
    float4 TexCd : TEXCOORD0)
{
    vs2ps Out = (vs2ps)0;

    //inverse light direction in view space
    Out.LightDirV = normalize(-mul(lDir, tV));
    
    //position (projected)
	float4 p = PosO;
	p.xyz = tex2Dlod(PSamp,TexCd);
    Out.PosWVP  = mul(p, tWVP);
    Out.TexCd = mul(TexCd, tTex);
    Out.ViewDirV = -normalize(mul(p, tWV));
    return Out;
}
// PIXELSHADERS ----------------------------------------------------------------
float Alpha <float uimin=0.0; float uimax=1.0;> = 1;
float4 PS(vs2ps In): COLOR
{
	float3 norm = tex2D(NSamp,In.TexCd);
	norm = mul(normalize(norm),tWV);
	
	float4 col = tex2D(Samp,In.TexCd);
    col.rgb *= PhongDirectional(norm, In.ViewDirV, In.LightDirV);
    col.a *= Alpha;

    return col;
}
// TECHNIQUES ------------------------------------------------------------------
technique TSimpleCloth
{
    pass P0
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader = compile ps_3_0 PS();
    }
}