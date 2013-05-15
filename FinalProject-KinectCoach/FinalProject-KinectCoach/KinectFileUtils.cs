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
    class KinectFileUtils
    {
        public static List<Skeleton> ReadSkeletonFromRecordingFile(string filename)
        {
            List<Skeleton> frames = new List<Skeleton>();

            string line;
            Skeleton skel = new Skeleton();
            int frameCount = 0;
            StreamReader reader = new StreamReader(filename);
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("ERRORS"))
                {
                    continue;
                }

                if (line.Contains("FRAME"))
                {
                    if (frameCount > 0)
                    {
                        frames.Add(skel);
                        skel = new Skeleton();
                    }
                    frameCount++;
                    continue;
                }
                string[] jointParts = line.Split(',');
                JointType jt = StringToJointType(jointParts[0].Trim());

                Joint j = skel.Joints[jt];
                j.TrackingState = StringToTrackingState(jointParts[1].Trim());

                SkeletonPoint p = new SkeletonPoint();
                p.X = float.Parse(jointParts[2].Trim());
                p.Y = float.Parse(jointParts[3].Trim());
                p.Z = float.Parse(jointParts[4].Trim());

                j.Position = p;
                skel.Joints[j.JointType] = j;
            }
            reader.Close();
            frames.Add(skel);

            return frames;
        }

        public static List<double> ReadErrorsFromRecordingFile(string filename, int errorSet)
        {
            List<double> errors = new List<Double>();
            StreamReader reader = new StreamReader(filename);
            string line;
            int count = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("ERRORS"))
                {
                    count++;
                }

                if (count == errorSet)
                {
                    line = line.Split(':')[1];
                    string[] error = line.Split(',');
                    foreach (string s in error)
                    {
                        errors.Add(double.Parse(s.Trim()));
                    }
                    reader.Close();
                    return errors;
                }
            }
            reader.Close();
            return errors;
        }

        public static Dictionary<string, string> GetRecordingFileHeaders(string filepath)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            StreamReader reader = new StreamReader(filepath);
            string line;
            bool inHeader = false;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("STARTHEADER"))
                {
                    inHeader = true;
                    continue;
                }
                else if (line.Contains("ENDHEADER"))
                {
                    inHeader = false;
                    continue;
                }

                if (inHeader)
                {
                    string[] parts = line.Split(':');
                    headers.Add(parts[0], parts[1]);
                }
            }
            reader.Close();
            return headers;
        }

        /// <summary>
        /// Get the tracking state corresponding to the string passed
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private static JointTrackingState StringToTrackingState(string n)
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
        private static JointType StringToJointType(string n)
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

    }
}
