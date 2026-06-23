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
        if (matchedNodes.Count >= 2)
        {
            // ETRAFINDAKİ ENGELLERİ (KUTU VE BALON) BUL
            List<Node> affectedObstacles = new List<Node>();
            List<Node> poppedBubbles = new List<Node>(); 

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

            // Engelleri tiplerine göre ayır
            foreach (Node obstacleNode in affectedObstacles)
            {
                if (obstacleNode.Obstacle == ObstacleType.Box)
                {
                    // Kutu tamamen yok olur, silinmesi için ana listeye ekle
                    if (!matchedNodes.Contains(obstacleNode)) matchedNodes.Add(obstacleNode);
                }
                else if (obstacleNode.Obstacle == ObstacleType.Bubble)
                {
                    // Balon sadece zarını kaybeder, bloğa dokunulmaz
                    poppedBubbles.Add(obstacleNode);
                }
            }

            // ORTAK TEMİZLEYİCİYİ ÇAĞIR (Verileri silme, event fırlatma işlemleri burada yapılır)
            ExecuteDestruction(matchedNodes, poppedBubbles);

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
    private void ExecuteDestruction(List<Node> nodesToDestroy, List<Node> bubblesToPop)
    {
        // 1. Manager'lar ve hedefler (Goals) okusun diye event fırlat
        if (nodesToDestroy.Count > 0)
        {
            OnBlocksMatched?.Invoke(nodesToDestroy);
        }

        // 2. Yok olanların verilerini tamamen sil (Roketler, Kutular, Renkler)
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

        // 3. Balonları patlat ve görsel güncelleme event'ini fırlat
        if (bubblesToPop.Count > 0)
        {
            foreach (Node bubbleNode in bubblesToPop)
            {
                bubbleNode.Obstacle = ObstacleType.None;
                bubbleNode.IsMatched = false;
            }
            OnNodesUpdated?.Invoke(bubblesToPop);
        }
    }


    private void ActivateBooster(Node startBooster)
    {
        List<Node> nodesToDestroy = new List<Node>();
        List<Node> bubblesToPop = new List<Node>();
        
        // Zincirleme reaksiyonları yönetmek için kuyruk (Queue) kullanıyoruz
        Queue<Node> boostersToTrigger = new Queue<Node>();

        boostersToTrigger.Enqueue(startBooster);
        startBooster.IsMatched = true;

        while (boostersToTrigger.Count > 0)
        {
            Node currentBooster = boostersToTrigger.Dequeue();
            
            // Roketin kendisini de yok edilecekler listesine ekle
            nodesToDestroy.Add(currentBooster);

            // Hedef hattı belirle
            List<Node> targetNodes = new List<Node>();

            if (currentBooster.Booster == BoosterType.RocketHorizontal)
            {
                // Yatay eksendeki tüm hücreleri hedefe al
                for (int x = 0; x < Width; x++) targetNodes.Add(GetNode(x, currentBooster.Y));
            }
            else if (currentBooster.Booster == BoosterType.RocketVertical)
            {
                // Dikey eksendeki tüm hücreleri hedefe al
                for (int y = 0; y < Height; y++) targetNodes.Add(GetNode(currentBooster.X, y));
            }

            // Hedef hattındaki objeleri incele ve karar ver
            foreach (Node target in targetNodes)
            {
                // Tahta dışıysa veya zaten bu reaksiyonda işleme alındıysa atla
                if (target == null || target.IsMatched) continue;

                // Boşlukları es geçmek performansı artırır
                if (target.IsEmpty()) continue;

                target.IsMatched = true;

                if (target.Obstacle == ObstacleType.Bubble)
                {
                    // Balonsa sadece zarı patlatılır
                    bubblesToPop.Add(target);
                }
                else if (target.Booster != BoosterType.None)
                {
                    // ZİNCİRLEME REAKSİYON: Roket başka bir roketi vurursa o da kuyruğa girip ateşlenir!
                    boostersToTrigger.Enqueue(target);
                }
                else
                {
                    // Kutuysa veya renkli bloksa tamamen yok edilir
                    nodesToDestroy.Add(target);
                }
            }
        }

        // Toplanan tüm hedefleri tek seferde temizle ve eventleri fırlat
        ExecuteDestruction(nodesToDestroy, bubblesToPop);
    }
}