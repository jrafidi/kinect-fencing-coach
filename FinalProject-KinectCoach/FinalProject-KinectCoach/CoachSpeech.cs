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
    /// Object to contain all automated coach speaking logic
    /// </summary>
    class CoachSpeech
    {
        private readonly int frameErrorMinimum = 6;
        private readonly int numberErrorsMentioned = 3;

        private SpeechSynthesizer synth;

        public bool isSpeaking { get; set; }

        public CoachSpeech()
        {
            synth = new SpeechSynthesizer();
            synth.SetOutputToDefaultAudioDevice();

            synth.SpeakStarted += SpeakStarted;
            synth.SpeakCompleted += SpeakCompleted;
            synth.SelectVoice("Microsoft Server Speech Text to Speech Voice (en-US, Helen)");
        }

        private void SpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            isSpeaking = true;
        }

        private void SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            isSpeaking = false;
        }

        public void Speak(string phrase)
        {
            synth.SpeakAsyncCancelAll();
            synth.SpeakAsync(phrase);
        }

        public void SayCorrect()
        {
            Random r = new Random();
            switch (r.Next(0, 4))
            {
                case 0:
                    Speak("That's right.");
                    break;
                case 1:
                    Speak("Good.");
                    break;
                case 2:
                    Speak("Well done.");
                    break;
                case 3:
                    Speak("Exactly.");
                    break;
                default:
                    Speak("Correct.");
                    break;
            }
        }

        public void SayPoseErrors(List<Pose.JointError> errorList)
        {
            string text = "";

            errorList.Sort((s1, s2) => s1.mag.CompareTo(s2.mag));
            errorList.Reverse();

            for (int i = 0; i < Math.Min(numberErrorsMentioned, errorList.Count); i++)
            {
                Pose.JointError err = errorList[i];
                text += "Your " + getStringFromJointType(err.joint) + " is " + getStringFromErrorType(err.error) + ".  ";
            }

            Speak(text);
        }

        public void SayActionErrors(List<Pose.JointError> errorList, int nFrames)
        {
            errorList.Sort((s1, s2) => s1.mag.CompareTo(s2.mag));
            errorList.Reverse();

            Dictionary<string, int> errorDict = new Dictionary<string, int>();

            for (int i = 0; i < errorList.Count; i++)
            {
                string text = "";
                Pose.JointError err = errorList[i];
                text += "Your " + getStringFromJointType(err.joint) + " was " + getStringFromErrorType(err.error);
                if (err.frame < nFrames / 3)
                {
                    text += " at the beginning.  ";
                }
                else if (err.frame < 2 * nFrames / 3)
                {
                    text += " in the middle.  ";
                }
                else
                {
                    text += " near the end.  ";
                }

                if (errorDict.ContainsKey(text))
                {
                    errorDict[text] += 1;
                }
                else
                {
                    errorDict[text] = 1;
                }
            }

            List<string> comments = errorDict.Keys.ToList();
            comments.Sort((k1, k2) => errorDict[k1].CompareTo(errorDict[k2]));
            comments.Reverse();

            string speech = "";
            for (int i = 0; i < Math.Min(numberErrorsMentioned, comments.Count); i++)
            {
                if (errorDict[comments[i]] < frameErrorMinimum)
                {
                    break;
                }
                speech += comments[i];
            }

            if (speech.Equals(""))
            {
                speech = "No significant errors found.";
            }

            Speak(speech);
        }

        private string getStringFromErrorType(Pose.ErrorType et)
        {
            switch (et)
            {
                case Pose.ErrorType.INSIDE: return "too far inside";
                case Pose.ErrorType.OUTSIDE: return "too far outside";
                case Pose.ErrorType.HIGH: return "too high";
                case Pose.ErrorType.LOW: return "too low";
                case Pose.ErrorType.FORWARD: return "too far forward";
                default: return "too far back";
            }
        }

        private string getStringFromJointType(JointType jt)
        {
            switch (jt)
            {
                case JointType.HipCenter: return "Hip Center";
                case JointType.Spine: return "Spine";
                case JointType.ShoulderCenter: return "Shoulder Center";
                case JointType.Head: return "Head";
                case JointType.ShoulderLeft: return "Left Shoulder";
                case JointType.ElbowLeft: return "Left Elbow";
                case JointType.WristLeft: return "Left Wrist";
                case JointType.HandLeft: return "Left Hand";
                case JointType.ShoulderRight: return "Right Shoulder";
                case JointType.ElbowRight: return "Right Elbow";
                case JointType.WristRight: return "Right Wrist";
                case JointType.HandRight: return "Right Hand";
                case JointType.HipLeft: return "Left Hip";
                case JointType.KneeLeft: return "Left Knee";
                case JointType.AnkleLeft: return "Left Ankle";
                case JointType.FootLeft: return "Left Foot";
                case JointType.HipRight: return "Right Hip";
                case JointType.KneeRight: return "Right Knee";
                case JointType.AnkleRight: return "Right Ankle";
                default: return "Right Foot";
            }
        }
    }
}
