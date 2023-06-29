namespace RemoteMaster.Shared.Dto;

public class MouseButtonClickDto
{
    public MouseButtonClickDto(string button, bool click = true)
    {
        Button = button;
        Click = click;
    }

    public string Button { get; set; }

    public bool Click { get; set; }
}
