using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UTS_NewPath))]
public class UTS_NewPathEditor : Editor
{
    public void OnSceneGUI()
    {
        UTS_NewPath _NewPath = target as UTS_NewPath;

        if (!_NewPath.exit)
        {
            // FORCE UNITY TO SEND MOUSE EVENTS TO US (prevent clicking other objects)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (Event.current.type == EventType.MouseMove)
                SceneView.RepaintAll();

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            // Debug drawing so the user knows if physics is working
            if (Physics.Raycast(ray, out hit, 3000))
            {
                _NewPath.mousePos = hit.point;

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    _NewPath.PointSet(_NewPath.pointLenght, hit.point);
                    _NewPath.pointLenght++;
                    GUI.changed = true;
                    Event.current.Use(); // Consomme l'événement
                }
            }
        }

        if (Event.current.keyCode == KeyCode.Escape || Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            _NewPath.exit = true;
            Event.current.Use();
        }
    }

    public override void OnInspectorGUI()
    {
        UTS_NewPath _NewPath = target as UTS_NewPath;
        EditorGUILayout.Space();

        // FORCER l'affichage de l'interface en tout temps (suppression de l'état bloquant)
        _NewPath.exit = false;

        EditorGUILayout.HelpBox("Cliquez GAUCHE sur le sol avec la souris pour dessiner, ou utilisez le bouton 'Ajouter Point Ici' en bas.", MessageType.Info);
        EditorGUILayout.Space();
            
        if (GUILayout.Button("Ajouter Point Ici (SOLUTION DE RECOURS)"))
        {
            // Si le clic ne marche pas, on place un point au centre de la caméra
            Vector3 pos = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            _NewPath.PointSet(_NewPath.pointLenght, pos);
            _NewPath.pointLenght++;
            GUI.changed = true;
        }
        EditorGUILayout.Space();

        _NewPath.pathName = EditorGUILayout.TextField("Path name: ", _NewPath.pathName);
        EditorGUILayout.Space();

        if (GUILayout.Button("Finish"))
        {
            if (!_NewPath.errors)
            {
                DestroyImmediate(_NewPath.par);
                _NewPath.gameObject.name = _NewPath.pathName;

                UTS_WalkPath wp = null;

                if (_NewPath.UTS_PathType == UTS_PathType.PeoplePath)
                    wp = _NewPath.gameObject.AddComponent<UTS_PeopleWalkPath>() as UTS_WalkPath;
                else if (_NewPath.UTS_PathType == UTS_PathType.BcyclesGyroPath)
                    wp = _NewPath.gameObject.AddComponent<BcycleGyroPath>() as UTS_WalkPath;
                else
                    wp = _NewPath.gameObject.AddComponent<UTS_AudiencePath>() as UTS_WalkPath;

                wp.pathPoint = _NewPath.PointsGet();
                wp.UTS_PathType = _NewPath.UTS_PathType;

                GameObject _myPoints = new GameObject();
                _myPoints.transform.parent = _NewPath.gameObject.transform;
                _myPoints.name = "points";

                for (int i = 0; i < wp.pathPoint.Count; i++)
                {
                    var _point = Instantiate(
                        GameObject.Find("Population System")
                            .GetComponent<UTS_PopulationSystemManager>().pointPrefab,
                        wp.pathPoint[i],
                        Quaternion.identity) as GameObject;
                    _point.name = "p" + i;
                    _point.transform.parent = _myPoints.transform;
                    wp.pathPointTransform.Add(_point);
                }

                ActiveEditorTracker.sharedTracker.isLocked = false;
                DestroyImmediate(_NewPath.gameObject.GetComponent<UTS_NewPath>());
            }
        }

        EditorGUILayout.Space();

        if (_NewPath.pointLenght < 2)
        {
            _NewPath.errors = true;
            EditorGUILayout.HelpBox("To create a path must be at least 2 points", MessageType.Warning);
        }

        if (string.IsNullOrEmpty(_NewPath.pathName))
        {
            _NewPath.errors = true;
            EditorGUILayout.HelpBox("Enter the path name", MessageType.Warning);
        }

        if (_NewPath.pointLenght >= 2 && !string.IsNullOrEmpty(_NewPath.pathName))
            _NewPath.errors = false;
    }
}