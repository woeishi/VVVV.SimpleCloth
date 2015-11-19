#region usings
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Utils.Streams;
using VVVV.Hosting.Pins.Input;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Cloth
{
	#region PluginInfo Create
	[PluginInfo(Name = "Cloth", Category = "Cloth", Version = "Create", Help = "creates a cloth plane in 3d space", Author = "woei")]
	#endregion PluginInfo
	public class CreateClothNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Resolution X", DefaultValue = 20)]
		public IDiffSpread<int> FResX;
		[Input("Resolution Y", DefaultValue = 20)]
		public IDiffSpread<int> FResY;
		
		[Input("Upper Left", DefaultValues = new double[]{-0.5,0.5,0})]
		public IDiffSpread<Vector3D> FUpperLeft;
		[Input("Upper Right", DefaultValues = new double[]{0.5,0.5,0})]
		public IDiffSpread<Vector3D> FUpperRight;
		[Input("Lower Left", DefaultValues = new double[]{-0.5,-0.5,0})]
		public IDiffSpread<Vector3D> FLowerLeft;
		[Input("Lower Right", DefaultValues = new double[]{0.5,-0.5,0})]
		public IDiffSpread<Vector3D> FLowerRight;
		
		[Input("Reset", IsBang = true)]
		public ISpread<bool> FReset;
		
		[Output("Cloth")]
		public ISpread<Cloth> FOutput;
		#endregion fields & pins
		
		public void OnImportsSatisfied()
		{
			FOutput.SliceCount = 0;
		}
		
		public void Evaluate(int spreadMax)
		{
			FOutput.ResizeAndDismiss(spreadMax, (slice) => CreateCloth(slice));
			
			for (int i=0; i<spreadMax; i++)
			{
				if ((FResX[i] != FOutput[i].ResolutionX) ||
				    (FResY[i] != FOutput[i].ResolutionY) ||
					(FUpperLeft[i] != FOutput[i].UpperLeft) ||
					(FUpperRight[i] != FOutput[i].UpperRight) ||
					(FLowerLeft[i] != FOutput[i].LowerLeft) ||
					(FLowerRight[i] != FOutput[i].LowerRight) ||
				    (FReset[i]))
				{
					FOutput[i] = CreateCloth(i);
				}
			}
		}
		
		private Cloth CreateCloth(int slice)
		{
			return new Cloth(FResX[slice],
							FResY[slice],
							FUpperLeft[slice],
							FUpperRight[slice],
							FLowerLeft[slice],
							FLowerRight[slice]);
		}
	}
	
	#region PluginInfo GlobalForce
	[PluginInfo(Name = "GlobalForce", Category = "Cloth", Help = "applies global parameters for a cloth", Author = "woei")]
	#endregion PluginInfo
	public class GlobalForceClothNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		[Input("Stiffness", DefaultValue = 0.5)]
		public ISpread<double> FStiff;
		
		[Input("Friction", DefaultValue = 0.5)]
		public ISpread<double> FFriction;
		
		[Input("Force")]
		public ISpread<Vector3D> FForce;
		
		[Output("Cloth")]
		public ISpread<Cloth> FOutput;
		#endregion fields & pins
		
		public void Evaluate(int spreadMax)
		{
			FOutput.AssignFrom(FInput);
			
			for (int i=0; i<FInput.SliceCount; i++)
			{
				if (FOutput[i] != null)
				{
					FOutput[i].GlobalVelocity = FForce[i];
					FOutput[i].Friction = 1-Math.Min(1,Math.Max(0,FFriction[i]));
					FOutput[i].Stiffness = Math.Min(0.5,Math.Max(0,FStiff[i]));
				}
			}
		}
	}
	
	#region PluginInfo Pin
	[PluginInfo(Name = "Pin", Category = "Cloth", Help = "pins a cloth vertex to its current position", Author = "woei")]
	#endregion PluginInfo
	public class PinClothNode : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		public IDiffSpread<ISpread<int>> FId;
		public ISpread<ISpread<bool>> FEnable;
		
		[Output("Cloth")]
		public ISpread<Cloth> FOutput;
		
		[Output("Position")]
		public ISpread<ISpread<Vector3D>> FPos;
		
		[Import()]
		public IIOFactory FFactory;
			
		Spread<Spread<int>> FIdBuffer = new Spread<Spread<int>>(0);
		#endregion fields & pins
		
		public void OnImportsSatisfied()
		{
			var binSizeIOContainer = FFactory.CreateIOContainer<IInStream<int>>(new InputAttribute("Bin Size"){DefaultValue = -1, Order = int.MaxValue});
			FId = new DiffInputBinSpread<int>(FFactory, new InputAttribute("Pin Index"), binSizeIOContainer);
			FEnable = new DiffInputBinSpread<bool>(FFactory, new InputAttribute("Enabled"){DefaultBoolean = true}, binSizeIOContainer);
		}
		
		public void Evaluate(int spreadMax)
		{
			FId.Sync();
			FEnable.Sync();
			
			FOutput.AssignFrom(FInput);
			FPos.SliceCount = FInput.SliceCount;
			FIdBuffer.ResizeAndDismiss(FInput.SliceCount, () => new Spread<int>(0));
			
			for (int i=0; i<FInput.SliceCount; i++)
			{
				if (FOutput[i] != null && (FId.IsChanged || FEnable.IsChanged || FOutput[i].IsNew))
				{
					
					if (!FOutput[i].IsNew)
						foreach (var r in FIdBuffer[i])
							FOutput[i].ResetPin(r);
					
					FPos[i].SliceCount = FId[i].SliceCount;
					for (int j=0; j<FId[i].SliceCount; j++)
					{
						if (FEnable[i][j])
								FOutput[i].SetPin(FId[i][j]);
						FPos[i][j] = FOutput[i].GetPosition(FId[i][j]);
					}
					FIdBuffer[i].AssignFrom(FId[i]);
				}
				if (FOutput[i] == null)
					FPos[i].SliceCount = 0;
			}
		}
		
		public void Dispose()
		{
			for (int i=0; i<FOutput.SliceCount; i++)
			{
				try
				{
					foreach (var r in FId[i])
						FOutput[i].ResetPin(r);
				}
				catch {}
			}
		}
	}
	
	#region PluginInfo Attractor
	[PluginInfo(Name = "Attractor", Category = "Cloth", Help = "distorts a cloth through a spherical attractor", Author = "woei")]
	#endregion PluginInfo
	public class AttractorClothNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		public ISpread<ISpread<Vector3D>> FAttractor;
		public ISpread<ISpread<double>> FStrength;
		public ISpread<ISpread<double>> FPower;
		public ISpread<ISpread<double>> FRadius;
		public IIOContainer<IInStream<int>> FBinSize;
				
		[Output("Cloth")]
		public ISpread<Cloth> FOutput;
		
		[Import()]
		public IIOFactory FFactory;
		
		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		public void OnImportsSatisfied()
		{
			FBinSize = FFactory.CreateBinSizeInput(new InputAttribute("Bin Size") { DefaultValue = -1, Order = int.MaxValue });
			FAttractor = FBinSize.CreateBinSizeSpread<Vector3D>(new InputAttribute("Attractor"));
			FStrength = FBinSize.CreateBinSizeSpread<double>(new InputAttribute("Strength"));
			FPower = FBinSize.CreateBinSizeSpread<double>(new InputAttribute("Power") { DefaultValue = 1 });
			FRadius = FBinSize.CreateBinSizeSpread<double>(new InputAttribute("Radius") { DefaultValue = 0.1 });
		}
		
		public void Evaluate(int spreadMax)
		{
			FOutput.AssignFrom(FInput);
			for (int i=0; i<FInput.SliceCount; i++)
			{
				if (FOutput[i] != null)
				{
					foreach (var a in CreateAttractors(i))
						FOutput[i].Attractors.Add(a);
				}
			}
		}
		
		private IEnumerable<Attractor> CreateAttractors(int slice)
		{
			for (int i=0; i<FAttractor[slice].SliceCount; i++)
				yield return new Attractor(FAttractor[slice][i],FStrength[slice][i],FPower[slice][i],FRadius[slice][i]);
		}
	}
	
	#region PluginInfo LocalForce
	[PluginInfo(Name = "LocalForce", Category = "Cloth", Help = "adds velocity to a cloth vertex", Author = "woei")]
	#endregion PluginInfo
	public class LocalForceClothNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		public ISpread<ISpread<Vector3D>> FForce;
		public ISpread<ISpread<int>> FId;
		public IIOContainer<IInStream<int>> FBinSize;
				
		[Output("Cloth")]
		public ISpread<Cloth> FOutput;
		
		[Import()]
		public IIOFactory FFactory;
		#endregion fields & pins
		
		public void OnImportsSatisfied()
		{
			FBinSize = FFactory.CreateBinSizeInput(new InputAttribute("Bin Size") { DefaultValue = -1, Order = int.MaxValue });
			FForce = FBinSize.CreateBinSizeSpread<Vector3D>(new InputAttribute("Force"));
			FId = FBinSize.CreateBinSizeSpread<int>(new InputAttribute("Index"));
		}
		
		public void Evaluate(int spreadMax)
		{
			FOutput.AssignFrom(FInput);
			
			for (int i=0; i<FInput.SliceCount; i++)
			{
				if (FOutput[i] != null)
				{
					for (int j=0; j<FForce[i].SliceCount; j++)
						FOutput[i].SetVelocity(FId[i][j],FForce[i][j]);
				}
			}
		}
	}
	
	#region PluginInfo Target
	[PluginInfo(Name = "Target", Category = "Cloth", Help = "sets a target position for a cloth vertex", Author = "woei")]
	#endregion PluginInfo
	public class TargetClothNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		public ISpread<ISpread<Vector3D>> FTarget;
		public ISpread<ISpread<int>> FId;
		public ISpread<ISpread<double>> FApply;
		public IIOContainer<IInStream<int>> FBinSize;
				
		[Output("Cloth")]
		public ISpread<Cloth> FOutput;
		
		[Import()]
		public IIOFactory FFactory;
		#endregion fields & pins
		
		public void OnImportsSatisfied()
		{
			FBinSize = FFactory.CreateBinSizeInput(new InputAttribute("Bin Size") { DefaultValue = -1, Order = int.MaxValue });
			FTarget = FBinSize.CreateBinSizeSpread<Vector3D>(new InputAttribute("Target"));
			FId = FBinSize.CreateBinSizeSpread<int>(new InputAttribute("Index"));
			FApply = FBinSize.CreateBinSizeSpread<double>(new InputAttribute("Apply"));
		}
		
		public void Evaluate(int spreadMax)
		{
			FOutput.AssignFrom(FInput);
			
			for (int i=0; i<FInput.SliceCount; i++)
			{
				if (FOutput[i] != null)
				{
					for (int j=0; j<FTarget[i].SliceCount; j++)
						FOutput[i].SetTarget(FId[i][j],FTarget[i][j],FApply[i][j]);
				}
			}
		}
	}
	
	#region PluginInfo Position
	[PluginInfo(Name = "SetPosition", Category = "Cloth", Help = "", Author = "woei")]
	#endregion PluginInfo
	public class SetPositionClothNode : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		public ISpread<ISpread<Vector3D>> FPosition;
		public ISpread<ISpread<int>> FId;
		public ISpread<ISpread<double>> FApply;
		public IIOContainer<IInStream<int>> FBinSize;
				
		[Output("Cloth")]
		public ISpread<Cloth> FOutput;
		
		[Import()]
		public IIOFactory FFactory;
		#endregion fields & pins
		
		public void OnImportsSatisfied()
		{
			FBinSize = FFactory.CreateBinSizeInput(new InputAttribute("Bin Size") { DefaultValue = -1, Order = int.MaxValue });
			FPosition = FBinSize.CreateBinSizeSpread<Vector3D>(new InputAttribute("Position"));
			FId = FBinSize.CreateBinSizeSpread<int>(new InputAttribute("Index"));
			FApply = FBinSize.CreateBinSizeSpread<double>(new InputAttribute("Apply"));
		}
		
		public void Evaluate(int spreadMax)
		{
			FOutput.AssignFrom(FInput);
			
			for (int i=0; i<FInput.SliceCount; i++)
			{
				if (FOutput[i] != null)
				{
					for (int j=0; j<FPosition[i].SliceCount; j++)
						FOutput[i].SetPosition(FId[i][j],FPosition[i][j],FApply[i][j]);
				}
			}
		}
	}
	
	#region PluginInfo Evaluate
	[PluginInfo(Name = "Evaluate", Category = "Cloth", Help = "does the physics calculatio of cloth planes", Author = "woei")]
	#endregion PluginInfo
	public class EvaluateClothNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		[Input("Delta Time", DefaultValue = 0.016666, Visibility = PinVisibility.Hidden)]
		public ISpread<double> FDelta;
		
		[Input("Fixed Timestep", Visibility = PinVisibility.Hidden)]
		public ISpread<bool> FFixed;
		
		[Input("Iterations", DefaultValue = 2)]
		public ISpread<int> FIter;
		
		[Input("Thread Count", DefaultValue = 1, Visibility = PinVisibility.OnlyInspector)]
		public ISpread<int> FThrCount;
		
		[Input("Evaluate", DefaultBoolean = true)]
		public ISpread<bool> FEval;
		
		[Output("Cloth")]
		public ISpread<Cloth> FOutput;
		
		[Import()]
		public IHDEHost FHDE;
		
		double lastTime = 0;
		#endregion fields & pins
		
		public void Evaluate(int spreadMax)
		{
			FOutput.AssignFrom(FInput);
			
			double time = FHDE.FrameTime;
			double dt = 1/60.0;
			if (lastTime == 0)
				lastTime = time;
			else
			{
				dt = time-lastTime;
				lastTime = time;
			}

			for (int i=0; i<FInput.SliceCount; i++)
			{
				if (FOutput[i] != null && FIter[i]>0 && FEval[i])
				{
					var _dt = dt;
					if (FFixed[i])
						_dt = FDelta[i];
					FOutput[i].Update(_dt, FIter[i], FThrCount[i]);
				}
			}
		}
	}
	
	#region PluginInfo Split
	[PluginInfo(Name = "Split", Category = "Cloth", Help = "outputs cloth vertices", Author = "woei")]
	#endregion PluginInfo
	public class SplitClothNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		[Output("Position")]
		public ISpread<Vector3D> FOutput;
		
		[Output("Position Bin Size")]
		public ISpread<int> FBinSize;
		#endregion fields & pins
		
		public void Evaluate(int spreadMax)
		{
			FOutput.SliceCount = 0;
			FBinSize.SliceCount = FInput.SliceCount;
			
			for (int i=0; i<FInput.SliceCount; i++)
			{
				if (FInput[i] != null)
				{
					var count = FOutput.SliceCount;
					var pts = FInput[i].Positions;
					FOutput.AddRange(pts);
					FBinSize[i] = FOutput.SliceCount - count;
				}
				else
					FBinSize[i] = 0;
			}
		}
	}
	
	#region PluginInfo Sample
	[PluginInfo(Name = "Sample", Category = "Cloth", Help = "outputs parameters of a point on the cloth plane", Author = "woei")]
	#endregion PluginInfo
	public class SampleClothNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Cloth")]
		public ISpread<Cloth> FInput;
		
		[Input("Lookup")]
		public ISpread<ISpread<Vector2D>> FLookup;
		
		[Output("Position")]
		public ISpread<Vector3D> FOutput;
		
		[Output("Normal")]
		public ISpread<Vector3D> FNormal;
		
		[Output("Rest Position")]
		public ISpread<Vector3D> FRest;
		
		[Output("Position Bin Size")]
		public ISpread<int> FBinSize;
		#endregion fields & pins
		
		public void Evaluate(int spreadMax)
		{
			spreadMax = FLookup.SliceCount.CombineSpreads(FInput.SliceCount);
			
			FOutput.SliceCount = 0;
			FRest.SliceCount = 0;
			FNormal.SliceCount = 0;
			FBinSize.SliceCount = spreadMax;
			for (int i=0; i<spreadMax; i++)
			{
				if (FInput[i] != null)
				{
					FBinSize[i] = FLookup[i].SliceCount;
					for (int j=0; j<FLookup[i].SliceCount; j++)
					{
						FOutput.Add(FInput[i].SamplePosition(FLookup[i][j].x,FLookup[i][j].y));
						FRest.Add(FInput[i].SampleRestPosition(FLookup[i][j].x,FLookup[i][j].y));
						FNormal.Add(FInput[i].SampleNormal(FLookup[i][j].x,FLookup[i][j].y));
					}
				}
				else
					FBinSize[i] = 0;
			}
		}
	}
}
