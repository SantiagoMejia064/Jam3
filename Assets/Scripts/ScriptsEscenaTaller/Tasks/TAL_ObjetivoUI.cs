using UnityEngine;
using TMPro;

public class TAL_ObjetivoUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text objectiveText;
    public TMP_Text piecesText;

    public string CurrentObjective { get; private set; }
    public string CurrentHint { get; private set; }

    void Update()
    {
        RefreshObjective();

        // El Cuaderno ya ocupa la pantalla: ocultamos esta pequeña ayuda.
        if (panel != null)
            panel.SetActive(NotebookController.Instance == null ||
                            !NotebookController.Instance.IsOpen);
    }

    public void RefreshObjective()
    {
        ChooseObjective();

        if (objectiveText != null && objectiveText.text != CurrentObjective)
            objectiveText.text = CurrentObjective;

        int pieces = 0;
        if (Collected("PiezaOscuridad")) pieces++;
        if (Collected("PiezaPulso")) pieces++;
        if (Collected("PiezaCalor")) pieces++;
        if (Collected("PiezaBruma")) pieces++;

        string count = "PIEZAS " + pieces + "/4";
        if (piecesText != null && piecesText.text != count)
            piecesText.text = count;
    }

    void ChooseObjective()
    {
        int act = SAL_Progress.Act;

        if (SAL_Progress.HasFlag(SAL_Progress.FinalComplete))
            SetObjective("Quiero volver a mi Taller.", "Todavía puedo dejar un último dibujo en el Taller.");
        else if (act >= 9 || SAL_Progress.HasFlag(SAL_Progress.FinalAvailable))
            SetObjective("Debo llevarla a la esclusa.", "La palanca de la esclusa está en el Pasillo.");
        else if (act >= 8)
            SetObjective("Debo volver a leer lo que guardé.", "Revisa las notas del Cuaderno. Ahora puedo entender sus tachados.");
        else if (act >= 7 || SAL_Progress.HasFlag("EllaFlorecio") ||
                 SAL_Progress.HasFlag("IncubadoraAfinada"))
            SetObjective("Quiero verla florecer.", "Vuelve junto a Ella, en el Invernadero.");
        else if (act >= 6)
            SetObjective("Debo montar y afinar la incubadora.", "Las marcas de N3 indican las piezas. Recuerda el ritmo de la pulidora.");
        else if (SeedObtained())
        {
            if (!SAL_Progress.HasFlag(TAL_Cargador.NoteFound))
                SetObjective("Limpia detrás del Cargador para recoger N10.", "Vuelve al Taller. Con la semilla, ya puedes limpiar el polvo del panel trasero del Cargador.");
            else
                SetObjective("Debo llevar la semilla al Invernadero.", "Lleva la semilla y las cuatro piezas al Invernadero. El Cortador abre el paso.");
        }
        else if (!Collected("Pelota"))
            SetObjective("Revisa el Reactor de muestras y recoge la Pelota.", "Limpia el brazo polvoriento del Laboratorio y mira lo que cae debajo.");
        else if (!Collected("Cortador") ||
                 (!Collected("Cable") && !SAL_Progress.HasFlag("TAL_CableInstalado")))
            SetObjective("Recoge el Cortador y el Cable del banco del Taller.", "Lleva el Cortador y el Cable a la pulidora. Después limpia el motor y conecta el cable.");
        else if (!SAL_Progress.HasFlag("TAL_CasilleroAbierto"))
            SetObjective("Quiero abrir el casillero atascado.", Collected("Pelota")
                ? "Selecciona la Pelota y úsala en el casillero de limpieza. Puede hacer de agarradero."
                : "Necesito la Pelota para tirar del casillero. Busca bajo el muelle del Laboratorio.");
        else if (!SAL_Progress.HasFlag("TAL_N3Encontrada"))
            SetObjective("Busca la tablilla N3 en el estante por reparar.", "Lee las cuatro marcas de la tablilla para saber qué piezas necesita la pulidora.");
        else if (!Collected("PiezaPulso"))
        {
            if (!SAL_Progress.HasFlag("TAL_MotorLimpio"))
                SetObjective("Limpia el motor de la pulidora.", "Mantén E frente al motor sucio hasta quitar la grasa.");
            else if (!SAL_Progress.HasFlag("TAL_CableInstalado"))
                SetObjective("Conecta el Cable a la pulidora.", "Selecciona Cable en el inventario y pulsa E junto al borne libre de la pulidora.");
            else
                SetObjective("Prueba la pulidora reparada.", "Pulsa E junto a la pulidora y recoge la Pieza Pulso.");
        }
        else if (act < 4 && !SAL_Progress.HasFlag(SAL_Progress.DrawingsFound))
            SetObjective("Quiero saber quién es ella.", "Busca entre los dibujos del basurero de la Sala de máquinas.");
        else if (!Collected("PiezaOscuridad"))
            SetObjective("Me falta la Pieza Oscuridad.", "Revisa el gabinete del Laboratorio. La Pelota puede ayudarme a abrirlo.");
        else if (!Collected("PiezaCalor"))
            SetObjective("Me falta la Pieza Calor.", "Revisa la cámara de la Bodega fría. Sus bandejas esconden una pista.");
        else if (!Collected("PiezaBruma"))
            SetObjective("Me falta la Pieza Bruma.", "Revisa el conjunto del Filtro 14 en la Sala de máquinas. La Pelota puede servir en la válvula.");
        else if (!SAL_Progress.HasFlag(SAL_Progress.TriangleSeal) ||
                 !SAL_Progress.HasFlag(SAL_Progress.DiamondSeal) ||
                 !SAL_Progress.HasFlag(SAL_Progress.CrossSeal))
            SetObjective("Debo completar los sellos de la ronda.", "Busca las marcas del Laboratorio, Taller y Bodega. N2 guarda su orden en el Cuaderno.");
        else if (!SAL_Progress.HasFlag(SAL_Progress.PanelSolved))
            SetObjective("Debo seguir el orden de N2 en el panel.", "Usa la ruta de N2 en el panel de reciclaje de la Sala de máquinas.");
        else
            SetObjective("Debo encontrar lo que guardé.", "Revisa los bloques del compactador. Busca mi firma: dos rayas y un punto.");
    }

    void SetObjective(string objective, string hint)
    {
        CurrentObjective = objective;
        CurrentHint = hint;
    }

    bool Collected(string id)
    {
        // Los objetos utilizados siguen contando: no pedimos recogerlos otra vez.
        return CORE_InventoryBridge.WasCollected(id);
    }

    public static bool SeedObtained()
    {
        return SAL_Progress.HasFlag(SAL_Progress.SeedObtained) ||
               CORE_InventoryBridge.WasCollected("Semilla");
    }
}
