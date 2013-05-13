using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FinalProject_KinectCoach
{
    /// <summary>
    /// Interaction logic for SpeechCommands.xaml
    /// </summary>
    public partial class HotkeyHelp : Window
    {
        private string text = "";

        public HotkeyHelp()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            text += "Main Window:\n";
            text += "  Spacebar: pause/play viewport\n";

            text += "\nRecordings Window:\n";
            text += "  Down Arrow: pause/play recording viewport\n";
            text += "  Left Arrow: play recording backward\n";
            text += "  Right Arrow: play recording forward\n";

            HelpText.Text = text;
        }
    }
}
