using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BOD_SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Bodega.unity";
    private static Material dark, metal, white, cyan, dirt, tray1, tray2, tray3, tray4;

    [MenuItem("Jam3/Construir Bodega Fría")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Sal de Play antes de reconstruir Bodega."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = SceneManager.GetActiveScene().path == ScenePath
            ? SceneManager.GetActiveScene() : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var old = GameObject.Find("BOD_Generated");
        if (old) UnityEngine.Object.DestroyImmediate(old);
        MakeMaterials();

        var root = new GameObject("BOD_Generated");
        root.transform.position = Vector3.zero;
        Environment(root.transform);
        Trays(root.transform);
        KeypadAndChamber(root.transform);
        Story(root.transform);
        StampSetup.ConfigureBodega();
        Doors(root.transform);
        ConfigureArrival(scene, root.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Bodega Fría construida y guardada.");
    }

    private static void MakeMaterials()
    {
        dark = Mat("BOD_AzulOscuro", new Color(.035f,.11f,.17f)); metal = Mat("BOD_MetalAzulado", new Color(.18f,.31f,.39f));
        white = Mat("BOD_BlancoFrio", new Color(.72f,.86f,.9f)); cyan = Mat("BOD_Cian", new Color(.05f,.85f,1f)); dirt = Mat("BOD_Suciedad", new Color(.07f,.055f,.05f));
        tray1 = Mat("BOD_GrisOscuro", new Color(.16f,.18f,.2f)); tray2 = Mat("BOD_Gris2", new Color(.31f,.34f,.37f));
        tray3 = Mat("BOD_Gris3", new Color(.49f,.53f,.56f)); tray4 = Mat("BOD_GrisClaro", new Color(.72f,.76f,.78f));
    }
    private static Material Mat(string name, Color color)
    {
        var path = "Assets/Scenes/" + name + ".mat";
        var value = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!value) { value = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(value, path); }
        value.color = color; value.SetFloat("_Metallic", .55f); value.SetFloat("_Smoothness", .5f); EditorUtility.SetDirty(value); return value;
    }
    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent); go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        // Decorative geometry never blocks the horizontal walking lane.
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    private static GameObject Empty(string name, Transform parent, Vector3 position)
    { var go = new GameObject(name); go.transform.SetParent(parent); go.transform.localPosition = position; return go; }
    private static void Environment(Transform parent)
    {
        var env = Empty("BOD_Escenario", parent, Vector3.zero);
        Box("Suelo", env.transform, new Vector3(0,-.18f,1), new Vector3(44,.36f,4), dark).AddComponent<BoxCollider>();
        Box("ParedFondo", env.transform, new Vector3(0,3,2.3f), new Vector3(44,6,.35f), metal);
        for (var x=-20; x<=20; x+=4) Box("Panel_Refrigeracion", env.transform, new Vector3(x,2.8f,1.95f), new Vector3(3.45f,4.9f,.16f), white);
        for (var x=-18; x<=18; x+=6) { var l=Empty("Luz_Fria", env.transform,new Vector3(x,4.5f,-.5f)).AddComponent<Light>(); l.type=LightType.Point; l.color=new Color(.25f,.8f,1); l.range=7; l.intensity=3; l.shadows=LightShadows.None; }
        Box("LimiteIzquierdo",env.transform,new Vector3(-22,2,0),new Vector3(.3f,4,4),dark).AddComponent<BoxCollider>();
        Box("LimiteDerecho",env.transform,new Vector3(22,2,0),new Vector3(.3f,4,4),dark).AddComponent<BoxCollider>();
        var vent=Empty("BOD_Ducto", parent,new Vector3(4,3.8f,1));
        var vv=Empty("Visual",vent.transform,Vector3.zero);
        Box("Carcasa",vv.transform,Vector3.zero,new Vector3(2.8f,1.6f,.8f),metal);
        Box("SalidaAire",vv.transform,new Vector3(0,0,-.45f),new Vector3(2.4f,1.25f,.08f),dark);
        for (int i=-3;i<=3;i++) Box("LamaRejilla",vv.transform,new Vector3(i*.31f,0,-.52f),new Vector3(.06f,1.15f,.08f),white);
        Box("ConductoSuperior",vv.transform,new Vector3(0,1,0),new Vector3(.8f,.6f,.65f),metal);
        Text3D("Rotulo",vent.transform,new Vector3(0,-1,-.55f),"UNIDAD DE AIRE FRÍO",white,.22f);
    }
    private static void Trays(Transform parent)
    {
        Tray(parent,"BOD_Bandeja_7_Oscura",new Vector3(-12,.85f,.7f),"7",tray1);
        Tray(parent,"BOD_Bandeja_9_Gris3",new Vector3(-8,.7f,.7f),"9",tray3);
        Tray(parent,"BOD_Bandeja_2_Gris2",new Vector3(-4,.22f,.7f),"2",tray2);
        Tray(parent,"BOD_Bandeja_4_Clara",new Vector3(0,.85f,.7f),"4",tray4);
    }
    private static void Tray(Transform parent, string name, Vector3 pos, string number, Material material)
    {
        var root=Empty(name,parent,pos);
        var col=root.AddComponent<BoxCollider>(); col.isTrigger=true; col.center=new Vector3(0,.4f,-.3f); col.size=new Vector3(1.9f,2.1f,1.8f);
        var clean=root.AddComponent<BOD_Tray>();
        var visual=Empty("Visual",root.transform,Vector3.zero);
        Box("Bandeja",visual.transform,Vector3.zero,new Vector3(1.65f,.3f,.9f),material);
        Box("BordeIzquierdo",visual.transform,new Vector3(-.78f,.2f,0),new Vector3(.1f,.2f,.9f),material);
        Box("BordeDerecho",visual.transform,new Vector3(.78f,.2f,0),new Vector3(.1f,.2f,.9f),material);
        if (pos.y>.4f)
        {
            Box("Soporte",visual.transform,new Vector3(0,-.23f,.12f),new Vector3(1.9f,.12f,.85f),metal);
            for(int i=-1;i<=1;i+=2) Box("Pata",visual.transform,new Vector3(i*.7f,-pos.y*.5f-.1f,.3f),new Vector3(.08f,pos.y-.2f,.12f),metal);
        }
        var dust=Box("Suciedad",root.transform,new Vector3(0,.07f,-.47f),new Vector3(1.4f,.28f,.06f),dirt);
        var label=Text3D("Numero_"+number,root.transform,new Vector3(0,.03f,-.51f),number,white,.26f); label.SetActive(false);
        Set(clean,"cleanSeconds",1f); Set(clean,"dirtVisual",dust); Set(clean,"numberVisual",label);
    }
    private static void KeypadAndChamber(Transform parent)
    {
        var chamber=Empty("BOD_CamaraDescongelado",parent,new Vector3(12,1.6f,1.1f));
        var visual=Empty("Visual",chamber.transform,Vector3.zero);
        Box("Fondo",visual.transform,new Vector3(0,0,.55f),new Vector3(4,3.2f,.15f),dark);
        Box("MarcoIzquierdo",visual.transform,new Vector3(-1.85f,0,0),new Vector3(.3f,3.2f,1),white);
        Box("MarcoDerecho",visual.transform,new Vector3(1.85f,0,0),new Vector3(.3f,3.2f,1),white);
        Box("Techo",visual.transform,new Vector3(0,1.5f,0),new Vector3(4,.25f,1),white);
        Text3D("Rotulo",chamber.transform,new Vector3(0,1.9f,-.7f),"CÁMARA DE DESCONGELADO",white,.23f);
        var door=Empty("PuertaCamara",chamber.transform,new Vector3(0,0,-.55f));
        var doorVisual=Empty("Visual",door.transform,Vector3.zero);
        Box("Panel",doorVisual.transform,Vector3.zero,new Vector3(3.3f,2.8f,.18f),metal);
        Box("Ventana",doorVisual.transform,new Vector3(0,.4f,-.12f),new Vector3(1.4f,.6f,.08f),dark);
        Box("Manija",doorVisual.transform,new Vector3(1,-.2f,-.2f),new Vector3(.1f,.6f,.12f),cyan);
        var piece=Empty("PiezaCalor",chamber.transform,new Vector3(0,-.6f,-.8f));
        var pv=GameObject.CreatePrimitive(PrimitiveType.Sphere); pv.name="Visual"; pv.transform.SetParent(piece.transform,false); pv.transform.localScale=Vector3.one*.42f; pv.GetComponent<Renderer>().sharedMaterial=cyan;
        UnityEngine.Object.DestroyImmediate(pv.GetComponent<Collider>());
        var pcol=piece.AddComponent<BoxCollider>(); pcol.isTrigger=true; pcol.size=new Vector3(1.8f,2,1.5f);
        var pc=piece.AddComponent<CORE_Collectible>(); Set(pc,"itemId","PiezaCalor"); Set(pc,"displayName","Pieza Calor"); piece.SetActive(false);
        var key=Empty("BOD_Teclado",parent,new Vector3(9,1,.25f)); var kc=key.AddComponent<BoxCollider>(); kc.isTrigger=true; kc.size=new Vector3(1.3f,2.1f,1.2f); Box("Visual",key.transform,Vector3.zero,new Vector3(.75f,1.1f,.22f),dark);
        Text3D("Rotulo",key.transform,new Vector3(0,.8f,-.2f),"TECLADO · E",white,.18f);
        var controller=key.AddComponent<BOD_KeypadController>(); var hint=Box("PistaDucto",parent,new Vector3(4,3.8f,.44f),new Vector3(2.5f,1.25f,.05f),cyan); hint.SetActive(false); var hintLight=hint.AddComponent<Light>(); hintLight.type=LightType.Point; hintLight.color=cyan.color; hintLight.range=4; hintLight.enabled=false;
        var canvas=Canvas(controller, key.transform); Set(controller,"keypadCanvas",canvas); Set(controller,"chamberDoor",door); Set(controller,"heatPiece",piece); Set(controller,"ductHint",hint); Set(controller,"ductHintLight",hintLight);
    }
    private static GameObject Canvas(BOD_KeypadController controller, Transform parent)
    {
        var canvas=new GameObject("BOD_KeypadCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvas.transform.SetParent(parent); var c=canvas.GetComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay;
        var panel=UI("Panel",canvas.transform,new Vector2(370,470),new Vector2(0,0),new Color(.02f,.08f,.12f,.94f));
        var display=UIText("Pantalla",panel.transform,new Vector2(320,55),new Vector2(0,155),"————",28); var feedback=UIText("Pista",panel.transform,new Vector2(330,40),new Vector2(0,105),"OSCURO → CLARO",18);
        Set(controller,"displayText",display); Set(controller,"feedbackText",feedback);
        int n=1; for(int row=0;row<3;row++) for(int col=0;col<3;col++){ var b=Button(panel.transform,((n-1)%3-1)*92,55-row*70,n.ToString()); UnityEventTools.AddStringPersistentListener(b.onClick,controller.AddDigit,n.ToString()); n++; }
        var zero=Button(panel.transform,0,-155,"0"); UnityEventTools.AddStringPersistentListener(zero.onClick,controller.AddDigit,"0");
        var clear=Button(panel.transform,-110,-225,"Borrar"); UnityEventTools.AddPersistentListener(clear.onClick,controller.Clear); var ok=Button(panel.transform,0,-225,"Confirmar"); UnityEventTools.AddPersistentListener(ok.onClick,controller.Confirm); var close=Button(panel.transform,110,-225,"Cerrar"); UnityEventTools.AddPersistentListener(close.onClick,controller.ClosePanel);
        canvas.SetActive(false); return canvas;
    }
    private static GameObject UI(string name,Transform parent,Vector2 size,Vector2 pos,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.sizeDelta=size;rt.anchoredPosition=pos;go.GetComponent<Image>().color=color;return go;}
    private static Text UIText(string name,Transform parent,Vector2 size,Vector2 pos,string value,int font){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.sizeDelta=size;rt.anchoredPosition=pos;var t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=font;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;return t;}
    private static Button Button(Transform parent,float x,float y,string value){var go=UI("Boton_"+value,parent,new Vector2(80,52),new Vector2(x,y),new Color(.12f,.35f,.44f));var b=go.AddComponent<Button>();UIText("Texto",go.transform,new Vector2(78,50),Vector2.zero,value,15);return b;}
    private static void Story(Transform parent)
    {
        var bucket=Empty("BOD_BaldeHerbicida",parent,new Vector3(4,.5f,.8f)); var b=GameObject.CreatePrimitive(PrimitiveType.Cylinder); b.name="Visual";b.transform.SetParent(bucket.transform,false);b.transform.localScale=new Vector3(.55f,.5f,.55f);b.GetComponent<Renderer>().sharedMaterial=white;UnityEngine.Object.DestroyImmediate(b.GetComponent<Collider>());Text3D("Etiqueta",bucket.transform,new Vector3(0,.8f,-.5f),"HERBICIDA\nDesinfección de emergencia",white,.27f);
        var note=Empty("BOD_PlacaN4",parent,new Vector3(2.2f,1.2f,.45f));var nc=note.AddComponent<BoxCollider>();nc.isTrigger=true;nc.size=new Vector3(1.2f,2,1.3f);note.AddComponent<BOD_NotePlate>();Box("Visual",note.transform,Vector3.zero,new Vector3(1,.8f,.12f),white);Text3D("Texto",note.transform,new Vector3(0,0,-.1f),"N4",dark,.27f);
        var seal=Empty("BOD_PuntoRonda_Cruz",parent,new Vector3(6.5f,.3f,.55f));var sc=seal.AddComponent<BoxCollider>();sc.isTrigger=true;sc.center=new Vector3(0,.4f,0);sc.size=new Vector3(1.2f,1.8f,1.5f);var sd=seal.AddComponent<CORE_Dirt>();Text3D("Visual",seal.transform,new Vector3(0,0,-.1f),"✚",cyan,.4f);var cover=Box("Suciedad",seal.transform,new Vector3(0,0,-.15f),new Vector3(.9f,.4f,.05f),dirt);
        var reward=Empty("SelloCruz",seal.transform,new Vector3(0,.3f,-.2f));Text3D("Visual",reward.transform,Vector3.zero,"SELLO ✚",cyan,.24f);var rcol=reward.AddComponent<BoxCollider>();rcol.isTrigger=true;rcol.size=new Vector3(1,1.5f,1.5f);var rc=reward.AddComponent<CORE_Collectible>();reward.SetActive(false);Set(rc,"itemId","SelloCruz");Set(rc,"displayName","Sello ✚");Set(sd,"prompt","Mantén E para limpiar el punto de ronda ✚");Set(sd,"dustVisual",cover);Set(sd,"revealOnClean",reward);
        Box("ManchaOscura",parent,new Vector3(4.8f,.04f,.6f),new Vector3(1.3f,.08f,.6f),dirt);
    }
    private static void Doors(Transform parent){Door(parent,"BOD_PuertaIzquierda",new Vector3(-19,1.35f,.55f),false);Door(parent,"BOD_PuertaDerecha",new Vector3(19,1.35f,.55f),true);}
    private static void Door(Transform parent,string name,Vector3 pos,bool locked)
    {
        var root=Empty(name,parent,pos);var c=root.AddComponent<BoxCollider>();c.isTrigger=true;c.size=new Vector3(1.2f,3f,1.8f);var d=root.AddComponent<CORE_ZoneDoor>();
        var visual=Empty("Visual",root.transform,Vector3.zero);Box("Panel",visual.transform,Vector3.zero,new Vector3(1.5f,2.7f,.25f),metal);
        Box("Marco",visual.transform,new Vector3(0,1.4f,0),new Vector3(1.8f,.15f,.3f),cyan);
        Box("Manija",visual.transform,new Vector3(.5f,0,-.2f),new Vector3(.1f,.45f,.1f),white);
        Text3D("Rotulo",root.transform,new Vector3(0,1.8f,-.2f),"VOLVER A INICIO",white,.22f);
        Set(d,"prompt",locked?"E · Volver a Inicio (requiere Pieza Calor)":"E · Volver a Inicio");Set(d,"requiredItem",locked?"PiezaCalor":"");Set(d,"destinationScene","Inicio");
    }
    private static void ConfigureArrival(Scene scene, Transform root)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach(var point in go.GetComponentsInChildren<SpawnPoint>(true))
            { point.transform.position=new Vector3(-16.5f,.8f,0); EditorUtility.SetDirty(point.transform); }
            foreach(var follow in go.GetComponentsInChildren<CORE_CameraFollow>(true)) Set(follow,"xLimits",new Vector2(-16,16));
        }
        var entry=root.gameObject.AddComponent<BOD_RoomEntry>();
        Set(entry,"arrival",Empty("Llegada_PiesSobreSuelo",root,new Vector3(-16.5f,0,0)).transform);
        Set(entry,"cameraTarget",Empty("ObjetivoCamara",root,new Vector3(-16.5f,1,0)).transform);
    }
    private static GameObject Text3D(string name,Transform parent,Vector3 position,string value,Material mat,float size)
    {
        var go=new GameObject(name,typeof(TextMesh));go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var t=go.GetComponent<TextMesh>();t.text=value;t.characterSize=.1f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.fontSize=80;t.color=mat.color;
        t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");go.GetComponent<Renderer>().sharedMaterial=t.font.material;
        var height=go.GetComponent<Renderer>().bounds.size.y;
        if(height>.0001f)go.transform.localScale=Vector3.one*(size/height);
        return go;
    }
    private static void Set(object target,string field,object value){var f=target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic);if(f==null)throw new MissingFieldException(target.GetType().Name,field);f.SetValue(target,value);EditorUtility.SetDirty((UnityEngine.Object)target);}
}
