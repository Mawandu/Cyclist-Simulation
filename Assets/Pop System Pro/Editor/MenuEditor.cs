using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class MenuEditor : MonoBehaviour
{
    [MenuItem("Population System/New Scene", menuItem = "Population System/New Scene", priority = 100, validate = false)]
    static void MakeScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

        GameObject g = (GameObject)Instantiate(AssetDatabase.LoadAssetAtPath(@"Assets/Prefabs/Population System.prefab", typeof(UnityEngine.GameObject)));
        g.name = g.name.Split('(')[0];
    }

    [MenuItem("Population System/Walking people")]
    private static void CreateWalkingPeople()
    {
        CreatePath(PathType.PeoplePath);
    }

    [MenuItem("Population System/Audience Path")]
    private static void CreateAudiencePath()
    {
        CreatePath(PathType.AudiencePath);
    }

    [MenuItem("Population System/Audience")]
    private static void CreateAudience()
    {
        var populationSystemManager = GetPopulationSystemManager();
        Selection.activeGameObject = populationSystemManager.gameObject;
        ActiveEditorTracker.sharedTracker.isLocked = true;
        populationSystemManager.isConcert = true;
    }

    [MenuItem("Population System/Talking people")]
    private static void CreateTalkingPeople()
    {
        var populationSystemManager = GetPopulationSystemManager();
        Selection.activeGameObject = populationSystemManager.gameObject;
        ActiveEditorTracker.sharedTracker.isLocked = true;
        populationSystemManager.isStreet = true;
    }

    private static void CreatePath(PathType pathType)
    {
        GetPopulationSystemManager();

        GameObject newPath = new GameObject { name = "New path" };
        NewPath newPathComponent = newPath.AddComponent<NewPath>();
        newPathComponent.PathType = pathType;
        Selection.activeGameObject = newPath;
    }

    private static PopulationSystemManager GetPopulationSystemManager()
    {
        if (FindAnyObjectByType<PopulationSystemManager>() == null)
        {
            string[] managerPrefabs = AssetDatabase.FindAssets("Population System t:Prefab");
            if (managerPrefabs.Length > 0)
            {
                string managerPath = AssetDatabase.GUIDToAssetPath(managerPrefabs[0]);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(managerPath));
            }
        }

        return FindAnyObjectByType<PopulationSystemManager>();
    }
}