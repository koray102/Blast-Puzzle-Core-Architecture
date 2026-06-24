public enum BlockType
{
    None = 0, // Boş hücre
    Red = 1,
    Blue = 2,
    Green = 3,
    Yellow = 4,
    Purple = 5
}

public enum ObstacleType
{
    None = 0,
    Box = 1, // Kırılabilir kutu (Sabit durur)
    Bubble = 2 // Bloğun üstünü kaplayan baloncuk (Blokla beraber düşebilir)
}

public enum BoosterType
{
    None = 0,
    RocketVertical = 1,
    RocketHorizontal = 2,
    Bomb = 3,
    DiscoBall = 4
}

public enum GameState 
{ 
    WaitingForInput, 
    Processing, 
    Paused, 
    GameOver 
}

public enum VFXType
{
    BlockDestroy,
    BlockDestroyV2,
    RocketExplosion,
    BombExplosion,
    DiscoBallExplosion
}