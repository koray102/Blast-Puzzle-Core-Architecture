using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Board Settings")]
    public int boardWidth = 8;
    public int boardHeight = 10;

    
    [Header("Game Settings")]
    public int totalMoves; // Hamle sayısı

    // [SerializeReference] sayesinde editörde farklı alt sınıfları aynı listede tutabiliriz
    [SerializeReference] 
    public List<LevelGoal> levelGoals = new List<LevelGoal>();
    
    // Ağırlıklı rastgelelik için renk havuzu da burada olabilir
    public List<BlockType> availableColors;
}