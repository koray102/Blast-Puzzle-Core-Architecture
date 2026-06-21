public struct BlockMoveData
{
    public int FromX;
    public int FromY;
    public int ToX;
    public int ToY;

    public BlockMoveData(int fromX, int fromY, int toX, int toY)
    {
        FromX = fromX;
        FromY = fromY;
        ToX = toX;
        ToY = toY;
    }
}