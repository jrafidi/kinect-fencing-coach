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

namespace FinalProject_KinectCoach
{
    class KinectFileUtils
    {

        public static List<Skeleton> ReadRecordingFile(string filename)
        {
            List<Skeleton> frames = new List<Skeleton>();
            string file = File.ReadAllText(filename);

            string line;
            Skeleton skel = new Skeleton();

            StreamReader reader = new StreamReader(filename);
            line = reader.ReadLine();
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("FRAME"))
                {
                    frames.Add(skel);
                    skel = new Skeleton();
                    continue;
                }
                string[] jointParts = line.Split(',');
                JointType jt = stringToJointType(jointParts[0].Trim());

                Joint j = skel.Joints[jt];
                j.TrackingState = stringToTrackingState(jointParts[1].Trim());

                SkeletonPoint p = new SkeletonPoint();
                p.X = float.Parse(jointParts[2].Trim());
                p.Y = float.Parse(jointParts[3].Trim());
                p.Z = float.Parse(jointParts[4].Trim());

                j.Position = p;
                skel.Joints[j.JointType] = j;
            }

            frames.Add(skel);

            return frames;
        }

        /// <summary>
        /// Get the tracking state corresponding to the string passed
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private static JointTrackingState stringToTrackingState(string n)
        {
            if (n.Equals("Tracked"))
            {
                return JointTrackingState.Tracked;
            }
            else if (n.Equals("Inferred"))
            {
                return JointTrackingState.Inferred;
            }
            else
            {
                return JointTrackingState.NotTracked;
            }
        }

        /// <summary>
        /// Get the joint type corresponding to the string passed
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private static JointType stringToJointType(string n)
        {
            if (n.Equals("HipCenter"))
            {
                return JointType.HipCenter;
            }
            else if (n.Equals("Spine"))
            {
                return JointType.Spine;
            }
            else if (n.Equals("ShoulderCenter"))
            {
                return JointType.ShoulderCenter;
            }
            else if (n.Equals("Head"))
            {
                return JointType.Head;
            }
            else if (n.Equals("ShoulderLeft"))
            {
                return JointType.ShoulderLeft;
            }
            else if (n.Equals("ElbowLeft"))
            {
                return JointType.ElbowLeft;
            }
            else if (n.Equals("WristLeft"))
            {
                return JointType.WristLeft;
            }
            else if (n.Equals("HandLeft"))
            {
                return JointType.HandLeft;
            }
            else if (n.Equals("ShoulderRight"))
            {
                return JointType.ShoulderRight;
            }
            else if (n.Equals("ElbowRight"))
            {
                return JointType.ElbowRight;
            }
            else if (n.Equals("WristRight"))
            {
                return JointType.WristRight;
            }
            else if (n.Equals("HandRight"))
            {
                return JointType.HandRight;
            }
            else if (n.Equals("HipLeft"))
            {
                return JointType.HipLeft;
            }
            else if (n.Equals("KneeLeft"))
            {
                return JointType.KneeLeft;
            }
            else if (n.Equals("AnkleLeft"))
            {
                return JointType.AnkleLeft;
            }
            else if (n.Equals("FootLeft"))
            {
                return JointType.FootLeft;
            }
            else if (n.Equals("HipRight"))
            {
                return JointType.HipRight;
            }
            else if (n.Equals("KneeRight"))
            {
                return JointType.KneeRight;
            }
            else if (n.Equals("AnkleRight"))
            {
                return JointType.AnkleRight;
            }
            else
            {
                return JointType.FootRight;
            }
        }

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
                dists.Add(getDistBetweenFrames(skels.ElementAt(i), skels.ElementAt(i+1)));
            }

            for (int i = 0; i< diff; i++) 
            {
                double max = dists.Min();
                int index = dists.IndexOf(max);

                Skeleton insert = getMidFrame(skels.ElementAt(index), skels.ElementAt(index+1));
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
                dists.Add(getDistBetweenFrames(skels.ElementAt(i), skels.ElementAt(i+1)));
            }

            List<int> ignore = new List<int>();
            int j = 0;
            while (j < diff)
            {
                double min = dists.Min();
                int index = dists.IndexOf(min);
                if (index < skels.Count - 3) {
                    ignore.Add(index);
                    j++;
                }
                dists.RemoveAt(index);
            }

            for (int i = 0; i < skels.Count; i++) 
            {
                if (ignore.Contains(i)) {
                    continue;
                }

                result.Add(skels.ElementAt(i));
            }

            return alignFrames(result, nFrames);
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

        public static double totalDistTraveled(List<Skeleton> frames)
        {
            double result = 0;
            for (int i = 0; i < frames.Count - 1; i++)
            {
                result += getDistBetweenFrames(frames.ElementAt(i), frames.ElementAt(i + 1));
            }
            return result;
        }
    }
}
