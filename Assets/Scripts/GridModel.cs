using System;
using System.Collections.Generic;

public class GridModel
{
    private Node[,] _grid;
    public int Width { get; private set; }
    public int Height { get; private set; }

    // Patlayan blokların listesini dışarıya fırlatacak event
    public event Action<List<Node>> OnBlocksMatched;
    public event Action<List<BlockMoveData>> OnBlocksFell;
    public event Action<List<Node>> OnNewBlocksSpawned;
    public event Action<List<Node>> OnNodesUpdated; // Sadece görseli yenilenecekler
    public event Action<Node, List<Node>> OnBoosterDetonated;


    // DEĞİŞİKLİK: Parametre artık CellSetupData[,] alıyor
    public GridModel(int width, int height, CellSetupData[,] initialBoard = null)
    {
        Width = width;
        Height = height;
        _grid = new Node[width, height];
        
        InitializeGrid(initialBoard);
    }

    private void InitializeGrid(CellSetupData[,] initialBoard)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Node node = new Node(x, y);

                if (initialBoard != null)
                {
                    // DİKKAT: Yeni katmanlı isimlendirmelere göre atama yapılıyor
                    node.ColorBlock = initialBoard[x, y].colorBlock;
                    node.Obstacle = initialBoard[x, y].obstacle;
                    node.Booster = initialBoard[x, y].booster;
                }
                else
                {
                    // Dışarıdan veri gelmediyse varsayılan olarak rastgele renk ata, engel/booster None kalır
                    node.ColorBlock = (BlockType)UnityEngine.Random.Range(1, 6);
                }

                if (node.ColorBlock == BlockType.None && 
                    node.Obstacle == ObstacleType.None && 
                    node.Booster == BoosterType.None)
                {
                    node.ColorBlock = (BlockType)UnityEngine.Random.Range(1, 6);
                }

                _grid[x, y] = node;
            }
        }
    }

    public Node GetNode(int x, int y)
    {
        if (IsValidPosition(x, y))
            return _grid[x, y];
        return null;
    }


    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }


    // Oyuncunun tıkladığı yerin geçerli bir hamle olup olmadığını verileri silmeden kontrol eder
    public bool CanMatch(int x, int y)
    {
        Node node = GetNode(x, y);
        
        // Tahta dışıysa veya doğrudan tıklanamayan bir engelse
        if (node == null || node.Obstacle != ObstacleType.None) return false;
        
        // Eğer roket/bombaysa her türlü tıklanabilir
        if (node.Booster != BoosterType.None) return true;
        
        // Eğer boşsa veya zaten eşleşmişse
        if (node.ColorBlock == BlockType.None || node.IsMatched) return false;

        // Etrafındaki 4 komşuyu kontrol et
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };
        
        for (int i = 0; i < 4; i++)
        {
            Node neighbor = GetNode(node.X + dx[i], node.Y + dy[i]);
            // Eğer komşu aynı renkse ve doğrudan bir kutu/engel değilse eşleşme MÜMKÜNDÜR
            if (neighbor != null && neighbor.Obstacle == ObstacleType.None && neighbor.ColorBlock == node.ColorBlock)
            {
                return true; 
            }
        }
        
        return false;
    }
    

    public bool CheckAndMatch(int startX, int startY)
    {
        Node startNode = GetNode(startX, startY);
        
        // Boş bir yere veya tahta dışına tıklandıysa iptal et
        if (startNode == null) return false; 
        
        // KURAL 1: Oyuncu bir Engele (Obstacle) doğrudan TIKLAYAMAZ!
        if (startNode.Obstacle != ObstacleType.None) return false; 
        
        // KURAL 2: Eğer tıklanan hücrede BOOSTER varsa, roket ateşlenir!
        if (startNode.Booster != BoosterType.None)
        {
            if (startNode.IsMatched) return false; 
            
            ActivateBooster(startNode);
            return true;
        }

        // KURAL 3: Tıklanan objenin bir rengi olmalı ve zaten patlatılmamış olmalı
        if (startNode.ColorBlock == BlockType.None || startNode.IsMatched) return false;

        BlockType targetType = startNode.ColorBlock;
        List<Node> matchedNodes = new List<Node>();
        Queue<Node> nodesToCheck = new Queue<Node>();
        
        // Başlangıç noktasını kuyruğa ekle
        nodesToCheck.Enqueue(startNode);
        startNode.IsMatched = true;

        // Yönler (Yukarı, Aşağı, Sağ, Sol)
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        // BFS Döngüsü
        while (nodesToCheck.Count > 0)
        {
            Node currentNode = nodesToCheck.Dequeue();
            matchedNodes.Add(currentNode);

            // 4 yöndeki komşuları kontrol et
            for (int i = 0; i < 4; i++)
            {
                int nx = currentNode.X + dx[i];
                int ny = currentNode.Y + dy[i];

                Node neighbor = GetNode(nx, ny);

                // Komşu uygun renkte mi ve daha önce eklenmemiş mi?
                if (neighbor != null && !neighbor.IsMatched && neighbor.ColorBlock == targetType)
                {
                    // DİKKAT: Engeller (Kutu/Balon) eşleşme döngüsüne (BFS) dahil edilmez
                    if (neighbor.Obstacle == ObstacleType.None) 
                    {
                        neighbor.IsMatched = true;
                        nodesToCheck.Enqueue(neighbor);
                    }
                }
            }
        }

        // En az 2 blok yan yanaysa patlar
        // En az 2 blok yan yanaysa patlar
        if (matchedNodes.Count >= 2)
        {
            List<Node> affectedObstacles = new List<Node>();
            List<Node> nodesToUpdate = new List<Node>(); // İsim değişti

            // --- BOOSTER ÜRETİM (SPAWN) MANTIĞI ---
            int matchCount = matchedNodes.Count;
            BoosterType boosterToSpawn = BoosterType.None;

            if (matchCount >= 9) boosterToSpawn = BoosterType.DiscoBall;
            else if (matchCount >= 7) boosterToSpawn = BoosterType.Bomb;
            else if (matchCount >= 5)
            {
                // Roketin yönünü %50 ihtimalle rastgele belirliyoruz
                boosterToSpawn = UnityEngine.Random.value > 0.5f ? BoosterType.RocketHorizontal : BoosterType.RocketVertical;
            }
            // -------------------------------------

            // (Kutu ve balon bulma döngüsü aynı kalıyor...)
            foreach (Node matchedNode in matchedNodes)
            {
                for (int i = 0; i < 4; i++)
                {
                    int nx = matchedNode.X + dx[i];
                    int ny = matchedNode.Y + dy[i];
                    Node neighbor = GetNode(nx, ny);

                    if (neighbor != null && neighbor.Obstacle != ObstacleType.None && !affectedObstacles.Contains(neighbor))
                    {
                        affectedObstacles.Add(neighbor);
                    }
                }
            }

            foreach (Node obstacleNode in affectedObstacles)
            {
                if (obstacleNode.Obstacle == ObstacleType.Box)
                {
                    if (!matchedNodes.Contains(obstacleNode)) matchedNodes.Add(obstacleNode);
                }
                else if (obstacleNode.Obstacle == ObstacleType.Bubble)
                {
                    nodesToUpdate.Add(obstacleNode);
                }
            }

            // --- YENİ EKLENEN KISIM: TIKLANAN NOKTAYI BOOSTER'A DÖNÜŞTÜR ---
            if (boosterToSpawn != BoosterType.None)
            {
                // 1. Tıklanan bloğu silinmekten kurtar
                matchedNodes.Remove(startNode);
                
                // 2. Verisini güncelle
                startNode.ColorBlock = BlockType.None;
                startNode.Booster = boosterToSpawn;
                
                // 3. Görselinin "Booster Rengine" dönmesi için güncelleme listesine ekle
                nodesToUpdate.Add(startNode); 
            }
            // ----------------------------------------------------------------

            ExecuteDestruction(matchedNodes, nodesToUpdate);
            return true;
        }
        else
        {
            // Eşleşme başarısız olduysa IsMatched durumlarını geri al
            foreach (var node in matchedNodes)
            {
                node.IsMatched = false;
            }
            return false;
        }
    }


    // Bu fonksiyonu BoardController'dan, patlatma işleminden hemen sonra çağıracağız.
    public void ApplyGravity()
    {
        List<BlockMoveData> moveMoves = new List<BlockMoveData>();

        for (int x = 0; x < Width; x++)
        {
            // O sütundaki en alt boşluğu takip edeceğiz.
            int emptyY = -1; 

            // İşlemi AŞAĞIDAN YUKARIYA doğru yapıyoruz ki blokları en dibe çekebilelim
            for (int y = 0; y < Height; y++)
            {
                Node currentNode = GetNode(x, y);

                if (currentNode.Obstacle == ObstacleType.Box)
                {
                    // KURAL 1: Kutuya çarptık! Kutu yolu tıkar. 
                    // Bu noktadan sonra üstten gelen hiçbir blok kutunun altına DÜŞEMEZ.
                    emptyY = -1; 
                }
                else if (currentNode.CanBlockFallInto())
                {
                    // Eğer henüz boş bir yer işaretlemediysek, burası yeni dip noktamızdır
                    if (emptyY == -1) emptyY = y;
                }
                else 
                {
                    // Hücrede hareket edebilir bir şey var (Renk, Booster veya BALONLU Blok)
                    if (emptyY != -1)
                    {
                        Node targetNode = GetNode(x, emptyY);
                        
                        targetNode.ColorBlock = currentNode.ColorBlock;
                        targetNode.Booster = currentNode.Booster;
                        
                        // YENİ EKLENEN KISIM: Eğer blokta balon varsa o da beraber düşer
                        targetNode.Obstacle = currentNode.Obstacle; 
                        
                        currentNode.ColorBlock = BlockType.None;
                        currentNode.Booster = BoosterType.None;
                        currentNode.Obstacle = ObstacleType.None;

                        moveMoves.Add(new BlockMoveData(x, y, x, emptyY));
                        
                        emptyY++;
                    }
                }
            }
        }

        if (moveMoves.Count > 0)
        {
            OnBlocksFell?.Invoke(moveMoves);
        }
    }


    // Yerçekiminden sonra boş kalan tepe kısımları doldurur
    public void FillEmptySpaces()
    {
        List<Node> newNodes = new List<Node>();

        for (int x = 0; x < Width; x++)
        {
            // Yukarıdan aşağı doğru (gökyüzünden yere) tarama yapıyoruz
            for (int y = Height - 1; y >= 0; y--)
            {
                Node node = GetNode(x, y);

                if (node.Obstacle == ObstacleType.Box)
                {
                    // Kutuya denk geldik! Kutu yolu tıkadığı için altındaki boşluklara yağmur yağamaz.
                    // Döngüyü kırıp diğer sütuna geçiyoruz.
                    break; 
                }
                
                if (node.CanBlockFallInto())
                {
                    BlockType randomType = (BlockType)UnityEngine.Random.Range(1, 6);
                    node.ColorBlock = randomType;
                    newNodes.Add(node);
                }
            }
        }

        if (newNodes.Count > 0)
        {
            OnNewBlocksSpawned?.Invoke(newNodes);
        }
    }

   
    // Hem normal eşleşmelerin hem de roket/bomba patlamalarının ortak temizlik noktası
    private void ExecuteDestruction(List<Node> nodesToDestroy, List<Node> nodesToUpdate)
    {
        if (nodesToDestroy.Count > 0)
        {
            OnBlocksMatched?.Invoke(nodesToDestroy);
        }

        foreach (Node node in nodesToDestroy)
        {
            node.ColorBlock = BlockType.None;
            node.Booster = BoosterType.None; 
            
            if (node.Obstacle == ObstacleType.Box) 
            {
                node.Obstacle = ObstacleType.None;
            }
            node.IsMatched = false;
        }

        // Görseli güncellenecek (Silinmeyecek) hücreler: Balonlar ve Yeni Boosterlar
        if (nodesToUpdate.Count > 0)
        {
            foreach (Node node in nodesToUpdate)
            {
                // Eğer balonsa zarını patlat, booster ise zaten aşağıda ayarlandı
                if (node.Obstacle == ObstacleType.Bubble) 
                {
                    node.Obstacle = ObstacleType.None;
                }
                node.IsMatched = false; // Ortak güvenlik sıfırlaması
            }
            OnNodesUpdated?.Invoke(nodesToUpdate);
        }
    }


    private void ActivateBooster(Node startBooster)
    {
        List<Node> allNodesToClear = new List<Node>();
        List<Node> bubblesToPop = new List<Node>();
        List<Node> specificDestroyedNodes = new List<Node>(); 

        allNodesToClear.Add(startBooster);
        startBooster.IsMatched = true;

        List<Node> targetNodes = new List<Node>();

        // --- HEDEF BELİRLEME MANTIĞI AYNI KALIYOR ---
        if (startBooster.Booster == BoosterType.RocketHorizontal)
        {
            for (int x = 0; x < Width; x++) targetNodes.Add(GetNode(x, startBooster.Y));
        }
        else if (startBooster.Booster == BoosterType.RocketVertical)
        {
            for (int y = 0; y < Height; y++) targetNodes.Add(GetNode(startBooster.X, y));
        }
        else if (startBooster.Booster == BoosterType.Bomb) 
        {
            for (int x = startBooster.X - 1; x <= startBooster.X + 1; x++)
            {
                for (int y = startBooster.Y - 1; y <= startBooster.Y + 1; y++)
                {
                    Node targetNode = GetNode(x, y);
                    if (targetNode != null) targetNodes.Add(targetNode);
                }
            }
        }
        else if (startBooster.Booster == BoosterType.DiscoBall)
        {
            BlockType targetColor = GetMostAbundantColor();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Node targetNode = GetNode(x, y);
                    if (targetNode != null && targetNode.ColorBlock == targetColor && !targetNode.IsMatched) 
                    {
                        targetNodes.Add(targetNode);
                    }
                }
            }
        }

        // --- HEDEFLERİ İNCELE ---
        foreach (Node target in targetNodes)
        {
            if (target == null || target.IsMatched) continue;
            if (target.IsEmpty()) continue;

            if (target.Obstacle == ObstacleType.Bubble)
            {
                bubblesToPop.Add(target); 
                target.IsMatched = true;
            }
            else if (target.Booster != BoosterType.None)
            {
                target.IsMatched = true;
                specificDestroyedNodes.Add(target); 
            }
            else
            {
                target.IsMatched = true;
                specificDestroyedNodes.Add(target); 
                allNodesToClear.Add(target); 
            }
        }

        OnBoosterDetonated?.Invoke(startBooster, specificDestroyedNodes);

        // 2. TEMİZLİK (Aynen Kalıyor)
        if (allNodesToClear.Count > 0)
        {
            foreach (Node node in allNodesToClear)
            {
                node.ColorBlock = BlockType.None;
                node.Booster = BoosterType.None; 
                if (node.Obstacle == ObstacleType.Box) node.Obstacle = ObstacleType.None;
                node.IsMatched = false;
            }
        }

        if (bubblesToPop.Count > 0)
        {
            foreach (Node node in bubblesToPop)
            {
                if (node.Obstacle == ObstacleType.Bubble) node.Obstacle = ObstacleType.None;
                node.IsMatched = false;
            }
            OnNodesUpdated?.Invoke(bubblesToPop);
        }
    }


    // Zincirleme reaksiyonlarda View'dan gelen "Ben oraya ulaştım, patlat" emri.
    public void DetonateChainedBooster(int x, int y)
    {
        Node node = GetNode(x, y);
        
        // Zincirleme reaksiyonda hedefin IsMatched değeri önceden true yapılmış olabilir.
        // O yüzden sadece hala bir booster ise (önceden patlayıp temizlenmemişse) zorla patlatıyoruz!
        if (node != null && node.Booster != BoosterType.None)
        {
            ActivateBooster(node);
        }
    }


    // Tahtadaki en popüler rengi bulan yardımcı metod
    // GridModel.cs içindeki metod:
    private BlockType GetMostAbundantColor()
    {
        Dictionary<BlockType, int> colorCounts = new Dictionary<BlockType, int>();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Node node = GetNode(x, y);
                // KRİTİK DÜZELTME: Zaten eşleşmiş (başka bir booster tarafından patlatılmakta olan) blokları SAYMA!
                if (node != null && node.ColorBlock != BlockType.None && !node.IsMatched)
                {
                    if (!colorCounts.ContainsKey(node.ColorBlock))
                    {
                        colorCounts[node.ColorBlock] = 0;
                    }
                    colorCounts[node.ColorBlock]++;
                }
            }
        }

        BlockType mostAbundant = BlockType.None;
        int maxCount = 0;

        foreach (var pair in colorCounts)
        {
            if (pair.Value > maxCount)
            {
                maxCount = pair.Value;
                mostAbundant = pair.Key;
            }
        }

        // Eğer uygun renk kalmadıysa (Tüm tahta zaten patlıyorsa) rastgele seç
        if (mostAbundant == BlockType.None)
        {
            mostAbundant = (BlockType)UnityEngine.Random.Range(1, 6);
        }

        return mostAbundant;
    }
}