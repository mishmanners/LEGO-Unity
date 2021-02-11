// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LEGOModelImporter
{
	public enum Axis
	{
		None = -1,
		X = 0,
		Y = 1,
		Z = 2,
		XY = 3,
		XZ = 4,
		YZ = 5,
		XYZ = 6
	}

	public static class MathUtils
	{
		public class Cone
        {
            public Vector3 origin;
            public Vector3 direction;
            public float angle;
            public float cosAngle;
            public float sinAngle;
            public float invSinAngle;
            public float cosAngleSqr;

            public Cone(Vector3 origin, Vector3 direction, float angle)
            {
                this.origin = origin;
                this.direction = direction;
                this.angle = angle;
                cosAngle = Mathf.Cos(angle);
                sinAngle = Mathf.Sin(angle);
                invSinAngle = 1.0f / sinAngle;
                cosAngleSqr = cosAngle * cosAngle;
            }
        }

		//FIXME: Consider documenting/naming improvements
        /// <summary>
        /// Checks intersection of a sphere and a cone
        /// </summary>
        /// <param name="sphereRadius">The radius of the sphere</param>
        /// <param name="sphereCenter">The center of the sphere</param>
        /// <param name="cone">The cone we want to check intersection on</param>
        /// <returns></returns>
        public static bool SphereIntersectCone(float sphereRadius, Vector3 sphereCenter, Cone cone)
        {
            // https://www.geometrictools.com/Documentation/IntersectionSphereCone.pdf
            var u = cone.origin - (sphereRadius * cone.invSinAngle) * cone.direction;
            var centerMinusU = sphereCenter - u;
            var dirDotCenterMinusU = Vector3.Dot(cone.direction, centerMinusU);
            if (dirDotCenterMinusU > 0.0f)
            {
                var sqrLengthCenterMinusU = Vector3.Dot(centerMinusU, centerMinusU);
                if (dirDotCenterMinusU * dirDotCenterMinusU >= sqrLengthCenterMinusU * cone.cosAngleSqr)
                {
                    var centerMinusOrigin = sphereCenter - cone.origin;
                    var dirDotCenterMinusOrigin = Vector3.Dot(cone.direction, centerMinusOrigin);

                    if (dirDotCenterMinusOrigin < -sphereRadius)
                    {
                        return false;
                    }

                    var rSinAngle = sphereRadius * cone.sinAngle;
                    if (dirDotCenterMinusOrigin >= -rSinAngle)
                    {
                        return true;
                    }
                    var sprLengthCenterMinusOrigin = Vector3.Dot(centerMinusOrigin, centerMinusOrigin);
                    return sprLengthCenterMinusOrigin <= sphereRadius * sphereRadius;
                }
            }
            return false;
        }


		public static Plane[] GetFrustumPlanesPerspective(Vector3 position, Quaternion rotation, float fov, float aspect, float near, float far)
		{
			var direction = rotation * Vector3.forward;
			var right = rotation * Vector3.right;
			var up = rotation * Vector3.up;

			var nearCenter = position + direction * near;
			var farCenter = position + direction * far;

			var halfNearHeight = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * near;
			var halfNearWidth = halfNearHeight * aspect;
			var halfFarHeight = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * far;
			var halfFarWidth = halfFarHeight * aspect;

			var farTopLeft = farCenter + up * halfFarHeight - right * halfFarWidth;
			var farBottomLeft = farCenter - up * halfFarHeight - right * halfFarWidth;
			var farBottomRight = farCenter - up * halfFarHeight + right * halfFarWidth;
			var nearTopLeft = nearCenter + up * halfNearHeight - right * halfNearWidth;
			var nearTopRight = nearCenter + up * halfNearHeight + right * halfNearWidth;
			var nearBottomRight = nearCenter - up * halfNearHeight + right * halfNearWidth;

			Plane[] planes =
			{
			new Plane(nearTopLeft, farTopLeft, farBottomLeft),          // Left
            new Plane(nearTopRight, nearBottomRight, farBottomRight),   // Right
            new Plane(farBottomLeft, farBottomRight, nearBottomRight),  // Bottom
            new Plane(farTopLeft, nearTopLeft, nearTopRight),           // Top
            new Plane(nearBottomRight, nearTopRight, nearTopLeft),      // Near
            new Plane(farBottomRight, farBottomLeft, farTopLeft)        // Far
        };

			return planes;
		}

		public static Plane[] GetFrustumPlanesOrtho(Vector3 position, Quaternion rotation, float size, float aspect, float near, float far)
		{
			var direction = rotation * Vector3.forward;
			var right = rotation * Vector3.right;
			var up = rotation * Vector3.up;

			var nearCenter = position + direction * near;
			var farCenter = position + direction * far;

			var halfHeight = size;
			var halfWidth = halfHeight * aspect;

			var farTopLeft = farCenter + up * halfHeight - right * halfWidth;
			var farBottomLeft = farCenter - up * halfHeight - right * halfWidth;
			var farBottomRight = farCenter - up * halfHeight + right * halfWidth;
			var nearTopLeft = nearCenter + up * halfHeight - right * halfWidth;
			var nearTopRight = nearCenter + up * halfHeight + right * halfWidth;
			var nearBottomRight = nearCenter - up * halfHeight + right * halfWidth;

			Plane[] planes =
			{
			new Plane(nearTopLeft, farTopLeft, farBottomLeft),          // Left
            new Plane(nearTopRight, nearBottomRight, farBottomRight),   // Right
            new Plane(farBottomLeft, farBottomRight, nearBottomRight),  // Bottom
            new Plane(farTopLeft, nearTopLeft, nearTopRight),           // Top
            new Plane(nearBottomRight, nearTopRight, nearTopLeft),      // Near
            new Plane(farBottomRight, farBottomLeft, farTopLeft)        // Far
        };

			return planes;
		}

		public static Quaternion FlipQuaternion(Quaternion q)
		{
			return new Quaternion(-q.x, -q.y, -q.z, -q.w);
		}

		public static Vector4 ColorToVector4(Color c)
		{
			return new Vector4(c.r, c.g, c.b, c.a);
		}

		public static float ConvertAngleTo(float rotInDegress)
		{
			return Mathf.Rad2Deg * Mathf.Acos(Mathf.Abs(Mathf.Cos(rotInDegress * Mathf.Deg2Rad)));
		}

		public static Vector3 GetEdgeTangent(Vector3 v0, Vector3 v1)
		{
			return Vector3.Cross(v0 - v1, Vector3.forward).normalized;
		}

		// Volume of a sphere
		public static float SphereVolume(float radius)
		{
			return (4.0f / 3.0f) * Mathf.PI * Mathf.Pow(radius, 3.0f);
		}

		// Volume of a capsule
		public static float CapsuleVolume(float radius, float height)
		{
			float volume = (4.0f / 3.0f) * Mathf.PI * Mathf.Pow(radius, 3.0f);
			volume += Mathf.PI * Mathf.Pow(radius, 2.0f) * height;
			return volume;
		}

		// Volume of a capsule with one of the spherical ends removed, ie. volume of a cylinder + volume of a hemisphere
		public static float HemiCapsuleVolume(float radius, float height)
		{
			float volume = (4.0f / 3.0f) * Mathf.PI * Mathf.Pow(radius, 3.0f) * 0.5f;
			volume += Mathf.PI * Mathf.Pow(radius, 2.0f) * height;
			return volume;
		}

		// Returns actual sign of a float, unlike Unitys built in function which returns 1 when the value is 0.0f
		public static float ActualSign(float a)
		{
			if (a > 0)
			{
				return 1;
			}
			else if (a < 0)
			{
				return -1;
			}
			return 0;
		}

		public static bool SameSign(float a, float b)
		{
			return ((a > 0 && b > 0) || (a < 0 && b < 0) || (a == b));
		}
		public static bool SameSign(int a, int b)
		{
			return ((a > 0 && b > 0) || (a < 0 && b < 0) || (a == b));
		}
		public static bool SameSign(float a, int b)
		{
			return ((a > 0 && b > 0) || (a < 0 && b < 0) || (a == 0 && b == 0));
		}

		public static Vector3 SqrVector(Vector3 v)
		{
			Vector3 r;
			r.x = Mathf.Sqrt(Mathf.Abs(v.x)) * Mathf.Sign(v.x);
			r.y = Mathf.Sqrt(Mathf.Abs(v.y)) * Mathf.Sign(v.y);
			r.z = Mathf.Sqrt(Mathf.Abs(v.z)) * Mathf.Sign(v.z);
			return r;
		}

		public static Vector3 AxisToVector3(Axis a)
		{
			switch (a)
			{
				case Axis.X:
					return Vector3.right;

				case Axis.Y:
					return Vector3.up;

				case Axis.Z:
					return Vector3.forward;

			}
			return Vector3.zero;
		}

		public static Axis GetMajorAxis(Vector3 v)
		{
			float x = Mathf.Abs(v.x);
			float y = Mathf.Abs(v.y);
			float z = Mathf.Abs(v.z);
			if (x > y && x > z)
			{
				return Axis.X;
			}
			else if (y > z)
			{
				return Axis.Y;
			}
			return Axis.Z;
		}

		public static Vector3 SnapMajorAxis(Vector3 v, bool keepSign)
		{
			Vector3 snapped = Vector3.zero;
			float x = Mathf.Abs(v.x);
			float y = Mathf.Abs(v.y);
			float z = Mathf.Abs(v.z);
			if (x > y && x > z)
			{
				snapped.x = Mathf.Sign(v.x);
			}
			else if (y > z)
			{
				snapped.y = Mathf.Sign(v.y);
			}
			else
			{
				snapped.z = Mathf.Sign(v.z);
			}
			if (!keepSign)
			{
				snapped.x = Mathf.Abs(snapped.x);
				snapped.y = Mathf.Abs(snapped.y);
				snapped.z = Mathf.Abs(snapped.z);
			}
			return snapped;
		}

		public enum VectorDirection
		{
			up,
			right,
			forward,
			down,
			left,
			back
		}

		public static Vector3 FindClosestAxis(Transform source, Vector3 axis, out VectorDirection direction)
		{
			var sourceRight = source.right;
			var sourceUp = source.up;
			var sourceForward = source.forward;
			var sourceLeft = -sourceRight;
			var sourceDown = -sourceUp;
			var sourceBack = -sourceForward;

			var rightAngle = Vector3.Angle(sourceRight, axis);
			var upAngle = Vector3.Angle(sourceUp, axis);
			var forwardAngle = Vector3.Angle(sourceForward, axis);
			var leftAngle = Vector3.Angle(sourceLeft, axis);
			var downAngle = Vector3.Angle(sourceDown, axis);
			var backAngle = Vector3.Angle(sourceBack, axis);

			var axisAngles = new (Vector3, float, VectorDirection)[6]{(sourceRight, rightAngle, VectorDirection.right), (sourceUp, upAngle, VectorDirection.up), (sourceForward, forwardAngle, VectorDirection.forward), 
			(sourceLeft, leftAngle, VectorDirection.left), (sourceDown, downAngle, VectorDirection.down), (sourceBack, backAngle, VectorDirection.back)};
			var chosenIndex = -1;
			var smallestAngle = float.PositiveInfinity;
			for(var i = 0; i < axisAngles.Length; i++)
			{
				if(axisAngles[i].Item2 < smallestAngle)
				{
					smallestAngle = axisAngles[i].Item2;
					chosenIndex = i;
				}
			}

			direction = axisAngles[chosenIndex].Item3;
			return axisAngles[chosenIndex].Item1;
		}

		public static (Vector3, Vector3) GetRelatedAxes(Transform source, VectorDirection direction)
		{
			switch(direction)
			{
				case MathUtils.VectorDirection.up:
				return (source.transform.right, source.transform.forward);
				case MathUtils.VectorDirection.right:
				return (source.transform.up, source.transform.forward);
				case MathUtils.VectorDirection.forward:
				return (source.transform.up, source.transform.right);
				case MathUtils.VectorDirection.left:
				return (-source.transform.up, -source.transform.forward);
				case MathUtils.VectorDirection.down:
				return (-source.transform.right, -source.transform.forward);
				case MathUtils.VectorDirection.back:
				return (-source.transform.up, -source.transform.right);
			}
			return (Vector3.zero, Vector3.zero);
		}

		/// <summary>
		/// Align the closest of a selection of axes to a destination matrix
		/// </summary>
		/// <param name="sourceAxes">The axes to check for alignment</param>
		/// <param name="destination">The matrix to align to</param>
		/// <returns></returns>
		public static Quaternion AlignRotation(Vector3[] sourceAxes, Matrix4x4 destination)
		{
			var angleAxes = new List<(float, Vector3, Vector3)>();
			var inverseDst = destination.inverse;

			foreach(var axis in sourceAxes)
			{
				var localAxis = inverseDst.MultiplyVector(axis);
				var snappedLocalAxis = MathUtils.SnapMajorAxis(localAxis, true);
				var snappedWorld = destination.MultiplyVector(snappedLocalAxis);

				angleAxes.Add((Vector3.Angle(axis, snappedWorld), axis, snappedWorld));
			}

			foreach(var a1 in angleAxes)
			{
				var smallest = true;
				foreach(var a2 in angleAxes)
				{
					if(a1.Item1 > a2.Item1)
					{
						smallest = false;
						break;
					}
					
					if(smallest)
					{
						return Quaternion.FromToRotation(a1.Item2, a1.Item3);
					}
				}
			}
			return Quaternion.identity;
		}

		/// <summary>
		/// Get a rotation to align the right or forward axis of the source matrix with a destination matrix
		/// </summary>
		/// <param name="source">The matrix to align</param>
		/// <param name="destination">The matrix to align with</param>
		/// <returns></returns>
		public static Quaternion AlignRotation(Matrix4x4 source, Matrix4x4 destination)
        {
			var right = source.GetColumn(0);
			var forward = source.GetColumn(2);
			return AlignRotation(new Vector3[2]{right, forward}, destination);
        }

		public static float GetQuatLength(Quaternion q)
		{
			return Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
		}

		public static Quaternion GetQuatConjugate(Quaternion q)
		{
			return new Quaternion(-q.x, -q.y, -q.z, q.w);
		}

		public static Vector3 GetMatrixScale(Matrix4x4 m)
		{
			/*
			Vector3 i = m.GetColumn(0);
			Vector3 j = m.GetColumn(1);
			Vector3 k = m.GetColumn(2);
			*/

			Vector3 x = m.GetRow(0);
			Vector3 y = m.GetRow(1);
			Vector3 z = m.GetRow(2);

			return new Vector3(x.magnitude, y.magnitude, z.magnitude);
		}

		public static Rect Lerp(Rect a, Rect b, float t)
		{
			return new Rect(Vector2.Lerp(a.min, b.min, t), Vector2.Lerp(a.size, b.size, t));
		}

		public static Matrix4x4 Lerp(Matrix4x4 a, Matrix4x4 b, float t)
		{
			if (t <= 0.0f)
				return a;
			if (t >= 1.0f)
				return b;

			Quaternion qa = MatrixToQuaternion(a);
			Quaternion qb = MatrixToQuaternion(b);
			Quaternion q = Quaternion.Slerp(qa, qb, t);

			Vector3 pa = a.GetColumn(3);
			Vector3 pb = b.GetColumn(3);
			Vector3 p = Vector3.Lerp(pa, pb, t);

			Vector3 sa = GetMatrixScale(a);
			Vector3 sb = GetMatrixScale(b);
			Vector3 s = Vector3.Lerp(sa, sb, t);

			return Matrix4x4.TRS(p, q, s);
		}

		public static Matrix4x4 OrthogonalizeMatrix(Matrix4x4 m)
		{

			Matrix4x4 n = Matrix4x4.identity;

			Vector3 i = m.GetColumn(0);
			Vector3 j = m.GetColumn(1);
			Vector3 k = m.GetColumn(2);
			Vector4 t = m.GetColumn(3);

			k = k.normalized;
			i = Vector3.Cross(j, k).normalized;
			j = Vector3.Cross(k, i).normalized;
			t.w = 1.0f;

			n.SetColumn(0, i);
			n.SetColumn(1, j);
			n.SetColumn(2, k);
			n.SetColumn(3, t);
			return n;
		}

		public static void QuaternionNormalize(ref Quaternion q)
		{
			float invMag = 1.0f / Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
			q.x *= invMag;
			q.y *= invMag;
			q.z *= invMag;
			q.w *= invMag;
		}
		/*
		public static Quaternion QuaternionFromMatrix(Matrix4x4 m) 
		{	
			// Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm 
			Quaternion q = new Quaternion(); 
			q.w = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] + m[1,1] + m[2,2] ) ) / 2; 
			q.x = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] - m[1,1] - m[2,2] ) ) / 2; 
			q.y = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] + m[1,1] - m[2,2] ) ) / 2; 
			q.z = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] - m[1,1] + m[2,2] ) ) / 2; 
			q.x *= Mathf.Sign( q.x * ( m[2,1] - m[1,2] ) ); 
			q.y *= Mathf.Sign( q.y * ( m[0,2] - m[2,0] ) ); 
			q.z *= Mathf.Sign( q.z * ( m[1,0] - m[0,1] ) ); 
			// normalize
			QuaternionNormalize(ref q);
			return q; 
		}
		*/
		static public Quaternion MatrixToQuaternion(Matrix4x4 mat)
		{
			return MatrixToQuaternion(mat.GetRow(0), mat.GetRow(1), mat.GetRow(2));
		}

		static public Quaternion MatrixToQuaternion(Vector3 i, Vector3 j, Vector3 k)
		{
			float tr = i.x + j.y + k.z;
			float s;
			Quaternion q = new Quaternion();

			if (tr >= 0)
			{
				s = Mathf.Sqrt(tr + 1);
				q.w = 0.5f * s;
				s = 0.5f / s;
				q.x = (k.y - j.z) * s;
				q.y = (i.z - k.x) * s;
				q.z = (j.x - i.y) * s;
			}
			else
			{
				int t = 0;
				if (j.y > i.x) t = 1;

				if (t == 0)
				{
					if (k.z > i.x) t = 2;
				}
				else
				{
					if (k.z > j.y) t = 2;
				}

				switch (t)
				{
					case 0:
						s = Mathf.Sqrt((i.x - (j.y + k.z)) + 1);
						q.x = 0.5f * s;
						s = 0.5f / s;
						q.y = (i.y + j.x) * s;
						q.z = (k.x + i.z) * s;
						q.w = (k.y - j.z) * s;
						break;
					case 1:
						s = Mathf.Sqrt((j.y - (k.z + i.x)) + 1);
						q.y = 0.5f * s;
						s = 0.5f / s;
						q.z = (j.z + k.y) * s;
						q.x = (i.y + j.x) * s;
						q.w = (i.z - k.x) * s;
						break;
					case 2:
						s = Mathf.Sqrt((k.z - (i.x + j.y)) + 1);
						q.z = 0.5f * s;
						s = 0.5f / s;
						q.x = (k.x + i.z) * s;
						q.y = (j.z + k.y) * s;
						q.w = (j.x - i.y) * s;
						break;
				}
			}

			QuaternionNormalize(ref q);

			return q;
		}

		/// <summary>
		/// Logarithm of a unit quaternion. The result is not necessary a unit quaternion.
		/// </summary>
		public static Quaternion GetQuatLog(Quaternion q)
		{
			Quaternion res = q;
			res.w = 0;

			if (Mathf.Abs(q.w) < 1.0f)
			{
				float theta = Mathf.Acos(q.w);
				float sin_theta = Mathf.Sin(theta);

				if (Mathf.Abs(sin_theta) > 0.0001)
				{
					float coef = theta / sin_theta;
					res.x = q.x * coef;
					res.y = q.y * coef;
					res.z = q.z * coef;
				}
			}

			return res;
		}

		public static Quaternion GetQuatExp(Quaternion q)
		{
			Quaternion res = q;

			float fAngle = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z);
			float fSin = Mathf.Sin(fAngle);

			res.w = Mathf.Cos(fAngle);

			if (Mathf.Abs(fSin) > 0.0001)
			{
				float coef = fSin / fAngle;
				res.x = coef * q.x;
				res.y = coef * q.y;
				res.z = coef * q.z;
			}

			return res;
		}

		/// <summary>
		/// SQUAD Spherical Quadrangle interpolation [Shoe87]
		/// </summary>
		public static Quaternion GetQuatSquad(float t, Quaternion q0, Quaternion q1, Quaternion a0, Quaternion a1)
		{
			float slerpT = 2.0f * t * (1.0f - t);

			Quaternion slerpP = Slerp(q0, q1, t);
			Quaternion slerpQ = Slerp(a0, a1, t);
			Quaternion slerp = Slerp(slerpP, slerpQ, slerpT);

			// normalize quaternion
			float l = Mathf.Sqrt(slerp.x * slerp.x + slerp.y * slerp.y + slerp.z * slerp.z + slerp.w * slerp.w);
			slerp.x /= l;
			slerp.y /= l;
			slerp.z /= l;
			slerp.w /= l;

			return slerp;
		}

		public static Quaternion GetSquadIntermediate(Quaternion q0, Quaternion q1, Quaternion q2)
		{
			Quaternion q1Inv = GetQuatConjugate(q1);
			Quaternion p0 = GetQuatLog(q1Inv * q0);
			Quaternion p2 = GetQuatLog(q1Inv * q2);
			Quaternion sum = new Quaternion(-0.25f * (p0.x + p2.x), -0.25f * (p0.y + p2.y), -0.25f * (p0.z + p2.z), -0.25f * (p0.w + p2.w));

			return q1 * GetQuatExp(sum);
		}

		/// <summary>
		/// Smooths the input parameter t.
		/// If less than k1 ir greater than k2, it uses a sin.
		/// Between k1 and k2 it uses linear interp.
		/// </summary>
		public static float Ease(float t, float k1, float k2)
		{
			float f; float s;

			f = k1 * 2 / Mathf.PI + k2 - k1 + (1.0f - k2) * 2 / Mathf.PI;

			if (t < k1)
			{
				s = k1 * (2 / Mathf.PI) * (Mathf.Sin((t / k1) * Mathf.PI / 2 - Mathf.PI / 2) + 1);
			}
			else
				if (t < k2)
			{
				s = (2 * k1 / Mathf.PI + t - k1);
			}
			else
			{
				s = 2 * k1 / Mathf.PI + k2 - k1 + ((1 - k2) * (2 / Mathf.PI)) * Mathf.Sin(((t - k2) / (1.0f - k2)) * Mathf.PI / 2);
			}

			return (s / f);
		}

		/// <summary>
		/// We need this because Quaternion.Slerp always uses the shortest arc.
		/// </summary>
		public static Quaternion Slerp(Quaternion p, Quaternion q, float t)
		{
			Quaternion ret;

			float fCos = Quaternion.Dot(p, q);

			if ((1.0f + fCos) > 0.00001)
			{
				float fCoeff0, fCoeff1;

				if ((1.0f - fCos) > 0.00001)
				{
					float omega = Mathf.Acos(fCos);
					float invSin = 1.0f / Mathf.Sin(omega);
					fCoeff0 = Mathf.Sin((1.0f - t) * omega) * invSin;
					fCoeff1 = Mathf.Sin(t * omega) * invSin;
				}
				else
				{
					fCoeff0 = 1.0f - t;
					fCoeff1 = t;
				}

				ret.x = fCoeff0 * p.x + fCoeff1 * q.x;
				ret.y = fCoeff0 * p.y + fCoeff1 * q.y;
				ret.z = fCoeff0 * p.z + fCoeff1 * q.z;
				ret.w = fCoeff0 * p.w + fCoeff1 * q.w;
			}
			else
			{
				float fCoeff0 = Mathf.Sin((1.0f - t) * Mathf.PI * 0.5f);
				float fCoeff1 = Mathf.Sin(t * Mathf.PI * 0.5f);

				ret.x = fCoeff0 * p.x - fCoeff1 * p.y;
				ret.y = fCoeff0 * p.y + fCoeff1 * p.x;
				ret.z = fCoeff0 * p.z - fCoeff1 * p.w;
				ret.w = p.z;
			}

			return ret;
		}

		public static bool IsInsideOrientedBoxCollider(Vector3 worldPos, BoxCollider box)
		{
			worldPos = box.transform.InverseTransformPoint(worldPos) - box.center;
			Vector3 extents = box.size.Abs() * 0.5f;

			return (worldPos.x < extents.x && worldPos.x > -extents.x &&
				worldPos.y < extents.y && worldPos.y > -extents.y &&
				worldPos.z < extents.z && worldPos.z > -extents.z);
		}

		public static Vector3 InsideSphereCylinder(Vector3 cylinderA, Vector3 cylinderB, float cylinderRadius, Vector3 sphereOrigin, float sphereRadius, bool cappedA, bool cappedB, out bool collided, out float t)
		{
			collided = false;

			float cylRad = cylinderRadius - sphereRadius;
			float capRad = sphereRadius;

			// Test caps first
			Vector3 capNormal = (cylinderB - cylinderA).normalized;

			// test cap A
			if (cappedA)
			{
				Plane capA = new Plane(capNormal, cylinderA + capNormal * capRad);
				if (DistanceToPlane(sphereOrigin, capA) < -0.001f)
				{
					sphereOrigin = ClosestPtPointPlane(sphereOrigin, capA);
					collided = true;
				}
			}

			// test cap B
			if (cappedB)
			{
				Plane capB = new Plane(-capNormal, cylinderB - capNormal * capRad);
				if (DistanceToPlane(sphereOrigin, capB) < -0.001f)
				{
					sphereOrigin = ClosestPtPointPlane(sphereOrigin, capB);
					collided = true;
				}
			}

			// Test segment
			t = 0.0f;
			Vector3 closest = ClosestPtPointSegment(sphereOrigin, cylinderA, cylinderB, ref t);
			Vector3 delta = sphereOrigin - closest;
			float dist = delta.magnitude;
			if (dist > cylRad)
			{
				sphereOrigin = closest + delta.normalized * cylRad;
				collided = true;
			}

			return sphereOrigin;
		}

		public static bool IntersectPlanePlane(Plane p0, Plane p1, ref Vector3 a, ref Vector3 b)
		{
			// Compute direction of intersection line
			Vector3 d = Vector3.Cross(p0.normal, p1.normal);

			// If d is (near) zero, the planes are parallel (and separated)
			// or coincident, so they're not considered intersecting
			float denom = Vector3.Dot(d, d);
			if (denom < Mathf.Epsilon) return false;

			// Compute point on intersection line
			a = Vector3.Cross(p1.distance * p0.normal - p0.distance * p1.normal, d) / denom;
			b = a + d * 10.0f;

			return true;
		}

		//static function SphereCast (ray : Ray, radius : float, out hitInfo : RaycastHit, distance : float = Mathf.Infinity, layerMask : int = kDefaultRaycastLayers) : boolean
		public static bool SafeSphereCast(Ray ray, float radius, out RaycastHit hitInfo, float distance, int layerMask)
		{
			if (!Physics.SphereCast(ray, radius, out hitInfo, distance, layerMask))
			{
				return false;
			}

			bool bError = false;

			if (float.IsInfinity(hitInfo.point.x) || float.IsNaN(hitInfo.point.x) ||
			float.IsInfinity(hitInfo.point.y) || float.IsNaN(hitInfo.point.y) ||
			float.IsInfinity(hitInfo.point.z) || float.IsNaN(hitInfo.point.z))
			{

				bError = true;

				Debug.Log("[MathUtils :: SafeSphereCast (ssc_err0)] ERROR :  Bad hitinfo.point " + hitInfo.point + " " + hitInfo.collider + " " + hitInfo.collider.transform.position);
				if (hitInfo.collider == null)
				{
					return false;
				}
				Vector3 safe = hitInfo.collider.transform.position;
				if (!Physics.Raycast(ray, out hitInfo, 2.0f, layerMask))
				{
					hitInfo.point = safe;
					//	Debug.LogError( "[MathUtils :: SafeSphereCast (ssc_err1)] ERROR : Raycast failed. Defaulting to collider position " + hitInfo.point + " " + hitInfo.collider + " " + hitInfo.collider.transform.position);
					//	Debug.Log( "[MathUtils :: SafeSphereCast (ssc_err1)] ERROR : Raycast failed. Defaulting to collider position " + hitInfo.point + " " + hitInfo.collider + " " + hitInfo.collider.transform.position);
				}
				Debug.Log("Climb target after raycast: " + hitInfo.point);

				if (float.IsInfinity(hitInfo.point.x) || float.IsNaN(hitInfo.point.x) ||
				float.IsInfinity(hitInfo.point.y) || float.IsNaN(hitInfo.point.y) ||
				float.IsInfinity(hitInfo.point.z) || float.IsNaN(hitInfo.point.z))
				{
					hitInfo.point = hitInfo.collider.transform.position;
					Debug.Log("[MathUtils :: SafeSphereCast (ssc_err2)] ERROR : Bad hitinfo.point even after simple raycast " + hitInfo.point + " " + hitInfo.collider + " " + hitInfo.collider.transform.position);
				}
			}

			if (bError)
			{
				//			System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(0);
				//			Debug.LogError( "[MathUtils :: SafeSphereCast (ssc_err_generic)] ERROR : Supplied params were (ray : " + ray + ", radius : " + radius + ", hitInfo : " + hitInfo + ", distance : " + distance + ", layerMask : " + layerMask + ")\n"
				//				+ "\n" + st.ToString());
				Debug.Log("[MathUtils :: SafeSphereCast (ssc_err_generic)] ERROR : Supplied params were (ray : " + ray + ", radius : " + radius + ", hitInfo : " + hitInfo + ", distance : " + distance + ", layerMask : " + layerMask + ")\n");

			}
			return true;
		}

		public static Vector3 IntersectRayPlane(Ray ray, Vector3 position)
		{
			return IntersectRayPlane(ray, position, Vector3.forward);
		}

		public static Vector3 IntersectRayPlane(Ray ray, Vector3 position, Vector3 normal)
		{
			Plane plane = new Plane(normal, position);
			float enter;
			if (plane.Raycast(ray, out enter))
			{
				return ray.origin + ray.direction * enter;
			}
			return ray.origin + ray.direction;
		}

		public static Vector3 IntersectRayPlane(Ray ray, Plane plane)
		{
			float enter;
			if (plane.Raycast(ray, out enter))
			{
				return ray.origin + ray.direction * enter;
			}
			return ray.origin + ray.direction;
		}

		public static bool IntersectRayQuad(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, bool bidirectional, ref RaycastHit hit)
		{
			if (IntersectRayTriangle(ray, v0, v1, v2, false, ref hit))
			{
				return true;
			}
			if (IntersectRayTriangle(ray, v0, v2, v3, false, ref hit))
			{
				return true;
			}
			return false;
		}

		public static bool IntersectPlaneQuad(Plane p, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, ref Vector3[] hits, ref int hitCount)
		{
			hitCount = 0;

			Vector3 e0 = (v1 - v0);
			Vector3 e1 = (v2 - v1);
			Vector3 e2 = (v3 - v2);
			Vector3 e3 = (v0 - v3);

			float d0 = e0.magnitude;
			float d1 = e1.magnitude;
			float d2 = e2.magnitude;
			float d3 = e3.magnitude;

			e0.Normalize();
			e1.Normalize();
			e2.Normalize();
			e3.Normalize();

			float e = 0.0f;
			if (p.Raycast(new Ray(v0, e0), out e))
			{
				if (e > 0 && e < d0)
					hits[hitCount++] = v0 + e0 * e;
			}

			if (p.Raycast(new Ray(v1, e1), out e))
			{
				if (e > 0 && e < d1)
					hits[hitCount++] = v1 + e1 * e;
			}

			if (p.Raycast(new Ray(v2, e2), out e))
			{
				if (e > 0 && e < d2)
					hits[hitCount++] = v2 + e2 * e;
			}

			if (p.Raycast(new Ray(v3, e3), out e))
			{
				if (e > 0 && e < d3)
					hits[hitCount++] = v3 + e3 * e;
			}

			return (hitCount >= 2);
		}

		public static bool IntersectPlaneTriangle(Plane p, Vector3 v0, Vector3 v1, Vector3 v2, ref Vector3[] hits, ref int hitCount)
		{
			hitCount = 0;

			Vector3 e0 = (v1 - v0);
			Vector3 e1 = (v2 - v1);
			Vector3 e2 = (v0 - v2);

			float d0 = e0.magnitude;
			float d1 = e1.magnitude;
			float d2 = e2.magnitude;

			e0.Normalize();
			e1.Normalize();
			e2.Normalize();

			float e = 0.0f;
			if (p.Raycast(new Ray(v0, e0), out e))
			{
				if (e > 0 && e < d0)
					hits[hitCount++] = v0 + e0 * e;
			}

			if (p.Raycast(new Ray(v1, e1), out e))
			{
				if (e > 0 && e < d1)
					hits[hitCount++] = v1 + e1 * e;
			}

			if (p.Raycast(new Ray(v2, e2), out e))
			{
				if (e > 0 && e < d2)
					hits[hitCount++] = v2 + e2 * e;
			}

			return (hitCount >= 2);
		}

		public static bool IntersectRayLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, ref float t, ref Vector2 inter)
		{
			float ua_t = (b2.x - b1.x) * (a1.y - b1.y) - (b2.y - b1.y) * (a1.x - b1.x);
			float ub_t = (a2.x - a1.x) * (a1.y - b1.y) - (a2.y - a1.y) * (a1.x - b1.x);
			float u_b = (b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y);

			if (u_b != 0)
			{
				float ua = ua_t / u_b;
				float ub = ub_t / u_b;

				if (0 <= ub && ub <= 1)
				{
					inter.x = a1.x + ua * (a2.x - a1.x);
					inter.y = a1.y + ua * (a2.y - a1.y);
					t = ua;
					return true;
				}
			}

			return false;
		}

		public static bool IntersectRayRay(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, ref float t, ref Vector2 inter)
		{
			float ua_t = (b2.x - b1.x) * (a1.y - b1.y) - (b2.y - b1.y) * (a1.x - b1.x);
			float ub_t = (a2.x - a1.x) * (a1.y - b1.y) - (a2.y - a1.y) * (a1.x - b1.x);
			float u_b = (b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y);

			if (u_b != 0)
			{
				float ua = ua_t / u_b;
				float ub = ub_t / u_b;

				if (ua > 0 && ub > 0)
				{
					inter.x = a1.x + ua * (a2.x - a1.x);
					inter.y = a1.y + ua * (a2.y - a1.y);
					t = ua;
					return true;
				}
			}

			return false;
		}

		public static bool IntersectLineLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, ref float t, ref Vector2 inter)
		{
			float ua_t = (b2.x - b1.x) * (a1.y - b1.y) - (b2.y - b1.y) * (a1.x - b1.x);
			float ub_t = (a2.x - a1.x) * (a1.y - b1.y) - (a2.y - a1.y) * (a1.x - b1.x);
			float u_b = (b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y);

			if (u_b != 0)
			{
				float ua = ua_t / u_b;
				float ub = ub_t / u_b;

				if (0 <= ua && ua <= 1 && 0 <= ub && ub <= 1)
				{
					inter.x = a1.x + ua * (a2.x - a1.x);
					inter.y = a1.y + ua * (a2.y - a1.y);
					t = ua;
					return true;
				}
			}

			return false;
		}

		public static bool IntersectLineRect(Vector2 s0, Vector2 s1, Rect r, ref Vector2 closestHit)
		{
			Vector2 q = Vector3.zero;
			float t = Mathf.Infinity;

			float minT = t;

			if (IntersectLineLine(s0, s1, new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax), ref t, ref q))
			{
				minT = t;
				closestHit = q;
			}

			if (IntersectLineLine(s0, s1, new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), ref t, ref q))
			{
				if (t < minT)
				{
					minT = t;
					closestHit = q;
				}
			}
			if (IntersectLineLine(s0, s1, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), ref t, ref q))
			{
				if (t < minT)
				{
					minT = t;
					closestHit = q;
				}
			}
			if (IntersectLineLine(s0, s1, new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), ref t, ref q))
			{
				if (t < minT)
				{
					minT = t;
					closestHit = q;
				}
			}

			return (minT < Mathf.Infinity);
		}

		public static bool IntersectRayMesh(Ray ray, Vector3[] vertices, int[] triangles, ref RaycastHit hit)
		{
			bool hitit = false;
			RaycastHit temp = new RaycastHit();
			float dist = Mathf.Infinity;

			if (triangles == null)
				return false;

			for (int i = 0; i < triangles.Length; i += 3)
			{
				if (IntersectRayTriangle(ray, vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]], false, ref temp))
				{
					if (temp.distance < dist)
					{
						dist = temp.distance;
						hit = temp;
						hitit = true;
					}
				}
			}

			return hitit;
		}


		// intersect_RayTriangle(): intersect a ray with a 3D triangle
		//    Input:  a ray R, and 3 vector3 forming a triangle
		//    Output: *I = intersection point (when it exists)
		//    Return: null = no intersection
		//            RaycastHit = intersection

		//  -1 = triangle is degenerate (a segment or point)
		//             0 = disjoint (no intersect)
		//             1 = intersect in unique point I1
		//             2 = are in the same plane
		public static bool IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, bool bidirectional, ref RaycastHit hit)
		{

			Vector3 ab = v1 - v0;
			Vector3 ac = v2 - v0;

			// Compute triangle normal. Can be precalculated or cached if
			// intersecting multiple segments against the same triangle
			Vector3 n = Vector3.Cross(ab, ac);

			// Compute denominator d. If d <= 0, segment is parallel to or points
			// away from triangle, so exit early
			float d = Vector3.Dot(-ray.direction, n);
			if (d <= 0.0f) return false;

			// Compute intersection t value of pq with plane of triangle. A ray
			// intersects iff 0 <= t. Segment intersects iff 0 <= t <= 1. Delay
			// dividing by d until intersection has been found to pierce triangle
			Vector3 ap = ray.origin - v0;
			float t = Vector3.Dot(ap, n);
			if ((t < 0.0f) && (!bidirectional)) return false;
			//if (t > d) return null; // For segment; exclude this code line for a ray test

			// Compute barycentric coordinate components and test if within bounds
			Vector3 e = Vector3.Cross(-ray.direction, ap);
			float v = Vector3.Dot(ac, e);
			if (v < 0.0f || v > d) return false;

			float w = -Vector3.Dot(ab, e);
			if (w < 0.0f || v + w > d) return false;

			// Segment/ray intersects triangle. Perform delayed division and
			// compute the last barycentric coordinate component
			float ood = 1.0f / d;
			t *= ood;
			v *= ood;
			w *= ood;
			float u = 1.0f - v - w;

			hit.point = ray.origin + t * ray.direction;
			hit.distance = t;
			hit.barycentricCoordinate = new Vector3(u, v, w);
			hit.normal = Vector3.Normalize(n);

			return true;
		}


		public static Vector3 ClosestPtRayPoint(Ray r, Vector3 p, out float t)
		{
			// Project c onto ab, computing parameterized position d(t) = a + t*(b - a)
			t = Vector3.Dot(r.direction, p - r.origin); // -> needed for unnormalized rays / Vector3.Dot( r.direction, r.direction );
														// If outside segment, clamp t (and therefore d) to the closest endpoint
			if (t < 0.0f) t = 0.0f;
			// Compute projected position from the clamped t
			return r.origin + r.direction * t;
		}

		// Returns closest point on segment
		// squaredDist = squared distance between the two closest points
		// s = offset along segment
		// t = offset along ray
		public static Vector3 ClosestPtSegmentRay(Vector3 p1, Vector3 q1, Ray ray, out float squaredDist, out float s, out float t, out Vector3 closestRay)
		{
			Vector3 p2 = ray.origin;
			//Vector3 q2 = ray.direction;

			Vector3 d1 = q1 - p1; // Direction vector of segment S1
			Vector3 d2 = ray.direction;//q2 - p2; // Direction vector of segment S2
			Vector3 r = p1 - p2;
			float a = Vector3.Dot(d1, d1); // Squared length of segment S1, always nonnegative
			float e = Vector3.Dot(d2, d2); // Squared length of segment S2, always nonnegative
			float f = Vector3.Dot(d2, r);

			t = 0.0f;

			/*
			// Check if either or both segments degenerate into points
			if (a <= Mathf.Epsilon && e <= Mathf.Epsilon)
			{
				// Both segments degenerate into points
				squaredDist = Vector3.Dot(p1 - p2, p1 - p2);
				s = 0.0f;
				closestRay = p2;
				return p1;
			}
			*/
			if (a <= Mathf.Epsilon)
			{
				// First segment degenerates into a point
				s = 0.0f;
				t = f / e; // s = 0 => t = (b*s + f) / e = f / e
				t = Mathf.Max(t, 0.0f);
				//t = Mathf.Clamp(t, 0.0f, 1.0f);
			}
			else
			{
				float c = Vector3.Dot(d1, r);
				if (e <= Mathf.Epsilon)
				{
					// Second segment degenerates into a point
					t = 0.0f;
					s = Mathf.Clamp(-c / a, 0.0f, 1.0f); // t = 0 => s = (b*t - c) / a = -c / a
				}
				else
				{
					// The general nondegenerate case starts here
					float b = Vector3.Dot(d1, d2);
					float denom = a * e - b * b; // Always nonnegative

					// If segments not parallel, compute closest point on L1 to L2, and
					// clamp to segment S1. Else pick arbitrary s (here 0)
					if (denom != 0.0f)
					{
						s = Mathf.Clamp((b * f - c * e) / denom, 0.0f, 1.0f);
					}
					else
					{
						s = 0.0f;
					}

					// Compute point on L2 closest to S1(s) using
					// t = Dot((P1+D1*s)-P2,D2) / Dot(D2,D2) = (b*s + f) / e
					t = (b * s + f) / e;

					// If t in [0,1] done. Else clamp t, recompute s for the new value
					// of t using s = Dot((P2+D2*t)-P1,D1) / Dot(D1,D1)= (t*b - c) / a
					// and clamp s to [0, 1]
					if (t < 0.0f)
					{
						t = 0.0f;
						s = Mathf.Clamp(-c / a, 0.0f, 1.0f);
					}
					/*
					else if (t > 1.0f) 
					{
						t = 1.0f;
						s = Mathf.Clamp((b - c) / a, 0.0f, 1.0f);
					}
					*/
				}
			}

			Vector3 c1 = p1 + d1 * s;
			Vector3 c2 = p2 + d2 * t;
			squaredDist = Vector3.Dot(c1 - c2, c1 - c2);
			closestRay = c2;
			return c1;
		}

		public static bool IntersectRayCylinder(Ray ray, Vector3 cylA, Vector3 cylB, float cylRadius, ref float t, ref Vector3 inter)
		{
			Vector3 d = cylB - cylA;
			Vector3 m = ray.origin - cylA;
			Vector3 n = ray.direction;
			float md = Vector3.Dot(m, d);
			float nd = Vector3.Dot(n, d);
			float dd = Vector3.Dot(d, d);
			// Test if segment fully outside either endcap of cylinder
			if (md < 0.0f && md + nd < 0.0f) return false; // Segment outside ��p�� side of cylinder
			if (md > dd && md + nd > dd) return false;     // Segment outside ��q�� side of cylinder
			float nn = Vector3.Dot(n, n);
			float mn = Vector3.Dot(m, n);
			float a = dd * nn - nd * nd;
			float k = Vector3.Dot(m, m) - cylRadius * cylRadius;
			float c = dd * k - md * md;
			if (Mathf.Abs(a) < Mathf.Epsilon)
			{
				// Segment runs parallel to cylinder axis
				if (c > 0.0f) return false; // ��a�� and thus the segment lie outside cylinder
											// Now known that segment intersects cylinder; figure out how it intersects
				if (md < 0.0f) t = -mn / nn; // Intersect segment against ��p�� endcap
				else if (md > dd) t = (nd - mn) / nn; // Intersect segment against ��q�� endcap
				else t = 0.0f; // ��a�� lies inside cylinder

				inter = ray.origin + ray.direction * t;

				return true;
			}
			float b = dd * mn - nd * md;
			float discr = b * b - a * c;
			if (discr < 0.0f) return false; // No real roots; no intersection
			t = (-b - Mathf.Sqrt(discr)) / a;
			if (t < 0.0f) return false; // Intersection lies outside segment
			if (md + t * nd < 0.0f)
			{
				// Intersection outside cylinder on ��p�� side
				if (nd <= 0.0f) return false; // Segment pointing away from endcap
				t = -md / nd;

				inter = ray.origin + ray.direction * t;

				// Keep intersection if Dot(S(t) - p, S(t) - p) <= r^2
				return k + 2 * t * (mn + t * nn) <= 0.0f;
			}
			else if (md + t * nd > dd)
			{
				// Intersection outside cylinder on ��q�� side
				if (nd >= 0.0f) return false; // Segment pointing away from endcap
				t = (dd - md) / nd;

				inter = ray.origin + ray.direction * t;

				// Keep intersection if Dot(S(t) - q, S(t) - q) <= r^2
				return k + dd - 2 * md + t * (2 * (mn - nd) + t * nn) <= 0.0f;
			}

			// Segment intersects cylinder between the end-caps; t is correct
			inter = ray.origin + ray.direction * t;

			return true;
		}

		public static bool IntersectRaySegment(Ray ray, Vector3 segA, Vector3 segB, float segRadius, ref float t, ref Vector3 q)
		{
			float sqDist = 0.0f;
			float s = 0.0f;
			ClosestPtSegmentRay(segA, segB, ray, out sqDist, out s, out t, out q);
			if (sqDist < segRadius * segRadius)
			{
				t -= (segRadius - Mathf.Sqrt(sqDist));
				return true;
			}
			return false;
		}

		public static bool IntersectRaySphere(Ray ray, Vector3 sphereOrigin, float sphereRadius, ref float t, ref Vector3 q)
		{
			Vector3 m = ray.origin - sphereOrigin;
			float b = Vector3.Dot(m, ray.direction);
			float c = Vector3.Dot(m, m) - (sphereRadius * sphereRadius);
			// Exit if r�s origin outside s (c > 0)and r pointing away from s (b > 0)
			if ((c > 0.0f) && (b > 0.0f)) return false;
			float discr = (b * b) - c;

			// A negative discriminant corresponds to ray missing sphere
			if (discr < 0.0f) return false;

			// Ray now found to intersect sphere, compute smallest t value of intersection
			t = -b - Mathf.Sqrt(discr);

			// If t is negative, ray started inside sphere so clamp t to zero
			if (t < 0.0f) t = 0.0f;
			q = ray.origin + t * ray.direction;
			return true;
		}

		public static bool IntersectSphereSphere(Vector3 aOrigin, float aRadius, Vector3 bOrigin, float bRadius, ref Vector3 q)
		{
			Vector3 delta = bOrigin - aOrigin;
			float dist = delta.magnitude;

			// Closest intersection point
			q = aOrigin + delta.normalized * (aRadius + bRadius);

			return (dist < aRadius + bRadius);
		}

		// Intersect ray R(t) = p + t*d against AABB a. When intersecting,
		// return intersection distance tmin and point q of intersection
		static public bool IntersectRayBounds(Ray ray, Bounds a, ref float tmin, ref Vector3 q)
		{
			tmin = 0.0f;          // set to -FLT_MAX to get first hit on line
			float tmax = Mathf.Infinity; // set to max distance ray can travel (for segment)

			// For all three slabs
			for (int i = 0; i < 3; i++)
			{
				if (Mathf.Abs(ray.direction[i]) < Mathf.Epsilon)
				{
					// Ray is parallel to slab. No hit if origin not within slab
					if (ray.origin[i] < a.min[i] || ray.origin[i] > a.max[i]) return false;
				}
				else
				{
					// Compute intersection t value of ray with near and far plane of slab
					float ood = 1.0f / ray.direction[i];
					float t1 = (a.min[i] - ray.origin[i]) * ood;
					float t2 = (a.max[i] - ray.origin[i]) * ood;
					// Make t1 be intersection with near plane, t2 with far plane
					if (t1 > t2)
					{
						float temp = t1;
						t1 = t2;
						t2 = temp;
					};
					// Compute the intersection of slab intersections intervals
					tmin = Mathf.Max(t1, tmin);
					tmax = Mathf.Min(t2, tmax);
					// Exit with no collision as soon as slab intersection becomes empty
					if (tmin > tmax) return false;
				}
			}
			// Ray intersects all 3 slabs. Return point (q) and intersection t value (tmin) 
			q = ray.origin + ray.direction * tmin;
			return true;
		}

		// Closest point
		public static bool ClosestPtRaySphere(Ray ray, Vector3 sphereOrigin, float sphereRadius, ref float t, ref Vector3 q)
		{
			Vector3 m = ray.origin - sphereOrigin;
			float b = Vector3.Dot(m, ray.direction);
			float c = Vector3.Dot(m, m) - (sphereRadius * sphereRadius);
			// Exit if r�s origin outside s (c > 0)and r pointing away from s (b > 0)
			if ((c > 0.0f) && (b > 0.0f))
			{
				// ray origin is closest
				t = 0.0f;
				q = ray.origin;
				return true;
			}

			float discr = (b * b) - c;

			// A negative discriminant corresponds to ray missing sphere
			if (discr < 0.0f)
			{
				discr = 0.0f;
			}

			// Ray now found to intersect sphere, compute smallest t value of intersection
			t = -b - Mathf.Sqrt(discr);

			// If t is negative, ray started inside sphere so clamp t to zero
			if (t < 0.0f) t = 0.0f;
			q = ray.origin + t * ray.direction;
			return true;
		}

		public static Vector3 ClosestPtPointPath(Vector3 c, Vector3[] path, ref float t)
		{
			if (path == null || path.Length == 0)
			{
				t = 0.0f;
				return c;
			}

			float totalLength = 0.0f;
			float closestLength = 0.0f;
			Vector3 closestPoint = path[0];
			float closestDist = Vector3.Distance(c, path[0]);

			for (int i = 1; i < path.Length; i++)
			{
				float thisLength = Vector3.Distance(path[i - 1], path[i]);
				float time = 0.0f;
				Vector3 point = ClosestPtPointSegment(c, path[i - 1], path[i], ref time);
				float dist = Vector3.Distance(c, point);

				if (dist < closestDist)
				{
					closestLength = totalLength + (time * thisLength);
					closestPoint = point;
					closestDist = dist;
				}

				totalLength += thisLength;
			}

			t = closestLength / Mathf.Max(totalLength, 0.001f);
			return closestPoint;
		}

		public static Vector3 ClosestPtPointPath(Vector3 c, Transform[] path, ref float t)
		{
			Vector3[] path2 = new Vector3[path.Length];
			for (int i = 0; i < path.Length; i++)
			{
				path2[i] = path[i].position;
			}
			return ClosestPtPointPath(c, path2, ref t);
		}

		public static Vector3 ClosestPtRayPath(Ray ray, Vector3[] path, ref float t)
		{
			if (path == null || path.Length == 0)
			{
				t = 0.0f;
				return ray.origin;
			}

			float totalLength = 0.0f;
			float closestLength = 0.0f;
			Vector3 closestPoint = path[0];
			float closestDist = 10000000000.0f;

			for (int i = 1; i < path.Length; i++)
			{
				float thisLength = Vector3.Distance(path[i - 1], path[i]);
				float time = 0.0f;
				float sime = 0.0f;
				float dist = 0.0f;
				Vector3 closeRay;
				Vector3 point = ClosestPtSegmentRay(path[i - 1], path[i], ray, out dist, out sime, out time, out closeRay);

				if (dist < closestDist)
				{
					closestLength = totalLength + (sime * thisLength);
					closestPoint = point;
					closestDist = dist;
				}

				totalLength += thisLength;
			}

			t = closestLength / Mathf.Max(totalLength, 0.001f);
			return closestPoint;
		}

		public static Vector3 ClosestPtPointSegment(Vector3 c, Vector3 a, Vector3 b, ref float t)
		{
			Vector3 ab = b - a;
			// Project c onto ab, but deferring divide by Dot(ab, ab)
			t = Vector3.Dot(c - a, ab);
			if (t <= 0.0f)
			{
				// c projects outside the [a,b] interval, on the a side; clamp to a
				t = 0.0f;
				return a;
			}
			else
			{
				float denom = Vector3.Dot(ab, ab); // Always nonnegative since denom = ||ab||^2
				if (t >= denom)
				{
					// c projects outside the [a,b] interval, on the b side; clamp to b
					t = 1.0f;
					return b;
				}
				else
				{
					// c projects inside the [a,b] interval; must do deferred divide now
					t = t / denom;
					return (a + (ab * t));
				}
			}
		}

		public static Vector3 ClosestPtPointRay(Vector3 c, Ray r, ref float t)
		{
			// Project c onto ab, but deferring divide by Dot(ab, ab)
			t = Vector3.Dot(c - r.origin, r.direction);
			float denom = Vector3.Dot(r.direction, r.direction); // Always nonnegative since denom = ||ab||^2
			t = t / denom;
			return (r.origin + (r.direction * t));
		}

		public static Vector3 ClosestPtPointLine(Vector3 c, Vector3 a, Vector3 b, ref float t)
		{
			Vector3 ab = b - a;
			// Project c onto ab, but deferring divide by Dot(ab, ab)
			t = Vector3.Dot(c - a, ab);
			float denom = Vector3.Dot(ab, ab); // Always nonnegative since denom = ||ab||^2
			t = t / denom;
			return (a + (ab * t));
		}

		public static float DistanceToPlane(Vector3 q, Plane p)
		{
			return (p.distance + Vector3.Dot(p.normal, q));
		}

		public static Vector3 ClosestPtPointPlane(Vector3 q, Plane p)
		{
			float t = p.distance + Vector3.Dot(p.normal, q);
			return q - p.normal * t;
		}

		public static Vector3 ClosestPtPointBounds(Vector3 q, Bounds b)
		{
			q.x = Mathf.Clamp(q.x, b.min.x, b.max.x);
			q.y = Mathf.Clamp(q.y, b.min.y, b.max.y);
			q.z = Mathf.Clamp(q.z, b.min.z, b.max.z);
			return q;
		}

		public static Vector2 ClosestPtPointRect(Vector2 q, Rect b, bool edgeOnly = false)
		{
			if (edgeOnly)
			{

				Vector2 d = q - b.center;
				Vector2 q2 = Vector2.zero;
				q2.x = d.x < 0 ? b.xMin : b.xMax;
				q2.y = d.y < 0 ? b.yMin : b.yMax;
				if (Mathf.Abs(q.x - q2.x) < Mathf.Abs(q.y - q2.y))
				{
					q.x = q2.x;
					q.y = Mathf.Clamp(q.y, b.yMin, b.yMax);
				}
				else
				{
					q.x = Mathf.Clamp(q.x, b.xMin, b.xMax);
					q.y = q2.y;
				}
			}
			else
			{
				q.x = Mathf.Clamp(q.x, b.xMin, b.xMax);
				q.y = Mathf.Clamp(q.y, b.yMin, b.yMax);
			}
			return q;
		}

		// For a plane passing through 0,0,0 we can simplify it
		public static Vector3 ClosestPtPointPlane(Vector3 q, Vector3 pNormal)
		{
			return q - pNormal * Vector3.Dot(pNormal, q);
		}

		public static Vector3 ClosestPtPointQuad(Vector3 p, Vector3 a, Vector3 b, Vector3 c, Vector3 d, ref bool inside)
		{
			Vector3 closest = ClosestPtPointTriangle(p, a, b, c, ref inside);
			if (inside)
			{
				return closest;
			}
			return ClosestPtPointTriangle(p, a, c, d, ref inside);
		}

		public static Vector3 ClosestPtPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c, ref bool inside)
		{
			inside = false;

			// Check if P in vertex region outside A
			Vector3 ab = b - a;
			Vector3 ac = c - a;
			Vector3 ap = p - a;
			float d1 = Vector3.Dot(ab, ap);
			float d2 = Vector3.Dot(ac, ap);
			if (d1 <= 0.0f && d2 <= 0.0f) return a; // barycentric coordinates (1,0,0)

			// Check if P in vertex region outside B
			Vector3 bp = p - b;
			float d3 = Vector3.Dot(ab, bp);
			float d4 = Vector3.Dot(ac, bp);
			if (d3 >= 0.0f && d4 <= d3) return b; // barycentric coordinates (0,1,0)

			float v, w;

			// Check if P in edge region of AB, if so return projection of P onto AB
			float vc = d1 * d4 - d3 * d2;
			if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
			{
				v = d1 / (d1 - d3);
				return a + v * ab; // barycentric coordinates (1-v,v,0)
			}

			// Check if P in vertex region outside C
			Vector3 cp = p - c;
			float d5 = Vector3.Dot(ab, cp);
			float d6 = Vector3.Dot(ac, cp);
			if (d6 >= 0.0f && d5 <= d6) return c; // barycentric coordinates (0,0,1)

			// Check if P in edge region of AC, if so return projection of P onto AC
			float vb = d5 * d2 - d1 * d6;
			if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
			{
				w = d2 / (d2 - d6);
				return a + w * ac; // barycentric coordinates (1-w,0,w)
			}

			// Check if P in edge region of BC, if so return projection of P onto BC
			float va = d3 * d6 - d5 * d4;
			if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
			{
				w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
				return b + w * (c - b); // barycentric coordinates (0,1-w,w)
			}

			// P inside face region. Compute Q through its barycentric coordinates (u,v,w)
			float denom = 1.0f / (va + vb + vc);
			v = vb * denom;
			w = vc * denom;

			inside = true;

			return a + ab * v + ac * w; // = u*a + v*b + w*c, u = va * denom = 1.0f - v - w
		}

		public static Color InterpolateBilinear(Color v0, Color v1, Color v2, Color v3, float s, float t)
		{
			Color i = Color.Lerp(v0, v1, s);
			Color j = Color.Lerp(v3, v2, s);
			return Color.Lerp(i, j, t);
		}


		public static Vector4 InterpolateBilinear(Vector4 v0, Vector4 v1, Vector4 v2, Vector4 v3, float s, float t)
		{
			Vector4 i = Vector4.Lerp(v0, v1, s);
			Vector4 j = Vector4.Lerp(v3, v2, s);
			return Vector4.Lerp(i, j, t);
		}

		public static Vector3 InterpolateBilinear(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, float s, float t)
		{
			Vector3 i = Vector3.Lerp(v0, v1, s);
			Vector3 j = Vector3.Lerp(v3, v2, s);
			return Vector3.Lerp(i, j, t);
		}

		public static Vector2 InterpolateBilinear(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, float s, float t)
		{
			Vector2 i = Vector2.Lerp(v0, v1, s);
			Vector2 j = Vector2.Lerp(v3, v2, s);
			return Vector2.Lerp(i, j, t);
		}

		public static float InterpolateBilinear(float v0, float v1, float v2, float v3, float s, float t)
		{
			float i = Mathf.Lerp(v0, v1, s);
			float j = Mathf.Lerp(v3, v2, s);
			return Mathf.Lerp(i, j, t);
		}

		public static float PerlinNoise(float x, float y, int octaves)
		{
			float v = 0.0f;
			for (int i = 0; i < octaves; i++)
			{
				v += Mathf.PerlinNoise(x, y);
				x /= 2;
				y /= 2;
			}
			return v / (octaves + 1);
		}

		public static Vector3 CatmullRom(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, float t)
		{
			float t2 = t * t;
			float t3 = t2 * t;
			Vector3 q = 0.5f * ((2.0f * v1) +
						  (-v0 + v2) * t +
						(2.0f * v0 - 5.0f * v1 + 4.0f * v2 - v3) * t2 +
						(-v0 + 3.0f * v1 - 3.0f * v2 + v3) * t3);

			return q;
		}

		public static Matrix4x4 CarmullRomPrecalc(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
		{
			Matrix4x4 precalc = Matrix4x4.identity;

			Vector3 t1 = (2.0f * v1) + (-v0 + v2);
			Vector3 t2 = (2.0f * v0 - 5.0f * v1 + 4.0f * v2 - v3);
			Vector3 t3 = (-v0 + 3.0f * v1 - 3.0f * v2 + v3);

			precalc.SetRow(0, new Vector4(t1.x, t1.y, t1.z, 0.0f));
			precalc.SetRow(1, new Vector4(t2.x, t2.y, t2.z, 0.0f));
			precalc.SetRow(2, new Vector4(t3.x, t3.y, t3.z, 0.0f));
			precalc.SetRow(3, new Vector4(0, 0, 0, 0.5f));

			return precalc;
		}

		public static Vector3 CatmullRom(Matrix4x4 precalc, float t)
		{
			return precalc.MultiplyPoint(new Vector3(t, t * t, t * t * t));
		}


		// Compute barycentric coordinates (u, v, w) for 
		// point p with respect to triangle (a, b, c)
		public static void Barycentric(Vector3 a, Vector3 b, Vector3 c, Vector3 p, out float u, out float v, out float w)
		{
			Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
			float d00 = Vector3.Dot(v0, v0);
			float d01 = Vector3.Dot(v0, v1);
			float d11 = Vector3.Dot(v1, v1);
			float d20 = Vector3.Dot(v2, v0);
			float d21 = Vector3.Dot(v2, v1);
			float denom = d00 * d11 - d01 * d01;
			v = (d11 * d20 - d01 * d21) / denom;
			w = (d00 * d21 - d01 * d20) / denom;
			u = 1.0f - v - w;
		}

		// Test if point p is contained in triangle (a, b, c)
		public static bool TestPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
		{
			float u, v, w;
			Barycentric(a, b, c, p, out u, out v, out w);
			return (v >= 0.0f && w >= 0.0f && (v + w) <= 1.0f);
		}

		public static float Cross(Vector2 a, Vector2 b)
		{
			return (a.x * b.y) - (a.y * b.x);
		}

		public static Vector2 ClosestPtPointPoly(Vector2 a, Vector2[] poly, bool testInside)
		{
			if (testInside && TestPointPoly(poly, a))
			{
				return a;
			}

			Vector2 c = a;
			float bestT = float.MaxValue;
			float t = 0;

			int j = poly.Length - 1;
			for (int i = 0; i < poly.Length; i++)
			{
				Vector2 b = ClosestPtPointSegment(a, poly[i], poly[j], ref t);
				float d = Vector2.Distance(b, a);
				if (d < bestT)
				{
					bestT = d;
					c = b;
				}
				j = i;
			}

			return c;
		}

		public static Vector3 ClosestPtPointPoly(Vector3 a, List<Vector3> poly)
		{
			//Vector2 inter
			Vector3 c = a;
			float bestT = float.MaxValue;
			float t = 0;

			int j = poly.Count - 1;
			for (int i = 0; i < poly.Count; i++)
			{
				Vector3 b = ClosestPtPointSegment(a, poly[i], poly[j], ref t);
				float d = Vector3.Distance(b, a);
				if (d < bestT)
				{
					bestT = d;
					c = b;
				}
				j = i;
			}

			return c;
		}

		public static Vector3 ClosestPtPointPoly(Vector3 a, Vector3[] poly)
		{
			//Vector2 inter
			Vector3 c = a;
			float bestT = float.MaxValue;
			float t = 0;

			int j = poly.Length - 1;
			for (int i = 0; i < poly.Length; i++)
			{
				Vector3 b = ClosestPtPointSegment(a, poly[i], poly[j], ref t);
				float d = Vector3.Distance(b, a);
				if (d < bestT)
				{
					bestT = d;
					c = b;
				}
				j = i;
			}

			return c;
		}

		public static bool IntersectSegmentPoly(Vector3 a, Vector3 b, List<Vector3> poly, ref Vector3 hit)
		{
			Vector2 inter = Vector2.zero;
			float t = 0;

			float bestT = 1.0f;

			bool didHit = false;

			int j = poly.Count - 1;
			for (int i = 0; i < poly.Count; i++)
			{
				if (IntersectRayLine(a, b, poly[i], poly[j], ref t, ref inter))
				{
					if (t > 0 && t < bestT)
					{
						hit = inter;
						bestT = t;
						didHit = true;
					}
				}
				j = i;
			}

			return didHit;
		}

		public static bool IntersectSegmentPoly(Vector2 a1, Vector2 a2, Vector2[] poly, ref Vector2 inter, ref float t, float mint = 0.0f)
		{
			bool didHit = false;

			t = 1;

			Vector2 d = a2 - a1;

			int j = poly.Length - 1;
			for (int i = 0; i < poly.Length; i++)
			{
				//float ua_t = (b2.x - b1.x) * (a1.y - b1.y) - (b2.y - b1.y) * (a1.x - b1.x);
				//float ub_t = (a2.x - a1.x) * (a1.y - b1.y) - (a2.y - a1.y) * (a1.x - b1.x);
				//float u_b  = (b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y);

				float u_b = (poly[j].y - poly[i].y) * (d.x) - (poly[j].x - poly[i].x) * (d.y);

				if (u_b != 0)
				{
					float ua_t = (poly[j].x - poly[i].x) * (a1.y - poly[i].y) - (poly[j].y - poly[i].y) * (a1.x - poly[i].x);
					float ub_t = (d.x) * (a1.y - poly[i].y) - (d.y) * (a1.x - poly[i].x);

					float ua = ua_t / u_b;
					float ub = ub_t / u_b;

					if (mint < ua && ua < t && 0 <= ub && ub <= 1)
					{
						inter.x = a1.x + ua * d.x;
						inter.y = a1.y + ua * d.y;
						t = ua;
						didHit = true;
					}
				}
				j = i;
			}
			return didHit;
		}

		public static bool TestPointPoly(Vector2[] poly, Vector2 b1)
		{
			int inters = 0;

			int j = poly.Length - 1;
			for (int i = 0; i < poly.Length; i++)
			{
				float u_b = -(poly[j].y - poly[i].y);

				if (u_b != 0)
				{
					float ua_t = (poly[i].y - b1.y);
					float ub_t = (poly[j].x - poly[i].x) * (poly[i].y - b1.y) - (poly[j].y - poly[i].y) * (poly[i].x - b1.x);

					float ua = ua_t / u_b;
					float ub = ub_t / u_b;

					if (0 <= ua && ua <= 1 && 0 <= ub)
					{
						inters++;
					}
				}

				j = i;
			}
			return ((inters % 2) == 1);
		}

		public static bool TestPointPoly(List<Vector2> poly, Vector2 b1)
		{
			Vector2 a2 = poly[poly.Count - 1];

			int inters = 0;

			foreach (Vector2 a1 in poly)
			{
				float ua_t = (a1.y - b1.y);
				float ub_t = (a2.x - a1.x) * (a1.y - b1.y) - (a2.y - a1.y) * (a1.x - b1.x);
				float u_b = -(a2.y - a1.y);

				if (u_b != 0)
				{
					float ua = ua_t / u_b;
					float ub = ub_t / u_b;

					if (0 <= ua && ua <= 1 && 0 <= ub)
					{
						inters++;
					}
				}

				a2 = a1;
			}
			return ((inters % 2) == 1);
		}

		public static Vector3 ConvexPolygonCenter(List<Vector3> polygon)
		{
			if (polygon.Count == 0)
				return Vector3.zero;

			Vector3 center = Vector3.zero;
			foreach (Vector3 v in polygon)
			{
				center += v;
			}
			center /= polygon.Count;
			return center;
		}


		// Splits first polygon in list into 1 or more non-overlapping polygons..
		public static void SplitPolygons(List<List<Vector3>> polygons)
		{
			List<Vector3> temp = polygons[0];

			//foreach (List<Vector3> temp in polygons)
			{
				// Split self
				float t = 0;
				Vector2 inter = Vector2.zero;
				//			int preCount = temp.Count;
				//int intersectionCount = 0;
				for (int i = 0; i < temp.Count; i++)
				{
					Vector3 a0 = temp[i];
					Vector3 a1 = temp[(i + 1) % temp.Count];

					for (int j = i + 1; j < temp.Count; j++)
					{
						int k = (j + 1) % temp.Count;

						Vector3 b0 = temp[j];
						Vector3 b1 = temp[k];

						if (MathUtils.IntersectLineLine(a0, a1, b0, b1, ref t, ref inter))
						{
							if (float.IsNaN(t) || float.IsInfinity(t))
							{
								Debug.LogError("break");
								return;
							}
							if (t > 0 && t < 1)
							{
								List<Vector3> tempA = new List<Vector3>(temp.GetRange(i + 1, temp.Count - i - 1));
								tempA.Insert(0, inter);
								polygons.Add(tempA);

								temp.RemoveRange(i + 1, temp.Count - i - 1);
								temp.Insert(0, inter);

								temp = tempA;
								i = -1;
								//intersectionCount++;
								break;
							}
						}
					}
				}
			}
		}

		// Given n-gon specified by points v[], compute a good representative plane p
		public static Plane NewellPlane(List<Vector3> v)
		{
			// Compute normal as being proportional to projected areas of polygon onto the yz,
			// xz, and xy planes. Also compute centroid as representative point on the plane
			Vector3 centroid = Vector3.zero;
			Vector3 normal = Vector3.zero;

			int n = v.Count;

			for (int i = n - 1, j = 0; j < n; i = j, j++)
			{
				normal.x += (v[i].y - v[j].y) * (v[i].z + v[j].z); // projection on yz
				normal.y += (v[i].z - v[j].z) * (v[i].x + v[j].x); // projection on xz
				normal.z += (v[i].x - v[j].x) * (v[i].y + v[j].y); // projection on xy
				centroid += v[j];
			}

			// Normalize normal and fill in the plane equation fields
			return new Plane(normal.normalized, centroid / n);
		}

		public static float QuadArea(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
		{
			Vector3 ca = c - a;
			return (Vector3.Cross(b - a, ca).magnitude + Vector3.Cross(d - a, ca).magnitude) * 0.5f;
		}

		public static float PolyArea(List<Vector2> poly)
		{
			float area = 0;
			if (poly.Count > 2)
			{
				Vector2 a = poly[0];
				Vector2 b = poly[1];
				foreach (Vector2 c in poly)
				{
					area += Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
					b = c;
				}
			}
			return area * 0.5f;
		}

		public static float PolyArea(List<Vector3> poly)
		{
			float area = 0;
			if (poly.Count > 2)
			{
				Vector3 a = poly[0];
				Vector3 b = poly[1];
				foreach (Vector3 c in poly)
				{
					area += Vector3.Cross(b - a, c - a).magnitude;
					b = c;
				}
			}
			return area * 0.5f;
		}

		public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
		{
			return Vector3.Cross(b - a, c - a).magnitude * 0.5f;
		}

		public static Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
		{
			return Vector3.Cross(b - a, c - a).normalized;
		}

		public static Vector3 TriangleNormalUnnormalized(Vector3 a, Vector3 b, Vector3 c)
		{
			return Vector3.Cross(b - a, c - a);
		}

		public static Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
		{
			float u = Random.value;
			float v = Random.value;
			if (u + v > 1.0f)
				v = 1.0f - u;

			return a + (b - a) * u + (c - a) * v;
		}

		public static Vector3 QuadNormal(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
		{
			Vector3 edge1 = b - a;
			Vector3 edge2 = c - a;
			Vector3 edge3 = d - a;

			return (Vector3.Cross(edge1, edge2) + Vector3.Cross(edge2, edge3)).normalized;
		}

		public static Vector3 Round(Vector3 a)
		{
			return new Vector3(Mathf.Round(a.x), Mathf.Round(a.y), Mathf.Round(a.z));
		}

	}

}