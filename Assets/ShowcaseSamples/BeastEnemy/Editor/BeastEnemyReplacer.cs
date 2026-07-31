using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using AnimatorController = UnityEditor.Animations.AnimatorController;

public static class BeastEnemyReplacer
{
    private const string MenuPath =
        "Tools/Meeting Room/Replace Pig With Beast";

    private const string PigObjectName = "Pig No Root";
    private const string BeastObjectName = "Beast Enemy";

    private const string BeastRoot =
        "Assets/Horror Creatures/Horror Creature - Beast";

    private const string BeastPrefabPath =
        BeastRoot + "/Prefabs/Beast.prefab";

    private const string SourceMaterialPath =
        BeastRoot + "/Materials/Beast.mat";

    private const string IdleClipPath =
        BeastRoot + "/Animation/Beast@Idle1.FBX";

    private const string ChaseClipPath =
        BeastRoot + "/Animation/Beast@Chase.FBX";

    private const string GeneratedFolder =
        "Assets/ShowcaseSamples/BeastEnemy/Generated";

    private const string ControllerPath =
        GeneratedFolder + "/BeastEnemy.controller";

    private const string UrpMaterialPath =
        GeneratedFolder + "/Beast_URP.mat";

    [MenuItem(MenuPath)]
    private static void ReplacePigWithBeast()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError(
                "Exit Play Mode before replacing the enemy model."
            );
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject existingBeast = FindRootObject(scene, BeastObjectName);

        if (existingBeast != null)
        {
            Selection.activeGameObject = existingBeast;
            EditorGUIUtility.PingObject(existingBeast);
            Debug.Log("The scene already contains Beast Enemy.");
            return;
        }

        GameObject pig = FindRootObject(scene, PigObjectName);
        if (pig == null)
        {
            Debug.LogError(
                $"Could not find the root object '{PigObjectName}' " +
                $"in scene '{scene.name}'."
            );
            return;
        }

        GameObject beastPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(BeastPrefabPath);

        if (beastPrefab == null)
        {
            Debug.LogError(
                "Beast.prefab has not finished importing. Try again after " +
                "Unity completes asset import."
            );
            return;
        }

        EnsureFolder(GeneratedFolder);
        SetClipLooping(IdleClipPath);
        SetClipLooping(ChaseClipPath);

        AnimatorController controller = CreateAnimatorController();
        Material urpMaterial = CreateUrpMaterial();

        if (controller == null || urpMaterial == null)
        {
            Debug.LogError(
                "Could not prepare the Beast animator or URP material."
            );
            return;
        }

        GameObject enemyRoot = new GameObject(BeastObjectName);
        Undo.RegisterCreatedObjectUndo(enemyRoot, "Create Beast Enemy");

        enemyRoot.transform.SetPositionAndRotation(
            pig.transform.position,
            pig.transform.rotation
        );
        enemyRoot.transform.localScale = Vector3.one;
        enemyRoot.transform.SetSiblingIndex(pig.transform.GetSiblingIndex());
        enemyRoot.layer = pig.layer;
        enemyRoot.tag = pig.tag;

        CopyRequiredComponents(pig, enemyRoot);
        ConfigureEnemyRoot(enemyRoot);

        GameObject beastVisual = (GameObject)PrefabUtility.InstantiatePrefab(
            beastPrefab,
            scene
        );

        Undo.RegisterCreatedObjectUndo(beastVisual, "Create Beast Visual");
        beastVisual.name = "Beast Visual";
        beastVisual.transform.SetParent(enemyRoot.transform, false);
        beastVisual.transform.localPosition = Vector3.zero;
        beastVisual.transform.localRotation = Quaternion.identity;
        beastVisual.transform.localScale = Vector3.one;

        Animator animator = beastVisual.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogError("The Beast prefab does not contain an Animator.");
            Undo.DestroyObjectImmediate(enemyRoot);
            return;
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        ApplyMaterial(beastVisual, urpMaterial);
        FitVisualToHeight(beastVisual.transform, enemyRoot.transform, 1.85f);

        EnemyLocomotionAnimator locomotion =
            Undo.AddComponent<EnemyLocomotionAnimator>(enemyRoot);

        SerializedObject locomotionObject = new SerializedObject(locomotion);
        locomotionObject.FindProperty("agent").objectReferenceValue =
            enemyRoot.GetComponent<NavMeshAgent>();
        locomotionObject.FindProperty("targetAnimator").objectReferenceValue =
            animator;
        locomotionObject.ApplyModifiedPropertiesWithoutUndo();

        Undo.DestroyObjectImmediate(pig);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = enemyRoot;
        EditorGUIUtility.PingObject(enemyRoot);

        Debug.Log(
            "Pig model replaced with Beast. Navigation, catch trigger, " +
            "shooting stun and knockback components were preserved."
        );
    }

    private static GameObject FindRootObject(Scene scene, string objectName)
    {
        return scene
            .GetRootGameObjects()
            .FirstOrDefault(root => root.name == objectName);
    }

    private static void CopyRequiredComponents(
        GameObject source,
        GameObject destination)
    {
        CopyComponent<Rigidbody>(source, destination);
        CopyComponent<SphereCollider>(source, destination);
        CopyComponent<CapsuleCollider>(source, destination);
        CopyComponent<NavMeshAgent>(source, destination);
        CopyComponent<EnemyPigChase>(source, destination);
        CopyComponent<EnemyPigCatchPlayer>(source, destination);
        CopyComponent<EnemyPigStun>(source, destination);
    }

    private static T CopyComponent<T>(
        GameObject source,
        GameObject destination)
        where T : Component
    {
        T sourceComponent = source.GetComponent<T>();
        if (sourceComponent == null)
        {
            Debug.LogWarning($"{source.name} does not contain {typeof(T).Name}.");
            return null;
        }

        UnityEditorInternal.ComponentUtility.CopyComponent(sourceComponent);
        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(destination);
        return destination.GetComponent<T>();
    }

    private static void ConfigureEnemyRoot(GameObject enemyRoot)
    {
        Rigidbody body = enemyRoot.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.useGravity = false;
            body.isKinematic = true;
        }

        SphereCollider catchTrigger = enemyRoot.GetComponent<SphereCollider>();
        if (catchTrigger != null)
        {
            catchTrigger.isTrigger = true;
            catchTrigger.radius = 0.85f;
            catchTrigger.center = new Vector3(0f, 0.95f, 0f);
        }

        CapsuleCollider hitCollider = enemyRoot.GetComponent<CapsuleCollider>();
        if (hitCollider != null)
        {
            hitCollider.isTrigger = false;
            hitCollider.direction = 1;
            hitCollider.radius = 0.48f;
            hitCollider.height = 1.9f;
            hitCollider.center = new Vector3(0f, 0.95f, 0f);
        }

        NavMeshAgent agent = enemyRoot.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.radius = 0.48f;
            agent.height = 1.9f;
            agent.baseOffset = 0f;
            agent.speed = 3.5f;
            agent.stoppingDistance = 0.2f;
        }

        EnemyPigChase chase = enemyRoot.GetComponent<EnemyPigChase>();
        if (chase != null)
        {
            SerializedObject chaseObject = new SerializedObject(chase);
            SerializedProperty chaseSpeed =
                chaseObject.FindProperty("chaseSpeed");

            if (chaseSpeed != null && chaseSpeed.floatValue <= 0f)
            {
                chaseSpeed.floatValue = 3.5f;
            }

            chaseObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetClipLooping(string modelPath)
    {
        ModelImporter importer =
            AssetImporter.GetAtPath(modelPath) as ModelImporter;

        if (importer == null)
        {
            Debug.LogError($"Could not load ModelImporter for {modelPath}.");
            return;
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }

        bool changed = false;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            if (!clip.loopTime || !clip.loopPose)
            {
                clip.loopTime = true;
                clip.loopPose = true;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    private static AnimatorController CreateAnimatorController()
    {
        AnimatorController existing =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (existing != null)
        {
            return existing;
        }

        AnimationClip idleClip = LoadAnimationClip(IdleClipPath);
        AnimationClip chaseClip = LoadAnimationClip(ChaseClipPath);

        if (idleClip == null || chaseClip == null)
        {
            Debug.LogError("Could not load Beast Idle1 or Chase animation clip.");
            return null;
        }

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine stateMachine =
            controller.layers[0].stateMachine;

        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;

        AnimatorState chaseState = stateMachine.AddState("Chase");
        chaseState.motion = chaseClip;
        chaseState.speed = 1f;

        stateMachine.defaultState = idleState;

        AnimatorStateTransition startMoving =
            idleState.AddTransition(chaseState);
        startMoving.hasExitTime = false;
        startMoving.duration = 0.12f;
        startMoving.AddCondition(
            AnimatorConditionMode.Greater,
            0.08f,
            "Speed"
        );

        AnimatorStateTransition stopMoving =
            chaseState.AddTransition(idleState);
        stopMoving.hasExitTime = false;
        stopMoving.duration = 0.15f;
        stopMoving.AddCondition(
            AnimatorConditionMode.Less,
            0.08f,
            "Speed"
        );

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip LoadAnimationClip(string modelPath)
    {
        return AssetDatabase
            .LoadAllAssetsAtPath(modelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(
                clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.OrdinalIgnoreCase
                )
            );
    }

    private static Material CreateUrpMaterial()
    {
        Material source =
            AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (source == null || urpLit == null)
        {
            Debug.LogError("Could not load Beast material or URP/Lit shader.");
            return null;
        }

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(UrpMaterialPath);

        if (material == null)
        {
            material = new Material(urpLit)
            {
                name = "Beast_URP"
            };
            AssetDatabase.CreateAsset(material, UrpMaterialPath);
        }
        else
        {
            material.shader = urpLit;
        }

        CopyTexture(source, "_MainTex", material, "_BaseMap");
        CopyTexture(source, "_BumpMap", material, "_BumpMap");
        CopyTexture(
            source,
            "_MetallicGlossMap",
            material,
            "_MetallicGlossMap"
        );
        CopyTexture(source, "_OcclusionMap", material, "_OcclusionMap");
        CopyTexture(source, "_EmissionMap", material, "_EmissionMap");

        if (source.HasProperty("_Color"))
        {
            material.SetColor("_BaseColor", source.GetColor("_Color"));
        }

        if (source.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", source.GetFloat("_Metallic"));
        }

        if (source.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
        }

        if (source.HasProperty("_OcclusionStrength"))
        {
            material.SetFloat(
                "_OcclusionStrength",
                source.GetFloat("_OcclusionStrength")
            );
        }

        if (source.HasProperty("_EmissionColor"))
        {
            material.SetColor(
                "_EmissionColor",
                source.GetColor("_EmissionColor")
            );
        }

        SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
        SetKeyword(
            material,
            "_METALLICSPECGLOSSMAP",
            material.GetTexture("_MetallicGlossMap")
        );
        SetKeyword(
            material,
            "_OCCLUSIONMAP",
            material.GetTexture("_OcclusionMap")
        );
        SetKeyword(
            material,
            "_EMISSION",
            material.GetTexture("_EmissionMap")
        );

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void CopyTexture(
        Material source,
        string sourceProperty,
        Material destination,
        string destinationProperty)
    {
        if (!source.HasProperty(sourceProperty) ||
            !destination.HasProperty(destinationProperty))
        {
            return;
        }

        destination.SetTexture(
            destinationProperty,
            source.GetTexture(sourceProperty)
        );
        destination.SetTextureScale(
            destinationProperty,
            source.GetTextureScale(sourceProperty)
        );
        destination.SetTextureOffset(
            destinationProperty,
            source.GetTextureOffset(sourceProperty)
        );
    }

    private static void SetKeyword(
        Material material,
        string keyword,
        Texture texture)
    {
        if (texture != null)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }

    private static void ApplyMaterial(
        GameObject beastVisual,
        Material material)
    {
        foreach (Renderer renderer in
                 beastVisual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                materials[index] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static void FitVisualToHeight(
        Transform visual,
        Transform enemyRoot,
        float targetHeight)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
        {
            bounds.Encapsulate(renderer.bounds);
        }

        if (bounds.size.y > 0.001f)
        {
            float uniformScale = targetHeight / bounds.size.y;
            visual.localScale = Vector3.one * uniformScale;
        }

        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
        {
            bounds.Encapsulate(renderer.bounds);
        }

        visual.position += Vector3.up *
            (enemyRoot.position.y - bounds.min.y);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }
            current = next;
        }
    }
}
