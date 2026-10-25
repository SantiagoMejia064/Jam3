using UnityEngine;
using TMPro;

public class TAL_Pulidora : MonoBehaviour
{
    public GameObject cleaningArea;
    public GameObject interactionArea;

    public GameObject brokenCableVisual;
    public GameObject installedCableVisual;

    public Transform rotatingPart;
    public float rotationSpeed = 25f;
    public Vector3 rotationAxis = Vector3.forward;

    public AudioSource pulseSource;
    public AudioClip pulseClip;
    public float pulseInterval = 1.2f;

    public TMP_Text statusText;
    public Renderer statusLight;
    public Material repairMaterial;
    public Material readyMaterial;
    public Material runningMaterial;

    private const string MotorClean = "TAL_MotorLimpio";
    private const string CableInstalled = "TAL_CableInstalado";
    private const string MachineTested = "TAL_PulidoraProbada";

    private bool running;
    private float nextPulse;
    private AudioClip generatedPulse;

    void Start()
    {
        // Sonido provisional que ya utiliza el sistema de audio.
        if (pulseClip == null)
        {
            generatedPulse = SAL_ProceduralSound.CreatePulse();
            pulseClip = generatedPulse;
        }

        Refresh();
    }

    void Update()
    {
        if (!running)
            return;

        if (rotatingPart != null)
        {
            rotatingPart.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }

        if (pulseSource != null && Time.time >= nextPulse)
        {
            pulseSource.PlayOneShot(pulseClip);
            nextPulse = Time.time + Mathf.Max(0.6f, pulseInterval);
        }
    }

    public void CleanMotor()
    {
        SAL_Progress.SetFlag(MotorClean);
        Refresh();
    }

    public void Interact()
    {
        if (NotebookController.Instance != null && NotebookController.Instance.IsOpen)
        {
            return;
        }

        if (!SAL_Progress.HasFlag(MotorClean))
        {
            CORE_InteractionHUD.ShowMessage("Primero debo limpiar el motor.");
            return;
        }

        if (!SAL_Progress.HasFlag(CableInstalled))
        {
            if (!CORE_InventoryBridge.HasItem("Cable"))
            {
                CORE_InteractionHUD.ShowMessage("Falta un cable.");
                return;
            }

            if (!CORE_InventoryBridge.IsSelected("Cable"))
            {
                CORE_InteractionHUD.ShowMessage("Selecciona el Cable en el inventario.");
                return;
            }

            SAL_Progress.SetFlag(CableInstalled);
            CORE_InventoryBridge.RemoveItem("Cable");

            Refresh();

            CORE_InteractionHUD.ShowMessage("Cable instalado. Pulsa E para probarla.");

            return;
        }

        if (!SAL_Progress.HasFlag(MachineTested))
        {
            bool alreadyCollected = CORE_InventoryBridge.WasCollected("PiezaPulso");

            if (!alreadyCollected && !CORE_InventoryBridge.TryAddItem("PiezaPulso"))
            {
                CORE_InteractionHUD.ShowMessage("No se pudo obtener la Pieza Pulso.");
                return;
            }

            SAL_Progress.SetFlag(MachineTested);
            Refresh();

            CORE_InteractionHUD.ShowMessage("Gira lento… Pieza Pulso obtenida.");

            return;
        }

        CORE_InteractionHUD.ShowMessage("Tum… tum… Ese ritmo me resulta familiar.");
    }

    void Refresh()
    {
        bool clean = SAL_Progress.HasFlag(MotorClean);
        bool connected = SAL_Progress.HasFlag(CableInstalled);

        running = SAL_Progress.HasFlag(MachineTested);

        if (statusText != null)
            statusText.text = running ? "PULSO ACTIVO" :
                connected ? "03 / PROBAR" : clean ? "02 / CONECTAR CABLE" : "01 / LIMPIAR MOTOR";

        Material lightMaterial = running ? runningMaterial : connected ? readyMaterial : repairMaterial;
        if (statusLight != null && lightMaterial != null)
            statusLight.sharedMaterial = lightMaterial;

        if (cleaningArea != null)
            cleaningArea.SetActive(!clean);

        if (interactionArea != null)
            interactionArea.SetActive(clean);

        if (brokenCableVisual != null)
            brokenCableVisual.SetActive(clean && !connected);

        if (installedCableVisual != null)
            installedCableVisual.SetActive(connected);
    }

    void OnDestroy()
    {
        if (generatedPulse != null)
            Destroy(generatedPulse);
    }
}
