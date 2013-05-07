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
using System.Threading;
using System.Windows.Media.Imaging;
using System.Linq;

namespace FinalProject_KinectCoach
{
    class FencingAction
    {
        string filepath;
        public List<Skeleton> frames;
        public Pose startPose;
        public Pose endPose;

        static readonly double defaultError = 0.1;

        public double torsoError = defaultError;
        public double leftArmError = defaultError;
        public double rightArmError = defaultError;
        public double leftLegError = defaultError;
        public double rightLegError = defaultError;

        public static FencingAction getAction(string filepath, Pose startPose, Pose endPose)
        {
            FencingAction fa = new FencingAction();
            fa.filepath = filepath;
            fa.startPose = startPose;
            fa.endPose = endPose;
            fa.frames = KinectFileUtils.ReadRecordingFile(filepath);
            return fa;
        }

        public FencingAction setErrors(double te, double lae, double rae, double lle, double rle)
        {
            this.torsoError = te;
            this.leftArmError = lae;
            this.rightArmError = rae;
            this.leftLegError = lle;
            this.rightLegError = rle;

            return this;
        }
    }
}
