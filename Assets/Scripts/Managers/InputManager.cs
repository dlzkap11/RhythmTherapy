using RhythmTherapy.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class InputManager : MonoBehaviour
{
    [Header("JubgementLine")]
    [SerializeField] private SpriteRenderer[] judgmentLine;
    [SerializeField] private Color[] alphaColor;
    private float maxAlpha = 1.0f;
    private float midAlpha = 0.5f;

    [Tooltip("마지막 입력이 찍힌 노래 시각(ms). 표시 전용 — 인스펙터에서 값 확인용.")]
    [SerializeField] private int inputTimeMs;

    [Header("Player")]
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
        playerInput.actions["Pause"].performed += OnPause;

    }


    private void OnDisable()
    {
        playerInput.actions["Lane1"].performed -= OnLane1;
        playerInput.actions["Lane1"].canceled -= OnLane1;
        playerInput.actions["Lane2"].performed -= OnLane2;
        playerInput.actions["Lane2"].canceled -= OnLane2;
        playerInput.actions["Pause"].performed -= OnPause;

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
            Pop(0, context.time);
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
            Pop(1, context.time);
            alphaColor[1].a = Mathf.Clamp01(maxAlpha);
            judgmentLine[1].color = alphaColor[1];
        }
        else if (context.canceled)
        {
            alphaColor[1].a = Mathf.Clamp01(midAlpha);
            judgmentLine[1].color = alphaColor[1];
        }
    }


    void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (Conductor.Instance != null)
            {
                if (Conductor.Instance.IsPaused)
                {
                    Conductor.Instance.Resume();
                }
                else
                {
                    Conductor.Instance.Pause();
                }
            }
        }
    }

    /// <summary>
    /// 입력 1회를 판정으로 넘긴다. 판정 기준 시각은 콜백이 도착한 시점이 아니라
    /// OS 가 키를 인식한 시점(eventTime)이다 — Input System 은 이벤트를 프레임당 1회
    /// 처리하므로, 그대로 두면 최대 한 프레임(약 8ms)만큼 늦게 찍힌다.
    /// </summary>
    private void Pop(int lane, double eventTime)
    {
        Conductor conductor = Conductor.Instance;
        if (conductor == null || conductor.IsPaused)
            return;

        double staleMs = (InputState.currentTime - eventTime) * 1000.0;
        inputTimeMs = Mathf.RoundToInt((float)(conductor.SongTimeMs - staleMs));

        LaneManager.Instance.FindAndGetNote(lane, inputTimeMs);
    }
}
