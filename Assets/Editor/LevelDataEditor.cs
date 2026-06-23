using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private enum PaintMode { ColorBlock, Obstacle, Booster, Clear }
    private PaintMode _currentMode = PaintMode.ColorBlock;

    private BlockType _selectedColor = BlockType.Red;
    private ObstacleType _selectedObstacle = ObstacleType.Box;
    private BoosterType _selectedBooster = BoosterType.RocketVertical;

    private Type[] _goalTypes;

    private void OnEnable()
    {
        _goalTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(LevelGoal)) && !type.IsAbstract)
            .ToArray();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelData data = (LevelData)target;

        // --- HEDEF (GOAL) EKLEME PANELİ ---
        GUILayout.Space(20);
        GUILayout.Label("✨ Hedef Ekleme Paneli", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        if (_goalTypes != null)
        {
            foreach (Type type in _goalTypes)
            {
                if (GUILayout.Button($"+ {type.Name} Ekle", GUILayout.Height(25)))
                {
                    Undo.RecordObject(data, "Hedef Ekle"); 
                    
                    LevelGoal newGoal = (LevelGoal)Activator.CreateInstance(type);
                    newGoal.Init();
                    data.levelGoals.Add(newGoal);
                    
                    EditorUtility.SetDirty(data);
                }
            }
        }

        if (data.levelGoals != null && data.levelGoals.Count > 0)
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Son Hedefi Sil", GUILayout.Height(20)))
            {
                Undo.RecordObject(data, "Hedef Sil");
                
                data.levelGoals.RemoveAt(data.levelGoals.Count - 1);
                EditorUtility.SetDirty(data);
            }
        }
        EditorGUILayout.EndVertical();
        // ----------------------------------


        if (!data.useManualSetup)
        {
            return;
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("--- LEVEL DESIGNER TOOL ---", EditorStyles.boldLabel);

        serializedObject.Update();

        // --- PALET / FIRÇA AYARLARI PANELİ ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Fırça Seçenekleri", EditorStyles.miniBoldLabel);
        _currentMode = (PaintMode)EditorGUILayout.EnumPopup("Fırça Modu", _currentMode);

        switch (_currentMode)
        {
            case PaintMode.ColorBlock:
                // SADECE AVAILABLE COLORS LİSTESİNDEKİ RENKLERİ GÖSTER
                if (data.availableColors == null || data.availableColors.Count == 0)
                {
                    EditorGUILayout.HelpBox("Lütfen önce yukarıdaki 'Available Colors' listesine renk ekleyin!", MessageType.Warning);
                }
                else
                {
                    // Seçili rengin listedeki indeksini bul, yoksa 0'a sabitle
                    int currentIndex = Mathf.Max(0, data.availableColors.IndexOf(_selectedColor));
                    
                    // Listedeki renkleri string dizisine çevirip Popup (Açılır menü) olarak göster
                    string[] options = data.availableColors.Select(c => c.ToString()).ToArray();
                    currentIndex = EditorGUILayout.Popup("Boyanacak Renk", currentIndex, options);
                    
                    // Seçilen rengi değişkene ata
                    _selectedColor = data.availableColors[currentIndex];
                }
                break;
                
            case PaintMode.Obstacle:
                _selectedObstacle = (ObstacleType)EditorGUILayout.EnumPopup("Boyanacak Engel", _selectedObstacle);
                break;
                
            case PaintMode.Booster:
                _selectedBooster = (BoosterType)EditorGUILayout.EnumPopup("Boyanacak Booster", _selectedBooster);
                break;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Tahta Görünümü (Tıklayarak Boyayın)", EditorStyles.miniBoldLabel);

        // --- GÖRSEL MATRİS (GRID) ÇİZİMİ ---
        SerializedProperty startingBoardProp = serializedObject.FindProperty("startingBoard");

        if (startingBoardProp == null || startingBoardProp.arraySize != data.boardHeight)
        {
            EditorGUILayout.HelpBox("Grid dizilimi hazırlanıyor... Değişiklikleri görmek için useManualSetup kutusunu kapatıp açın.", MessageType.Info);
            return;
        }

        for (int y = data.boardHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            
            SerializedProperty rowProp = startingBoardProp.GetArrayElementAtIndex(y);
            SerializedProperty columnsProp = rowProp.FindPropertyRelative("columns");

            if (columnsProp == null || columnsProp.arraySize != data.boardWidth)
            {
                EditorGUILayout.EndHorizontal();
                continue;
            }

            for (int x = 0; x < data.boardWidth; x++)
            {
                SerializedProperty cellProp = columnsProp.GetArrayElementAtIndex(x);
                SerializedProperty colorProp = cellProp.FindPropertyRelative("colorBlock");
                SerializedProperty obstacleProp = cellProp.FindPropertyRelative("obstacle");
                SerializedProperty boosterProp = cellProp.FindPropertyRelative("booster");

                string cellText = GetCellTextSummary(colorProp, obstacleProp, boosterProp);
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = GetCellVisualColor((BlockType)colorProp.enumValueIndex, (ObstacleType)obstacleProp.enumValueIndex, (BoosterType)boosterProp.enumValueIndex);

                if (GUILayout.Button(cellText, GUILayout.Width(65), GUILayout.Height(50)))
                {
                    ApplyPaintBrush(colorProp, obstacleProp, boosterProp, data);
                }

                GUI.backgroundColor = originalColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private string GetCellTextSummary(SerializedProperty colorProp, SerializedProperty obstacleProp, SerializedProperty boosterProp)
    {
        BlockType color = (BlockType)colorProp.enumValueIndex;
        ObstacleType obs = (ObstacleType)obstacleProp.enumValueIndex;
        BoosterType boost = (BoosterType)boosterProp.enumValueIndex;

        if (obs != ObstacleType.None) return $"[{obs}]";
        if (boost != BoosterType.None) return $"*{boost}*";
        if (color != BlockType.None) return color.ToString();
        
        return "-";
    }

    private Color GetCellVisualColor(BlockType color, ObstacleType obs, BoosterType boost)
    {
        if (obs == ObstacleType.Box) return new Color(0.5f, 0.35f, 0.25f); 
        if (boost != BoosterType.None) return Color.cyan; 

        switch (color)
        {
            case BlockType.Red: return new Color(1f, 0.4f, 0.4f);
            case BlockType.Blue: return new Color(0.4f, 0.6f, 1f);
            case BlockType.Green: return new Color(0.4f, 1f, 0.4f);
            case BlockType.Yellow: return new Color(1f, 0.9f, 0.4f);
            case BlockType.Purple: return new Color(0.7f, 0.4f, 0.8f);
            default: return Color.white; 
        }
    }

    // Metoda data parametresi eklendi, böylece fırça atamasında güvenlik kontrolü yapılabilir
    private void ApplyPaintBrush(SerializedProperty colorProp, SerializedProperty obstacleProp, SerializedProperty boosterProp, LevelData data)
    {
        switch (_currentMode)
        {
            case PaintMode.ColorBlock:
                // Güvenlik: Eğer listede hiç renk yoksa boyama yapma
                if (data.availableColors == null || data.availableColors.Count == 0) return;
                
                colorProp.enumValueIndex = (int)_selectedColor;
                obstacleProp.enumValueIndex = (int)ObstacleType.None;
                boosterProp.enumValueIndex = (int)BoosterType.None;
                break;
                
            case PaintMode.Obstacle:
                obstacleProp.enumValueIndex = (int)_selectedObstacle;
                boosterProp.enumValueIndex = (int)BoosterType.None;
                if (_selectedObstacle == ObstacleType.Box)
                {
                    colorProp.enumValueIndex = (int)BlockType.None;
                }
                break;
                
            case PaintMode.Booster:
                boosterProp.enumValueIndex = (int)_selectedBooster;
                colorProp.enumValueIndex = (int)BlockType.None;
                obstacleProp.enumValueIndex = (int)ObstacleType.None;
                break;
                
            case PaintMode.Clear:
                colorProp.enumValueIndex = (int)BlockType.None;
                obstacleProp.enumValueIndex = (int)ObstacleType.None;
                boosterProp.enumValueIndex = (int)BoosterType.None;
                break;
        }
    }
}