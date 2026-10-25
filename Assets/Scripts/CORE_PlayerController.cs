using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Side-on movement for Siete. Keeps the character on a fixed depth plane.</summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class CORE_PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float fixedZ;
    [Header("Audio de movimiento - acompana al robot entre escenas")]
    [SerializeField] private AudioClip movimientoRobot;
    [SerializeField, HideInInspector] private AudioSource movimientoSource;
    private Rigidbody body;
    private InputAction moveAction;
    private InputAction interactAction;
    private float horizontalInput;
    public bool InteractPressed => interactAction != null && interactAction.WasPressedThisFrame();
    public bool InteractHeld => interactAction != null && interactAction.IsPressed();

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
    }

    private void OnEnable() { moveAction?.Enable(); interactAction?.Enable(); }
    private void OnDisable() { moveAction?.Disable(); interactAction?.Disable(); if (movimientoSource) movimientoSource.Stop(); }
    private void OnDestroy() { moveAction?.Dispose(); interactAction?.Dispose(); }

    private void Update()
    {
        if (NotebookController.Instance && NotebookController.Instance.IsOpen)
        {
            horizontalInput = 0;
            return;
        }
        float actionX = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;
        float keyboardX = 0f;
        if (Keyboard.current != null)
        {
            bool left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
            bool right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
            keyboardX = (right ? 1f : 0f) - (left ? 1f : 0f);
        }
        horizontalInput = keyboardX != 0f ? keyboardX : actionX;
    }

    private void LateUpdate()
    {
        if (!movimientoSource) return;
        if (movimientoSource.clip != movimientoRobot)
        {
            movimientoSource.Stop();
            movimientoSource.clip = movimientoRobot;
        }
        bool moving = body && Mathf.Abs(body.linearVelocity.x) > .1f && Time.timeScale > 0f;
        if (moving && movimientoRobot)
        {
            if (!movimientoSource.isPlaying) movimientoSource.Play();
        }
        else if (movimientoSource.isPlaying) movimientoSource.Stop();
    }

    private void FixedUpdate()
    {
        float x = horizontalInput;
        body.linearVelocity = new Vector3(x * moveSpeed, 0f, 0f);
        Vector3 p = body.position; p.z = fixedZ; body.position = p;
    }
}
