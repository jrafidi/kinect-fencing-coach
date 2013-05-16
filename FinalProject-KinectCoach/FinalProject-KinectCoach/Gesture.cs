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
    class Gesture
    {
        public static string ACTION_DIRECTORY = "..\\..\\Recordings\\Actions";

        private string filepath;
        private List<List<Skeleton>> trainingDataFrames = new List<List<Skeleton>>();

        public string name { get; set; }
        public List<Skeleton> bestFrames { get; set; }
        public Pose startPose { get; set; }
        public Pose endPose { get; set; }
        public double dist { get; set; }

        static readonly double defaultError = 0.2;

        public double torsoError = defaultError;
        public double leftArmError = defaultError;
        public double rightArmError = defaultError;
        public double leftLegError = defaultError;
        public double rightLegError = defaultError;

        public Gesture(string name)
        {
            this.name = name;
            filepath = Gesture.ACTION_DIRECTORY + "\\" + name + ".action";
            Dictionary<string, string> headers = KinectFileUtils.GetRecordingFileHeaders(filepath);

            startPose = new Pose(headers["STARTPOSE"]);
            endPose = new Pose(headers["ENDPOSE"]);
            endPose.ScaleErrors(float.Parse(headers["ENDSCALAR"]));

            string[] td = headers["TRAININGDATA"].Split(',');
            foreach (string s in td)
            {
                trainingDataFrames.Add(KinectFileUtils.ReadSkeletonFromRecordingFile(ACTION_DIRECTORY + "\\" + s));
            }

            string[] errors = headers["ERRORS"].Split(',');
            torsoError = double.Parse(errors[0]);
            leftArmError = double.Parse(errors[1]);
            rightArmError = double.Parse(errors[2]);
            leftLegError = double.Parse(errors[3]);
            rightLegError = double.Parse(errors[4]);

            foreach (List<Skeleton> train in trainingDataFrames)
            {
                for (int i = 0; i < 3; i++)
                {
                    train.Add(train[train.Count - 1]);
                }
            }

            bestFrames = trainingDataFrames[0];
            dist = KinectFrameUtils.GetTotalDistTraveled(bestFrames);
        }

        /// <summary>
        /// Set bestFrames to the closest match to our test data.
        /// Closest match has smallest distance separation over all frames, 
        /// normalized for that training data frame count.
        /// </summary>
        /// <param name="test"></param>
        public void DetermineBestFrames(List<Skeleton> test)
        {
            double best = -1;
            foreach (List<Skeleton> train in trainingDataFrames)
            {
                List<Skeleton> corrected = KinectFrameUtils.GetStartCorrectedFrames(test, train);
                double dist = KinectFrameUtils.GetTotalDistBetween(corrected, train) / (train.Count);

                if (dist < best | best < 0)
                {
                    best = dist;
                    bestFrames = train;
                }
            }
        }

        public void SetErrors(double te, double lae, double rae, double lle, double rle)
        {
            this.torsoError = te;
            this.leftArmError = lae;
            this.rightArmError = rae;
            this.leftLegError = lle;
            this.rightLegError = rle;
        }

        public void ShiftBestFrames(SkeletonPoint position)
        {
            SkeletonPoint shift = KinectFrameUtils.SubtractPosition(bestFrames[0].Joints[JointType.HipCenter].Position, position);

            for (int i = 0; i < bestFrames.Count; i++)
            {
                bestFrames[i] = KinectFrameUtils.ShiftFrame(bestFrames[i], shift);
            }
        }

        public void ApplyRotation(Matrix3x3 trans)
        {
            for (int j = 0; j < trainingDataFrames.Count; j++)
            {
                List<Skeleton> tFrames = trainingDataFrames[j];
                List<Skeleton> newFrames = new List<Skeleton>();
                for (int i = 0; i < tFrames.Count; i++)
                {
                    newFrames.Add(KinectFrameUtils.TransRotateTrans(tFrames[i], trans));
                }
                trainingDataFrames[j] = newFrames;
            }
            bestFrames = trainingDataFrames[0];
            startPose.ApplyTransform(trans);
            endPose.ApplyTransform(trans);
        }

        public List<Pose.JointError> GetSignificantErrors(List<Skeleton> testFrames, Matrix3x3 trans)
        {
            List<Pose.JointError> res = new List<Pose.JointError>();

            for (int i = 0; i < bestFrames.Count; i++)
            {
                List<Pose.JointError> iter = GetPoseOfFrame(i).GetSignificantErrors(testFrames[i], trans);
                for (int j = 0; j < iter.Count; j++ )
                {
                    Pose.JointError je = iter[j];
                    je.frame = i;
                    iter[j] = je;
                }
                res.AddRange(iter);
            }

            return res;
        }

        public Pose GetPoseOfFrame(int index)
        {
            Pose p = new Pose(bestFrames[index]);
            p.SetErrors(torsoError, leftArmError, rightArmError, leftLegError, rightLegError);
            return p;
        }
    }
}
