using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(TwisonExtractor))]
public class TwisonExtractorDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Extract"))
        {
            TwisonExtractor extractor = (TwisonExtractor)target;
            extractor.Extract();
        }
        DrawDefaultInspector();
    }
}
