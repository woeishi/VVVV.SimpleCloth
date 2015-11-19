#region usings
using System;

using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.SlimDX;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Cloth
{
	public class TextureHelper
	{
		public int Slice = 0;
		public int ResolutionX = 2;
		public int ResolutionY = 2;
	}
	
	#region PluginInfo
	[PluginInfo(Name = "AsTexture", 
				Category = "Cloth",
				Help = "outputs cloth plane vertices and normals as textures", 
				Author = "woei")]
	#endregion PluginInfo
	public class AsTextureClothNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		[Output("Texture")]
		public ISpread<TextureResource<TextureHelper>> FTex;
		
		[Output("Normals")]
		public ISpread<TextureResource<TextureHelper>> FNorm;
		
		[Import]
		public ILogger FLogger;
		#endregion fields & pins
		
		public void OnImportsSatisfied()
		{
			FTex.SliceCount = 0;
			FNorm.SliceCount = 0;
		}
		
		public void Evaluate(int spreadMax)
		{
			FTex.ResizeAndDispose(spreadMax, (int slice) => CreateTexResource(CreateInfo(slice)));
			FNorm.ResizeAndDispose(spreadMax, (int slice) => CreateNormResource(CreateInfo(slice)));
			for (int i=0; i<spreadMax; i++)
			{
				TextureHelper info = CreateInfo(i);
				if ((FTex[i].Metadata.Slice != info.Slice) ||
					(FTex[i].Metadata.ResolutionX != info.ResolutionX) ||
					(FTex[i].Metadata.ResolutionY != info.ResolutionY))
				{
					FTex[i].Dispose();
					FTex[i] = CreateTexResource(info);
					FTex[i].NeedsUpdate = true;
					
					FNorm[i].Dispose();
					FNorm[i] = CreateNormResource(info);
					FNorm[i].NeedsUpdate = true;
				}
			}
		}
		
		private TextureHelper CreateInfo(int slice)
		{
			TextureHelper info = new TextureHelper();
			info.Slice = slice;
			if (FInput[slice] != null)
			{
				info.ResolutionX = FInput[slice].ResolutionX;
				info.ResolutionY = FInput[slice].ResolutionY;
			}
			return info;
		}
		
		#region texture
		TextureResource<TextureHelper> CreateTexResource(TextureHelper info)
		{
			return TextureResource.Create(info, CreateTex, UpdateTex);
		}
		
		TextureResource<TextureHelper> CreateNormResource(TextureHelper info)
		{
			return TextureResource.Create(info, CreateTex, UpdateNorm);
		}
		
		Texture CreateTex(TextureHelper info, Device device)
		{
			var pool = Pool.Managed;
			var usage = Usage.None;
			if (device is DeviceEx)
			{
				pool = Pool.Default;
				usage = Usage.Dynamic;
			}
			return new Texture(device, info.ResolutionX, info.ResolutionY, 1, usage, Format.A32B32G32R32F, pool);
		}
		
		unsafe void UpdateTex(TextureHelper info, Texture tex)
		{
			if (FInput[info.Slice] != null)
			{
				var rect = tex.LockRectangle(0, LockFlags.None);
				rect.Data.Position = 0;
				
				int i=0;
				int pitch = 0;
				var enumerator = FInput[info.Slice].Positions.GetEnumerator();
				while (enumerator.MoveNext())
				{
					var pos = enumerator.Current;
					
					rect.Data.Write((float)pos.x);
					rect.Data.Write((float)pos.y); 
					rect.Data.Write((float)pos.z);
					rect.Data.Write((float)1.0);
					
					i++;
					if (i>=info.ResolutionX)
					{
						i=0;
						pitch += rect.Pitch;
						rect.Data.Position = pitch;
					}
				}
				tex.UnlockRectangle(0);
			}
		}
		
		unsafe void UpdateNorm(TextureHelper info, Texture tex)
		{
			if (FInput[info.Slice] != null)
			{
				var rect = tex.LockRectangle(0, LockFlags.None);
				rect.Data.Position = 0;
				
				int i=0;
				int pitch = 0;
				var enumerator = FInput[info.Slice].Normals.GetEnumerator();
				while (enumerator.MoveNext())
				{
					var norm = enumerator.Current;
					
					rect.Data.Write((float)norm.x);
					rect.Data.Write((float)norm.y); 
					rect.Data.Write((float)norm.z);
					rect.Data.Write((float)1.0);
					
					i++;
					if (i>=info.ResolutionX)
					{
						i=0;
						pitch += rect.Pitch;
						rect.Data.Position = pitch;
					}
				}
				tex.UnlockRectangle(0);
			}
		}
		#endregion texture
	}
}
