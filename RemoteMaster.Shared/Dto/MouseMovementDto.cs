namespace RemoteMaster.Shared.Dto;

public class MouseMovementDto
{
    public MouseMovementDto(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; set; }

    public int Y { get; set; }
}
