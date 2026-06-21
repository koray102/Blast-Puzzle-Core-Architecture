public class Node
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public BlockType Type { get; set; }
    public bool IsMatched { get; set; } = false;

    public Node(int x, int y, BlockType type = BlockType.None)
    {
        X = x;
        Y = y;
        Type = type;
    }

    // İleride buraya "Bu node engellenmiş mi (kutu mu var)?", "Boş mu?" gibi mantıklar ekleyeceğiz.
    public bool IsEmpty() => Type == BlockType.None;
}