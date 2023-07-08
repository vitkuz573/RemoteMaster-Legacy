namespace RemoteMaster.Shared.Dto;

public class ScreenUpdateDto
{
    public byte[] Data { get; set; }

    public bool IsEndOfImage { get; set; }
}