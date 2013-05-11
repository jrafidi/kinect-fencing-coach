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
using AForge.Math;

namespace FinalProject_KinectCoach
{
    class FencingAction
    {
        public static string ACTION_DIRECTORY = "C:\\Users\\Joey Rafidi\\Documents\\GitHub\\kinect-fencing-coach\\FinalProject-KinectCoach\\FinalProject-KinectCoach\\Recordings";
        string filepath;
        public List<List<Skeleton>> trainingDataFrames = new List<List<Skeleton>>();
        public List<Skeleton> frames;
        public Pose startPose;
        public Pose endPose;
        public double dist;

        static readonly double defaultError = 0.2;

        public double torsoError = defaultError;
        public double leftArmError = defaultError;
        public double rightArmError = defaultError;
        public double leftLegError = defaultError;
        public double rightLegError = defaultError;

        public FencingAction(string headerFilePath)
        {
            filepath = headerFilePath;
            Dictionary<string, string> headers = KinectFileUtils.getRecordingFileHeaders(headerFilePath);

            startPose = new Pose(headers["STARTPOSE"]);
            endPose = new Pose(headers["ENDPOSE"]);
            endPose.scaleErrors(float.Parse(headers["ENDSCALAR"]));

            string[] td = headers["TRAININGDATA"].Split(',');
            foreach (string s in td)
            {
                trainingDataFrames.Add(KinectFileUtils.ReadSkeletonFromRecordingFile(ACTION_DIRECTORY + "\\" + s));
            }

            // TEMP
            frames = trainingDataFrames[0];

            dist = KinectFrameUtils.totalDistTraveled(frames);
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
            rawFrames = KinectFrameUtils.alignFrames(rawFrames, frames.Count);

            int frameStart = -1;
            int rawStart = -1;

            for (int i = 0; i < frames.Count; i++)
            {
                if (KinectFrameUtils.totalDistTraveled(frames.GetRange(0, i)) > (dist / 5) && frameStart < 0)
                {
                    frameStart = i;
                }

                if (KinectFrameUtils.totalDistTraveled(rawFrames.GetRange(0, i)) > (dist / 5) && rawStart < 0)
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

        public void applyRotation(Matrix3x3 trans)
        {
            foreach (List<Skeleton> tFrames in trainingDataFrames)
            {
                for (int i = 0; i < tFrames.Count; i++)
                {
                    frames[i] = KinectFrameUtils.transRotateTrans(frames[i], trans);
                }
            }

            frames = trainingDataFrames[0];

            startPose.applyTransform(trans);
            endPose.applyTransform(trans);
        }

        public Pose getPoseOfFrame(int index)
        {
            return new Pose(frames[index]);
        }
    }
}
