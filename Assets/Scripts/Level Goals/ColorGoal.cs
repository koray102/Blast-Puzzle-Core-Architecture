using System;

[Serializable]
public class ColorGoal : LevelGoal
{
    public BlockType targetColor;
    public int targetAmount;
    private int _currentAmount = 0;

    public override void UpdateGoal(Node node)
    {
        // DİKKAT: Type yerine ColorBlock kontrol ediliyor
        if (node.ColorBlock == targetColor)
        {
            _currentAmount++;
            OnGoalUpdated?.Invoke();
        }
    }

    
    public override void Init()
    {
        _currentAmount = 0;
    }


    public override bool IsMet() => _currentAmount >= targetAmount;
    public override int GetRemainingCount() => targetAmount - _currentAmount;
}