using System;
using System.IO;
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
using Microsoft.Win32;
using Microsoft.Kinect;
using AForge.Math;

namespace FinalProject_KinectCoach
{
    class KinectFrameUtils
    {

        public static List<Skeleton> alignFrames(List<Skeleton> skels, int nFrames)
        {
            int diff = skels.Count - nFrames;
            if (diff == 0)
            {
                return skels;
            }
            else if (diff < 0)
            {
                return interpolateFrames(skels, nFrames);
            }
            else
            {
                return removeFrames(skels, nFrames);
            }
        }

        private static List<Skeleton> interpolateFrames(List<Skeleton> skels, int nFrames)
        {
            int diff = nFrames - skels.Count;
            List<Skeleton> result = skels;

            List<double> dists = new List<double>();
            for (int i = 0; i < skels.Count - 1; i++)
            {
                dists.Add(getDistBetweenFrames(skels.ElementAt(i), skels.ElementAt(i + 1)));
            }

            for (int i = 0; i < diff; i++)
            {
                if (dists.Count == 0)
                {
                    break;
                }
                double max = dists.Min();
                int index = dists.IndexOf(max);

                Skeleton insert = getMidFrame(skels.ElementAt(index), skels.ElementAt(index + 1));
                result.Insert(index, insert);
                dists.RemoveAt(index);
            }

            return alignFrames(result, nFrames);
        }

        private static List<Skeleton> removeFrames(List<Skeleton> skels, int nFrames)
        {
            int diff = skels.Count - nFrames;
            List<Skeleton> result = new List<Skeleton>();

            List<double> dists = new List<double>();
            for (int i = 0; i < skels.Count - 1; i++)
            {
                dists.Add(getDistBetweenFrames(skels.ElementAt(i), skels.ElementAt(i + 1)));
            }

            List<int> ignore = new List<int>();
            int j = 0;
            while (j < diff)
            {
                double min = dists.Min();
                int index = dists.IndexOf(min);
                if (index < skels.Count - 3)
                {
                    ignore.Add(index);
                    j++;
                }
                dists.RemoveAt(index);
            }

            for (int i = 0; i < skels.Count; i++)
            {
                if (ignore.Contains(i))
                {
                    continue;
                }

                result.Add(skels.ElementAt(i));
            }

            return alignFrames(result, nFrames);
        }

        private static Skeleton getMidFrame(Skeleton frame1, Skeleton frame2)
        {
            Skeleton mid = new Skeleton();

            foreach (Joint j1 in frame1.Joints)
            {
                Joint j2 = frame2.Joints[j1.JointType];

                Joint j = mid.Joints[j1.JointType];
                j.TrackingState = j1.TrackingState;

                SkeletonPoint p = new SkeletonPoint();
                p.X = (j1.Position.X + j2.Position.X) / 2.0f;
                p.Y = (j1.Position.Y + j2.Position.Y) / 2.0f;
                p.Z = (j1.Position.Z + j2.Position.Z) / 2.0f;

                j.Position = p;
                mid.Joints[j.JointType] = j;
            }

            return mid;
        }

        public static double getDistBetweenFrames(Skeleton frame1, Skeleton frame2)
        {
            double dist = 0;

            foreach (Joint j in frame1.Joints)
            {
                Joint jc = frame2.Joints[j.JointType];
                SkeletonPoint p = j.Position;
                SkeletonPoint pc = jc.Position;

                dist += Math.Sqrt(Math.Pow(p.X - pc.X, 2) + Math.Pow(p.Y - pc.Y, 2) + Math.Pow(p.Z - pc.Z, 2));
            }
            return dist;
        }

        public static double totalDistTraveled(List<Skeleton> frames)
        {
            double result = 0;
            for (int i = 0; i < frames.Count - 1; i++)
            {
                result += getDistBetweenFrames(frames.ElementAt(i), frames.ElementAt(i + 1));
            }
            return result;
        }

        public static Matrix3x3 getRotationMatrix(Skeleton mFrame, Skeleton rFrame)
        {
            Skeleton mFrameShift = shiftFrame(mFrame, mFrame.Joints[JointType.HipCenter].Position);
            Skeleton rFrameShift = shiftFrame(rFrame, rFrame.Joints[JointType.HipCenter].Position);

            Matrix3x3 rotMatrix = new Matrix3x3();
            double bestDist = -1;
            float bestR = 0;
            for (float r = (float)-Math.PI / 2; r < Math.PI / 2; r = r + (float)(Math.PI / 100))
            {
                rotMatrix = Matrix3x3.CreateRotationX(r);
                double dist = getDistBetweenFrames(mFrameShift, applyTransformToFrame(rFrameShift, rotMatrix));
                if (dist < bestDist || bestDist < 0)
                {
                    bestR = r;
                    bestDist = dist;
                }
            }
            float alpha = bestR;
            rFrameShift = applyTransformToFrame(rFrameShift, Matrix3x3.CreateRotationX(bestR));

            bestDist = -1;
            bestR = 0;
            for (float r = (float)-Math.PI / 2; r < Math.PI / 2; r = r + (float)(Math.PI / 100))
            {
                rotMatrix = Matrix3x3.CreateRotationY(r);
                double dist = getDistBetweenFrames(mFrameShift, applyTransformToFrame(rFrameShift, rotMatrix));
                if (dist < bestDist || bestDist < 0)
                {
                    bestR = r;
                    bestDist = dist;
                }
            }
            float beta = bestR;
            rFrameShift = applyTransformToFrame(rFrameShift, Matrix3x3.CreateRotationY(bestR));

            bestDist = -1;
            bestR = 0;
            for (float r = (float)-Math.PI / 2; r < Math.PI / 2; r = r + (float)(Math.PI / 100))
            {
                rotMatrix = Matrix3x3.CreateRotationZ(r);
                double dist = getDistBetweenFrames(mFrameShift, applyTransformToFrame(rFrameShift, rotMatrix));
                if (dist < bestDist || bestDist < 0)
                {
                    bestR = r;
                    bestDist = dist;
                }
            }
            float gamma = bestR;

            return Matrix3x3.CreateFromYawPitchRoll(beta, alpha, gamma);
        }

        public static Skeleton transRotateTrans(Skeleton frame, Matrix3x3 rot)
        {
            Skeleton frameShift = shiftFrame(frame, frame.Joints[JointType.HipCenter].Position);
            Skeleton frameRot = applyTransformToFrame(frameShift, rot);
            return shiftFrame(frameRot, negPos(frame.Joints[JointType.HipCenter].Position));
        }

        public static Skeleton applyTransformToFrame(Skeleton frame, Matrix3x3 trans)
        {
            Skeleton res = new Skeleton();
            foreach (Joint j in frame.Joints)
            {
                Joint j2 = j;
                j2.Position = multPosMat(trans, j2.Position);
                res.Joints[j2.JointType] = j2;
            }
            return res;
        }

        public static Skeleton shiftFrame(Skeleton frame, SkeletonPoint p)
        {
            Skeleton res = new Skeleton();
            foreach (Joint j in frame.Joints)
            {
                Joint j2 = j;
                j2.Position = subtractPos(j.Position, p);
                res.Joints[j2.JointType] = j2;
            }
            return res;
        }

        public static double distancePos(SkeletonPoint p, SkeletonPoint pc)
        {
            return Math.Sqrt(Math.Pow(p.X - pc.X, 2) + Math.Pow(p.Y - pc.Y, 2) + Math.Pow(p.Z - pc.Z, 2));
        }

        public static SkeletonPoint subtractPos(SkeletonPoint p1, SkeletonPoint p2)
        {
            SkeletonPoint pos = new SkeletonPoint();
            pos.X = p1.X - p2.X;
            pos.Y = p1.Y - p2.Y;
            pos.Z = p1.Z - p2.Z;
            return pos;
        }

        public static SkeletonPoint multPosMat(Matrix3x3 mat, SkeletonPoint p)
        {
            SkeletonPoint pos = new SkeletonPoint();
            pos.X = p.X * mat.V00 + p.Y * mat.V01 + p.Z * mat.V02;
            pos.Y = p.X * mat.V10 + p.Y * mat.V11 + p.Z * mat.V12;
            pos.Z = p.X * mat.V20 + p.Y * mat.V21 + p.Z * mat.V22;
            return pos;
        }

        public static SkeletonPoint negPos(SkeletonPoint p)
        {
            SkeletonPoint r = new SkeletonPoint();
            r.X = -p.X;
            r.Y = -p.Y;
            r.Z = -p.Z;
            return r;
        }
    }
}
