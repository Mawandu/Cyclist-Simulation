using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UTS_MenuEditor : MonoBehaviour
{
    [MenuItem("UTS PRO/Create/Vehicles")]
    private static void CreateVehiclePath()
    {
        CreatePath(UTS_PathType.VehiclePath);
    }

    [MenuItem("UTS PRO/Create/Bicycles\\Gyro")]
    private static void CreateBcyclesGyroPath()
    {
        CreatePath(UTS_PathType.BcyclesGyroPath);
    }

    [MenuItem("UTS PRO/Create/Population/Walking people")]
    private static void CreateWalkingPeople()
    {
        CreatePath(UTS_PathType.PeoplePath);
    }

    [MenuItem("UTS PRO/Create/Population/Audience Path")]
    private static void CreateAudiencePath()
    {
        CreatePath(UTS_PathType.UTS_AudiencePath);
    }

    [MenuItem("UTS PRO/Create/Population/Audience")]
    private static void CreateAudience()
    {
        var populationSystemManager = GetPopulationSystemManager();
        Selection.activeGameObject = populationSystemManager.gameObject;
        ActiveEditorTracker.sharedTracker.isLocked = true;
        populationSystemManager.isConcert = true;
    }

    [MenuItem("UTS PRO/Create/Population/Talking people")]
    private static void CreateTalkingPeople()
    {
        var populationSystemManager = GetPopulationSystemManager();
        Selection.activeGameObject = populationSystemManager.gameObject;
        ActiveEditorTracker.sharedTracker.isLocked = true;
        populationSystemManager.isStreet = true;
    }

    private static void CreatePath(UTS_PathType pathType)
    {
        GetPopulationSystemManager();

        GameObject newPath = new GameObject { name = "New path" };
        UTS_NewPath newPathComponent = newPath.AddComponent<UTS_NewPath>();
        newPathComponent.UTS_PathType = pathType;
        Selection.activeGameObject = newPath;
    }

    private static UTS_PopulationSystemManager GetPopulationSystemManager()
    {
        if (FindAnyObjectByType<UTS_PopulationSystemManager>() == null)
        {
            string[] managerPrefabs = AssetDatabase.FindAssets("Population System t:Prefab");
            if (managerPrefabs.Length > 0)
            {
                string managerPath = AssetDatabase.GUIDToAssetPath(managerPrefabs[0]);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(managerPath));
            }
        }

        return FindAnyObjectByType<UTS_PopulationSystemManager>();
    }
}