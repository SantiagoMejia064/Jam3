using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>Builds a graybox directly into Javier.unity in edit mode; objects remain editable in its hierarchy.</summary>
public static class JavierSceneBuilder
{
    [MenuItem("Jam3/Build Javier Lab and Hallway Scene")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if(scene.name != "Javier") scene=EditorSceneManager.OpenScene("Assets/Scenes/Javier.unity",OpenSceneMode.Single);
        foreach(var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
        var light=new GameObject("CORE_Luz_Frontal"); light.transform.rotation=Quaternion.Euler(35,-25,0); var directional=light.AddComponent<Light>(); directional.type=LightType.Directional; directional.intensity=1.2f; directional.color=new Color(.78f,.88f,1f);

        var lab = new GameObject("LAB_Laboratorio");
        var hall = new GameObject("LAB_Pasillo");
        Box("LAB_Fondo_Laboratorio", lab.transform, new Vector3(-4,2.5f,3), new Vector3(14,5.5f,.5f), new Color(.16f,.22f,.27f));
        Box("LAB_Piso_Laboratorio", lab.transform, new Vector3(-4,-.25f,0), new Vector3(14,.5f,2), new Color(.38f,.42f,.44f));
        Box("LAB_Fondo_Pasillo", hall.transform, new Vector3(12,2.5f,3), new Vector3(20,5.5f,.5f), new Color(.12f,.18f,.22f));
        Box("LAB_Piso_Pasillo", hall.transform, new Vector3(12,-.25f,0), new Vector3(20,.5f,2), new Color(.32f,.37f,.4f));
        Box("LAB_Pared_Trasera", lab.transform, new Vector3(-4,5.3f,0), new Vector3(14,.2f,2), new Color(.22f,.27f,.3f));
        Box("LAB_Pared_Trasera_Pasillo", hall.transform, new Vector3(12,5.3f,0), new Vector3(20,.2f,2), new Color(.19f,.24f,.28f));

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule); player.name="CORE_Siete"; player.transform.position=new Vector3(-9.5f,.8f,0); player.transform.localScale=new Vector3(.65f,.8f,.45f);
        var playerCapsule=player.GetComponent<CapsuleCollider>(); playerCapsule.direction=1; playerCapsule.center=new Vector3(0,.38f,0); playerCapsule.radius=.65f; playerCapsule.height=2.7f;
        player.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.2f,.68f,.72f));
        var rb=player.AddComponent<Rigidbody>(); rb.useGravity=false; rb.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
        player.AddComponent<CORE_PlayerController>();
        BuildRobot(player.transform);
        var cam= new GameObject("CORE_Camera"); cam.tag="MainCamera"; cam.transform.position=new Vector3(-8,2,-12);
        var camera=cam.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=5; camera.backgroundColor=new Color(.045f,.065f,.08f); cam.AddComponent<AudioListener>();
        var follow=cam.AddComponent<CORE_CameraFollow>(); var so=new SerializedObject(follow); so.FindProperty("target").objectReferenceValue=player.transform; so.FindProperty("xLimits").vector2Value=new Vector2(-10,22); so.ApplyModifiedPropertiesWithoutUndo();
        BuildHUD();
        SolidBox("LAB_Limite_Izquierdo",lab.transform,new Vector3(-11,1.5f,0),new Vector3(.3f,3.5f,2),new Color(.22f,.27f,.3f));
        SolidBox("LAB_Limite_Final_Pasillo",hall.transform,new Vector3(29,1.5f,0),new Vector3(.3f,3.5f,2),new Color(.19f,.24f,.28f));

        Box("LAB_Riel_Luz",lab.transform,new Vector3(-4,5.25f,-.1f),new Vector3(13,.09f,.12f),new Color(.34f,.55f,.6f));
        Label("LAB_Rotulo_Laboratorio",lab.transform,new Vector3(-4,4.55f,-.2f),"01  /  LABORATORIO");
        Label("LAB_Rotulo_Pasillo",hall.transform,new Vector3(12,4.55f,-.2f),"02  /  PASILLO");
        var arm=Prop("LAB_Brazo_Muelle",lab.transform,new Vector3(-8,.8f,0),new Vector3(2.8f,.45f,.65f),new Color(.48f,.55f,.58f));
        var dustVisual=Prop("LAB_Polvo_Muelle",lab.transform,new Vector3(-8,1.05f,-.35f),new Vector3(2.35f,.06f,.56f),new Color(.66f,.65f,.56f));
        Box("LAB_Brazo_Soporte",lab.transform,new Vector3(-8,1.55f,.2f),new Vector3(.18f,1.9f,.4f),new Color(.4f,.49f,.51f));
        Box("LAB_Banco_Muelle",lab.transform,new Vector3(-8,.3f,.3f),new Vector3(3.2f,.18f,.9f),new Color(.35f,.43f,.47f));
        Box("LAB_Pantalla_Carcasa",lab.transform,new Vector3(-8,2.65f,.15f),new Vector3(2.3f,1.25f,.5f),new Color(.16f,.35f,.41f));
        var screen=Label("LAB_Pantalla_Muelle",lab.transform,new Vector3(-8,3.25f,-.12f),"PANTALLA DE SERVICIO · A1");
        var screenInteraction=screen.AddComponent<CORE_Interactable>(); SetString(screenInteraction,"prompt","E · Leer pantalla A1"); AddReport(screenInteraction,"A1 · Formateo completado. Motivo: desobediencia.");
        var screenCollider=screen.GetComponent<BoxCollider>();
        if(!screenCollider) { var oldCollider=screen.GetComponent<Collider>(); if(oldCollider) Object.DestroyImmediate(oldCollider); screenCollider=screen.AddComponent<BoxCollider>(); }
        screenCollider.size=new Vector3(3,3,1); screenCollider.center=new Vector3(0,-2,0); screenCollider.isTrigger=true;
        var note=Label("LAB_N1_Cinta",lab.transform,new Vector3(-8,1.6f,-.4f),"N1 · Limpia donde nadie mira."); note.SetActive(false);
        var ball=Prop("LAB_Pelota",lab.transform,new Vector3(-6,.45f,0),new Vector3(.55f,.55f,.55f),new Color(.96f,.68f,.2f)); ball.AddComponent<CORE_Collectible>(); ball.GetComponent<Collider>().isTrigger=true;
        var ballData=new SerializedObject(ball.GetComponent<CORE_Collectible>()); ballData.FindProperty("itemId").stringValue="Pelota"; ballData.FindProperty("displayName").stringValue="Pelota"; ballData.ApplyModifiedPropertiesWithoutUndo(); ball.SetActive(false);
        var dirt=arm.AddComponent<CORE_Dirt>(); arm.GetComponent<Collider>().isTrigger=true; SetString(dirt,"prompt","Mantén E - Limpiar muelle"); SetString(dirt,"completedMessage","Muelle limpio. Pelota descubierta.");
        SetRef(dirt,"promptVisual",Hint("LAB_Ayuda_Limpiar",lab.transform,new Vector3(-8,1.8f,-.6f),"MANTÉN E · LIMPIAR"));
        SetRef(dirt,"dustVisual",dustVisual);
        SetRef(dirt,"revealOnClean",note);
        SetRef(dirt,"revealOnCleanExtra",ball);

        var marker=Prop("LAB_Punto_Ronda_Triangulo",lab.transform,new Vector3(-2,.25f,0),new Vector3(.55f,.08f,.5f),new Color(.1f,.85f,.8f));
        var md=marker.AddComponent<CORE_Dirt>(); marker.GetComponent<Collider>().isTrigger=true; SetString(md,"prompt","Mantén E - Limpiar y revelar el sello"); SetString(md,"completedMessage","Sello de rondas revelado."); SetRef(md,"promptVisual",Hint("LAB_Ayuda_Sello",lab.transform,new Vector3(-2,1.3f,-.6f),"MANTÉN E · LIMPIAR")); var seal=Label("LAB_Sello_Triangulo",lab.transform,new Vector3(-2,1,0),"SELLO ▲"); seal.SetActive(false);
        SetRef(md,"revealOnClean",seal);

        var cabinet=Prop("LAB_Gabinete_Muestras",lab.transform,new Vector3(1,.9f,0),new Vector3(1.25f,1.8f,.8f),new Color(.37f,.48f,.51f));
        var cInt=cabinet.AddComponent<CORE_Interactable>(); cabinet.GetComponent<Collider>().isTrigger=true; SetString(cInt,"prompt","E · Abrir gabinete (requiere Pelota)"); AddReport(cInt,"Gabinete abierto: pieza Oscuridad obtenida."); SetRef(cInt,"promptVisual",Hint("LAB_Ayuda_Gabinete",lab.transform,new Vector3(1,2.2f,-.6f),"E · USAR PELOTA"));
        var cData=new SerializedObject(cInt); cData.FindProperty("requiredItem").stringValue="Pelota"; cData.FindProperty("resultItem").stringValue="PiezaOscuridad"; cData.FindProperty("lockedMessage").stringValue="No agarra. Prueba con la Pelota."; cData.ApplyModifiedPropertiesWithoutUndo();
        var terminal=Label("LAB_Terminal",lab.transform,new Vector3(-4,1.5f,0),"TERMINAL · A3 / RONDAS -21 DÍAS");
        var terminalCollider=terminal.AddComponent<BoxCollider>(); terminalCollider.size=new Vector3(3,2,1); terminalCollider.center=new Vector3(0,-.5f,0); terminalCollider.isTrigger=true;
        var terminalInteraction=terminal.AddComponent<CORE_Interactable>(); SetString(terminalInteraction,"prompt","E · Leer terminal A3"); AddReport(terminalInteraction,"A3 · La unidad ya no distingue lo útil de lo bello."); SetRef(terminalInteraction,"promptVisual",Hint("LAB_Ayuda_Terminal",lab.transform,new Vector3(-4,2,-.6f),"E · LEER REGISTRO"));

        Door("LAB_Puerta_Taller",hall.transform,new Vector3(5,1,0),"TAL",false);
        Door("LAB_Puerta_Sala",hall.transform,new Vector3(9,1,0),"SAL",false);
        Door("LAB_Puerta_Bodega_Sellada",hall.transform,new Vector3(13,1,0),"BOD",true);
        Door("LAB_Puerta_Invernadero_Sellada",hall.transform,new Vector3(17,1,0),"INV",true);
        var cabin=Prop("LAB_Cabina_Vega_Sellada",hall.transform,new Vector3(22,1,0),new Vector3(1.4f,2.3f,.8f),new Color(.45f,.2f,.2f));
        var cabInt=cabin.AddComponent<CORE_Interactable>(); cabin.GetComponent<Collider>().isTrigger=true; SetString(cabInt,"prompt","E · Inspeccionar cabina"); AddReport(cabInt,"A2 · ¡No puedes sacar eso de aquí!");
        var cabData=new SerializedObject(cabInt); cabData.FindProperty("oneShot").boolValue=true; cabData.ApplyModifiedPropertiesWithoutUndo();
        Label("LAB_Ropa_y_Bandejas_Vega",hall.transform,new Vector3(22,2.8f,0),"ROPA · BANDEJAS · 21 DÍAS");
        Door("LAB_Esclusa_Carga",hall.transform,new Vector3(27,1,0),"LAB",false);

        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Javier graybox guardado en la escena. Los objetos son editables desde la jerarquía.");
    }

    private static void AddReport(CORE_Interactable c,string msg)
    {
        if(!c) return;
        var so=new SerializedObject(c); so.FindProperty("responseMessage").stringValue=msg; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetString(Object component,string field,string value)
    {
        if(!component)return;
        var so=new SerializedObject(component); var property=so.FindProperty(field); if(property!=null){property.stringValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    }
    private static void Door(string name,Transform parent,Vector3 pos,string scene,bool locked)
    {
        var frame=Prop(name+"_Marco",parent,pos+new Vector3(0,0,.28f),new Vector3(.95f,2.55f,.25f),new Color(.19f,.28f,.32f));
        Object.DestroyImmediate(frame.GetComponent<Collider>());
        var go=Prop(name,parent,pos,new Vector3(.65f,2.2f,.8f),locked?new Color(.42f,.22f,.2f):new Color(.27f,.48f,.5f));
        var door=go.AddComponent<CORE_ZoneDoor>(); go.GetComponent<Collider>().isTrigger=true; var so=new SerializedObject(door); so.FindProperty("destinationScene").stringValue=scene; so.FindProperty("requiredItem").stringValue=locked?"Cortador":""; so.FindProperty("prompt").stringValue=locked?"E · Usar cortador":"E · Entrar"; so.FindProperty("promptVisual").objectReferenceValue=Hint(name+"_Ayuda",parent,pos+new Vector3(0,1.5f,-.6f),locked?"E · USAR CORTADOR":"E · ENTRAR"); so.ApplyModifiedPropertiesWithoutUndo();
        string room=scene=="TAL"?"TALLER":scene=="SAL"?"SALA DE MÁQUINAS":scene=="BOD"?"BODEGA":"INVERNADERO";
        Label(name+"_Rotulo",parent,pos+Vector3.up*1.5f,locked?room+" · SELLADA":room);
    }
    private static GameObject Prop(string name,Transform parent,Vector3 pos,Vector3 scale,Color color)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent); g.transform.position=pos; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=Mat(color); Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
    }
    private static GameObject SolidBox(string name,Transform parent,Vector3 pos,Vector3 scale,Color color)
    {
        var g=Prop(name,parent,pos,scale,color); g.AddComponent<BoxCollider>(); return g;
    }
    private static void BuildHUD()
    {
        var root=new GameObject("CORE_HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CORE_InteractionHUD));
        var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=20;
        var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
        var panel=new GameObject("HUD_Panel_Interaccion",typeof(RectTransform),typeof(UnityEngine.UI.Image)); panel.transform.SetParent(root.transform,false);
        var panelRect=panel.GetComponent<RectTransform>(); panelRect.anchorMin=new Vector2(.5f,0); panelRect.anchorMax=new Vector2(.5f,0); panelRect.pivot=new Vector2(.5f,0); panelRect.anchoredPosition=new Vector2(0,42); panelRect.sizeDelta=new Vector2(820,132);
        var image=panel.GetComponent<UnityEngine.UI.Image>(); image.color=new Color(.025f,.055f,.08f,.94f); image.raycastTarget=false;
        var controlsPanel=new GameObject("HUD_Panel_Controles",typeof(RectTransform),typeof(UnityEngine.UI.Image)); controlsPanel.transform.SetParent(root.transform,false);
        var controlsRect=controlsPanel.GetComponent<RectTransform>(); controlsRect.anchorMin=new Vector2(0,1); controlsRect.anchorMax=new Vector2(0,1); controlsRect.pivot=new Vector2(0,1); controlsRect.anchoredPosition=new Vector2(28,-28); controlsRect.sizeDelta=new Vector2(510,64);
        var controlsImage=controlsPanel.GetComponent<UnityEngine.UI.Image>(); controlsImage.color=new Color(.025f,.055f,.08f,.88f); controlsImage.raycastTarget=false;
        var controls=CreateHudText("HUD_Controles",controlsPanel.transform,23,FontStyle.Bold); controls.text="A / D  ·  MOVER       E  ·  INTERACTUAR"; controls.alignment=TextAnchor.MiddleCenter;
        var controlsTextRect=controls.rectTransform; controlsTextRect.anchorMin=Vector2.zero; controlsTextRect.anchorMax=Vector2.one; controlsTextRect.offsetMin=new Vector2(12,8); controlsTextRect.offsetMax=new Vector2(-12,-8);
        var prompt=CreateHudText("HUD_Accion",panel.transform,30,FontStyle.Bold);
        var promptRect=prompt.rectTransform; promptRect.anchorMin=new Vector2(0,0.48f); promptRect.anchorMax=new Vector2(1,1); promptRect.offsetMin=new Vector2(22,0); promptRect.offsetMax=new Vector2(-22,-10);
        var message=CreateHudText("HUD_Mensaje",panel.transform,23,FontStyle.Normal);
        var messageRect=message.rectTransform; messageRect.anchorMin=new Vector2(0,0); messageRect.anchorMax=new Vector2(1,.5f); messageRect.offsetMin=new Vector2(22,10); messageRect.offsetMax=new Vector2(-22,0);
        panel.SetActive(false);
        var so=new SerializedObject(root.GetComponent<CORE_InteractionHUD>()); so.FindProperty("panel").objectReferenceValue=panel; so.FindProperty("promptText").objectReferenceValue=prompt; so.FindProperty("messageText").objectReferenceValue=message; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static Text CreateHudText(string name,Transform parent,int size,FontStyle style)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
        var text=go.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=size; text.fontStyle=style; text.color=Color.white; text.alignment=TextAnchor.MiddleCenter; text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Overflow; text.raycastTarget=false; return text;
    }
    private static void Box(string name,Transform parent,Vector3 pos,Vector3 scale,Color color)=>Prop(name,parent,pos,scale,color);
    private static GameObject Label(string name,Transform parent,Vector3 pos,string text)
    {
        var g=new GameObject(name); g.transform.SetParent(parent); g.transform.position=pos; var tm=g.AddComponent<TextMesh>(); tm.text=text; tm.characterSize=.055f; tm.fontSize=28; tm.anchor=TextAnchor.MiddleCenter; tm.color=new Color(.75f,.88f,.9f); return g;
    }
    private static void BuildRobot(Transform parent)
    {
        parent.GetComponent<Renderer>().enabled=false;
        var model=new GameObject("CORE_Modelo_Siete"); model.transform.SetParent(parent,false);
        RobotPart("CORE_Torso",model.transform,new Vector3(0,.08f,0),new Vector3(1.3f,1.2f,1f),new Color(.32f,.53f,.57f));
        RobotPart("CORE_Cabeza",model.transform,new Vector3(0,1.04f,0),new Vector3(1.12f,.76f,1f),new Color(.65f,.73f,.72f));
        RobotPart("CORE_Visor",model.transform,new Vector3(0,1.08f,-.51f),new Vector3(.72f,.28f,.08f),new Color(.08f,.16f,.2f));
        RobotPart("CORE_Ojo_Izquierdo",model.transform,new Vector3(-.16f,1.09f,-.57f),new Vector3(.09f,.09f,.04f),new Color(.4f,.92f,.95f));
        RobotPart("CORE_Ojo_Derecho",model.transform,new Vector3(.16f,1.09f,-.57f),new Vector3(.09f,.09f,.04f),new Color(.4f,.92f,.95f));
        RobotPart("CORE_Pierna",model.transform,new Vector3(0,-.76f,0),new Vector3(.62f,.34f,.9f),new Color(.22f,.35f,.39f));
        RobotPart("CORE_Antena",model.transform,new Vector3(.37f,1.52f,0),new Vector3(.08f,.35f,.1f),new Color(.76f,.56f,.24f));
    }
    private static void RobotPart(string name,Transform parent,Vector3 pos,Vector3 scale,Color color)
    {
        var part=Prop(name,parent,pos,scale,color); var collider=part.GetComponent<Collider>(); if(collider) Object.DestroyImmediate(collider);
    }
    private static GameObject Hint(string name,Transform parent,Vector3 pos,string text) { var g=Label(name,parent,pos,text); g.SetActive(false); return g; }
    private static void SetRef(Object component,string field,GameObject target) { var so=new SerializedObject(component); so.FindProperty(field).objectReferenceValue=target; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static Material Mat(Color color)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color=color; return m;
    }
}

/// <summary>Completes the requested one-time scene setup when the project Editor imports these scripts.</summary>
[InitializeOnLoad]
internal static class JavierSceneSetup
{
    private const string DoneKey="Jam3.JavierSceneBuilt.v1";
    static JavierSceneSetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += BuildIfRequestedSceneIsAlreadyOpen;
    }
    private static void BuildIfRequestedSceneIsAlreadyOpen()
    {
        if(!EditorPrefs.GetBool(DoneKey,false) && SceneManager.GetActiveScene().name=="Javier") BuildOnce();
    }
    private static void OnSceneOpened(Scene scene,OpenSceneMode mode)
    {
        if(!EditorPrefs.GetBool(DoneKey,false) && scene.name=="Javier") EditorApplication.delayCall += BuildOnce;
    }
    private static void BuildOnce()
    {
        if(EditorPrefs.GetBool(DoneKey,false)) return;
        EditorPrefs.SetBool(DoneKey,true);
        JavierSceneBuilder.Build();
    }
}
