using TMPro;
using UnityEngine;

public class ComboView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI combo;

    private void Start()
    {
        Updated(GameManager.Instance.Combo);

        GameManager.Instance.ComboChanged += OnComboChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ComboChanged -= OnComboChanged;
    }

    private void OnComboChanged(int current)
    {
        Updated(current);
    }


    private void Updated(int current)
    {
        combo.text = "Combo : " + current;
    }
}
