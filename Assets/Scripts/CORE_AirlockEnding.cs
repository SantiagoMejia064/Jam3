using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opens Javier's authored airlock hatch and finishes the story when Ella reaches it.</summary>
public sealed class CORE_AirlockEnding : MonoBehaviour
{
    [SerializeField] private Transform hatch;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private TMP_Text endingText;
    [SerializeField, Min(0.1f)] private float slideDistance = 2.4f;
    [SerializeField, Min(0.1f)] private float openDuration = 1.6f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 1.6f;
    [SerializeField, Min(0f)] private float returnToMenuDelay = 5f;
    [SerializeField, TextArea] private string endingMessage = "Ella acabó con toda la vida en la nave, solo te queda esperar a oxidarte o apagarte...";

    private Vector3 closedLocalPosition;
    private bool opening;

    private void Awake()
    {
        if (hatch) closedLocalPosition = hatch.localPosition;
        if (fadeOverlay)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.interactable = false;
            fadeOverlay.blocksRaycasts = false;
        }
        if (endingText) endingText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (opening || !SAL_Progress.HasFlag(SAL_Progress.FinalComplete)) return;
        if (!SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito)) return;
        if (!hatch)
        {
            Debug.LogError("CORE_AirlockEnding: falta asignar la escotilla en el Inspector.", this);
            enabled = false;
            return;
        }

        opening = true;
        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        Vector3 start = hatch.localPosition;
        Vector3 end = closedLocalPosition + Vector3.up * slideDistance;
        float elapsed = 0f;
        if (fadeOverlay)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = true;
        }

        while (elapsed < Mathf.Max(openDuration, fadeDuration))
        {
            elapsed += Time.deltaTime;
            float doorT = Mathf.Clamp01(elapsed / openDuration);
            doorT = doorT * doorT * (3f - 2f * doorT);
            hatch.localPosition = Vector3.LerpUnclamped(start, end, doorT);
            if (fadeOverlay) fadeOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            if (endingText && fadeOverlay && fadeOverlay.alpha >= 1f && !endingText.gameObject.activeSelf)
            {
                endingText.text = endingMessage;
                endingText.gameObject.SetActive(true);
            }
            yield return null;
        }

        hatch.localPosition = end;

        INV_IncubatorCart cart = INV_IncubatorCart.ActiveAttachedInstance;
        if (!cart) cart = FindFirstObjectByType<INV_IncubatorCart>(FindObjectsInactive.Include);
        if (cart) cart.SetEllaVisible(false);

        SAL_Progress.SetFlag(SAL_Progress.FinalComplete);
        CORE_PlayerController player = PlayerPersistence.Instance
            ? PlayerPersistence.Instance.GetComponent<CORE_PlayerController>()
            : FindFirstObjectByType<CORE_PlayerController>();
        if (player)
        {
            if (player.TryGetComponent(out Rigidbody body)) body.linearVelocity = Vector3.zero;
            player.enabled = false;
        }

        if (endingText)
        {
            endingText.text = endingMessage;
            endingText.gameObject.SetActive(true);
        }
        else
        {
            CORE_InteractionHUD.ShowMessage(endingMessage, returnToMenuDelay);
        }

        yield return new WaitForSecondsRealtime(returnToMenuDelay);

        if (cart) Destroy(cart.gameObject);
        if (PlayerPersistence.Instance) Destroy(PlayerPersistence.Instance.gameObject);
        if (CORE_Inventory.Instance) Destroy(CORE_Inventory.Instance.gameObject);
        if (NotebookController.Instance) Destroy(NotebookController.Instance.gameObject);

        Time.timeScale = 1f;
        SceneManager.LoadScene("Assets/inicio/MenuInicio.unity", LoadSceneMode.Single);
    }
}
