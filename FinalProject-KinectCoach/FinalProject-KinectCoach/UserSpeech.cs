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
    /// <summary>
    /// Object to contain automated user speech for session viewer
    /// Yes, the user is Australian. Deal with it.
    /// </summary>
    class UserSpeech
    {
        private SpeechSynthesizer synth;
        public bool isSpeaking { get; set; }

        public UserSpeech()
        {
            synth = new SpeechSynthesizer();
            synth.SetOutputToDefaultAudioDevice();

            synth.SpeakStarted += synth_SpeakStarted;
            synth.SpeakCompleted += synth_SpeakCompleted;
            synth.SelectVoice("Microsoft Server Speech Text to Speech Voice (en-AU, Hayley)");
        }

        private void synth_SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            isSpeaking = true;
        }

        private void synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            isSpeaking = false;
        }

        public void speak(string phrase)
        {
            synth.SpeakAsyncCancelAll();
            synth.SpeakAsync(phrase);
        }
    }
}