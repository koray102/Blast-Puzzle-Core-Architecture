using System;
using UnityEngine;

[Serializable]
public abstract class LevelGoal
{
    // UI bu event'i dinleyecek
    public Action OnGoalUpdated; 

    // Unity Inspector'ından bu hedef için bir ikon seçeceğiz
    public Sprite goalIcon;
    
    public abstract void Init(); // Oyun başlarken sayacı sıfırlamak için
    public abstract bool IsMet(); // Oyun bitti mi kontrolu
    public abstract void UpdateGoal(BlockType poppedBlockType);
    public abstract int GetRemainingCount();
}
