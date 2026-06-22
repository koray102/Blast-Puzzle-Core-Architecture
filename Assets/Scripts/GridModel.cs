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


    public GridModel(int width, int height, BlockType[,] initialBoard = null)
    {
        Width = width;
        Height = height;
        _grid = new Node[width, height];
        
        InitializeGrid(initialBoard);
    }


    private void InitializeGrid(BlockType[,] initialBoard)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Node node = new Node(x, y);

                // Eğer dışarıdan manuel bir dizilim verildiyse onu kullan
                if (initialBoard != null)
                {
                    node.Type = initialBoard[x, y];
                }
                else
                {
                    // Verilmediyse tamamen rastgele üret
                    node.Type = (BlockType)UnityEngine.Random.Range(1, 6);
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
        if (startNode == null || startNode.IsEmpty() || startNode.IsMatched) return false;

        BlockType targetType = startNode.Type;
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

                if (neighbor != null && !neighbor.IsMatched && neighbor.Type == targetType)
                {
                    neighbor.IsMatched = true;
                    nodesToCheck.Enqueue(neighbor);
                }
            }
        }

        // En az 2 blok yan yanaysa patlar
        if (matchedNodes.Count >= 2)
        {
            // VFX, SFX ve View için event fırlat
            OnBlocksMatched?.Invoke(matchedNodes);

            // Modelden blokları temizle
            foreach (var node in matchedNodes)
            {
                node.Type = BlockType.None;
                node.IsMatched = false; // Sonraki turlar için reset
            }

            // Eşleşme BAŞARILI, Controller'a TRUE döndür
            return true;
        }
        else
        {
            // Patlama olmadıysa eşleşme durumlarını sıfırla
            foreach (var node in matchedNodes)
            {
                node.IsMatched = false;
            }

            // Eşleşme BAŞARISIZ, Controller'a FALSE döndür
            return false;
        }
    }


    // Bu fonksiyonu BoardController'dan, patlatma işleminden hemen sonra çağıracağız.
    public void ApplyGravity()
    {
        List<BlockMoveData> moveMoves = new List<BlockMoveData>();

        // Sütun sütun tarıyoruz (Soldan sağa)
        for (int x = 0; x < Width; x++)
        {
            // Aşağıdan yukarıya doğru boşluk arıyoruz (y=0 en alt satır kabul ediyoruz)
            int emptyY = 0; 

            for (int y = 0; y < Height; y++)
            {
                Node currentNode = GetNode(x, y);

                if (!currentNode.IsEmpty())
                {
                    // Eğer blok boşluktan daha yukarıdaysa, onu aşağı (emptyY'ye) çek
                    if (y > emptyY)
                    {
                        Node targetNode = GetNode(x, emptyY);
                        targetNode.Type = currentNode.Type; // Rengi aşağıya kopyala
                        currentNode.Type = BlockType.None;  // Eski yeri boşalt

                        moveMoves.Add(new BlockMoveData(x, y, x, emptyY));
                    }
                    emptyY++; // Bir sonraki olası boşluk bir üst satır
                }
            }
        }

        // Eğer hareket eden blok varsa View'a haber ver
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
            for (int y = 0; y < Height; y++)
            {
                Node node = GetNode(x, y);
                if (node.IsEmpty())
                {
                    // Yeni rastgele renk ata
                    BlockType randomType = (BlockType)UnityEngine.Random.Range(1, 6);
                    node.Type = randomType;
                    newNodes.Add(node);
                }
            }
        }

        // Yeni bloklar oluştuysa View'a haber ver
        if (newNodes.Count > 0)
        {
            OnNewBlocksSpawned?.Invoke(newNodes);
        }
    }
}