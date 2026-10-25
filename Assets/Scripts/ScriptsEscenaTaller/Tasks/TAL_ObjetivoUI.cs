using UnityEngine;
using UnityEngine.SceneManagement;
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
        bool seed = SeedObtained();
        bool missingNote = seed && !SAL_Progress.HasFlag(TAL_Cargador.NoteFound);
        bool allPieces = Collected("PiezaOscuridad") && Collected("PiezaPulso") &&
                         Collected("PiezaCalor") && Collected("PiezaBruma");

        if (SAL_Progress.HasFlag(SAL_Progress.FinalComplete))
            SetObjective("Quiero volver a mi Taller.", "Todavía puedo dejar un último dibujo en el Taller.");
        else if (act >= 9 || SAL_Progress.HasFlag(SAL_Progress.FinalAvailable))
            SetObjective("Debo llevarla a la esclusa.", "La palanca de la esclusa está en el Pasillo.");
        else if (act >= 8 || SAL_Progress.HasFlag(SAL_Progress.Revealed))
            SetObjective("Debo volver a leer lo que guardé.", "Revisa las notas del Cuaderno. Ahora puedo entender sus tachados.");
        else if (act >= 7 || SAL_Progress.HasFlag("EllaFlorecio") ||
                 SAL_Progress.HasFlag("IncubadoraAfinada"))
            SetObjective("Quiero verla florecer.", "Vuelve junto a Ella, en el Invernadero.");
        else if (act >= 6 && seed && allPieces)
            SetObjective("Debo montar y afinar la incubadora.", "Las marcas de N3 indican las piezas. Recuerda el ritmo de la pulidora.");
        // Si ya volví al Taller con la semilla, aprovecho para recoger la nota.
        else if (missingNote && SceneManager.GetActiveScene().name == "Taller")
            SetObjective("Quiero revisar detrás de mi Cargador.", "La semilla me recuerda algo. Mantendré E en el panel de servicio, a la izquierda del Cargador.");
        else if (!Collected("Pelota"))
            SetObjective("Debo recoger la Pelota del Laboratorio.", "Limpiaré el Reactor de muestras y miraré lo que cae debajo. La Pelota puede servirme de agarradero.");
        else if (!Collected("PiezaOscuridad") && (SceneManager.GetActiveScene().name == "Javier" || SceneManager.GetActiveScene().name == "Inicio"))
            SetObjective("Debo abrir la cápsula de muestras.", "Seleccionaré la Pelota y la usaré en la cápsula sin agarradero del Laboratorio.");
        else if (!Collected("Cortador") ||
                 (!Collected("Cable") && !SAL_Progress.HasFlag("TAL_CableInstalado")))
            SetObjective("Necesito el Cortador y el Cable del Taller.", "Los recogeré en el banco de trabajo. El Cable sirve para la pulidora; el Cortador, para las puertas selladas.");
        else if (!SAL_Progress.HasFlag("TAL_CasilleroAbierto"))
            SetObjective("Quiero abrir el casillero atascado.", Collected("Pelota")
                ? "Selecciona la Pelota y úsala en el casillero de limpieza. Puede hacer de agarradero."
                : "Necesito la Pelota para tirar del casillero. Busca bajo el muelle del Laboratorio.");
        else if (!SAL_Progress.HasFlag("TAL_N3Encontrada"))
            SetObjective("Debo mirar el estante por reparar.", "La tablilla muestra cuatro marcas. Me ayudará a reconocer las piezas de la incubadora.");
        else if (!Collected("PiezaPulso"))
        {
            if (!SAL_Progress.HasFlag("TAL_MotorLimpio"))
                SetObjective("Debo limpiar el motor de la pulidora.", "Mantendré E frente al motor sucio hasta quitar la grasa.");
            else if (!SAL_Progress.HasFlag("TAL_CableInstalado"))
                SetObjective("Debo conectar el Cable al borne libre.", "Seleccionaré el Cable en el inventario y pulsaré E junto a la pulidora.");
            else
                SetObjective("La pulidora está lista. Debo probarla.", "Soltaré E y pulsaré de nuevo para ponerla en marcha. Quiero escuchar su ritmo.");
        }
        // La marca del Taller se recoge antes de continuar la ronda por otras salas.
        else if (!SAL_Progress.HasFlag(SAL_Progress.DiamondSeal))
            SetObjective("Me falta la marca de ronda del Taller.", "Mantendré E en la placa PUNTO DE RONDA. El sello quedará guardado en el Cuaderno.");
        else if (!seed && !SAL_Progress.HasFlag(SAL_Progress.DrawingsFound))
            SetObjective("Quiero saber quién es ella.", "Busca entre los dibujos del basurero de la Sala de máquinas.");
        else if (!Collected("PiezaOscuridad"))
            SetObjective("Me falta la Pieza Oscuridad.", "Revisa el gabinete del Laboratorio. La Pelota puede ayudarme a abrirlo.");
        else if (!Collected("PiezaCalor"))
            SetObjective("Me falta la Pieza Calor.", "Revisa la cámara de la Bodega fría. Sus bandejas esconden una pista.");
        else if (!Collected("PiezaBruma"))
            SetObjective("Me falta la Pieza Bruma.", "Revisa el conjunto del Filtro 14 en la Sala de máquinas. La Pelota puede servir en la válvula.");
        else if (seed)
        {
            if (missingNote)
                SetObjective("Quiero revisar detrás de mi Cargador.", "Volveré al Taller y limpiaré el panel de servicio del Cargador. Recuerdo haber guardado algo allí.");
            else
                SetObjective("Debo llevar la semilla al Invernadero.", "Ya tengo la semilla y las cuatro piezas. Usaré el Cortador para abrir el paso al Invernadero.");
        }
        else if (!SAL_Progress.HasFlag(SAL_Progress.TriangleSeal))
            SetObjective("Me falta la marca de ronda del Laboratorio.", "Limpiaré el punto de ronda del Laboratorio para guardar su sello en el Cuaderno.");
        else if (!SAL_Progress.HasFlag(SAL_Progress.CrossSeal))
            SetObjective("Me falta la marca de ronda de la Bodega.", "Limpiaré el punto de ronda de la Bodega. N2 guarda el orden de las tres marcas.");
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
