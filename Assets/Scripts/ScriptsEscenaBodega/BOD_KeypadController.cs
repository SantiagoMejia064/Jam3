using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Scene-authored keypad logic. The Canvas, chamber and visuals are assigned in the Inspector.
/// Button OnClick events call AddDigit, Clear, Confirm and ClosePanel.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class BOD_KeypadController : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private string correctCode = "7294";
    [SerializeField] private string prompt = "E · Usar teclado";

    [Header("Scene references")]
    [SerializeField] private GameObject keypadCanvas;
    [SerializeField] private Text displayText;
    [SerializeField] private Text feedbackText;
    [SerializeField] private GameObject chamberDoor;
    [SerializeField] private GameObject heatPiece;
    [SerializeField] private GameObject ductHint;
    [SerializeField] private Light ductHintLight;
    [SerializeField] private float hintFlashSeconds = 2f;
    [SerializeField] private BOD_Audio audioFeedback;

    private CORE_PlayerController player;
    private Collider triggerCollider;
    private string enteredCode = string.Empty;
    private int failedAttempts;
    private bool solved;
    private const string ChamberOpened = "BOD_CamaraAbierta";
    private Coroutine hintRoutine;
    private bool pausedPlayer;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (keypadCanvas) keypadCanvas.SetActive(false);
        solved = SAL_Progress.HasFlag(ChamberOpened) || CORE_InventoryBridge.WasCollected("PiezaCalor");
        if (heatPiece) heatPiece.SetActive(solved && !CORE_InventoryBridge.WasCollected("PiezaCalor"));
        if (chamberDoor) chamberDoor.SetActive(!solved);
        if (solved) enteredCode = correctCode;
        if (ductHint) ductHint.SetActive(false);
        RefreshDisplay();
    }

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CORE_PlayerController found))
        {
            player = found;
            CORE_InteractionHUD.SetPrompt(this, prompt);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CORE_PlayerController>())
        {
            ClosePanel();
            player = null;
            CORE_InteractionHUD.ClearPrompt(this);
        }
    }

    private void Update()
    {
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) { ClosePanel(); return; }
        if (keypadCanvas && keypadCanvas.activeSelf)
        {
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)) ClosePanel();
            return;
        }
        if (!PlayerIsInRange())
        {
            if (player) CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            return;
        }

        if (player.InteractPressed && keypadCanvas && !keypadCanvas.activeSelf)
            OpenPanel();
    }

    public void OpenPanel()
    {
        if (!keypadCanvas || (NotebookController.Instance && NotebookController.Instance.IsOpen)) return;
        keypadCanvas.SetActive(true);
        SetFeedback(solved ? "Cámara abierta." : "OSCURO → CLARO");
        if (player && player.enabled)
        {
            player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            player.enabled = false;
            pausedPlayer = true;
        }
        RefreshDisplay();
    }

    public void ClosePanel()
    {
        if (keypadCanvas) keypadCanvas.SetActive(false);
        if (pausedPlayer && player) player.enabled = true;
        pausedPlayer = false;
    }

    public void AddDigit(string digit)
    {
        if (solved || string.IsNullOrEmpty(digit) || enteredCode.Length >= correctCode.Length) return;
        enteredCode += digit[0];
        RefreshDisplay();
    }

    public void Clear()
    {
        if (solved) return;
        enteredCode = string.Empty;
        RefreshDisplay();
        SetFeedback("OSCURO → CLARO");
    }

    public void Confirm()
    {
        if (solved) return;
        if (enteredCode.Length != correctCode.Length)
        {
            SetFeedback("Introduce los cuatro dígitos.");
            return;
        }
        if (enteredCode == correctCode)
        {
            solved = true;
            SAL_Progress.SetFlag(ChamberOpened);
            if (audioFeedback)
            {
                audioFeedback.PlayCorrectCode();
                audioFeedback.PlayChamberOpening();
            }
            if (chamberDoor) chamberDoor.SetActive(false);
            if (heatPiece) heatPiece.SetActive(true);
            SetFeedback("Cámara abierta. Pieza Calor disponible.");
            CORE_InteractionHUD.ShowMessage("Cámara de descongelado abierta.");
            return;
        }

        failedAttempts++;
        if (audioFeedback) audioFeedback.PlayWrongCode();
        enteredCode = string.Empty;
        RefreshDisplay();
        SetFeedback("Código incorrecto.");
        if (failedAttempts >= 2)
        {
            if (hintRoutine != null) StopCoroutine(hintRoutine);
            hintRoutine = StartCoroutine(FlashDuctHint());
        }
    }

    private IEnumerator FlashDuctHint()
    {
        if (ductHint) ductHint.SetActive(true);
        if (ductHintLight) ductHintLight.enabled = true;
        yield return new WaitForSeconds(hintFlashSeconds);
        if (ductHintLight) ductHintLight.enabled = false;
        if (ductHint) ductHint.SetActive(false);
    }

    private void RefreshDisplay()
    {
        if (displayText) displayText.text = enteredCode.PadRight(correctCode.Length, '—');
    }

    private void SetFeedback(string value)
    {
        if (feedbackText) feedbackText.text = value;
    }

    private bool PlayerIsInRange()
    {
        Collider playerCollider = player ? player.GetComponent<Collider>() : null;
        return triggerCollider && playerCollider && triggerCollider.bounds.Intersects(playerCollider.bounds);
    }

    private void OnDisable()
    {
        ClosePanel();
        CORE_InteractionHUD.ClearPrompt(this);
        player = null;
    }
}
