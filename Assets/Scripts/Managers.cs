using UnityEngine;
using UnityHFSM;
public class Managers : MonoBehaviour
{
    [SerializeField] private StateMachine fsm;

    void Start()
    {
        fsm = new StateMachine();

    }

    void Update()
    {
    }
}
