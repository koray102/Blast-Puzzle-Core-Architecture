using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoalUIView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText; 

    private LevelGoal _trackedGoal;

    public void Init(LevelGoal goal)
    {
        _trackedGoal = goal;
        iconImage.sprite = goal.goalIcon;
        
        // Başlangıç değerini UI'a yazdır
        UpdateUI();

        // Hedefteki değişiklikleri dinlemeye başla
        _trackedGoal.OnGoalUpdated += UpdateUI;
    }

    private void UpdateUI()
    {
        int remaining = _trackedGoal.GetRemainingCount();
        
        if (remaining <= 0)
        {
            countText.text = "0"; // Hedef bittiyse tik işareti koy
        }
        else
        {
            countText.text = remaining.ToString();
        }
    }

    private void OnDestroy()
    {
        // Bellek sızıntısını önlemek için obje silinirken aboneliği bırak
        if (_trackedGoal != null)
        {
            _trackedGoal.OnGoalUpdated -= UpdateUI;
        }
    }
}