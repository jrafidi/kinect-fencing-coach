using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.Synthesis;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Linq;
using AForge.Math;
using Microsoft.Win32;

namespace FinalProject_KinectCoach
{
    /// <summary>
    /// Interaction logic for SessionViewer.xaml
    /// </summary>
    public partial class SessionViewer : Window
    {
        ///////////////////
        // MAIN WINDOW CONTROL CODE
        ///////////////////

        /// <summary>
        /// Active Kinect sensor.
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public SessionViewer(KinectSensor sensor)
        {
            InitializeComponent();
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
            this.sensor = sensor;

            this.KeyDown += new KeyEventHandler(OnButtonKeyDown);
        }

        /// <summary>
        /// Execute initialization tasks.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            if (null == this.sensor)
            {
                signal.Text = "No Kinect detected";
                return;
            }
        }

        /// <summary>
        /// Execute uninitialization tasks.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
                this.sensor = null;
            }
        }

        /// <summary>
        /// Handle keypress events on windows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {
        }

        ///////////////////
        // SPEECH CODE
        ///////////////////

        /// <summary>
        /// Speech generation coach
        /// </summary>
        private CoachSpeech coach = new CoachSpeech();

        /// <summary>
        /// Previous speech event, for repeating
        /// </summary>
        private string previousSpeech;

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        public void SpeechRecognized(string recognizedSpeech)
        {
            // Ignore speech if the coach is talking
            // (Does not work perfectly, confidence threshold set higher for this reason)
            if (coach.isSpeaking)
            {
                return;
            }

            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            string[] parts = recognizedSpeech.Split(':');

            switch (parts[0])
            {
                case "simple":
                    switch (parts[1])
                    {
                        case "show":
                            if (currentPose != null)
                            {
                                dm = DemoMode.POSE;
                            }
                            else if (currentAction != null)
                            {
                                dm = DemoMode.ACTION;
                            }
                            break;
                        case "hide":
                            dm = DemoMode.NONE;
                            break;
                        case "again":
                            if (previousSpeech != null)
                            {
                                SpeechRecognized(previousSpeech);
                            }
                            break;
                        case "exit":
                            this.Close();
                            break;
                        case "clear":
                            clearAll();
                            break;
                        case "callibrate":
                            cpm = CompareMode.CALLIBRATE;
                            break;
                        case "showdemoaction":
                            actionFrameCount = 0;
                            cpm = cpm == CompareMode.ACTION ? CompareMode.SIMUL_ACTION : cpm;
                            break;
                        case "hidedemoaction":
                            actionFrameCount = 0;
                            cpm = cpm == CompareMode.SIMUL_ACTION ? CompareMode.ACTION : cpm;
                            break;
                    }
                    break;
                case "pose":
                    checkPose(parts[1]);
                    previousSpeech = recognizedSpeech;
                    break;
                case "action":
                    watchAction(parts[1]);
                    previousSpeech = recognizedSpeech;
                    break;
            }
            
        }

        ///////////////////
        // SKELETON CODE
        ///////////////////

        /// <summary>
        /// Determine whether or not to draw next skeleton
        /// </summary>
        private bool paused = false;

        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private Pen trackedBonePen = new Pen(Brushes.DarkBlue, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.LightBlue, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                Pen oldPen = trackedBonePen;
                switch (dm)
                {
                    case DemoMode.POSE:
                        trackedBonePen = new Pen(Brushes.DarkGreen, 6);
                        this.DrawBonesAndJoints(currentPose.frame, dc);
                        break;
                    case DemoMode.ACTION:
                        if (actionFrameCount == currentAction.bestFrames.Count)
                        {
                            actionFrameCount = 0;
                        }
                        trackedBonePen = new Pen(Brushes.DarkGreen, 6);
                        this.DrawBonesAndJoints(currentAction.bestFrames.ElementAt(actionFrameCount), dc);
                        actionFrameCount++;
                        break;
                }
                trackedBonePen = oldPen;

                bool hasDrawableSkeleton = skeletons.Length != 0;

                switch (cpm)
                {
                    case CompareMode.POSE:
                        foreach (Skeleton skel in skeletons)
                        {
                            if (skel.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                this.DrawBonesAndJointsWithComparison(skel, currentPose, dc);
                                if (currentPose.matchesSkeleton(skel, 1))
                                {
                                    coach.sayCorrect();
                                    clearAll();
                                }
                            }
                        }
                        break;
                    case CompareMode.CALLIBRATE:
                        foreach (Skeleton skel in skeletons)
                        {
                            if (skel.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                currentPose = new Pose("ready");
                                rotTransform = KinectFrameUtils.getRotationMatrix(skel, currentPose.frame);
                                coach.speak("Rotational differences recorded");
                                cpm = CompareMode.NONE;
                            }
                        }
                        break;
                    case CompareMode.ACTION:
                        if (actionFrameCount >= currentAction.bestFrames.Count)
                        {
                            actionFrameCount = 0;
                        }
                        this.DrawBonesAndJointsWithComparison(actionFrames.ElementAt(actionFrameCount), currentAction.GetPoseOfFrame(actionFrameCount), dc);
                        actionFrameCount++;
                        break;
                    case CompareMode.SIMUL_ACTION:
                        if (actionFrameCount >= currentAction.bestFrames.Count)
                        {
                            actionFrameCount = 0;
                        }
                        trackedBonePen = new Pen(Brushes.DarkGreen, 6);
                        this.DrawBonesAndJoints(currentAction.bestFrames.ElementAt(actionFrameCount), dc);
                        trackedBonePen = new Pen(Brushes.DarkBlue, 6);
                        this.DrawBonesAndJointsWithComparison(actionFrames.ElementAt(actionFrameCount), currentAction.GetPoseOfFrame(actionFrameCount), dc);
                        actionFrameCount++;
                        break;
                    case CompareMode.NONE:
                        if (hasDrawableSkeleton)
                        {
                            foreach (Skeleton skel in skeletons)
                            {
                                if (skel.TrackingState == SkeletonTrackingState.Tracked)
                                {

                                    if (watchingAction)
                                    {
                                        if (!actionStarted)
                                        {
                                            checkActionStart(skel);
                                            this.DrawBonesAndJointsWithComparison(skel, currentAction.startPose, dc);
                                        }
                                        else
                                        {
                                            this.DrawBonesAndJoints(skel, dc);
                                            actionFrames.Add(skel);
                                            checkActionEnd(skel);
                                        }
                                    }
                                    else
                                    {
                                        RenderClippedEdges(skel, dc);
                                        this.DrawBonesAndJoints(skel, dc);
                                    }
                                }
                            }
                        }
                        break;
                }
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {

            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight, trackedBonePen);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft, trackedBonePen);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight, trackedBonePen);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft, trackedBonePen);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight, trackedBonePen);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight, trackedBonePen);

            // Sword (right handed)
            this.DrawSword(skeleton, drawingContext);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1, Pen BonePen)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = BonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Draws a gray line from the right hand joint outward, following the trajectory
        /// from the right wrist to the right hand.
        /// </summary>
        /// <param name="skel"></param>
        /// <param name="dc"></param>
        private void DrawSword(Skeleton skel, DrawingContext dc)
        {
            Joint rHand = skel.Joints[JointType.HandRight];
            Joint rWrist = skel.Joints[JointType.WristRight];

            SkeletonPoint diff = new SkeletonPoint();
            diff.X = 1.2f * rHand.Position.X - rWrist.Position.X;
            diff.Y = 1.2f * rHand.Position.Y - rWrist.Position.Y;
            diff.Z = 1.2f * rHand.Position.Z - rWrist.Position.Z;

            dc.DrawLine(new Pen(Brushes.DarkGray, 5), this.SkeletonPointToScreen(rHand.Position), this.SkeletonPointToScreen(diff));
        }

        ////////////////////
        // COMPARISON DRAWING CODE
        ////////////////////

        /// <summary>
        /// Pen for bones that are in the correct position
        /// </summary>
        private Pen correctBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen for bones who have at least one joint in the incorrect position
        /// </summary>
        private Pen incorrectBonePen = new Pen(Brushes.Red, 6);

        /// <summary>
        /// Compare the passed skeleton "skeleton" with the skeleton "compare" to get an error map, then draw bones according to pose and error
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="compare"></param>
        /// <param name="drawingContext"></param>
        private void DrawBonesAndJointsWithComparison(Skeleton skeleton, Pose currentP, DrawingContext drawingContext)
        {
            Dictionary<JointType, double> errorMap = currentP.getErrorMap(skeleton);

            // Render Torso
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter, errorMap, currentP.torsoError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft, errorMap, currentP.torsoError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight, errorMap, currentP.torsoError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine, errorMap, currentP.torsoError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter, errorMap, currentP.torsoError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft, errorMap, currentP.torsoError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight, errorMap, currentP.torsoError);

            // Left Arm
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft, errorMap, currentP.leftArmError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft, errorMap, currentP.leftArmError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft, errorMap, currentP.leftArmError);

            // Right Arm
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight, errorMap, currentP.rightArmError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight, errorMap, currentP.rightArmError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight, errorMap, currentP.rightArmError);

            // Left Leg
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft, errorMap, currentP.leftLegError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft, errorMap, currentP.leftLegError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft, errorMap, currentP.leftLegError);

            // Right Leg
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight, errorMap, currentP.rightLegError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight, errorMap, currentP.rightLegError);
            this.CheckErrorAndDrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight, errorMap, currentP.rightLegError);

            // Sword (right handed)
            this.DrawSword(skeleton, drawingContext);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                drawingContext.DrawEllipse(this.trackedJointBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
            }


        }

        /// <summary>
        /// Draw a bone with the correct color, depending on the error of that joint and the threshold passed
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="drawingContext"></param>
        /// <param name="jointType0"></param>
        /// <param name="jointType1"></param>
        /// <param name="errorMap"></param>
        /// <param name="errorThreshold"></param>
        private void CheckErrorAndDrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0,
            JointType jointType1, Dictionary<JointType, double> errorMap, double errorThreshold)
        {
            double error0 = errorMap[jointType0];
            double error1 = errorMap[jointType1];

            Pen bonePen;

            if (error0 > errorThreshold || error1 > errorThreshold)
            {
                bonePen = incorrectBonePen;
            }
            else
            {
                bonePen = correctBonePen;
            }

            DrawBone(skeleton, drawingContext, jointType0, jointType1, bonePen);
        }

        ////////////////////
        // POSE CHECKING CODE
        ////////////////////

        private enum CompareMode
        {
            NONE, POSE, ACTION, SIMUL_ACTION, CALLIBRATE
        }

        private enum DemoMode
        {
            NONE, POSE, ACTION
        }

        private CompareMode cpm = CompareMode.NONE;
        private DemoMode dm = DemoMode.NONE;

        private Pose currentPose = null;

        private Matrix3x3 rotTransform = Matrix3x3.Identity;

        private void checkPose(string pose)
        {
            coach.speak("Enter " + pose + " position");
            signal.Text = "Checking " + pose;
            currentPose = new Pose(pose);
            currentPose.applyTransform(rotTransform);
            cpm = CompareMode.POSE;
            correctBonePen = new Pen(Brushes.Green, 6);
        }

        private void clearAll()
        {
            signal.Text = "Ready";
            currentPose = null;
            currentAction = null;

            dm = DemoMode.NONE;
            cpm = CompareMode.NONE;

            watchingAction = false;
            actionStarted = false;
            actionFrames = new List<Skeleton>();
            actionFrameCount = 0;
        }

        ////////////////////
        // ACTION CHECKING CODE
        ////////////////////

        private bool watchingAction = false;
        private bool actionStarted = false;
        private List<Skeleton> actionFrames = new List<Skeleton>();
        private List<Skeleton> shiftedDemo = new List<Skeleton>();

        private int actionFrameCount = 0;

        private Gesture currentAction = null;
        private Skeleton lastFrame;

        private void checkActionStart(Skeleton skel)
        {
            if (currentAction.startPose.matchesSkeleton(skel, 1))
            {
                actionStarted = true;
                signal.Text = "Go";
                coach.speak("Begin");
            }
        }

        private void checkActionEnd(Skeleton skel)
        {
            if (actionFrames.Count < 30)
            {
                lastFrame = skel;
                return;
            }
            else if (actionFrames.Count > 300)
            {
                signal.Text = "Action too long";
                actionStarted = false;
                coach.speak("Action timeout");
                actionFrames = new List<Skeleton>();
                return;
            }

            double midDist = KinectFrameUtils.getDistBetweenFrames(lastFrame, skel);
            lastFrame = skel;
            double distTraveled = KinectFrameUtils.totalDistTraveled(actionFrames);
            if (currentAction.endPose.matchesSkeleton(skel, 1) && distTraveled > currentAction.dist / 2 && midDist < 0.1)
            {
                signal.Text = "Comparison above";
                actionStarted = false;
                watchingAction = false;
                coach.speak("Halt");
                evaluateAction();
                return;
            }
        }

        private void evaluateAction()
        {
            currentAction.DetermineBestFrames(actionFrames);
            actionFrames = KinectFrameUtils.startCorrectedFrames(actionFrames, currentAction.bestFrames);
            shiftedDemo = currentAction.GetShiftedFrames(actionFrames.ElementAt(0).Joints[JointType.HipCenter].Position);
            cpm = CompareMode.SIMUL_ACTION;
            dm = DemoMode.NONE;
            correctBonePen = new Pen(Brushes.Blue, 6);
        }

        private void watchAction(string action)
        {
            clearAll();
            signal.Text = "Take start pose for " + action;
            currentAction = new Gesture(action);
            currentAction.ApplyRotation(rotTransform);
            coach.speak("Enter starting position for " + action);
            actionFrameCount = 0;
            watchingAction = true;
            correctBonePen = new Pen(Brushes.DarkGreen, 6);
        }

        /// <summary>
        /// Shows file select dialog to select a recording
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openFileDialog(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Filter = "Recording files|*.txt;*.rec;*.pose;";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                string[] pieces = filename.Split('\\');
                FileNameTextBox.Text = pieces[pieces.Length - 1];
            }
        }
    }
}
