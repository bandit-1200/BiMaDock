using System.Reflection;
using System.Windows;

namespace BiMaDock
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            ShowVersion();
        }

        private void ShowVersion()
        {
            var informationalVersionAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            string informationalVersion = informationalVersionAttribute?.InformationalVersion ?? "Unbekannte Version";
            string clearVersion = informationalVersion.Split('+')[0];

            VersionTextBox.Text = $"Version: {clearVersion}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
