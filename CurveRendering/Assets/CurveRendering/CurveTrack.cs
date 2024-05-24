using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;

namespace CurveRendering
{
    [ExecuteInEditMode]
    public class CurveTrack : MonoBehaviour
    {
        public class Segment
        {
            public int startIndex;
            public int stepCount;
            public int totalStepCount;
        }

        public GameObject animatedTarget;
        [Min(1.0f)]public float totalTime;
        private float m_CurrentTime = 0;

        private List<Segment> m_Segments = new();
        private int m_TotalStepCount = 0;
        
        public enum CurveType
        {
            CatmullRomSplines
        }

#if UNITY_EDITOR
        public LayerMask rayCastLayer;
        public float hitPointExpand;
        public float pointRadius = 0.5f;

        // [SerializeField]
        private List<Vector3> curvePoints = new();
        
        public class KeyMap
        {
            public List<KeyCode> keyCodes = new ();

            public void Press(KeyCode code)
            {
                if (keyCodes.IndexOf(code) < 0)
                {
                    keyCodes.Add(code);
                }
            }

            public void Release(KeyCode code)
            {
                while (keyCodes.IndexOf(code) >=0)
                {
                    keyCodes.Remove(code);
                }
            }

            public bool IsMatch(KeyCode keyCode0, KeyCode keyCode1, KeyCode keyCode2)
            {
                return keyCodes.Count > 0
                       && keyCodes.IndexOf(keyCode0) >= 0
                       && keyCodes.IndexOf(keyCode1) >= 0
                       && keyCodes.IndexOf(keyCode2) >= 0;
            }
            
            public bool IsMatch(KeyCode keyCode0, KeyCode keyCode1)
            {
                return keyCodes.Count > 0
                       && keyCodes.IndexOf(keyCode0) >= 0  
                       && keyCodes.IndexOf(keyCode1) >= 0;
            }
            
            public bool IsMatch(KeyCode keyCode0)
            {
                return keyCodes.Count > 0
                       && keyCodes.IndexOf(keyCode0) >= 0;
            }
        }

        private KeyMap m_CurrentKeyMap = new();
#endif

        public CurveType type = CurveType.CatmullRomSplines;
        private CurveType m_OldType = CurveType.CatmullRomSplines;
        public List<Vector3> points = new();
        [Range(0, 1)] public float smoothness = 0f;
        private float m_OldSmoothness = 0f;

        private void Start()
        {
            RefreshSegments();
        }

        public void SetPoint(Vector3 position)
        {
            points[m_CurrentSelectedIndex] = position;
            EvalCurvePoints();
            RefreshSegments();
        }

        private void RefreshSegments()
        {
            switch (type)
            {
                case CurveType.CatmullRomSplines:
                    InitCatmullRomSegments();
                    break;
            }
        }

        private void Animate()
        {
            if (animatedTarget == null)
            {
                return;
            }

            if (m_Segments.Count <= 0)
            {
                return;
            }

            if (m_TotalStepCount <= 0)
            {
                return;
            }

            m_CurrentTime += Time.deltaTime;
            m_CurrentTime = m_CurrentTime % totalTime;

            var progress = m_CurrentTime / totalTime;
            var currentSteps = progress * m_TotalStepCount;
            var currentTotal = 0;
            int index = 0;
            for (int i = 0; i < m_Segments.Count; i++)
            {
                var segment = m_Segments[i];
                if (currentSteps < segment.totalStepCount)
                {
                    currentTotal = segment.stepCount;
                    index = segment.startIndex;
                    if (i > 0)
                    {
                        currentSteps -= m_Segments[i - 1].totalStepCount;
                    }
                    break;
                }
            }

            if (index > 0 && currentTotal > 0)
            {
                var position = CurveUtils.EvalCatmullRomSplines(currentSteps / currentTotal, points[index - 1],
                    points[index], points[index + 1], points[index + 2]);
                animatedTarget.transform.position = position;
            }
        }

        private void InitCatmullRomSegments()
        {
            if (points.Count < CurveUtils.k_CatmullRomPointCountLimit)
            {
                return;
            }

            m_Segments.Clear();
            m_TotalStepCount = 0;
            for (int i = 1; i < points.Count - 2; ++i)
            {
                var startPoint = points[i];
                var endPoint = points[i + 1];
                var distance = Vector3.Distance(startPoint, endPoint);
                var stepCount = CurveUtils.EvalStepCount(distance, smoothness);
                m_TotalStepCount += stepCount;
                m_Segments.Add(new Segment()
                {
                    startIndex = i,
                    stepCount = stepCount,
                    totalStepCount = m_TotalStepCount
                });
            }
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        private void OnDrawGizmosSelected()
        {
            var originMatrix = Gizmos.matrix;
            var originColor = Gizmos.color;
            DoDrawGizmosSelected();
            Gizmos.matrix = originMatrix;
            Gizmos.color = originColor;
        }

        private void Update()
        {
            if (m_OldType != type || m_OldSmoothness != smoothness)
            {
                m_OldType = type;
                m_OldSmoothness = smoothness;
                EvalCurvePoints();
                RefreshSegments();
            }
            
            Animate();
        }

        public void EvalCurvePoints()
        {
            curvePoints.Clear();
            switch (type)
            {
                case CurveType.CatmullRomSplines:
                    EvalCatmullRomSplinesPoints();
                    break;
            }
        }

        private void EvalCatmullRomSplinesPoints()
        {
            if (points.Count < CurveUtils.k_CatmullRomPointCountLimit)
            {
                return;
            }
            
            for (int i = 1; i < points.Count - 2; ++i)
            {
                var startPoint = points[i];
                var endPoint = points[i + 1];
                var geometry =
                    CurveUtils.BuildCatmullRomGeometry(points[i - 1], startPoint, endPoint, points[i + 2]);
                var distance = Vector3.Distance(startPoint, endPoint);
                var stepCount = CurveUtils.EvalStepCount(distance, smoothness);
                for (float step = 0; step <= stepCount; ++step)
                {
                    float t = step / stepCount;
                    var curvePoint = CurveUtils.EvalCatmullRomSplines(t, geometry);
                    curvePoints.Add(curvePoint);
                }
            }
        }

        private void DoDrawGizmosSelected()
        {
            // Draw Points
            {
                Gizmos.matrix = Matrix4x4.identity;
                
                for(int i = 0; i < points.Count; ++i)
                {
                    var color = i > 0 && i < points.Count - 1 ? Color.yellow : Color.red;
                    Gizmos.color = color;
                    Gizmos.DrawSphere(points[i], pointRadius);
                }
            }
            
            // Draw Curve
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = Color.green;
                for (int i = 0; i < curvePoints.Count - 1; ++i)
                {
                    Gizmos.DrawLine(curvePoints[i], curvePoints[i + 1]);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (m_CurrentSelectedIndex >= 0)
            {
                EditorGUI.BeginChangeCheck();
                var newPosition = Handles.PositionHandle(points[m_CurrentSelectedIndex], Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    points[m_CurrentSelectedIndex] = newPosition;
                }
            }
        }

        private int m_CurrentSelectedIndex = -1;

        public Vector3 CurrentSelectedPoint
        {
            get
            {
                if (m_CurrentSelectedIndex < 0)
                {
                    return CurveUtils.s_InvalidPoint;
                }

                return points[m_CurrentSelectedIndex];
            }
        }
        
        private void DuringSceneGUI(SceneView sceneView)
        {
            if (Selection.activeGameObject != gameObject)
            {
                return;
            }

            Event evt = Event.current;
            if (evt == null)
            {
                return;
            }

            {
                // handle event
                if (evt.isKey)
                {
                    if (evt.type == EventType.KeyDown)
                    {
                        m_CurrentKeyMap.Press(evt.keyCode);
                    }
                    else if (evt.type == EventType.KeyUp)
                    {
                        m_CurrentKeyMap.Release(evt.keyCode);
                    }
                }
            }

            // Shift + A + Left Click
            if (m_CurrentKeyMap.IsMatch(KeyCode.LeftShift, KeyCode.A) && evt.button == 0 && evt.type == EventType.MouseDown)
            {
                var position =
                    CurveUtils.GetMouseWorldPosition(rayCastLayer, evt.mousePosition, hitPointExpand);
                if (position.IsValid())
                {
                    points.Add(position);
                    EvalCurvePoints();
                }
            }

            // m_CurrentSelectedIndex = -1;
            // Shift + V + Left Click
            if (m_CurrentKeyMap.IsMatch(KeyCode.LeftShift, KeyCode.V) && evt.button == 0 &&
                evt.type == EventType.MouseDown && Selection.activeGameObject == gameObject)
            {
                var position =
                    CurveUtils.GetMouseWorldPosition(rayCastLayer, evt.mousePosition, hitPointExpand);
                if (position.IsValid())
                {
                    m_CurrentSelectedIndex = CurveUtils.FindNearestPointIndex(points, position);
                }
            }

        }
    }
}