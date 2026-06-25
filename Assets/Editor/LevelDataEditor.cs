using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Artık CustomEditor değil, bağımsız bir EditorWindow kullanıyoruz
public class LevelDesignerWindow : EditorWindow
{
    // --- Window State (Pencere Durumu) ---
    private List<LevelData> _allLevels = new List<LevelData>();
    private int _currentLevelIndex = 0;
    
    private Vector2 _mainScrollPos;
    private Vector2 _gridScrollPos;

    // --- Paint State (Fırça Durumu) ---
    private enum PaintMode { ColorBlock, Obstacle, Booster, Clear }
    private PaintMode _currentMode = PaintMode.ColorBlock;
    private BlockType _selectedColor = BlockType.Red;
    private ObstacleType _selectedObstacle = ObstacleType.Box;
    private BoosterType _selectedBooster = BoosterType.RocketVertical;

    private Type[] _goalTypes;

    // Üst menüye ekleme (Tools sekmesinden açabilirsin)
    [MenuItem("Tools/Level Designer Window")]
    public static void ShowWindow()
    {
        // Pencereyi aç ve ismini belirle
        LevelDesignerWindow window = GetWindow<LevelDesignerWindow>("Level Designer");
        window.minSize = new Vector2(500, 600);
    }

    private void OnEnable()
    {
        // Hedef tiplerini bul (Eski kodla aynı mantık)
        _goalTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(LevelGoal)) && !type.IsAbstract)
            .ToArray();

        LoadAllLevels();
    }

    // Projedeki tüm LevelData SO'larını otomatik bulur!
    private void LoadAllLevels()
    {
        _allLevels.Clear();
        string[] guids = AssetDatabase.FindAssets("t:LevelData");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (level != null)
            {
                _allLevels.Add(level);
            }
        }
        
        // İsimlerine göre sırala (Örn: Level_1, Level_2)
        _allLevels = _allLevels.OrderBy(l => l.name).ToList();
        
        if (_currentLevelIndex >= _allLevels.Count) _currentLevelIndex = 0;
    }

    private void OnGUI()
    {
        // Tüm içeriği kaydırılabilir yap
        _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);

        // 1. ÜSTTEKİ NAVİGASYON BAR
        DrawNavigationBar();

        if (_allLevels.Count == 0)
        {
            EditorGUILayout.HelpBox("Projede hiç LevelData ScriptableObject bulunamadı!", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        LevelData currentData = _allLevels[_currentLevelIndex];
        SerializedObject serializedObject = new SerializedObject(currentData);

        EditorGUILayout.Space(10);
        
        // Levelin diğer standart ayarlarını çiz (Hamle sayısı vs.)
        EditorGUILayout.LabelField($"Şu An Düzenlenen: {currentData.name}", EditorStyles.boldLabel);
        
        serializedObject.Update();
        
        // 'useManualSetup', 'boardWidth', 'boardHeight', 'totalMoves' vb. özellikleri manuel çiziyoruz
        DrawLevelSettings(serializedObject, currentData);

        // --- HEDEF (GOAL) EKLEME PANELİ ---
        DrawGoalPanel(currentData);

        if (currentData.useManualSetup)
        {
            DrawDesignerTool(serializedObject, currentData);
        }

        serializedObject.ApplyModifiedProperties();
        
        // --- YENİ EKLENEN KISIM ---
        EditorGUILayout.Space(20); // Alet çantasından sonra biraz boşluk bırak
        
        // 2. ALTTAKİ NAVİGASYON BAR
        DrawNavigationBar(); 
        // -------------------------

        EditorGUILayout.EndScrollView();
    }

    private void DrawNavigationBar()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Level Navigasyonu", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();

        // Önceki Level Butonu
        GUI.enabled = _currentLevelIndex > 0;
        if (GUILayout.Button("◄ Önceki Level", GUILayout.Height(30)))
        {
            _currentLevelIndex--;
            GUI.FocusControl(null); // Fokus sıfırla ki fieldlar buga girmesin
        }
        GUI.enabled = true;

        // Ortada Açılır Menü (Tüm levelları listeden seçmek için)
        if (_allLevels.Count > 0)
        {
            string[] levelNames = _allLevels.Select(l => l.name).ToArray();
            _currentLevelIndex = EditorGUILayout.Popup(_currentLevelIndex, levelNames, GUILayout.Height(30));
        }

        // Sonraki Level Butonu
        GUI.enabled = _currentLevelIndex < _allLevels.Count - 1;
        if (GUILayout.Button("Sonraki Level ►", GUILayout.Height(30)))
        {
            _currentLevelIndex++;
            GUI.FocusControl(null);
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Levelları Yenile (Refresh)", GUILayout.Height(25)))
        {
            LoadAllLevels();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawLevelSettings(SerializedObject serializedObject, LevelData currentData)
    {
        EditorGUILayout.BeginVertical("box");
        
        SerializedProperty nextLevelProp = serializedObject.FindProperty("nextLevel");
        SerializedProperty totalMovesProp = serializedObject.FindProperty("totalMoves");
        SerializedProperty boardWidthProp = serializedObject.FindProperty("boardWidth");
        SerializedProperty boardHeightProp = serializedObject.FindProperty("boardHeight");
        SerializedProperty useManualSetupProp = serializedObject.FindProperty("useManualSetup");
        SerializedProperty availableColorsProp = serializedObject.FindProperty("availableColors");

        EditorGUILayout.PropertyField(nextLevelProp);
        EditorGUILayout.PropertyField(totalMovesProp);
        EditorGUILayout.PropertyField(boardWidthProp);
        EditorGUILayout.PropertyField(boardHeightProp);
        EditorGUILayout.PropertyField(availableColorsProp, true); // true = diziyi genişletilebilir yapar
        EditorGUILayout.PropertyField(useManualSetupProp);
        
        EditorGUILayout.EndVertical();
    }

    private void DrawGoalPanel(LevelData data)
    {
        GUILayout.Space(10);
        GUILayout.Label("✨ Hedef Ekleme Paneli", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        
        // Mevcut hedefleri Inspector gibi çiz (Hedef detaylarını ayarlayabilmek için)
        SerializedObject serializedObject = new SerializedObject(data);
        SerializedProperty goalsProp = serializedObject.FindProperty("levelGoals");
        EditorGUILayout.PropertyField(goalsProp, true);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        
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
    }

    private void DrawDesignerTool(SerializedObject serializedObject, LevelData data)
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("--- LEVEL DESIGNER TOOL ---", EditorStyles.boldLabel);

        // --- PALET / FIRÇA AYARLARI PANELİ ---
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Fırça Seçenekleri", EditorStyles.miniBoldLabel);
        _currentMode = (PaintMode)EditorGUILayout.EnumPopup("Fırça Modu", _currentMode);

        switch (_currentMode)
        {
            case PaintMode.ColorBlock:
                if (data.availableColors == null || data.availableColors.Count == 0)
                {
                    EditorGUILayout.HelpBox("Lütfen önce yukarıdaki 'Available Colors' listesine renk ekleyin!", MessageType.Warning);
                }
                else
                {
                    int currentIndex = Mathf.Max(0, data.availableColors.IndexOf(_selectedColor));
                    string[] options = data.availableColors.Select(c => c.ToString()).ToArray();
                    currentIndex = EditorGUILayout.Popup("Boyanacak Renk", currentIndex, options);
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

        // --- GÖRSEL MATRİS (GRID) ÇİZİMİ (SCROLL İLE) ---
        SerializedProperty startingBoardProp = serializedObject.FindProperty("startingBoard");

        if (startingBoardProp == null || startingBoardProp.arraySize != data.boardHeight)
        {
            EditorGUILayout.HelpBox("Grid dizilimi hazırlanıyor... Değişiklikleri görmek için useManualSetup kutusunu kapatıp açın.", MessageType.Info);
            return;
        }

        DrawBoardStats(startingBoardProp, data);
        // --------------------------------------------------------------------------

        EditorGUILayout.LabelField("Tahta Görünümü (Tıklayarak Boyayın)", EditorStyles.miniBoldLabel);

        // --- GÖRSEL MATRİS (GRID) ÇİZİMİ (SCROLL İLE) ---
        if (startingBoardProp == null || startingBoardProp.arraySize != data.boardHeight)
        {
            EditorGUILayout.HelpBox("Grid dizilimi hazırlanıyor... Değişiklikleri görmek için useManualSetup kutusunu kapatıp açın.", MessageType.Info);
            return;
        }

        // Devasa levelların sığması için Grid'i kendi özel ScrollView'unun içine alıyoruz
        _gridScrollPos = EditorGUILayout.BeginScrollView(_gridScrollPos, "box", GUILayout.Height(350));

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

                // Düğmelerin boyutu
                if (GUILayout.Button(cellText, GUILayout.Width(65), GUILayout.Height(50)))
                {
                    ApplyPaintBrush(colorProp, obstacleProp, boosterProp, data);
                }

                GUI.backgroundColor = originalColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView(); // Grid scroll sonu
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

    private void ApplyPaintBrush(SerializedProperty colorProp, SerializedProperty obstacleProp, SerializedProperty boosterProp, LevelData data)
    {
        switch (_currentMode)
        {
            case PaintMode.ColorBlock:
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

    private void DrawBoardStats(SerializedProperty startingBoardProp, LevelData data)
    {
        if (startingBoardProp == null || startingBoardProp.arraySize != data.boardHeight) return;

        Dictionary<string, int> stats = new Dictionary<string, int>();

        // Tüm tahtayı tarayıp neyin nerede olduğunu sayıyoruz
        for (int y = 0; y < data.boardHeight; y++)
        {
            SerializedProperty rowProp = startingBoardProp.GetArrayElementAtIndex(y);
            SerializedProperty columnsProp = rowProp.FindPropertyRelative("columns");

            if (columnsProp == null || columnsProp.arraySize != data.boardWidth) continue;

            for (int x = 0; x < data.boardWidth; x++)
            {
                SerializedProperty cellProp = columnsProp.GetArrayElementAtIndex(x);
                SerializedProperty colorProp = cellProp.FindPropertyRelative("colorBlock");
                SerializedProperty obstacleProp = cellProp.FindPropertyRelative("obstacle");
                SerializedProperty boosterProp = cellProp.FindPropertyRelative("booster");

                BlockType color = (BlockType)colorProp.enumValueIndex;
                ObstacleType obs = (ObstacleType)obstacleProp.enumValueIndex;
                BoosterType boost = (BoosterType)boosterProp.enumValueIndex;

                // Öncelik sırası: Engel > Booster > Renkli Blok
                if (obs != ObstacleType.None) 
                {
                    string key = obs.ToString();
                    if (!stats.ContainsKey(key)) stats[key] = 0;
                    stats[key]++;
                }
                else if (boost != BoosterType.None)
                {
                    string key = boost.ToString();
                    if (!stats.ContainsKey(key)) stats[key] = 0;
                    stats[key]++;
                }
                else if (color != BlockType.None)
                {
                    string key = color.ToString();
                    if (!stats.ContainsKey(key)) stats[key] = 0;
                    stats[key]++;
                }
            }
        }

        // --- UI ÇİZİMİ ---
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("📊 Tahta İstatistikleri", EditorStyles.boldLabel);

        if (stats.Count > 0)
        {
            // Sözlükteki verileri yan yana güzel bir formata çeviriyoruz (Örn: Red: 5 | Box: 2)
            string statsText = string.Join("   |   ", stats.Select(kv => $"{kv.Key}: {kv.Value}"));
            EditorGUILayout.LabelField(statsText, EditorStyles.wordWrappedLabel);
        }
        else
        {
            EditorGUILayout.LabelField("Tahta şu an tamamen boş.");
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }
}