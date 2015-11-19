using System;
using System.Collections.Generic;
using VVVV.Utils.VMath;

namespace VVVV.Cloth
{
	public class Attractor : IEquatable<Attractor>
	{
		Vector3D c;
		public Vector3D Center { get { return c; } set { c = value; } }
		double s;
		public double Strength { get { return s; } set { s = value; } }
		double p;
		public double Power { get { return p; } set { p = value; } }
		double r;
		public double Radius { get { return r; } set { r = value; } }
		
		public Attractor(Vector3D center, double strength, double power = 1, double radius = 0.1)
		{
			c = center;
			s = strength;
			p = power;
			r = radius;
		}
		
		public Vector3D Sample(Vector3D input)
		{
			var diff = input - c;
			
			var l = Math.Min((!diff) / r, 1.0);
			l = Math.Pow(l,p)-l;
			l *= s * r;
			return (~diff)*l;
		}
		
		public bool Equals(Attractor other)
		{
			if ((this.c == other.Center) &&
				(this.s == other.Strength) &&
				(this.p == other.Power) &&
				(this.r == other.Radius))
			{
				return true;
			}
			else
				return false;
		}
	}
}