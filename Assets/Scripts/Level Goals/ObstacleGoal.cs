using System;

[Serializable]
public class ObstacleGoal : LevelGoal
{
    public ObstacleType targetObstacle;
    public int targetAmount;
    private int _currentAmount = 0;

    public override void UpdateGoal(Node node)
    {
        // DİKKAT: ColorBlock yerine Obstacle enum'ı kontrol ediliyor
        if (node.Obstacle == targetObstacle)
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
    
    // Ufak bir UI Güvenliği: Eksi değerlere düşmesini engellemek için Math.Max kullanıyoruz.
    // (Örn: 1 kutu lazımsa ama roket 3 kutuyu aynı anda kırdıysa UI'da -2 yazmasını engeller)
    public override int GetRemainingCount() => Math.Max(0, targetAmount - _currentAmount);
}