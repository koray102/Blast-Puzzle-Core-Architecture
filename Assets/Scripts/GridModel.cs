using System;
using System.Collections.Generic;

public class GridModel
{
    private Node[,] _grid;
    public int Width { get; private set; }
    public int Height { get; private set; }

    // Patlayan blokların listesini dışarıya fırlatacak event
    public event Action<List<Node>> OnBlocksMatched;


    public GridModel(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new Node[width, height];
        InitializeGrid();
    }


    private void InitializeGrid()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                // Şimdilik rastgele dolduruyoruz (Test için)
                BlockType randomType = (BlockType)UnityEngine.Random.Range(1, 6);
                _grid[x, y] = new Node(x, y, randomType);
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
    

    public void CheckAndMatch(int startX, int startY)
    {
        Node startNode = GetNode(startX, startY);
        
        // Boş bir yere veya zaten eşleşmiş bir yere tıklandıysa iptal et
        if (startNode == null || startNode.IsEmpty() || startNode.IsMatched) return;

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
            // Modelden blokları temizle
            foreach (var node in matchedNodes)
            {
                node.Type = BlockType.None;
                node.IsMatched = false; // Sonraki turlar için reset
            }

            // VFX, SFX ve View için event fırlat
            OnBlocksMatched?.Invoke(matchedNodes);
        }
        else
        {
            // Patlama olmadıysa eşleşme durumlarını sıfırla
            foreach (var node in matchedNodes)
            {
                node.IsMatched = false;
            }
        }
    }
}