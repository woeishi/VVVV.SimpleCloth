using System;
using System.Collections.Generic;
using VVVV.Utils.VMath;
using System.Threading;

namespace VVVV.Cloth
{
	public class Cloth
	{
		#region constants
		int resX;
		public int ResolutionX { get { return resX; } }
		int resY;
		public int ResolutionY { get { return resY; } }
		
		Vector3D uLeft;
		public Vector3D UpperLeft { get { return uLeft; } }
		Vector3D uRight;
		public Vector3D UpperRight { get { return uRight; } }
		Vector3D lLeft;
		public Vector3D LowerLeft { get { return lLeft; } }
		Vector3D lRight;
		public Vector3D LowerRight { get { return lRight; } }
		#endregion constants
		
		#region tweakables
		Vector3D globalV;
		public Vector3D GlobalVelocity
		{
			get { return globalV; }
			set { globalV = value; }
		}
		double friction;
		public double Friction
		{
			get { return Friction; }
			set { friction = value; }
		}
		double stiff;
		public double Stiffness
		{
			get { return stiff; }
			set { stiff = value; }
		}
		
		List<Attractor> attrs;
		public List<Attractor> Attractors
		{
			get { return attrs; }
			set { attrs = value; }
		}
		#endregion tweakables
		
		#region animation
		Vector3D[,] org;
		Vector3D[,] pos;
		public IEnumerable<Vector3D> Positions
		{
			get 
			{
				for (int y=0; y<resY; y++)
					for (int x=0; x<resX; x++)
						yield return pos[x,y];
			}
		}
		
		Vector3D[,] pPos;
		Vector3D[,] vel;
		Vector3D[,] norm;
		public IEnumerable<Vector3D> Normals
		{
			get 
			{
				CalculateNormals();
				for (int y=0; y<resY; y++)
					for (int x=0; x<resX; x++)
						yield return norm[x,y];
			}
		}
		bool[,] pinned;
		
		bool isNew;
		public bool IsNew { get { return isNew; } }
		#endregion
		
		#region ctor
		public Cloth(int resolutionX, int resolutionY, Vector3D upperLeft, Vector3D upperRight, Vector3D lowerLeft, Vector3D lowerRight)
		{
			resX = resolutionX;
			resY = resolutionY;
			uLeft = upperLeft;
			uRight = upperRight;
			lLeft = lowerLeft;
			lRight = lowerRight;
			
			globalV = new Vector3D(0);
			friction = 1.0;
			stiff = 0.5;
			attrs = new List<Attractor>();
			
			org = new Vector3D[resX,resY];
			pos = new Vector3D[resX,resY];
			pPos = new Vector3D[resX,resY];
			vel = new Vector3D[resX,resY];
			norm = new Vector3D[resX,resY];
			pinned = new bool[resX,resY];
			isNew = true;
			
			for (int y=0; y<resY; y++)
			{
				for (int x=0; x<resX; x++)
				{
					var upper = VMath.Lerp(uLeft,uRight, x/(double)(resX-1));
					var lower = VMath.Lerp(lLeft,lRight, x/(double)(resX-1));
					
					org[x,y] = VMath.Lerp(upper,lower,y/((double)resY-1));
					pos[x,y] = org[x,y];
					pPos[x,y] = org[x,y];
					vel[x,y] = new Vector3D(0);
					norm[x,y] = new Vector3D(0,0,1);
					pinned[x,y] = false;
				}
			}
		}
		#endregion
		
		private void Index2d(int index, out int x, out int y)
		{
			index = Math.Max(0,Math.Min(resX*resY-1,index));
			x = index % resX;
			y = (int)Math.Floor(index/(double)resX);
		}
		
		public Vector3D GetPosition(int index)
		{
			int x = 0; int y = 0;
			Index2d(index,out x, out y);
			return pos[x,y];
		}
		
		public void SetPosition(int index, Vector3D target, double apply)
		{
			int x = 0; int y = 0;
			Index2d(index,out x, out y);
			pos[x,y] = VMath.Lerp(pos[x,y],target,apply);
		}
		
		public void SetTarget(int index, Vector3D target, double apply)
		{
			int x = 0; int y = 0;
			Index2d(index,out x, out y);
			vel[x,y] = VMath.Lerp(vel[x,y],target - pos[x,y],apply);
		}
		
		public void SetVelocity(int index, Vector3D velocity)
		{
			int x = 0; int y = 0;
			Index2d(index,out x, out y);
			vel[x,y] += velocity;
		}
		
		public Vector3D SamplePosition(double x, double y)
		{
			var _x = Math.Min(x,0.9999999)*(resX-1);
			var _y = Math.Min(y,0.9999999)*(resY-1);
			
			int h = (int)Math.Floor(_x);
			int v = (int)Math.Floor(_y);
			double hr = _x-(double)h;
			double vr = _y-(double)v;
			var upper = VMath.Lerp(pos[h,v],pos[h+1,v],hr);
			var lower = VMath.Lerp(pos[h,v+1],pos[h+1,v+1],hr);
			
			return VMath.Lerp(upper,lower,vr);
		}
		
		public Vector3D SampleNormal(double x, double y)
		{
			var _x = Math.Min(x,0.9999999)*(resX-1);
			var _y = Math.Min(y,0.9999999)*(resY-1);
			
			int h = (int)Math.Floor(_x);
			int v = (int)Math.Floor(_y);
			double hr = _x-(double)h;
			double vr = _y-(double)v;
			var upper = VMath.Lerp(norm[h,v],norm[h+1,v],hr);
			var lower = VMath.Lerp(norm[h,v+1],norm[h+1,v+1],hr);
			
			return VMath.Lerp(upper,lower,vr);
		}
		
		public Vector3D SampleRestPosition(double x, double y)
		{
			var _x = Math.Min(x,0.9999999)*(resX-1);
			var _y = Math.Min(y,0.9999999)*(resY-1);
			
			int h = (int)Math.Floor(_x);
			int v = (int)Math.Floor(_y);
			double hr = _x-(double)h;
			double vr = _y-(double)v;
			var upper = VMath.Lerp(org[h,v],org[h+1,v],hr);
			var lower = VMath.Lerp(org[h,v+1],org[h+1,v+1],hr);
			
			return VMath.Lerp(upper,lower,vr);
		}
		
		#region pinning
		public void SetPin(int index)
		{
			int x = 0; int y = 0;
			Index2d(index,out x, out y);
			pinned[x,y] = true;
		}
		public void ResetPin(int index)
		{
			int x = 0; int y = 0;
			Index2d(index,out x, out y);
			pinned[x,y] = false;
		}
		public void ResetPins()
		{
			for (int y=0; y<resY; y++)
				for (int x=0; x<resX; x++)
					pinned[x,y] = false;
		}
		#endregion pinning
		
		#region constraints
		private Vector3D Constraint(int aX, int aY, int bX, int bY)
		{
			var orgV = org[aX,aY]-org[bX,bY];
			var diffV = pos[aX,aY]-pos[bX,bY];
			
			return (orgV-diffV)*0.5;
		}
		
		private void ResolveConstraints()
		{
			for (int y=0; y<resY; y++)
			{
				for (int x=0; x<resX; x++)
				{
					if (y>0)
					{
						var upV = Constraint(x,y-1,x,y) * stiff;
						if (!pinned[x,y-1])
							pos[x,y-1] += upV;
						if (!pinned[x,y])
							pos[x,y] -= upV;
					}
					
					if (x>0)
					{
						var leftV = Constraint(x-1,y,x,y) * stiff;
						if (!pinned[x-1,y])
							pos[x-1,y] += leftV;
						if (!pinned[x,y])
							pos[x,y] -= leftV;
					}
				}
			}
		}
		#endregion constraints
		
		private void CalculateNormals()
		{
			for (int y=0; y<resY; y++)
			{
				var t = Math.Max(y-1,0);
				var b = Math.Min(y+1,resY-1);
				for (int x=0; x<resX; x++)
				{
					var l = Math.Max(x-1,0);
					var r = Math.Min(x+1,resX-1);
					
					int i = 0;
					Vector3D n = new Vector3D(0);
					if (l < x)
					{
						n += pos[l,y]-pos[x,y];
						i++;
					}
					if (x < r)
					{
						n += pos[x,y]-pos[r,y];
						i++;
					}
					if (t < y)
					{
						n += pos[x,t]-pos[x,y];
						i++;
					}
					if (y < b)
					{
						n += pos[x,y]-pos[x,b];
						i++;
					}
					norm[x,y] = n/(double)i;
				}
			}
		}
		
		private void Integrate(double deltaTime, int yStride = 1, int yOffset = 0, int xStride = 1, int xOffset = 0)
		{
			for (int y=yOffset; y<resY; y+=yStride)
			{
				for (int x=xOffset; x<resX; x+=xStride)
				{
					if (!pinned[x,y])
					{
						var next = pos[x,y] + ((pos[x,y]-pPos[x,y])*friction) + (vel[x,y]+globalV) * deltaTime * deltaTime;
//						var next = pos[x,y] +  (vel[x,y]+globalV) * deltaTime ;
						
						Vector3D delta = new Vector3D(0);
						foreach (var a in attrs)
							delta += a.Sample(pos[x,y]);
//						pos[x,y] += delta;
						pPos[x,y] = pos[x,y];
						pos[x,y] = next+delta;
						vel[x,y] = new Vector3D(0);
					}
				}
			}
		}
		
		private void IntegrateThreaded(object input)
		{
			object[] inputs = (object[])input;
			
			double deltaTime = (double)inputs[0];
			int yStride = (int)inputs[1];
			int yOffset = (int)inputs[2];
			Integrate(deltaTime, yStride, yOffset);
		}
		
		public void Update(double deltaTime, int iterations = 2, int threadCount = 1)
		{
			int thrCount = Math.Min(Environment.ProcessorCount - 1,threadCount);
			
			if (thrCount < 2)
			{
				Integrate(deltaTime);
			}
			else
			{
				Thread[] threads = new Thread[thrCount];
				for (int t=0; t<thrCount; t++)
				{
					threads[t] = new Thread(IntegrateThreaded);
					threads[t].Start(new object[] {deltaTime, thrCount, t});
				}
				for (int j=0; j<thrCount; j++)
					threads[j].Join();
			}
			attrs.Clear();
			
			for (int i=0; i<iterations; i++)
				ResolveConstraints();
			
			isNew = false;
		}
	}
}
