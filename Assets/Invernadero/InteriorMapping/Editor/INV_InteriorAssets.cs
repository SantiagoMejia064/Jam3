using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit tools only. No automatic scene edits or Play Mode execution.</summary>
public static class INV_InteriorAssets
{
    const string Root = "Assets/Invernadero/InteriorMapping";
    static readonly string[] Names = { "PX", "NX", "PY", "NY", "PZ", "NZ" };
    static readonly CubemapFace[] Faces = { CubemapFace.PositiveX, CubemapFace.NegativeX,
        CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ };

    [MenuItem("Jam3/Invernadero/Interior/1 Crear cubemap y material HDR 512")]
    public static void CreateAssets() => BuildAssets(512);

    [MenuItem("Jam3/Invernadero/Interior/1b Crear cubemap y material HDR 1024")]
    public static void CreateHighResolutionAssets() => BuildAssets(1024);

    static void BuildAssets(int faceResolution)
    {
        var textures = new Texture2D[6];
        int size = 0;
        for (int i = 0; i < 6; i++)
        {
            string path = $"{Root}/Textures/INV_{Names[i]}.exr";
            if (!File.Exists(path)) { Debug.LogError("Falta cara: " + path); return; }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("No se pudo importar: " + path); return; }
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = false;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = faceResolution;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = importer.GetDefaultPlatformTextureSettings();
            settings.format = TextureImporterFormat.RGBAHalf;
            settings.maxTextureSize = faceResolution;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (textures[i] == null || textures[i].width != textures[i].height ||
                (size > 0 && textures[i].width != size))
            { Debug.LogError("Las seis caras deben tener igual tamaño cuadrado."); return; }
            size = textures[i].width;
        }
        var cube = new Cubemap(size, TextureFormat.RGBAHalf, true) { name = "INV_Interior_HDR",
            wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        for (int i = 0; i < 6; i++) cube.SetPixels(textures[i].GetPixels(), Faces[i]);
        cube.Apply(true, true);
        string cubePath = Root + "/INV_Interior_HDR.asset";
        var stored = AssetDatabase.LoadAssetAtPath<Cubemap>(cubePath);
        if (stored) { EditorUtility.CopySerialized(cube, stored); Object.DestroyImmediate(cube); }
        else { AssetDatabase.CreateAsset(cube, cubePath); stored = cube; }
        Shader shader = Shader.Find("Jam3/Invernadero/Interior Mapping URP");
        if (!shader) { Debug.LogError("Falta shader INV_InteriorMapping."); return; }
        string matPath = Root + "/INV_Ventanal_Interior.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, matPath); }
        material.shader = shader;
        material.SetTexture("_InteriorCube", stored);
        material.SetVector("_RoomSize", new Vector4(30, 12, 20, 0));
        material.SetFloat("_DepthScale", 1);
        EditorUtility.SetDirty(stored); EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
        Selection.activeObject = material;
        Debug.Log("Cubemap HDR y material creados. No se modifico ninguna escena.");
    }

    [MenuItem("Jam3/Invernadero/Interior/2 Crear plano bajo objeto seleccionado")]
    public static void CreatePlane()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/INV_Ventanal_Interior.mat");
        if (!material) { Debug.LogError("Primero ejecutar Crear cubemap y material HDR."); return; }
        var mesh = new Mesh { name = "INV_Ventana_Unidad_XY" };
        mesh.vertices = new[] { new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),
            new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0,2,1,0,3,2 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        const string meshPath = Root + "/INV_Ventana_Unidad_XY.asset";
        var stored = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (!stored) { AssetDatabase.CreateAsset(mesh, meshPath); stored = mesh; }
        else Object.DestroyImmediate(mesh);
        var plane = new GameObject("INV_Plano_InteriorMapping"); Undo.RegisterCreatedObjectUndo(plane,"Crear plano Interior Mapping");
        plane.transform.SetParent(Selection.activeTransform, false);
        plane.AddComponent<MeshFilter>().sharedMesh = stored;
        plane.AddComponent<MeshRenderer>().sharedMaterial = material;
        plane.transform.localScale = new Vector3(18,7.3f,1);
        if (plane.transform.parent && plane.transform.parent.name.Contains("Ventanal"))
        {
            plane.transform.localPosition = new Vector3(-6,4.75f,-2.45f);
            plane.transform.localRotation = Quaternion.Euler(0,180,0);
        }
        Selection.activeGameObject = plane;
        Debug.Log("Plano creado; colocarlo en el hueco. Frente local -Z; interior local +Z. Sin collider.");
    }
}
