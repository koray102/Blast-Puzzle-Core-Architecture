using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private Type[] _goalTypes;

    private void OnEnable()
    {
        _goalTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(LevelGoal)))
            .ToArray();
    }

    // Tahtanın boyutlarının çizim için %100 hazır olup olmadığını kontrol eden fonksiyon
    private bool IsBoardReadyToDraw(LevelData data)
    {
        if (data.startingBoard == null || data.startingBoard.Length != data.boardHeight) 
            return false;
        
        for (int i = 0; i < data.boardHeight; i++)
        {
            if (data.startingBoard[i].columns == null || data.startingBoard[i].columns.Length != data.boardWidth)
                return false;
        }
        
        return true;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelData data = (LevelData)target;

        // ==========================================
        // BÖLÜM 1: MANUEL TAHTA DİZİLİMİ (GRID)
        // ==========================================
        if (data.useManualSetup)
        {
            GUILayout.Space(20);
            GUILayout.Label("🛠️ Manuel Tahta Dizilimi", EditorStyles.boldLabel);

            if (data.availableColors == null || data.availableColors.Count == 0)
            {
                EditorGUILayout.HelpBox("Lütfen önce 'Available Colors' listesine renk ekleyin!", MessageType.Warning);
            }
            // YENİ VE GÜVENLİ KONTROLÜMÜZ BURADA
            else if (!IsBoardReadyToDraw(data)) 
            {
                EditorGUILayout.HelpBox("Matris boyutları güncelleniyor... Lütfen Inspector'da boş bir yere tıklayın veya Enter'a basın.", MessageType.Info);
            }
            else
            {
                List<string> optionsList = new List<string> { "None" };
                List<BlockType> valueList = new List<BlockType> { BlockType.None };

                foreach (BlockType color in data.availableColors)
                {
                    optionsList.Add(color.ToString());
                    valueList.Add(color);
                }

                string[] options = optionsList.ToArray();

                EditorGUI.BeginChangeCheck();

                for (int y = data.boardHeight - 1; y >= 0; y--)
                {
                    EditorGUILayout.BeginHorizontal(); 
                    
                    for (int x = 0; x < data.boardWidth; x++)
                    {
                        BlockType currentBlock = data.startingBoard[y].columns[x];
                        
                        int selectedIndex = valueList.IndexOf(currentBlock);
                        if (selectedIndex == -1) selectedIndex = 0; 

                        // --- YENİ KISIM: RENK KUTUCUĞU ---
                        
                        // Seçilen rengin gerçek Unity karşılığını al (İkon veya Renk)
                        Color cellColor = GetColorForBlockType(currentBlock);
                        
                        // Görseli oluştur (Bir buton veya kutu çiziyoruz)
                        GUI.backgroundColor = cellColor;
                        string cellLabel = options[selectedIndex];
                        
                        // Dropdown'ı bir buton gibi çiziyoruz, tıklayınca popup açılıyor
                        if (EditorGUILayout.DropdownButton(new GUIContent(cellLabel), FocusType.Passive, GUILayout.Width(65), GUILayout.Height(25)))
                        {
                            GenericMenu menu = new GenericMenu();
                            
                            int currentX = x;
                            int currentY = y;

                            for (int i = 0; i < options.Length; i++)
                            {
                                int index = i; 
                                menu.AddItem(new GUIContent(options[i]), i == selectedIndex, () => {
                                    data.startingBoard[currentY].columns[currentX] = valueList[index];
                                    EditorUtility.SetDirty(data);
                                });
                            }
                            menu.ShowAsContext();
                        }
                        GUI.backgroundColor = Color.white; // Rengi sıfırla ki diğerleri etkilenmesin
                    }
                    
                    EditorGUILayout.EndHorizontal(); 
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(data);
                }
            }
        }

        // ==========================================
        // BÖLÜM 2: DİNAMİK HEDEF EKLEME PANELİ
        // ==========================================
        GUILayout.Space(20);
        GUILayout.Label("✨ Hedef Ekleme Paneli", EditorStyles.boldLabel);

        foreach (Type type in _goalTypes)
        {
            if (GUILayout.Button($"+ {type.Name} Ekle", GUILayout.Height(25)))
            {
                LevelGoal newGoal = (LevelGoal)Activator.CreateInstance(type);
                newGoal.Init();
                data.levelGoals.Add(newGoal);
                EditorUtility.SetDirty(data);
            }
        }

        if (data.levelGoals.Count > 0)
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Son Hedefi Sil", GUILayout.Height(20)))
            {
                data.levelGoals.RemoveAt(data.levelGoals.Count - 1);
                EditorUtility.SetDirty(data);
            }
        }
    }

    private Color GetColorForBlockType(BlockType type)
    {
        switch (type)
        {
            case BlockType.Red: return Color.red;
            case BlockType.Blue: return Color.blue;
            case BlockType.Green: return Color.green;
            case BlockType.Yellow: return Color.yellow;
            case BlockType.Purple: return new Color(0.5f, 0, 0.5f); // Mor
            default: return Color.gray; // None durumu
        }
    }
}