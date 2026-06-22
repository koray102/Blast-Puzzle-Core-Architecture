using System;

[Serializable]
public class ColorGoal : LevelGoal
{
    public BlockType targetColor;
    public int targetAmount;
    private int _currentAmount = 0;

    public override void UpdateGoal(BlockType poppedBlockType)
    {
        // Sadece kendi rengiyse sayacı artır
        if (poppedBlockType == targetColor)
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