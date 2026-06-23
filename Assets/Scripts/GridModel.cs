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
        
        // Boş bir yere veya zaten eşleşmiş bir yere tıklandıysa iptal et ve FALSE döndür
        if (startNode == null) return false; // Tahta dışı
        
        // KURAL 1: Oyuncu bir Engele (Obstacle) doğrudan TIKLAYAMAZ!
        if (startNode.Obstacle != ObstacleType.None) return false; 
        
        // KURAL 2: Tıklanan objenin bir rengi olmalı ve zaten patlatılmamış olmalı
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

                if (neighbor != null && !neighbor.IsMatched && neighbor.ColorBlock == targetType)
                {
                    neighbor.IsMatched = true;
                    nodesToCheck.Enqueue(neighbor);
                }
            }
        }

        // En az 2 blok yan yanaysa patlar
        if (matchedNodes.Count >= 2)
        {
            // 1. ADIM: ETRAFINDAKİ ENGELLERİ (KUTU VE BALON) BUL
            List<Node> affectedObstacles = new List<Node>();
            List<Node> poppedBubbles = new List<Node>(); // Sadece zarı patlayanlar

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

            // Manager'lar okusun diye event'i fırlat
            OnBlocksMatched?.Invoke(matchedNodes);

            // 2. ADIM: VERİLERİ TEMİZLE
            foreach (Node boxNode in affectedObstacles)
            {
                if (boxNode.Obstacle == ObstacleType.Box) boxNode.Obstacle = ObstacleType.None;
            }

            foreach (var node in matchedNodes)
            {
                node.ColorBlock = BlockType.None;
                node.IsMatched = false; 
            }

            // 3. ADIM: BALONLARI PATLAT VE GÖRSELİ GÜNCELLE
            if (poppedBubbles.Count > 0)
            {
                foreach (Node bubbleNode in poppedBubbles)
                {
                    bubbleNode.Obstacle = ObstacleType.None; // Balon gitti, içindeki renk kaldı!
                }
                
                // View'a "Bu hücreleri silme, sadece dış görünüşlerini (rengini) güncelle" diyoruz
                OnNodesUpdated?.Invoke(poppedBubbles); 
            }

            return true;
        }
        else
        {
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
}