#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace PaintIn3D
{
	/// <summary>This class allows you to raycast a mesh in-editor without any colliders.</summary>
	public static class P3dWindowIntersect
	{
		private static MethodInfo method_IntersectRayMesh;

		static P3dWindowIntersect() 
		{
			method_IntersectRayMesh = typeof(HandleUtility).GetMethod("IntersectRayMesh", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public static bool IntersectRayMesh(Ray ray, MeshFilter meshFilter, out RaycastHit hit)
		{
			return IntersectRayMesh(ray, meshFilter.mesh, meshFilter.transform.localToWorldMatrix, out hit);
		}

		public static bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit)
		{
			var parameters = new object[] { ray, mesh, matrix, null };
			var result     = (bool)method_IntersectRayMesh.Invoke(null, parameters);

			hit = (RaycastHit)parameters[3];

			return result;
		}
	}
}
#endif