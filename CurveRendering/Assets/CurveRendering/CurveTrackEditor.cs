using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CurveRendering
{
    [CustomEditor(typeof(CurveTrack)), CanEditMultipleObjects]
    public class CurveTrackEditor : Editor
    {
        private Vector3 positionTranslated = Vector3.zero;
        protected virtual void OnSceneGUI()
        {
            CurveTrack curveTrack = (CurveTrack)target;
            
            if (curveTrack.gameObject == Selection.activeGameObject && curveTrack.CurrentSelectedPoint.IsValid())
            {
                positionTranslated = Handles.PositionHandle(curveTrack.CurrentSelectedPoint, Quaternion.identity);
                curveTrack.SetPoint(positionTranslated);
            }
        }
    }
    

}