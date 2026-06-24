using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CameraShaker))]
public class CameraShakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("Test Shake"))
            ((CameraShaker)target).TestShake();
        GUI.enabled = true;

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Play 모드에서만 테스트 가능합니다.", MessageType.Info);
    }
}
