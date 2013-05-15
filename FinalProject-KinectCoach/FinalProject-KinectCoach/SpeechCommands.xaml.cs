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
    public partial class SpeechCommands : Window
    {
        private string text = "";

        public SpeechCommands()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            text += "Speech Control\n";
            text += "  'Start listening to me': starts recognition of all other speech commands\n";
            text += "  'Stop listening to me': stops recognition of all speech commands except command to start listening\n";

            text += "\nSimple Commands:\n";
            text += "  'Start Recording': starts recording skeletons to dated file\n";
            text += "  'Stop Recording': stops recording skeletons to file\n";
            text += "  'Calibrate': saves rotational differences. Must be done prior to working with default training data.\n";
            text += "  'Redo That/Once More': redo last command that was not a simple or speech control command.\n";
            text += "  'Clear All': ends existing practice (pose or action)\n";

            text += "\nPose Commands:\n";
            text += "  'Check my [Pose Name]': starts checking for pose [Pose Name]\n";
            text += "  'Demonstrate for me': shows sample of chosen pose\n";
            text += "  'Hide demo/Got it': hides sample of chosen pose\n";
            text += "  'What's wrong': coach will tell you which joints aren't matching the pose in which directions\n";

            text += "\nAction Commands:\n";
            text += "  'Watch my [Action Name]': starts watching for action [Action Name]\n";
            text += "  'Only show me': hides action sample and only shows recorded result\n";
            text += "  'Show on top of action': shows action sample with recorded result\n";
            text += "  'What's wrong': coach will tell you which joints were wrong in which directions at what times\n";

            HelpText.Text = text;
        }
    }
}
