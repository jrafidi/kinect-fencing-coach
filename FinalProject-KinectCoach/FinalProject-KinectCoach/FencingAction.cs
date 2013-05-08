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

        public List<Skeleton> getShiftedFrames(SkeletonPoint position)
        {
            SkeletonPoint shift = new SkeletonPoint();
            shift.X = frames[0].Joints[JointType.HipCenter].Position.X - position.X;
            shift.Y = frames[0].Joints[JointType.HipCenter].Position.Y - position.Y;
            shift.Z = frames[0].Joints[JointType.HipCenter].Position.Z - position.Z;

            List<Skeleton> result = frames;
            for (int i = 0; i < result.Count; i++)
            {
                foreach (Joint j in result.ElementAt(i).Joints)
                {
                    SkeletonPoint p = new SkeletonPoint();
                    p.X = j.Position.X - shift.X;
                    p.Y = j.Position.Y - shift.Y;
                    p.Z = j.Position.Z - shift.Z;
                    Joint j2 = j;
                    j2.Position = p;
                    result.ElementAt(i).Joints[j.JointType] = j2;
                }
            }
            return result;
        }

        public List<Skeleton> correctedFrames(List<Skeleton> rawFrames)
        {
            rawFrames = KinectFileUtils.alignFrames(rawFrames, frames.Count);

            int frameStart = -1;
            int rawStart = -1;

            for (int i = 0; i < frames.Count; i++)
            {
                if (KinectFileUtils.totalDistTraveled(frames.GetRange(0, i)) > 2 && frameStart < 0)
                {
                    frameStart = i;
                }

                if (KinectFileUtils.totalDistTraveled(rawFrames.GetRange(0, i)) > 2 && rawStart < 0)
                {
                    rawStart = i;
                }
            }

            int diff = frameStart - rawStart;
            if (diff > 0)
            {
                for (int i = 0; i < diff; i++)
                {
                    rawFrames.Insert(0, rawFrames[0]);
                }
            }
            else
            {
                for (int i = 0; i < -diff; i++)
                {
                    rawFrames.RemoveAt(0);
                    rawFrames.Add(rawFrames[rawFrames.Count - 1]);
                }
            }
            return rawFrames;
        }
    }
}
