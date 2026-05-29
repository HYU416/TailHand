using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;

public class BreakObjectWindow : EditorWindow
{
    GameObject targetObject;

    int gridX = 5;
    int gridY = 5;
    int gridZ = 5;

    float minRandomScale = 0.8f;
    float maxRandomScale = 1.0f;

    bool addRigidbody = true;
    float rigidbodyMass = 0.2f;

    Material fragmentMaterial;

    [MenuItem("Tools/ProBuilder Cube Fracture")]
    static void Open()
    {
        GetWindow<BreakObjectWindow>("ProBuilder Cube Fracture");
    }

    void OnGUI()
    {
        GUILayout.Label("ProBuilder Cube Fracture", EditorStyles.boldLabel);
        GUILayout.Space(10);

        targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Target Object",
            targetObject,
            typeof(GameObject),
            true
        );

        gridX = EditorGUILayout.IntSlider("Grid X", gridX, 1, 30);
        gridY = EditorGUILayout.IntSlider("Grid Y", gridY, 1, 30);
        gridZ = EditorGUILayout.IntSlider("Grid Z", gridZ, 1, 30);

        GUILayout.Space(10);

        minRandomScale = EditorGUILayout.Slider(
            "Min Random Scale",
            minRandomScale,
            0.1f,
            1.0f
        );

        maxRandomScale = EditorGUILayout.Slider(
            "Max Random Scale",
            maxRandomScale,
            0.1f,
            1.0f
        );

        GUILayout.Space(10);

        fragmentMaterial = (Material)EditorGUILayout.ObjectField(
            "Fragment Material",
            fragmentMaterial,
            typeof(Material),
            false
        );

        GUILayout.Space(10);

        addRigidbody = EditorGUILayout.Toggle("Add Rigidbody", addRigidbody);
        rigidbodyMass = EditorGUILayout.FloatField("Mass", rigidbodyMass);

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Cube Fragments"))
        {
            GenerateFragments();
        }
    }

    void GenerateFragments()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target Object Missing");
            return;
        }

        Renderer targetRenderer = targetObject.GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError("Renderer Missing");
            return;
        }

        Bounds bounds = targetRenderer.bounds;

        GameObject root = new GameObject(targetObject.name + "_CubeFragments");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(root, "Create Cube Fragments");

        Vector3 cellSize = new Vector3(
            bounds.size.x / gridX,
            bounds.size.y / gridY,
            bounds.size.z / gridZ
        );

        Material useMaterial =
            fragmentMaterial != null
            ? fragmentMaterial
            : targetRenderer.sharedMaterial;

        int index = 0;

        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                for (int z = 0; z < gridZ; z++)
                {
                    Vector3 center = bounds.min + new Vector3(
                        cellSize.x * (x + 0.5f),
                        cellSize.y * (y + 0.5f),
                        cellSize.z * (z + 0.5f)
                    );

                    float randomScale = Random.Range(
                        minRandomScale,
                        maxRandomScale
                    );

                    Vector3 finalSize = cellSize * randomScale;

                    GameObject fragment = ShapeFactory.Instantiate<Cube>();
                    fragment.name = "Fragment_" + index;

                    Undo.RegisterCreatedObjectUndo(
                        fragment,
                        "Create Fragment"
                    );

                    fragment.transform.SetParent(root.transform, true);
                    fragment.transform.position = center;
                    fragment.transform.rotation = Quaternion.identity;
                    fragment.transform.localScale = finalSize;

                    Renderer fragmentRenderer =
                        fragment.GetComponent<Renderer>();

                    if (fragmentRenderer != null && useMaterial != null)
                    {
                        fragmentRenderer.sharedMaterial = useMaterial;
                    }

                    if (addRigidbody)
                    {
                        Rigidbody rb = fragment.AddComponent<Rigidbody>();
                        rb.mass = rigidbodyMass;
                    }

                    index++;
                }
            }
        }

        Selection.activeGameObject = root;

        Debug.Log("Cube Fragment Complete");
    }
}