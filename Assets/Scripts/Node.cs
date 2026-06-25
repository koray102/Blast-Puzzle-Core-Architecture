public class Node
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public BlockType Type { get; set; }
    public BlockType ColorBlock { get; set; } = BlockType.None;
    public ObstacleType Obstacle { get; set; } = ObstacleType.None;
    public BoosterType Booster { get; set; } = BoosterType.None;
    public bool IsMatched { get; set; } = false;

    public Node(int x, int y, BlockType type = BlockType.None)
    {
        X = x;
        Y = y;
        Type = type;
    }

    // İleride buraya "Bu node engellenmiş mi (kutu mu var)?", "Boş mu?" gibi mantıklar ekleyeceğiz.
    public bool IsEmpty()
    {
        return ColorBlock == BlockType.None && 
               Obstacle == ObstacleType.None && 
               Booster == BoosterType.None;
    }

    public bool CanBlockFallInto()
    {
        return IsEmpty();
    }
}