using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Bu script sadece LevelData objelerine tıklandığında Inspector'ı değiştirir
[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    // Reflection ile bulduğumuz tipleri hafızada tutalım ki her saniye baştan aramasın
    private Type[] _goalTypes;

    private void OnEnable()
    {
        // 1. Projedeki tüm sınıfları (Assembly) tara
        // 2. LevelGoal sınıfından türeyenleri bul
        // 3. Abstract olmayan (yani gerçekten yaratılabilen) sınıfları filtrele
        _goalTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(LevelGoal)))
            .ToArray();
    }

    public override void OnInspectorGUI()
    {
        // Unity'nin varsayılan arayüzünü çiz (Total Moves ve eklediğimiz listeler görünsün)
        DrawDefaultInspector();

        // Üzerinde çalıştığımız LevelData dosyasını yakala
        LevelData data = (LevelData)target;

        GUILayout.Space(20);
        GUILayout.Label("Hedef Ekleme Paneli", EditorStyles.boldLabel);

        // Reflection ile bulduğumuz her bir hedef sınıfı için otomatik buton çiz
        foreach (Type type in _goalTypes)
        {
            if (GUILayout.Button($"+ {type.Name} Ekle", GUILayout.Height(25)))
            {
                // Activator ile o tipin instance'ını (saf C# objesini) yaratıyoruz
                LevelGoal newGoal = (LevelGoal)Activator.CreateInstance(type);
                
                // Objenin ilk ayarlarını (varsa sıfırlamalarını) yapıyoruz
                newGoal.Init();

                // Listeye ekliyoruz
                data.levelGoals.Add(newGoal);
                
                // Unity'ye dosyanın değiştiğini ve Ctrl+S yapıldığında kaydetmesi gerektiğini söylüyoruz
                EditorUtility.SetDirty(data);
            }
        }
    }
}