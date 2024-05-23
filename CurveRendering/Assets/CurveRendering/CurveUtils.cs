
using UnityEditor;
using UnityEngine;

namespace CurveRendering
{
    using Point = Vector3;
    public static class CurveUtils
    {
        private static readonly Point s_InvalidPoint = new(float.NaN, float.NaN, float.NaN);

        public static bool IsValid(this Point vector3)
        {
            return !(float.IsNaN(vector3.x) || float.IsNaN(vector3.y) || float.IsNaN(vector3.z));
        }

        public static Point GetMouseWorldPosition(LayerMask layerMask, Vector2 mousePositionScreen,
            float expandValue = 0.0f)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePositionScreen);
            if (Physics.Raycast(ray, out var hit, float.MaxValue, layerMask))
            {
                return hit.point + hit.normal * expandValue;
            }

            return s_InvalidPoint;
        }

        public static Point GetMouseLocalPosition(LayerMask layerMask, Vector2 mousePositionScreen,
            Transform localTransform, float expandValue = 0.0f)
        {
            var position = GetMouseWorldPosition(layerMask, mousePositionScreen, expandValue);
            if (position.IsValid())
            {
                return localTransform.InverseTransformPoint(position);
            }

            return s_InvalidPoint;
        }
        
        private static readonly float k_Step = 0.1f;

        public static int EvalStepCount(float maxDistance, float smoothness)
        {
            if (smoothness <= 0)
            {
                return 1;
            }

            if (maxDistance <= k_Step)
            {
                return 1;
            }
            
            return Mathf.CeilToInt(maxDistance / Mathf.Lerp(maxDistance, k_Step, smoothness));
        }
        
        public static readonly int k_CatmullRomPointCountLimit = 4;
        private static readonly Matrix4x4 k_CatmullRomCoefficient = new (
            new Vector4(0, 1, 0, 0),
            new Vector4(-1/2f, 0, 1/2f, 0),
            new Vector4(1, -5/2f, 2, -1/2f),
            new Vector4(-1/2f,3/2f,-3/2f,1/2f));

        public static Matrix4x4 BuildCatmullRomGeometry(Vector3 g1, Vector3 g2, Vector3 g3, Vector4 g4)
        {
            return new Matrix4x4(
                new Vector4(g1.x, g1.y, g1.z, 0f),
                new Vector4(g2.x, g2.y, g2.z, 0f),
                new Vector4(g3.x, g3.y, g3.z, 0f),
                new Vector4(g4.x, g4.y, g4.z, 0f)
                );
        }
        
        public static Vector3 EvalCatmullRomSplines(float t, Matrix4x4 geometry)
        {
            var t2 = t * t;
            var tVector = new Vector4(1, t, t2, t2 * t);
            tVector = k_CatmullRomCoefficient * tVector;
            return geometry * tVector;
        }

        public static Vector3 EvalCatmullRomSplines(float t, Vector3 g1, Vector3 g2, Vector3 g3, Vector4 g4)
        {
            return EvalCatmullRomSplines(
                t,
                BuildCatmullRomGeometry(g1, g2, g3, g4)
                );
        }
    }
}
