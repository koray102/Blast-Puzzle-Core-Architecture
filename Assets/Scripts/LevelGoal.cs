using System;

[Serializable]
public abstract class LevelGoal
{
    public abstract void Init(); // Oyun başlarken sayacı sıfırlamak için
    public abstract bool IsMet(); // Oyun bitti mi kontrolu
    public abstract void UpdateGoal(BlockType poppedBlockType);
    public abstract int GetRemainingCount();
}
