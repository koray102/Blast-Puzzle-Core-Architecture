using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[Serializable]
public struct BoardRow
{
    // Bu satırdaki sütunlar (blok renkleri)
    public BlockType[] columns; 
}

[CreateAssetMenu(fileName = "New Level", menuName = "Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Progression")]
    public LevelData nextLevel;

    [Header("Board Settings")]
    public int boardWidth = 8;
    public int boardHeight = 10;

    [Header("Manual Board Setup")]
    public bool useManualSetup = false; // Rastgele mi, el dizilimi mi?
    [HideInInspector]
    public BoardRow[] startingBoard;

    [Header("Game Settings")]
    public int totalMoves; // Hamle sayısı
    [SerializeReference] 
    public List<LevelGoal> levelGoals = new List<LevelGoal>();
    public List<BlockType> availableColors; // Ağırlıklı rastgelelik için renk havuzu da burada olabilir

    // Custom lvl designer icin
    private void OnValidate()
    {
        if (useManualSetup)
        {
            bool changed = false;

            if (startingBoard == null || startingBoard.Length != boardHeight)
            {
                Array.Resize(ref startingBoard, boardHeight);
                changed = true;
            }

            for (int i = 0; i < boardHeight; i++)
            {
                if (startingBoard[i].columns == null || startingBoard[i].columns.Length != boardWidth)
                {
                    Array.Resize(ref startingBoard[i].columns, boardWidth);
                    changed = true;
                }
            }

            // Eğer bir şeyler değiştiyse, Inspector'a "Hemen kendini tazele!" diyoruz
            if (changed)
            {
                EditorUtility.SetDirty(this);
            }
        }
    }
}