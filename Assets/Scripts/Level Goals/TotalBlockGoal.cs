using System;

[Serializable]
public class TotalBlockGoal : LevelGoal
{
    public int targetAmount;
    private int _currentAmount = 0;

    public override void UpdateGoal(BlockType poppedBlockType)
    {
        // Renge bakmadan her patlayan blokta sayacı artır
        if (poppedBlockType != BlockType.None)
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