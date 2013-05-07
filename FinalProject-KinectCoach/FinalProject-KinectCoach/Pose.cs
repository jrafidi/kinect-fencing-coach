﻿using System;
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
    class Pose
    {
        static readonly double defaultError = 0.1;

        string filepath;
        public Skeleton frame;

        public double torsoError = defaultError;
        public double leftArmError = defaultError;
        public double rightArmError = defaultError;
        public double leftLegError = defaultError;
        public double rightLegError = defaultError;

        public static Pose getPose(string filepath)
        {
            Pose p = new Pose();
            p.filepath = filepath;
            p.frame = KinectFileUtils.ReadRecordingFile(filepath).ElementAt(0);
            return p;
        }

        public Pose setErrors(double te, double lae, double rae, double lle, double rle)
        {
            this.torsoError = te;
            this.leftArmError = lae;
            this.rightArmError = rae;
            this.leftLegError = lle;
            this.rightLegError = rle;

            return this;
        }

        public Dictionary<JointType, double> getErrorMap(Skeleton skeleton)
        {
            Dictionary<JointType, double> errorMap = new Dictionary<JointType, double>();

            SkeletonPoint shift = new SkeletonPoint();
            shift.X = this.frame.Joints[JointType.HipCenter].Position.X - skeleton.Joints[JointType.HipCenter].Position.X;
            shift.Y = this.frame.Joints[JointType.HipCenter].Position.Y - skeleton.Joints[JointType.HipCenter].Position.Y;
            shift.Z = this.frame.Joints[JointType.HipCenter].Position.Z - skeleton.Joints[JointType.HipCenter].Position.Z;

            foreach (Joint j in skeleton.Joints)
            {
                Joint jc = this.frame.Joints[j.JointType];
                SkeletonPoint p = j.Position;
                SkeletonPoint pc = jc.Position;

                double error = Math.Sqrt(Math.Pow(p.X + shift.X - pc.X, 2) + Math.Pow(p.Y + shift.Y - pc.Y, 2) + Math.Pow(p.Z + shift.Z - pc.Z, 2));
                errorMap.Add(j.JointType, error);
            }

            return errorMap;
        }

        public bool matchesSkeleton(Skeleton skeleton, int allowedIncorrect)
        {
            Dictionary<JointType, double> errorMap = this.getErrorMap(skeleton);

            int incorrect = 0;

            // Torso
            incorrect += errorMap[JointType.Head] > torsoError ? 1 : 0;
            incorrect += errorMap[JointType.ShoulderCenter] > torsoError ? 1 : 0;
            incorrect += errorMap[JointType.Spine] > torsoError ? 1 : 0;
            incorrect += errorMap[JointType.HipCenter] > torsoError ? 1 : 0;

            // Left Arm
            incorrect += errorMap[JointType.ShoulderLeft] > leftArmError ? 1 : 0;
            incorrect += errorMap[JointType.ElbowLeft] > leftArmError ? 1 : 0;
            incorrect += errorMap[JointType.WristLeft]> leftArmError ? 1 : 0;
            incorrect += errorMap[JointType.HandLeft] > leftArmError ? 1 : 0;

            // Right Arm
            incorrect += errorMap[JointType.ShoulderRight] > rightArmError ? 1 : 0;
            incorrect += errorMap[JointType.ElbowRight] > rightArmError ? 1 : 0;
            incorrect += errorMap[JointType.WristRight] > rightArmError ? 1 : 0;
            incorrect += errorMap[JointType.HandRight] > rightArmError ? 1 : 0;

            // Left Leg
            incorrect += errorMap[JointType.HipLeft] > leftLegError ? 1 : 0;
            incorrect += errorMap[JointType.KneeLeft] > leftLegError ? 1 : 0;
            incorrect += errorMap[JointType.AnkleLeft] > leftLegError ? 1 : 0;
            incorrect += errorMap[JointType.FootLeft] > leftLegError ? 1 : 0;

            // Right Leg
            incorrect += errorMap[JointType.HipRight] > rightLegError ? 1 : 0;
            incorrect += errorMap[JointType.KneeRight] > rightLegError ? 1 : 0;
            incorrect += errorMap[JointType.AnkleRight] > rightLegError ? 1 : 0;
            incorrect += errorMap[JointType.FootRight] > rightLegError ? 1 : 0;

            return incorrect <= allowedIncorrect;
        }
    }
}