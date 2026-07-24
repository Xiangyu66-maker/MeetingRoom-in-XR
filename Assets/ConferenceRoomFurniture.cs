using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Furniture Generator")]
public sealed class ConferenceRoomFurniture : MonoBehaviour
{
    private const string FurnitureRootName = "Generated Furniture";

    [SerializeField] private Vector3 furnitureAnchor = new Vector3(0f, 0f, -6f);
    [SerializeField] private bool generateInEditMode = true;

    private Material woodMaterial;
    private Material darkWoodMaterial;
    private Material metalMaterial;
    private Material chairFabricMaterial;
    private Material blackPlasticMaterial;
    private Material screenMaterial;
    private Material glassMaterial;
    private Material projectionScreenMaterial;
    private Material projectorBeamMaterial;
    private Material windowGlassMaterial;
    private Material windowFrameMaterial;
    private Material skylineMaterial;
    private Material potMaterial;
    private Material soilMaterial;
    private Material plantLeafMaterial;
    private Material plantStemMaterial;
    private Material paperMaterial;
    private Material ceramicMaterial;

    private void OnEnable()
    {
        if (transform.Find(FurnitureRootName) != null)
        {
            return;
        }

        if (!Application.isPlaying && !generateInEditMode)
        {
            return;
        }

        GenerateFurniture();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!isActiveAndEnabled || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (this == null || !isActiveAndEnabled || !generateInEditMode || transform.Find(FurnitureRootName) != null)
            {
                return;
            }

            GenerateFurniture();
        };
#endif
    }

    [ContextMenu("Regenerate Furniture")]
    public void GenerateFurniture()
    {
        if (!Application.isPlaying && !generateInEditMode)
        {
            return;
        }

        CreateMaterials();
        ClearExistingFurniture();

        Transform root = new GameObject(FurnitureRootName).transform;
        root.SetParent(transform, false);
        root.localPosition = furnitureAnchor;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        BuildMainTable(root);
        BuildConferenceChairs(root);
        BuildComputerDesk(root);
        BuildPresentationSetup(root);
        BuildWallWindows(root);
        BuildPlants(root);
        BuildTableAccessories(root);

#if UNITY_EDITOR
        if (!Application.isPlaying && gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void CreateMaterials()
    {
        woodMaterial = CreateMaterial("Furniture Warm Wood", new Color(0.50f, 0.31f, 0.16f), 0.28f);
        darkWoodMaterial = CreateMaterial("Furniture Dark Edge", new Color(0.20f, 0.12f, 0.07f), 0.35f);
        metalMaterial = CreateMaterial("Furniture Brushed Metal", new Color(0.45f, 0.48f, 0.50f), 0.72f);
        chairFabricMaterial = CreateMaterial("Chair Deep Green Fabric", new Color(0.05f, 0.24f, 0.20f), 0.18f);
        blackPlasticMaterial = CreateMaterial("Computer Matte Black", new Color(0.015f, 0.017f, 0.019f), 0.45f);
        glassMaterial = CreateMaterial("Monitor Glass", new Color(0.03f, 0.07f, 0.11f), 0.86f);
        screenMaterial = CreateMaterial("Monitor Soft Blue Screen", new Color(0.12f, 0.42f, 0.78f), 0.55f, 1.6f);
        projectionScreenMaterial = CreateMaterial("Projection Screen Fabric", new Color(0.86f, 0.88f, 0.84f), 0.18f);
        projectorBeamMaterial = CreateTransparentMaterial("Soft Projector Beam", new Color(0.46f, 0.66f, 1.0f, 0.18f), 0.2f);
        windowGlassMaterial = CreateTransparentMaterial("Cool Window Glass", new Color(0.28f, 0.55f, 0.72f, 0.62f), 0.82f);
        windowFrameMaterial = CreateMaterial("Window Dark Aluminum", new Color(0.08f, 0.09f, 0.10f), 0.58f);
        skylineMaterial = CreateMaterial("Muted Outdoor Skyline", new Color(0.22f, 0.34f, 0.44f), 0.25f, 0.25f);
        potMaterial = CreateMaterial("Plant Clay Pot", new Color(0.42f, 0.18f, 0.10f), 0.24f);
        soilMaterial = CreateMaterial("Plant Soil", new Color(0.10f, 0.07f, 0.04f), 0.08f);
        plantLeafMaterial = CreateMaterial("Plant Deep Leaves", new Color(0.05f, 0.36f, 0.16f), 0.26f);
        plantStemMaterial = CreateMaterial("Plant Stems", new Color(0.17f, 0.26f, 0.08f), 0.22f);
        paperMaterial = CreateMaterial("Meeting Paper", new Color(0.92f, 0.90f, 0.82f), 0.12f);
        ceramicMaterial = CreateMaterial("White Ceramic", new Color(0.90f, 0.88f, 0.80f), 0.45f);
    }

    private Material CreateMaterial(string materialName, Color color, float smoothness, float emission = 0f)
    {
        Shader shader = Shader.Find("Standard");
        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Diffuse"));
        material.name = materialName;
        material.SetColor("_Color", color);
        material.SetFloat("_Glossiness", smoothness);

        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return material;
    }

    private Material CreateTransparentMaterial(string materialName, Color color, float smoothness)
    {
        Material material = CreateMaterial(materialName, color, smoothness);
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }

    private void ClearExistingFurniture()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name != FurnitureRootName)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void BuildMainTable(Transform root)
    {
        Transform table = CreateGroup("Large Conference Table", root, new Vector3(0f, 0f, 0f));

        AddBox(table, "Wood Table Top", new Vector3(0f, 0.78f, 0f), new Vector3(6.0f, 0.18f, 2.1f), woodMaterial);
        AddBox(table, "Dark Inset Edge", new Vector3(0f, 0.89f, 0f), new Vector3(5.55f, 0.035f, 1.68f), darkWoodMaterial);
        AddBox(table, "Cable Access Panel", new Vector3(0f, 0.915f, 0f), new Vector3(1.0f, 0.025f, 0.24f), blackPlasticMaterial);

        AddTableLeg(table, "Front Left Leg", -2.65f, 0.78f);
        AddTableLeg(table, "Front Right Leg", 2.65f, 0.78f);
        AddTableLeg(table, "Back Left Leg", -2.65f, -0.78f);
        AddTableLeg(table, "Back Right Leg", 2.65f, -0.78f);

        AddBox(table, "Left Metal Apron", new Vector3(-2.65f, 0.63f, 0f), new Vector3(0.12f, 0.12f, 1.75f), metalMaterial);
        AddBox(table, "Right Metal Apron", new Vector3(2.65f, 0.63f, 0f), new Vector3(0.12f, 0.12f, 1.75f), metalMaterial);
    }

    private void AddTableLeg(Transform parent, string name, float x, float z)
    {
        AddCylinder(parent, name, new Vector3(x, 0.36f, z), 0.16f, 0.72f, metalMaterial);
    }

    private void BuildConferenceChairs(Transform root)
    {
        float[] xPositions = { -2.25f, -0.75f, 0.75f, 2.25f };

        foreach (float x in xPositions)
        {
            BuildChair(root, new Vector3(x, 0f, -1.85f), 0f);
            BuildChair(root, new Vector3(x, 0f, 1.85f), 180f);
        }

        BuildChair(root, new Vector3(-3.65f, 0f, 0f), 90f);
        BuildChair(root, new Vector3(3.65f, 0f, 0f), -90f);
    }

    private void BuildChair(Transform parent, Vector3 position, float yRotation)
    {
        Transform chair = CreateGroup("Conference Chair", parent, position);
        chair.localRotation = Quaternion.Euler(0f, yRotation, 0f);

        AddBox(chair, "Seat Cushion", new Vector3(0f, 0.48f, 0f), new Vector3(0.82f, 0.16f, 0.76f), chairFabricMaterial);
        AddBox(chair, "Seat Base", new Vector3(0f, 0.40f, 0f), new Vector3(0.88f, 0.08f, 0.82f), blackPlasticMaterial);
        AddBox(chair, "Angled Back Cushion", new Vector3(0f, 0.98f, -0.39f), new Vector3(0.88f, 0.95f, 0.14f), chairFabricMaterial, Quaternion.Euler(-8f, 0f, 0f));
        AddBox(chair, "Back Frame", new Vector3(0f, 0.95f, -0.48f), new Vector3(0.96f, 0.1f, 0.08f), metalMaterial);

        AddBox(chair, "Left Arm Rest", new Vector3(-0.54f, 0.66f, 0f), new Vector3(0.1f, 0.1f, 0.72f), blackPlasticMaterial);
        AddBox(chair, "Right Arm Rest", new Vector3(0.54f, 0.66f, 0f), new Vector3(0.1f, 0.1f, 0.72f), blackPlasticMaterial);
        AddBox(chair, "Left Arm Support", new Vector3(-0.54f, 0.52f, 0.24f), new Vector3(0.08f, 0.28f, 0.08f), metalMaterial);
        AddBox(chair, "Right Arm Support", new Vector3(0.54f, 0.52f, 0.24f), new Vector3(0.08f, 0.28f, 0.08f), metalMaterial);

        AddChairLeg(chair, "Front Left Chair Leg", -0.32f, 0.27f);
        AddChairLeg(chair, "Front Right Chair Leg", 0.32f, 0.27f);
        AddChairLeg(chair, "Rear Left Chair Leg", -0.32f, -0.27f);
        AddChairLeg(chair, "Rear Right Chair Leg", 0.32f, -0.27f);
    }

    private void AddChairLeg(Transform parent, string name, float x, float z)
    {
        AddCylinder(parent, name, new Vector3(x, 0.24f, z), 0.07f, 0.48f, metalMaterial);
    }

    private void BuildComputerDesk(Transform root)
    {
        Transform desk = CreateGroup("Computer Desk", root, new Vector3(-4.9f, 0f, -0.25f));

        AddBox(desk, "Side Desk Top", new Vector3(0f, 0.74f, 0f), new Vector3(2.35f, 0.16f, 1.18f), woodMaterial);
        AddBox(desk, "Side Desk Back Edge", new Vector3(0f, 0.89f, -0.55f), new Vector3(2.35f, 0.14f, 0.08f), darkWoodMaterial);
        AddBox(desk, "Side Desk Front Edge", new Vector3(0f, 0.84f, 0.55f), new Vector3(2.35f, 0.08f, 0.08f), darkWoodMaterial);

        AddTableLeg(desk, "Desk Front Left Leg", -0.95f, 0.42f);
        AddTableLeg(desk, "Desk Front Right Leg", 0.95f, 0.42f);
        AddTableLeg(desk, "Desk Rear Left Leg", -0.95f, -0.42f);
        AddTableLeg(desk, "Desk Rear Right Leg", 0.95f, -0.42f);

        BuildComputer(desk);
    }

    private void BuildComputer(Transform desk)
    {
        Transform computer = CreateGroup("Desktop Computer", desk, new Vector3(0f, 0f, 0f));

        AddBox(computer, "Monitor Bezel", new Vector3(0f, 1.43f, -0.44f), new Vector3(1.28f, 0.76f, 0.08f), blackPlasticMaterial);
        AddBox(computer, "Monitor Glass", new Vector3(0f, 1.43f, -0.49f), new Vector3(1.12f, 0.58f, 0.018f), glassMaterial);
        AddBox(computer, "Blue Screen", new Vector3(0f, 1.43f, -0.505f), new Vector3(1.02f, 0.48f, 0.012f), screenMaterial);

        AddBox(computer, "Screen Header Bar", new Vector3(0f, 1.62f, -0.514f), new Vector3(0.82f, 0.055f, 0.012f), screenMaterial);
        AddBox(computer, "Screen Content Block", new Vector3(-0.24f, 1.44f, -0.516f), new Vector3(0.38f, 0.18f, 0.012f), screenMaterial);
        AddBox(computer, "Screen Side Panel", new Vector3(0.34f, 1.39f, -0.516f), new Vector3(0.22f, 0.30f, 0.012f), screenMaterial);

        AddCylinder(computer, "Monitor Neck", new Vector3(0f, 1.01f, -0.34f), 0.1f, 0.42f, metalMaterial);
        AddBox(computer, "Monitor Base", new Vector3(0f, 0.82f, -0.28f), new Vector3(0.62f, 0.06f, 0.34f), metalMaterial);

        AddBox(computer, "Keyboard", new Vector3(-0.12f, 0.86f, 0.18f), new Vector3(1.05f, 0.045f, 0.30f), blackPlasticMaterial);
        AddBox(computer, "Keyboard Key Row", new Vector3(-0.12f, 0.895f, 0.18f), new Vector3(0.92f, 0.018f, 0.20f), metalMaterial);
        AddBox(computer, "Mouse", new Vector3(0.72f, 0.86f, 0.20f), new Vector3(0.22f, 0.06f, 0.30f), blackPlasticMaterial);

        AddBox(computer, "Computer Tower", new Vector3(0.98f, 1.04f, -0.18f), new Vector3(0.34f, 0.62f, 0.48f), blackPlasticMaterial);
        AddBox(computer, "Tower Front Panel", new Vector3(0.98f, 1.04f, 0.07f), new Vector3(0.25f, 0.48f, 0.025f), metalMaterial);
        AddBox(computer, "Power Light", new Vector3(0.98f, 1.20f, 0.09f), new Vector3(0.08f, 0.08f, 0.018f), screenMaterial);

        GameObject glow = new GameObject("Monitor Glow Light");
        glow.transform.SetParent(computer, false);
        glow.transform.localPosition = new Vector3(0f, 1.43f, -0.68f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.25f, 0.55f, 1f);
        light.range = 2.0f;
        light.intensity = 0.35f;
    }

    private void BuildPresentationSetup(Transform root)
    {
        Transform presentation = CreateGroup("Presentation Setup", root, Vector3.zero);

        AddBox(presentation, "Projection Screen Surface", new Vector3(0f, 2.75f, -10.85f), new Vector3(5.75f, 2.55f, 0.045f), projectionScreenMaterial);
        AddBox(presentation, "Projection Screen Top Case", new Vector3(0f, 4.1f, -10.80f), new Vector3(6.05f, 0.16f, 0.18f), darkWoodMaterial);
        AddBox(presentation, "Projection Screen Bottom Bar", new Vector3(0f, 1.42f, -10.78f), new Vector3(5.9f, 0.09f, 0.12f), metalMaterial);
        AddBox(presentation, "Left Screen Rail", new Vector3(-2.98f, 2.75f, -10.79f), new Vector3(0.08f, 2.68f, 0.10f), metalMaterial);
        AddBox(presentation, "Right Screen Rail", new Vector3(2.98f, 2.75f, -10.79f), new Vector3(0.08f, 2.68f, 0.10f), metalMaterial);
        AddCylinder(presentation, "Screen Pull Cord", new Vector3(2.72f, 3.38f, -10.70f), 0.025f, 1.2f, blackPlasticMaterial);
        AddSphere(presentation, "Screen Pull Bead", new Vector3(2.72f, 2.72f, -10.70f), new Vector3(0.10f, 0.10f, 0.10f), blackPlasticMaterial);

        Transform projector = CreateGroup("Ceiling Projector", presentation, new Vector3(0f, 3.55f, -4.9f));
        AddCylinder(projector, "Projector Ceiling Cable", new Vector3(0f, 0.42f, 0f), 0.045f, 0.78f, metalMaterial);
        AddBox(projector, "Projector Ceiling Plate", new Vector3(0f, 0.78f, 0f), new Vector3(0.76f, 0.05f, 0.56f), metalMaterial);
        AddBox(projector, "Projector Body", new Vector3(0f, 0f, 0f), new Vector3(1.05f, 0.28f, 0.72f), ceramicMaterial);
        AddBox(projector, "Projector Vent Slots", new Vector3(-0.31f, 0.015f, 0.37f), new Vector3(0.34f, 0.07f, 0.025f), blackPlasticMaterial);
        AddCylinder(projector, "Projector Lens", new Vector3(0f, -0.01f, -0.42f), 0.24f, 0.20f, glassMaterial, Quaternion.Euler(90f, 0f, 0f));
        AddBox(projector, "Projector Status Light", new Vector3(0.38f, 0.04f, -0.37f), new Vector3(0.10f, 0.04f, 0.025f), screenMaterial);

        GameObject beam = AddBox(presentation, "Visible Projector Beam", new Vector3(0f, 2.78f, -7.85f), new Vector3(4.6f, 1.8f, 0.035f), projectorBeamMaterial, Quaternion.Euler(-5f, 0f, 0f));
        RemoveCollider(beam);
        AddSpotLight(presentation, "Projector Spot Light", new Vector3(0f, 3.45f, -5.28f), Quaternion.LookRotation(new Vector3(0f, -0.18f, -1f)), new Color(0.55f, 0.70f, 1f), 7.5f, 32f, 0.65f);
    }

    private void BuildWallWindows(Transform root)
    {
        Transform windows = CreateGroup("Wall Windows", root, Vector3.zero);

        BuildWindow(windows, "Left Window Front", new Vector3(-6.35f, 2.48f, -1.7f), 90f);
        BuildWindow(windows, "Left Window Rear", new Vector3(-6.35f, 2.48f, -6.1f), 90f);
        BuildWindow(windows, "Right Window Front", new Vector3(6.35f, 2.48f, -1.7f), -90f);
        BuildWindow(windows, "Right Window Rear", new Vector3(6.35f, 2.48f, -6.1f), -90f);
    }

    private void BuildWindow(Transform parent, string name, Vector3 position, float yRotation)
    {
        Transform window = CreateGroup(name, parent, position);
        window.localRotation = Quaternion.Euler(0f, yRotation, 0f);

        AddBox(window, "Outdoor View", new Vector3(0f, 0f, 0.022f), new Vector3(1.78f, 1.06f, 0.018f), skylineMaterial);
        AddBox(window, "Glass Pane", new Vector3(0f, 0f, -0.005f), new Vector3(2.0f, 1.34f, 0.032f), windowGlassMaterial);
        AddBox(window, "Top Frame", new Vector3(0f, 0.73f, 0f), new Vector3(2.18f, 0.09f, 0.08f), windowFrameMaterial);
        AddBox(window, "Bottom Frame", new Vector3(0f, -0.73f, 0f), new Vector3(2.18f, 0.09f, 0.08f), windowFrameMaterial);
        AddBox(window, "Left Frame", new Vector3(-1.08f, 0f, 0f), new Vector3(0.09f, 1.48f, 0.08f), windowFrameMaterial);
        AddBox(window, "Right Frame", new Vector3(1.08f, 0f, 0f), new Vector3(0.09f, 1.48f, 0.08f), windowFrameMaterial);
        AddBox(window, "Vertical Mullion", new Vector3(0f, 0f, -0.02f), new Vector3(0.07f, 1.36f, 0.08f), windowFrameMaterial);
        AddBox(window, "Horizontal Mullion", new Vector3(0f, 0f, -0.02f), new Vector3(2.0f, 0.055f, 0.08f), windowFrameMaterial);

        for (int i = 0; i < 4; i++)
        {
            AddBox(window, "Blind Slat", new Vector3(0f, 0.42f - i * 0.23f, -0.045f), new Vector3(1.84f, 0.025f, 0.05f), ceramicMaterial);
        }
    }

    private void BuildPlants(Transform root)
    {
        Transform plants = CreateGroup("Indoor Plants", root, Vector3.zero);

        BuildPottedPlant(plants, "Left Back Floor Plant", new Vector3(-5.6f, 0f, -9.55f), 1.05f);
        BuildPottedPlant(plants, "Right Back Floor Plant", new Vector3(5.35f, 0f, -9.45f), 0.95f);
        BuildPottedPlant(plants, "Entry Side Floor Plant", new Vector3(5.55f, 0f, 2.4f), 0.82f);
        BuildDesktopPlant(plants, "Small Table Plant", new Vector3(2.42f, 0.90f, 0.48f));
    }

    private void BuildPottedPlant(Transform parent, string name, Vector3 position, float scale)
    {
        Transform plant = CreateGroup(name, parent, position);
        plant.localScale = new Vector3(scale, scale, scale);

        AddCylinder(plant, "Plant Saucer", new Vector3(0f, 0.07f, 0f), 0.72f, 0.08f, darkWoodMaterial);
        AddCylinder(plant, "Clay Pot", new Vector3(0f, 0.34f, 0f), 0.58f, 0.58f, potMaterial);
        AddCylinder(plant, "Soil", new Vector3(0f, 0.65f, 0f), 0.50f, 0.06f, soilMaterial);
        AddCylinder(plant, "Main Stem", new Vector3(0f, 1.05f, 0f), 0.07f, 0.78f, plantStemMaterial);
        AddCylinder(plant, "Side Stem Left", new Vector3(-0.16f, 0.98f, 0.02f), 0.045f, 0.58f, plantStemMaterial, Quaternion.Euler(0f, 0f, -25f));
        AddCylinder(plant, "Side Stem Right", new Vector3(0.17f, 1.02f, -0.02f), 0.045f, 0.62f, plantStemMaterial, Quaternion.Euler(0f, 0f, 28f));

        AddLeaf(plant, "Leaf Crown", new Vector3(0f, 1.48f, 0f), new Vector3(0.72f, 0.36f, 0.58f), Quaternion.identity);
        AddLeaf(plant, "Leaf Left", new Vector3(-0.34f, 1.22f, 0.08f), new Vector3(0.62f, 0.22f, 0.36f), Quaternion.Euler(0f, 18f, 18f));
        AddLeaf(plant, "Leaf Right", new Vector3(0.34f, 1.27f, -0.04f), new Vector3(0.62f, 0.24f, 0.36f), Quaternion.Euler(0f, -18f, -16f));
        AddLeaf(plant, "Leaf Forward", new Vector3(0.04f, 1.17f, 0.34f), new Vector3(0.42f, 0.22f, 0.62f), Quaternion.Euler(12f, 0f, 0f));
        AddLeaf(plant, "Leaf Back", new Vector3(-0.04f, 1.25f, -0.32f), new Vector3(0.40f, 0.20f, 0.56f), Quaternion.Euler(-12f, 0f, 0f));
    }

    private void BuildDesktopPlant(Transform parent, string name, Vector3 position)
    {
        Transform plant = CreateGroup(name, parent, position);
        AddCylinder(plant, "Small Plant Pot", new Vector3(0f, 0.11f, 0f), 0.30f, 0.22f, potMaterial);
        AddCylinder(plant, "Small Plant Soil", new Vector3(0f, 0.24f, 0f), 0.25f, 0.04f, soilMaterial);
        AddLeaf(plant, "Small Leaf Cluster", new Vector3(0f, 0.44f, 0f), new Vector3(0.44f, 0.24f, 0.40f), Quaternion.identity);
        AddLeaf(plant, "Small Side Leaves", new Vector3(0.10f, 0.34f, 0.08f), new Vector3(0.36f, 0.14f, 0.28f), Quaternion.Euler(0f, -20f, 20f));
    }

    private void AddLeaf(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
    {
        AddSphere(parent, name, localPosition, localScale, plantLeafMaterial, localRotation);
    }

    private void BuildTableAccessories(Transform root)
    {
        Transform accessories = CreateGroup("Meeting Table Accessories", root, Vector3.zero);

        AddBox(accessories, "Open Notebook Left", new Vector3(-1.55f, 0.995f, -0.42f), new Vector3(0.76f, 0.022f, 0.46f), paperMaterial, Quaternion.Euler(0f, 8f, 0f));
        AddBox(accessories, "Open Notebook Right", new Vector3(1.42f, 0.995f, 0.38f), new Vector3(0.72f, 0.022f, 0.46f), paperMaterial, Quaternion.Euler(0f, -10f, 0f));
        AddBox(accessories, "Dark Folder", new Vector3(-0.55f, 0.998f, 0.46f), new Vector3(0.62f, 0.026f, 0.42f), darkWoodMaterial, Quaternion.Euler(0f, -18f, 0f));
        AddBox(accessories, "Presentation Remote", new Vector3(0.72f, 1.002f, -0.40f), new Vector3(0.36f, 0.035f, 0.13f), blackPlasticMaterial, Quaternion.Euler(0f, 15f, 0f));

        AddCylinder(accessories, "Conference Speaker", new Vector3(0f, 1.01f, 0f), 0.62f, 0.10f, blackPlasticMaterial);
        AddCylinder(accessories, "Speaker Metal Ring", new Vector3(0f, 1.075f, 0f), 0.48f, 0.025f, metalMaterial);
        AddBox(accessories, "Speaker Button", new Vector3(0f, 1.10f, -0.18f), new Vector3(0.16f, 0.018f, 0.05f), screenMaterial);

        AddMug(accessories, "Mug Left", new Vector3(-2.15f, 0.98f, 0.52f));
        AddMug(accessories, "Mug Right", new Vector3(2.08f, 0.98f, -0.52f));
        AddBox(accessories, "Pen Left", new Vector3(-1.32f, 1.03f, -0.08f), new Vector3(0.04f, 0.025f, 0.46f), blackPlasticMaterial, Quaternion.Euler(0f, 33f, 0f));
        AddBox(accessories, "Pen Right", new Vector3(1.10f, 1.03f, 0.03f), new Vector3(0.04f, 0.025f, 0.42f), screenMaterial, Quaternion.Euler(0f, -38f, 0f));
    }

    private void AddMug(Transform parent, string name, Vector3 position)
    {
        Transform mug = CreateGroup(name, parent, position);
        AddCylinder(mug, "Mug Body", new Vector3(0f, 0.11f, 0f), 0.24f, 0.22f, ceramicMaterial);
        AddCylinder(mug, "Coffee Surface", new Vector3(0f, 0.235f, 0f), 0.18f, 0.018f, darkWoodMaterial);
        AddBox(mug, "Mug Handle", new Vector3(0.15f, 0.12f, 0f), new Vector3(0.055f, 0.14f, 0.12f), ceramicMaterial);
    }

    private Transform CreateGroup(string name, Transform parent, Vector3 localPosition)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        group.transform.localPosition = localPosition;
        group.transform.localRotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;
        return group.transform;
    }

    private GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        return AddBox(parent, name, localPosition, localScale, material, Quaternion.identity);
    }

    private GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localRotation = localRotation;
        box.transform.localScale = localScale;
        ApplyMaterial(box, material);
        return box;
    }

    private GameObject AddCylinder(Transform parent, string name, Vector3 localPosition, float diameter, float height, Material material)
    {
        return AddCylinder(parent, name, localPosition, diameter, height, material, Quaternion.identity);
    }

    private GameObject AddCylinder(Transform parent, string name, Vector3 localPosition, float diameter, float height, Material material, Quaternion localRotation)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localRotation = localRotation;
        cylinder.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
        ApplyMaterial(cylinder, material);
        return cylinder;
    }

    private GameObject AddSphere(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        return AddSphere(parent, name, localPosition, localScale, material, Quaternion.identity);
    }

    private GameObject AddSphere(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Quaternion localRotation)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = localPosition;
        sphere.transform.localRotation = localRotation;
        sphere.transform.localScale = localScale;
        ApplyMaterial(sphere, material);
        return sphere;
    }

    private void AddSpotLight(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Color color, float range, float spotAngle, float intensity)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localRotation = localRotation;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.range = range;
        light.spotAngle = spotAngle;
        light.intensity = intensity;
        light.shadows = LightShadows.Soft;
    }

    private void ApplyMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(collider);
        }
        else
        {
            DestroyImmediate(collider);
        }
    }
}
