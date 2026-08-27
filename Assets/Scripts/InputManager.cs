using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("JubgementLine")]
    [SerializeField] private SpriteRenderer[] judgmentLine;
    [SerializeField] private Color[] alphaColor;
    private float maxAlpha = 1.0f;
    private float midAlpha = 0.5f;

    [Header("Player")]
    [SerializeField] private GameObject player;
    [SerializeField] private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        playerInput.actions["Lane1"].performed += OnLane1;
        playerInput.actions["Lane1"].canceled += OnLane1;
        playerInput.actions["Lane2"].performed += OnLane2;
        playerInput.actions["Lane2"].canceled += OnLane2;

    }


    private void OnDisable()
    {
        playerInput.actions["Lane1"].performed -= OnLane1;
        playerInput.actions["Lane1"].canceled -= OnLane1;
        playerInput.actions["Lane2"].performed -= OnLane2;
        playerInput.actions["Lane2"].canceled -= OnLane2;
    }


    void Start()
    {
        alphaColor = new Color[judgmentLine.Length];

        for(int i = 0;  i < judgmentLine.Length; i++)
        {
            alphaColor[i] = judgmentLine[i].color;
        }
        
    }
    
    void OnLane1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            alphaColor[0].a = Mathf.Clamp01(maxAlpha);
            judgmentLine[0].color = alphaColor[0];
        }
        else if (context.canceled)
        {
            alphaColor[0].a = Mathf.Clamp01(midAlpha);
            judgmentLine[0].color = alphaColor[0];
        }   
    }
    
    void OnLane2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            alphaColor[1].a = Mathf.Clamp01(maxAlpha);
            judgmentLine[1].color = alphaColor[1];
        }
        else if (context.canceled)
        {
            alphaColor[1].a = Mathf.Clamp01(midAlpha);
            judgmentLine[1].color = alphaColor[1];
        }
    }


    void OnPause(InputValue value)
    {
        Debug.Log("Pause!");
    }
    
}
