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
        public static string ACTION_DIRECTORY = "C:\\Users\\Joey Rafidi\\Documents\\GitHub\\kinect-fencing-coach\\FinalProject-KinectCoach\\FinalProject-KinectCoach\\Recordings";

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
            Dictionary<string, string> headers = KinectFileUtils.getRecordingFileHeaders(filepath);

            startPose = new Pose(headers["STARTPOSE"]);
            endPose = new Pose(headers["ENDPOSE"]);
            endPose.scaleErrors(float.Parse(headers["ENDSCALAR"]));

            string[] td = headers["TRAININGDATA"].Split(',');
            foreach (string s in td)
            {
                trainingDataFrames.Add(KinectFileUtils.ReadSkeletonFromRecordingFile(ACTION_DIRECTORY + "\\" + s));
            }

            bestFrames = trainingDataFrames[0];
            dist = KinectFrameUtils.totalDistTraveled(bestFrames);
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
                List<Skeleton> corrected = KinectFrameUtils.startCorrectedFrames(test, train);
                double dist = KinectFrameUtils.getTotalDistBetween(corrected, train) / (train.Count);
                Console.WriteLine(dist);
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

        public List<Skeleton> GetShiftedFrames(SkeletonPoint position)
        {
            SkeletonPoint shift = new SkeletonPoint();
            shift.X = bestFrames[0].Joints[JointType.HipCenter].Position.X - position.X;
            shift.Y = bestFrames[0].Joints[JointType.HipCenter].Position.Y - position.Y;
            shift.Z = bestFrames[0].Joints[JointType.HipCenter].Position.Z - position.Z;

            List<Skeleton> result = bestFrames;
            for (int i = 0; i < result.Count; i++)
            {
                result[i] = KinectFrameUtils.shiftFrame(result[i], shift);
            }
            return result;
        }

        public void ApplyRotation(Matrix3x3 trans)
        {
            foreach (List<Skeleton> tFrames in trainingDataFrames)
            {
                for (int i = 0; i < tFrames.Count; i++)
                {
                    tFrames[i] = KinectFrameUtils.transRotateTrans(tFrames[i], trans);
                }
            }

            startPose.applyTransform(trans);
            endPose.applyTransform(trans);
        }

        public Pose GetPoseOfFrame(int index)
        {
            return new Pose(bestFrames[index]);
        }
    }
}
