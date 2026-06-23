using System;

[Serializable]
public class TotalBlockGoal : LevelGoal
{
    public int targetAmount;
    private int _currentAmount = 0;

    public override void UpdateGoal(Node node)
    {
        // DİKKAT: Herhangi bir renkli blok patladıysa say
        if (node.ColorBlock != BlockType.None)
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