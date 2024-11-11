namespace DeepDungeonRadar.Enums;

public enum Direction
{
    None,
    正北,
    东北,
    正东,
    东南,
    正南,
    西南,
    正西,
    西北
}

public static class DirectionExtension
{
    public static Direction Reverse(this Direction direction)
    {
        return direction switch
        {
            Direction.正北 => Direction.正南,
            Direction.东北 => Direction.西南,
            Direction.正东 => Direction.正西,
            Direction.东南 => Direction.西北,
            Direction.正南 => Direction.正北,
            Direction.西南 => Direction.东北,
            Direction.正西 => Direction.正东,
            Direction.西北 => Direction.东南,
            _ => Direction.None
        };
    }
}