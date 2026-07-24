using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MeetingRoomBlockoutBuilder
{
    private const string ScenePath = "Assets/Scenes/MeetingRoom_Blockout.unity";
    private const string DocsFolder = "Assets/Documentation";
    private const string ScreenshotsFolder = "Assets/Documentation/Screenshots";
    private const string MaterialsFolder = "Assets/Materials/Blockout";
    private const string AutorunMarkerPath = "Assets/Documentation/.rebuild_meeting_room_blockout";

    private static readonly List<ObjectRecord> Records = new List<ObjectRecord>();
    private static Dictionary<string, Material> materials;

    [InitializeOnLoadMethod]
    private static void RunPendingAutorun()
    {
        if (!File.Exists(ToAbsolutePath(AutorunMarkerPath)))
        {
            return;
        }

        if (SessionState.GetBool("MeetingRoomBlockoutBuilder.AutorunQueued", false))
        {
            return;
        }

        SessionState.SetBool("MeetingRoomBlockoutBuilder.AutorunQueued", true);
        EditorApplication.delayCall += () =>
        {
            try
            {
                if (File.Exists(ToAbsolutePath(AutorunMarkerPath)))
                {
                    File.Delete(ToAbsolutePath(AutorunMarkerPath));
                }

                RebuildAndCapture();
            }
            finally
            {
                SessionState.SetBool("MeetingRoomBlockoutBuilder.AutorunQueued", false);
            }
        };
    }

    [MenuItem("Tools/Conference Room/Rebuild MeetingRoom Blockout")]
    public static void RebuildMeetingRoomBlockout()
    {
        Records.Clear();
        EnsureAssetFolder("Assets/Scenes");
        EnsureAssetFolder(DocsFolder);
        EnsureAssetFolder(ScreenshotsFolder);
        EnsureAssetFolder(MaterialsFolder);

        materials = BuildMaterials();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        scene.name = "MeetingRoom_Blockout";

        GameObject root = new GameObject("MeetingRoom_Blockout_v1");
        Transform room = CreateGroup("Room", root.transform);
        Transform furniture = CreateGroup("Furniture", root.transform);
        Transform taskObjects = CreateGroup("Task Objects", root.transform);
        Transform navigation = CreateGroup("Navigation Markers", root.transform);
        Transform labels = CreateGroup("ID Labels For Map Screenshot", root.transform);
        Transform cameras = CreateGroup("Screenshot Cameras", root.transform);
        Transform lighting = CreateGroup("Lighting", root.transform);

        BuildRoom(room);
        BuildFurniture(furniture);
        BuildTaskObjects(taskObjects);
        BuildNavigationMarkers(navigation);
        BuildMapLabels(labels);
        BuildCameras(cameras);
        BuildLighting(lighting);
        BuildPlayer(root.transform);

        ValidateObjectIds();
        WriteHandoffDocuments();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"MeetingRoom blockout rebuilt at {ScenePath} with {Records.Count} object_id entries.");
    }

    [MenuItem("Tools/Conference Room/Capture MeetingRoom Screenshots")]
    public static void CaptureMeetingRoomScreenshots()
    {
        if (!File.Exists(ToAbsolutePath(ScenePath)))
        {
            RebuildMeetingRoomBlockout();
        }
        else if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            Scene blockoutScene = FindLoadedScene(ScenePath);
            if (!blockoutScene.IsValid())
            {
                blockoutScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            SceneManager.SetActiveScene(blockoutScene);
        }

        EnsureAssetFolder(ScreenshotsFolder);
        Directory.CreateDirectory(ToAbsolutePath(ScreenshotsFolder));

        RenderCameraToPng("screenshot_map_camera", $"{ScreenshotsFolder}/meeting_room_blockout_map.png", 1920, 1080);
        RenderCameraToPng("screenshot_first_person_camera", $"{ScreenshotsFolder}/meeting_room_blockout_first_person.png", 1920, 1080);

        AssetDatabase.Refresh();
        Debug.Log("MeetingRoom screenshots captured.");
    }

    public static void RebuildAndCapture()
    {
        RebuildMeetingRoomBlockout();
        CaptureMeetingRoomScreenshots();
    }

    private static void BuildRoom(Transform parent)
    {
        AddBox(parent, "room_floor", new Vector3(0f, -0.05f, 0f), new Vector3(12f, 0.1f, 10f), materials["floor"],
            "地图结构", "房间地面", "会议室全区，12m x 10m", "定义可行走地面与整体地图范围，扩大后方便玩家移动", "S2/S3/S4");

        AddBox(parent, "wall_front", new Vector3(0f, 1.5f, 5f), new Vector3(12f, 3f, 0.1f), materials["wall"],
            "地图结构", "前墙", "Z=5，显示屏所在墙", "封闭房间前侧边界，承载 screen_01", "S2/S4");
        AddBox(parent, "wall_back", new Vector3(0f, 1.5f, -5f), new Vector3(12f, 3f, 0.1f), materials["wall"],
            "地图结构", "后墙", "Z=-5，玩家出生点后方", "封闭房间后侧边界，start_point 位于墙内侧", "S2/S4");
        AddBox(parent, "wall_left", new Vector3(-6f, 1.5f, 0f), new Vector3(0.1f, 3f, 10f), materials["wall"],
            "地图结构", "左墙", "X=-6，白板侧", "封闭房间左侧边界，承载 whiteboard_01", "S2/S4");
        AddBox(parent, "wall_right", new Vector3(6f, 1.5f, 0f), new Vector3(0.1f, 3f, 10f), materials["wall"],
            "地图结构", "右墙", "X=6，储物柜侧", "封闭房间右侧边界，靠近 cabinet_01", "S2/S4");
        AddBox(parent, "ceiling_01", new Vector3(0f, 3.05f, 0f), new Vector3(12f, 0.1f, 10f), materials["wall"],
            "地图结构", "天花板", "房间顶部", "封闭会议室上方空间，形成完整室内房间", "S2/S4");
    }

    private static void BuildFurniture(Transform parent)
    {
        AddBox(parent, "meeting_table_01", new Vector3(0f, 0.4f, 0f), new Vector3(3f, 0.2f, 1.5f), materials["table"],
            "家具", "会议桌", "会议室中央", "承载 laptop_01、wrong_cable_01、document_01 和 table_target_area", "S2/S3/S4");

        AddChair(parent, "chair_01", new Vector3(-1.2f, 0.25f, -1.2f), "会议桌后排左侧");
        AddChair(parent, "chair_02", new Vector3(0f, 0.25f, -1.2f), "会议桌后排中间");
        AddChair(parent, "chair_03", new Vector3(1.2f, 0.25f, -1.2f), "会议桌后排右侧");
        AddChair(parent, "chair_04", new Vector3(-1.2f, 0.25f, 1.2f), "会议桌前排左侧");
        AddChair(parent, "chair_05", new Vector3(0f, 0.25f, 1.2f), "会议桌前排中间");
        AddChair(parent, "chair_06", new Vector3(1.2f, 0.25f, 1.2f), "会议桌前排右侧");

        AddBox(parent, "cabinet_01", new Vector3(5.1f, 0.75f, 1.2f), new Vector3(1f, 1.5f, 0.5f), materials["cabinet"],
            "家具", "储物柜", "右侧墙边，X=5.1", "放置 hdmi_cable_01 和 remote_01 的搜索区域", "S2/S3/S4");

        AddBox(parent, "whiteboard_01", new Vector3(-5.93f, 1.45f, 1.0f), new Vector3(0.08f, 1.1f, 1.8f), materials["whiteboard"],
            "家具", "白板", "左侧墙面，X=-5.93", "会议室视觉参照物，可作为后续提示区域", "S2/S3/S4");

        AddBox(parent, "screen_01", new Vector3(0f, 1.55f, 4.93f), new Vector3(2.5f, 1.2f, 0.08f), materials["screen"],
            "设备", "显示屏", "房间前方墙面，Z=4.93", "T4 连接显示内容，T5 被遥控器打开", "S2/S3/S4");
    }

    private static void BuildTaskObjects(Transform parent)
    {
        GameObject laptop = AddBox(parent, "laptop_01", new Vector3(-0.8f, 0.6f, 0f), new Vector3(0.6f, 0.05f, 0.4f), materials["laptop"],
            "任务物体", "笔记本电脑", "会议桌左侧", "T1 找到并拿起电脑", "S2/S3/S4");
        AddBox(laptop.transform, "laptop_screen", new Vector3(0f, 0.19f, 0.18f), new Vector3(0.58f, 0.34f, 0.035f), materials["screen"], false);

        AddBox(parent, "hdmi_cable_01", new Vector3(5.1f, 1.55f, 1.0f), new Vector3(0.7f, 0.04f, 0.04f), materials["hdmi"],
            "任务物体", "HDMI 线", "储物柜上方", "T2 正确目标线缆，拿到后连接 laptop_01 与 screen_01", "S2/S3/S4", Quaternion.Euler(0f, 25f, 0f));

        AddBox(parent, "wrong_cable_01", new Vector3(0.8f, 0.6f, 0.3f), new Vector3(0.7f, 0.04f, 0.04f), materials["wrongCable"],
            "干扰物", "错误线缆", "会议桌右侧", "干扰项；S2 可用作拿错线缆的失败条件", "S2/S3/S4", Quaternion.Euler(0f, -20f, 0f));

        AddBox(parent, "remote_01", new Vector3(4.9f, 1.6f, 1.3f), new Vector3(0.25f, 0.04f, 0.1f), materials["remote"],
            "任务物体", "遥控器", "储物柜上方", "T5 打开 screen_01", "S2/S3/S4", Quaternion.Euler(0f, -12f, 0f));

        AddBox(parent, "document_01", new Vector3(0.2f, 0.62f, -0.3f), new Vector3(0.4f, 0.02f, 0.3f), materials["document"],
            "任务物体", "会议资料", "会议桌边缘", "T6 摆放到 table_target_area", "S2/S3/S4", Quaternion.Euler(0f, 8f, 0f));

        GameObject tableTarget = AddBox(parent, "table_target_area", new Vector3(0f, 0.635f, 0f), new Vector3(0.8f, 0.02f, 0.5f), materials["target"],
            "区域点", "桌面目标区域", "会议桌中心", "document_01 的放置目标区域", "S2/S3/S4");
        Collider targetCollider = tableTarget.GetComponent<Collider>();
        if (targetCollider != null)
        {
            targetCollider.isTrigger = true;
        }
    }

    private static void BuildNavigationMarkers(Transform parent)
    {
        GameObject start = AddCylinder(parent, "start_point", new Vector3(0f, 0.035f, -4.2f), 0.65f, 0.07f, materials["start"],
            "区域点", "玩家起点", "会议室内部后方，Z=-4.2", "玩家出生点，已放在封闭会议室内部并留出移动空间", "S2/S4");
        Collider startCollider = start.GetComponent<Collider>();
        if (startCollider != null)
        {
            startCollider.isTrigger = true;
        }

        GameObject presentation = AddCylinder(parent, "presentation_point", new Vector3(0f, 0.035f, 3.3f), 0.65f, 0.07f, materials["presentation"],
            "区域点", "演讲站位点", "显示屏前方，Z=3.3", "最终站位；任务完成点", "S2/S3/S4");
        Collider presentationCollider = presentation.GetComponent<Collider>();
        if (presentationCollider != null)
        {
            presentationCollider.isTrigger = true;
        }
    }

    private static void BuildMapLabels(Transform parent)
    {
        string[] labeledIds =
        {
            "start_point",
            "meeting_table_01",
            "laptop_01",
            "wrong_cable_01",
            "document_01",
            "table_target_area",
            "cabinet_01",
            "hdmi_cable_01",
            "remote_01",
            "whiteboard_01",
            "screen_01",
            "presentation_point"
        };

        foreach (string objectId in labeledIds)
        {
            GameObject target = GameObject.Find(objectId);
            if (target == null)
            {
                continue;
            }

            GameObject label = new GameObject($"label_{objectId}");
            label.transform.SetParent(parent, false);
            label.transform.position = target.transform.position + Vector3.up * 2.45f;
            label.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            TextMesh text = label.AddComponent<TextMesh>();
            text.text = objectId;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.12f;
            text.fontSize = 48;
            text.color = Color.black;
        }
    }

    private static void BuildCameras(Transform parent)
    {
        GameObject mapCameraObject = new GameObject("screenshot_map_camera");
        mapCameraObject.transform.SetParent(parent, false);
        mapCameraObject.transform.position = new Vector3(0f, 8.5f, 0f);
        mapCameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Camera mapCamera = mapCameraObject.AddComponent<Camera>();
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(0.78f, 0.80f, 0.82f);
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = 5.55f;
        mapCamera.nearClipPlane = 0.1f;
        mapCamera.farClipPlane = 20f;

        GameObject firstPersonCameraObject = new GameObject("screenshot_first_person_camera");
        firstPersonCameraObject.transform.SetParent(parent, false);
        firstPersonCameraObject.transform.position = new Vector3(0f, 1.55f, -4.2f);
        firstPersonCameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0.15f, 0.95f, 3.3f) - firstPersonCameraObject.transform.position, Vector3.up);
        Camera firstPersonCamera = firstPersonCameraObject.AddComponent<Camera>();
        firstPersonCamera.clearFlags = CameraClearFlags.SolidColor;
        firstPersonCamera.backgroundColor = new Color(0.70f, 0.74f, 0.78f);
        firstPersonCamera.fieldOfView = 68f;
        firstPersonCamera.nearClipPlane = 0.1f;
        firstPersonCamera.farClipPlane = 30f;
    }

    private static void BuildLighting(Transform parent)
    {
        RenderSettings.ambientLight = new Color(0.60f, 0.61f, 0.64f);

        GameObject directionalLightObject = new GameObject("main_directional_light");
        directionalLightObject.transform.SetParent(parent, false);
        directionalLightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light directionalLight = directionalLightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 0.85f;
        directionalLight.shadows = LightShadows.Soft;

        AddPointLight(parent, "ceiling_light_01", new Vector3(0f, 2.8f, 0f), new Color(1f, 0.92f, 0.78f), 5.5f, 1.3f);
        AddPointLight(parent, "cabinet_fill_light", new Vector3(3f, 2.2f, 1.2f), new Color(0.74f, 0.82f, 1f), 2.3f, 0.55f);
    }

    private static void BuildPlayer(Transform parent)
    {
        const string PlayerPrefabPath = "Assets/Standard Assets/Characters/FirstPersonCharacter/Prefabs/RigidBodyFPSController.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject player = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : new GameObject("Player");

        player.name = "Player";
        player.transform.SetParent(parent, false);
        player.transform.localPosition = new Vector3(0f, 0.85f, -4.2f);
        player.transform.localRotation = Quaternion.identity;
        player.transform.localScale = Vector3.one;

        Camera playerCamera = player.GetComponentInChildren<Camera>(true);
        if (playerCamera != null)
        {
            Vector3 cameraPosition = playerCamera.transform.localPosition;
            cameraPosition.y = 0.5f;
            playerCamera.transform.localPosition = cameraPosition;
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.height = 1.4f;
            capsule.radius = 0.35f;
        }
    }

    private static void AddChair(Transform parent, string objectId, Vector3 position, string area)
    {
        AddBox(parent, objectId, position, new Vector3(0.5f, 0.5f, 0.5f), materials["chair"],
            "家具", "椅子", area, "后续 T7 调整椅子的候选对象", "S2/S3/S4");
    }

    private static GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, bool addCollider = true)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localRotation = Quaternion.identity;
        box.transform.localScale = localScale;
        ApplyMaterial(box, material);

        if (!addCollider)
        {
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
        }

        return box;
    }

    private static GameObject AddBox(
        Transform parent,
        string objectId,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        string category,
        string chineseName,
        string area,
        string taskRole,
        string handoff,
        Quaternion? localRotation = null)
    {
        GameObject box = AddBox(parent, objectId, localPosition, localScale, material);
        box.transform.localRotation = localRotation ?? Quaternion.identity;
        AddIdentity(box, objectId);
        AddRecord(objectId, chineseName, category, area, box.transform.position, taskRole, handoff);
        return box;
    }

    private static GameObject AddCylinder(
        Transform parent,
        string objectId,
        Vector3 localPosition,
        float diameter,
        float height,
        Material material,
        string category,
        string chineseName,
        string area,
        string taskRole,
        string handoff)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = objectId;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localRotation = Quaternion.identity;
        cylinder.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
        ApplyMaterial(cylinder, material);
        AddIdentity(cylinder, objectId);
        AddRecord(objectId, chineseName, category, area, cylinder.transform.position, taskRole, handoff);
        return cylinder;
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static void AddPointLight(Transform parent, string name, Vector3 position, Color color, float range, float intensity)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
    }

    private static void AddIdentity(GameObject target, string objectId)
    {
        ObjectIdentity identity = target.GetComponent<ObjectIdentity>();
        if (identity == null)
        {
            identity = target.AddComponent<ObjectIdentity>();
        }

        identity.SetObjectId(objectId);
    }

    private static void AddRecord(string objectId, string chineseName, string category, string area, Vector3 position, string taskRole, string handoff)
    {
        Records.Add(new ObjectRecord
        {
            objectId = objectId,
            chineseName = chineseName,
            category = category,
            area = area,
            position = position,
            taskRole = taskRole,
            handoff = handoff
        });
    }

    private static void ApplyMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void ValidateObjectIds()
    {
        HashSet<string> ids = new HashSet<string>();
        foreach (ObjectIdentity identity in UnityEngine.Object.FindObjectsByType<ObjectIdentity>(FindObjectsSortMode.None))
        {
            if (string.IsNullOrWhiteSpace(identity.ObjectId))
            {
                throw new InvalidOperationException($"ObjectIdentity on {identity.name} has an empty object_id.");
            }

            if (!ids.Add(identity.ObjectId))
            {
                throw new InvalidOperationException($"Duplicate object_id found: {identity.ObjectId}");
            }

            if (identity.name != identity.ObjectId)
            {
                throw new InvalidOperationException($"GameObject name and object_id differ on {identity.name}: {identity.ObjectId}");
            }
        }
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        return new Dictionary<string, Material>
        {
            ["floor"] = EnsureMaterial("Blockout_Floor", new Color(0.54f, 0.55f, 0.52f), 0.18f),
            ["wall"] = EnsureMaterial("Blockout_Wall", new Color(0.72f, 0.73f, 0.70f), 0.12f),
            ["table"] = EnsureMaterial("Blockout_Table_Wood", new Color(0.42f, 0.26f, 0.13f), 0.22f),
            ["chair"] = EnsureMaterial("Blockout_Chair_Teal", new Color(0.06f, 0.36f, 0.34f), 0.20f),
            ["cabinet"] = EnsureMaterial("Blockout_Cabinet", new Color(0.34f, 0.30f, 0.25f), 0.25f),
            ["whiteboard"] = EnsureMaterial("Blockout_Whiteboard", new Color(0.92f, 0.94f, 0.90f), 0.35f),
            ["screen"] = EnsureMaterial("Blockout_Screen_Blue", new Color(0.08f, 0.24f, 0.58f), 0.42f, 0.5f),
            ["laptop"] = EnsureMaterial("Blockout_Laptop", new Color(0.08f, 0.09f, 0.10f), 0.35f),
            ["hdmi"] = EnsureMaterial("Blockout_HDMI_Cable", new Color(0.07f, 0.40f, 0.90f), 0.2f),
            ["wrongCable"] = EnsureMaterial("Blockout_Wrong_Cable", new Color(0.86f, 0.15f, 0.10f), 0.2f),
            ["remote"] = EnsureMaterial("Blockout_Remote", new Color(0.03f, 0.03f, 0.035f), 0.36f),
            ["document"] = EnsureMaterial("Blockout_Document", new Color(0.96f, 0.93f, 0.78f), 0.1f),
            ["target"] = EnsureMaterial("Blockout_Target_Area", new Color(0.15f, 0.80f, 0.32f, 0.46f), 0.2f, 0f, true),
            ["start"] = EnsureMaterial("Blockout_Start_Point", new Color(0.18f, 0.80f, 0.28f), 0.15f, 0.2f),
            ["presentation"] = EnsureMaterial("Blockout_Presentation_Point", new Color(0.98f, 0.68f, 0.16f), 0.15f, 0.2f)
        };
    }

    private static Material EnsureMaterial(string materialName, Color color, float smoothness, float emission = 0f, bool transparent = false)
    {
        string path = $"{MaterialsFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            material = new Material(shader != null ? shader : Shader.Find("Diffuse"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = materialName;
        material.SetColor("_Color", color);
        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", smoothness);
        }

        if (emission > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void WriteHandoffDocuments()
    {
        WriteMarkdownTable();
        WriteCsvTable();
        WriteJsonTable();
    }

    private static void WriteMarkdownTable()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# MeetingRoom_Blockout_v3 Object ID Handoff");
        builder.AppendLine();
        builder.AppendLine("统一规则：所有关键物体的 Unity GameObject 名称必须等于 `object_id`，并且已挂载 `ObjectIdentity` 组件，字段名为 `object_id`。学生 2、3、4 后续都只引用下表的 ID。");
        builder.AppendLine();
        builder.AppendLine("## 场景截图");
        builder.AppendLine();
        builder.AppendLine("- 地图俯视截图：`Assets/Documentation/Screenshots/meeting_room_blockout_map.png`");
        builder.AppendLine("- 第一人称截图：`Assets/Documentation/Screenshots/meeting_room_blockout_first_person.png`");
        builder.AppendLine();
        builder.AppendLine("## 场景同步说明");
        builder.AppendLine();
        builder.AppendLine("`Assets/Scenes/ConferenceRoom.unity` 和 `Assets/Scenes/MeetingRoom_Blockout.unity` 已同步为同一套任务实验会议室，两个场景都使用下表这 24 个 `object_id`。会议室区域为 12m x 10m：`room_floor` 位于 `(0, -0.05, 0)`，四面墙位于 `X=±6`、`Z=±5`，`start_point` 位于会议室内部后方 `(0, 0.035, -4.2)`，`Player` prefab 出生位置为 `(0, 0.85, -4.2)` 并面向 `screen_01`。");
        builder.AppendLine();
        builder.AppendLine("## Object ID 表");
        builder.AppendLine();
        builder.AppendLine("| Object ID | 中文名称 | 类型 | 所在区域 / 坐标 | 任务作用 | 交接对象 |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (ObjectRecord record in Records)
        {
            builder.AppendLine($"| `{record.objectId}` | {record.chineseName} | {record.category} | {record.area} / {FormatVector(record.position)} | {record.taskRole} | {record.handoff} |");
        }

        builder.AppendLine();
        builder.AppendLine("## 给学生 2：HTG / 任务图");
        builder.AppendLine();
        builder.AppendLine("建议正常任务路线：`start_point` -> `laptop_01` -> `cabinet_01` -> `hdmi_cable_01` -> `screen_01` -> `remote_01` -> `document_01` -> `table_target_area` -> `presentation_point`。");
        builder.AppendLine();
        builder.AppendLine("- 成功关键对象：`laptop_01`、`hdmi_cable_01`、`remote_01`、`document_01`、`table_target_area`、`presentation_point`。");
        builder.AppendLine("- 干扰对象：`wrong_cable_01`，位于会议桌右侧；可作为拿错线缆或错误选择的失败条件。");
        builder.AppendLine("- 椅子对象：`chair_01` 到 `chair_06`，后续可作为调整座位或清理动线任务。");
        builder.AppendLine();
        builder.AppendLine("## 给学生 3：高亮 / Act 层");
        builder.AppendLine();
        builder.AppendLine("优先支持高亮：`laptop_01`、`hdmi_cable_01`、`wrong_cable_01`、`remote_01`、`screen_01`、`document_01`、`table_target_area`、`presentation_point`、`cabinet_01`。");
        builder.AppendLine();
        builder.AppendLine("示例命令：");
        builder.AppendLine();
        builder.AppendLine("```json");
        builder.AppendLine("{");
        builder.AppendLine("  \"type\": \"highlight_object\",");
        builder.AppendLine("  \"object_id\": \"hdmi_cable_01\"");
        builder.AppendLine("}");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## 给学生 4：API / VLM 返回 JSON");
        builder.AppendLine();
        builder.AppendLine("API 返回时请使用稳定 `object_id`，不要返回自然语言描述。例如：");
        builder.AppendLine();
        builder.AppendLine("```json");
        builder.AppendLine("{");
        builder.AppendLine("  \"target_object\": \"hdmi_cable_01\",");
        builder.AppendLine("  \"target_area\": \"table_target_area\"");
        builder.AppendLine("}");
        builder.AppendLine("```");

        File.WriteAllText(ToAbsolutePath($"{DocsFolder}/Object_ID_List.md"), builder.ToString(), new UTF8Encoding(true));
    }

    private static void WriteCsvTable()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("object_id,chinese_name,category,area,position,task_role,handoff");
        foreach (ObjectRecord record in Records)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(record.objectId),
                EscapeCsv(record.chineseName),
                EscapeCsv(record.category),
                EscapeCsv(record.area),
                EscapeCsv(FormatVector(record.position)),
                EscapeCsv(record.taskRole),
                EscapeCsv(record.handoff)));
        }

        File.WriteAllText(ToAbsolutePath($"{DocsFolder}/Object_ID_List.csv"), builder.ToString(), new UTF8Encoding(true));
    }

    private static void WriteJsonTable()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[");
        for (int i = 0; i < Records.Count; i++)
        {
            ObjectRecord record = Records[i];
            builder.AppendLine("  {");
            builder.AppendLine($"    \"object_id\": \"{EscapeJson(record.objectId)}\",");
            builder.AppendLine($"    \"chinese_name\": \"{EscapeJson(record.chineseName)}\",");
            builder.AppendLine($"    \"category\": \"{EscapeJson(record.category)}\",");
            builder.AppendLine($"    \"area\": \"{EscapeJson(record.area)}\",");
            builder.AppendLine($"    \"position\": [{FormatFloat(record.position.x)}, {FormatFloat(record.position.y)}, {FormatFloat(record.position.z)}],");
            builder.AppendLine($"    \"task_role\": \"{EscapeJson(record.taskRole)}\",");
            builder.AppendLine($"    \"handoff\": \"{EscapeJson(record.handoff)}\"");
            builder.Append(i == Records.Count - 1 ? "  }\n" : "  },\n");
        }
        builder.AppendLine("]");

        File.WriteAllText(ToAbsolutePath($"{DocsFolder}/Object_ID_List.json"), builder.ToString(), new UTF8Encoding(true));
    }

    private static void RenderCameraToPng(string cameraName, string assetPath, int width, int height)
    {
        GameObject cameraObject = GameObject.Find(cameraName);
        if (cameraObject == null)
        {
            throw new InvalidOperationException($"Missing screenshot camera: {cameraName}");
        }

        Camera camera = cameraObject.GetComponent<Camera>();
        if (camera == null)
        {
            throw new InvalidOperationException($"GameObject {cameraName} does not have a Camera component.");
        }

        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;

        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();

            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            File.WriteAllBytes(ToAbsolutePath(assetPath), image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static Scene FindLoadedScene(string scenePath)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.path == scenePath)
            {
                return scene;
            }
        }

        return default;
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
        {
            throw new ArgumentException($"Asset folder must start with Assets: {assetFolder}");
        }

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, assetPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({FormatFloat(value.x)}, {FormatFloat(value.y)}, {FormatFloat(value.z)})";
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    private sealed class ObjectRecord
    {
        public string objectId;
        public string chineseName;
        public string category;
        public string area;
        public Vector3 position;
        public string taskRole;
        public string handoff;
    }
}
