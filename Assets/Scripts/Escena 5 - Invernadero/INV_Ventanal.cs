using UnityEngine;

/// <summary>Ventanal del Invernadero hacia el jardín exterior. Sus plantas cambian tras la revelación.</summary>
[RequireComponent(typeof(Collider))]
public sealed class INV_Ventanal : MonoBehaviour
{
    [SerializeField] private string normalMessage = "Plantas verdes y frondosas en el jardín de la nave.";
    [SerializeField] private string revealedMessage = "Las plantas del jardín exterior se han vuelto grises... marchitas por las esporas.";

    private CORE_PlayerController player;
    private bool inRange;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CORE_PlayerController pc))
        {
            player = pc;
            inRange = true;
            CORE_InteractionHUD.SetPrompt(this, "E · Mirar por el ventanal");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CORE_PlayerController>())
        {
            player = null;
            inRange = false;
            CORE_InteractionHUD.ClearPrompt(this);
        }
    }

    private void OnDisable()
    {
        inRange = false;
        player = null;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    private void Update()
    {
        if (!inRange || player == null) return;
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) return;

        if (player.InteractPressed)
        {
            bool revealed = SAL_Progress.HasFlag(SAL_Progress.Revealed);
            CORE_InteractionHUD.ShowMessage(revealed ? revealedMessage : normalMessage, 4f);
        }
    }
}
