using System.Windows;

namespace RemoteMaster.Client.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var viewer = new ViewerWindow("172.20.20.18");
        viewer.Show();
    }
}
