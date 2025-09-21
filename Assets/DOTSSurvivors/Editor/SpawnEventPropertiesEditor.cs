using UnityEditor;
using UnityEngine;

namespace TMG.DOTSSurvivors.EditorScripts
{
    /// <summary>
    /// Custom editor script to display <see cref="EnemySpawnEventProperties"/> in the editor.
    /// </summary>
    /// <remarks>
    /// <see cref="EnemySpawnEventProperties"/> is a ScriptableObject that holds data related to enemy spawn events. This editor script makes editing these spawn events easier as it only displays fields relevant to the chosen spawn formation.
    /// </remarks>
    [CustomEditor(typeof(EnemySpawnEventProperties))]
    public class SpawnEventPropertiesEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var spawnEventProperties = (EnemySpawnEventProperties)target;
            EditorGUI.BeginChangeCheck();
            
            spawnEventProperties.SpawnFormation = (SpawnFormation)EditorGUILayout.EnumPopup(new GUIContent("Spawn Formation", "Select the spawn formation type."), spawnEventProperties.SpawnFormation);

            if (spawnEventProperties.SpawnFormation == SpawnFormation.None) return;
            
            spawnEventProperties.EnemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", spawnEventProperties.EnemyType);
            
            spawnEventProperties.EnemyCount = EditorGUILayout.IntField("Enemy Count", spawnEventProperties.EnemyCount);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Spawn Formation Specific Properties", "Certain spawn formations require additional data which can be set below."), EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            switch (spawnEventProperties.SpawnFormation)
            {
                case SpawnFormation.LinearMoveGroup:
                    spawnEventProperties.MoveSpeed = EditorGUILayout.FloatField(new GUIContent("Move Speed", "Linear speed the enemy will be moving"), spawnEventProperties.MoveSpeed);
                    spawnEventProperties.EnemySpacing = EditorGUILayout.FloatField("Enemy Spacing", spawnEventProperties.EnemySpacing);
                    break;
                case SpawnFormation.EllipseAroundView:
                    spawnEventProperties.MoveSpeed = EditorGUILayout.FloatField(new GUIContent("Move Speed", "Linear speed the enemy will be moving"), spawnEventProperties.MoveSpeed);
                    spawnEventProperties.TimeToLive = EditorGUILayout.FloatField("Time to Live", spawnEventProperties.TimeToLive);
                    break;
                case SpawnFormation.SineMoveVerticalLine or SpawnFormation.SineMoveHorizontalLine:
                    spawnEventProperties.MoveSpeed = EditorGUILayout.FloatField(new GUIContent("Constant Move Speed", "Linear speed the enemy will be moving, perpendicular to the sine oscillation."), spawnEventProperties.MoveSpeed);
                    spawnEventProperties.Period = EditorGUILayout.FloatField(new GUIContent("Period", "Frequency of sine oscillations"), spawnEventProperties.Period);
                    spawnEventProperties.Amplitude = EditorGUILayout.FloatField(new GUIContent("Amplitude", "1/2 height of the full sine oscillation"), spawnEventProperties.Amplitude);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spawnEventProperties, "Updated SpawnEventProperties");
                EditorUtility.SetDirty(spawnEventProperties);
            }
        }
    }
}