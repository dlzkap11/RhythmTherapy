using TMPro;
using UnityEngine;

public class ScoreView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI score;
    [SerializeField] private TextMeshProUGUI scoreAvg;

    private void Start()
    {
        Updated(GameManager.Instance.Score);
        GameManager.Instance.ScoreChanged += OnScoreChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int current, int cnt)
    {
        Updated(current);
        AvgScore((float)current/ cnt);
    }


    private void Updated(int current)
    {
        score.text = "score : " + current;
    }


    private void AvgScore(float avg)
    {
        scoreAvg.text = avg.ToString("F2") + "%";
    }
}
