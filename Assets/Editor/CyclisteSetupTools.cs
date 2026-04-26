using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using rayzngames;

public class CyclisteSetupTools : EditorWindow
{
    [MenuItem("Projet 2/1. Setup Player Bicycle (Phase 1)")]
    public static void SetupPlayerBicycle()
    {
        string pipeline = "URP";
        if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.name.Contains("HDRP"))
        {
            pipeline = "HDRP";
        }

        string bikePath = pipeline == "URP" ?
            "Assets/RayznGames/BicycleSystem/URP/Prefabs/WithRIgs/Bicycle_2.0_RIg.prefab" :
            "Assets/RayznGames/BicycleSystem/HDRP/Prefabs/WithRIgs/Bicycle_2.0._Rig.prefab";

        string camPath = pipeline == "URP" ?
            "Assets/RayznGames/BicycleSystem/URP/Prefabs/Extras/CameraRig.prefab" :
            "Assets/RayznGames/BicycleSystem/HDRP/Prefabs/Extras/CameraRig.prefab";

        GameObject bikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bikePath);
        GameObject camPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(camPath);

        if (bikePrefab == null || camPrefab == null)
        {
            Debug.LogError("CyclisteSetupTools: Could not find the Easy Bike System prefabs! Ensure the folders are in Assets/RayznGames/BicycleSystem/.");
            return;
        }

        // 1. Spawn Bicycle
        GameObject bikeObj = (GameObject)PrefabUtility.InstantiatePrefab(bikePrefab);
        
        // Coordonnées exactes demandées par l'utilisateur
        Vector3 spawnPos = new Vector3(-3.51f, 2.5f, Random.Range(160f, 162f));
        bikeObj.transform.position = spawnPos;

        // Rotation orientée sur l'axe X (soit 90 degrés, soit -90 degrés selon si la route va à droite ou à gauche)
        // Je mets 90 par défaut (direction +X). 
        bikeObj.transform.rotation = Quaternion.Euler(0, 90f, 0);
        
        // 1. Prioritize currently selected object in Unity 
        if (Selection.activeGameObject != null)
        {
            // Commenté pour forcer vos coordonnées. Si vous voulez cibler un objet à nouveau, vous pouvez décommenter ça plus tard!
            // spawnPos = Selection.activeGameObject.transform.position;
            // spawnPos.y += 2.0f; 
            // bikeObj.transform.rotation = Selection.activeGameObject.transform.rotation;
        }

        bikeObj.transform.position = spawnPos;
        bikeObj.name = "Player_Bicycle";

        // 2. Add Controls if missing
        BikeControlsExample controls = bikeObj.GetComponent<BikeControlsExample>();
        if (controls == null)
        {
            controls = bikeObj.AddComponent<BikeControlsExample>();
        }
        controls.controllingBike = true;

        var bicycleVehicle = bikeObj.GetComponent<BicycleVehicle>();
        if (bicycleVehicle != null) bicycleVehicle.InControl(true);

        // 3. Spawn Camera
        GameObject camObj = (GameObject)PrefabUtility.InstantiatePrefab(camPrefab);
        camObj.name = "Player_CameraRig";
        
        CameraController camController = camObj.GetComponent<CameraController>();
        if (camController != null)
        {
            camController.target = bikeObj.transform;
            Debug.Log("CyclisteSetupTools: Attached camera to Player_Bicycle.");
        }

        // 4. Disable standard MainCamera if present
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.gameObject != camObj)
        {
            mainCam.gameObject.SetActive(false);
            Debug.Log("CyclisteSetupTools: Disabled existing MainCamera to prevent overlap.");
        }

        // 5. Select object
        Selection.activeGameObject = bikeObj;
        SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log($"CyclisteSetupTools: Successfully configured Phase 1 Player Control using {pipeline} models.");
    }

    [MenuItem("Projet 2/2. Setup AI Bicycle Path (Phase 2)")]
    public static void SetupAIBicyclePath()
    {
        // Find or instantiate Population Manager
        UTS_PopulationSystemManager manager = Object.FindFirstObjectByType<UTS_PopulationSystemManager>();
        if (manager == null)
        {
            string[] managerPrefabs = AssetDatabase.FindAssets("Population System t:Prefab");
            if (managerPrefabs.Length > 0)
            {
                string managerPath = AssetDatabase.GUIDToAssetPath(managerPrefabs[0]);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(managerPath));
            }
            manager = Object.FindFirstObjectByType<UTS_PopulationSystemManager>();
        }

        if (manager == null)
        {
            Debug.LogError("Could not find or spawn Population System Manager!");
            return;
        }

        // Create the Path
        GameObject newPath = new GameObject { name = "[AI] Bicycle Path" };
        UTS_NewPath newPathComponent = newPath.AddComponent<UTS_NewPath>();
        newPathComponent.UTS_PathType = UTS_PathType.BcyclesGyroPath;
        
        Selection.activeGameObject = newPath;
        SceneView.lastActiveSceneView.Focus();

        Debug.Log("CyclisteSetupTools: Successfully configured Phase 2 AI Path. Please click on the ground to draw your path waypoints out!");
    }

    [MenuItem("Projet 2/3. Complete Triggers & Obstacles (Phase 3)")]
    public static void SetupPhase3()
    {
        // Tag assignment for the player if it exists
        GameObject playerBike = GameObject.Find("Player_Bicycle");
        if (playerBike != null && playerBike.tag != "Player")
        {
            playerBike.tag = "Player";
            Debug.Log("CyclisteSetupTools: Tagged Player_Bicycle as 'Player'.");
        }

        // We will directly patch the UTS_FullPack walking prefabs to attach our new avoidance script!
        string[] bikePrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/UTS_FullPack" });
        int modifications = 0;
        foreach (string guid in bikePrefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                if (prefab.GetComponent<BcycleGyroController>() != null && prefab.GetComponent<AIBikeObstacleAvoidance>() == null)
                {
                    prefab.AddComponent<AIBikeObstacleAvoidance>();
                    EditorUtility.SetDirty(prefab);
                    modifications++;
                }
            }
        }
        AssetDatabase.SaveAssets();

        Debug.Log($"CyclisteSetupTools: Attached AIBikeObstacleAvoidance to {modifications} AI Bicycle Prefabs. Phase 3 triggers are now active!");
    }
}
