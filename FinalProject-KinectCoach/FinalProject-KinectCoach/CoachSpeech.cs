using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.Synthesis;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Linq;

namespace FinalProject_KinectCoach
{
    class CoachSpeech
    {
        private SpeechSynthesizer synth;
        public bool isSpeaking;

        public CoachSpeech()
        {
            synth = new SpeechSynthesizer();
            synth.SetOutputToDefaultAudioDevice();

            synth.SpeakStarted += synth_SpeakStarted;
            synth.SpeakCompleted += synth_SpeakCompleted;
        }

        void synth_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            isSpeaking = true;
        }

        void synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            isSpeaking = false;
        }

        public void speak(string phrase)
        {
            synth.SpeakAsyncCancelAll();
            synth.SpeakAsync(phrase);
        }

        public void sayCorrect()
        {
            Random r = new Random();
            switch (r.Next(0, 4))
            {
                case 0:
                    speak("That's right.");
                    break;
                case 1:
                    speak("Good.");
                    break;
                default:
                    speak("Correct.");
                    break;
            }
        }
    }
}
