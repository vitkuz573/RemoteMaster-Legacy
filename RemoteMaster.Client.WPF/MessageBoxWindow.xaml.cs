using RemoteMaster.Shared.Dtos;
using System.Windows;
using Windows.Win32.UI.WindowsAndMessaging;

namespace RemoteMaster.Client.WPF;

public partial class MessageBoxWindow : Window
{
    public MessageBoxWindow()
    {
        InitializeComponent();
    }

    private async void OnSendClick(object sender, RoutedEventArgs e)
    {
        var dto = new MessageBoxDto
        {
            Caption = captionTextBox.Text,
            Text = textTextBox.Text,
            Style = MESSAGEBOX_STYLE.MB_OK
        };

        await MainWindow.TryInvokeServerAsync("SendMessageBox", dto);

        this.Close();
    }
}
