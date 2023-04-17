using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace LaneFollower
{
    public partial class Form1 : Form
    {
        Robot LMCrobot;
        private VideoCapture capture;
        private Thread captureThread;
        private String comPort = "0";

        //trackbar thresholds for redFrame
        private int minHueR = 0;
        private int maxHueR = 0;
        private int minSaturationR = 0;
        private int maxSaturationR = 0;
        private int minValueR = 0;
        private int maxValueR = 0;

        //trackbar thresholds for yellowFrame
        private int minHueY = 0;
        private int maxHueY = 0;
        private int minSaturationY = 0;
        private int maxSaturationY = 0;
        private int minValueY;
        private int maxValueY = 0;

        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            capture = new VideoCapture(1);
            captureThread = new Thread(DisplayWebcam);
            captureThread.Start();

            //set default trackbar values
            minHueY = minHTrackY.Value; 
            maxHueY = maxHTrackY.Value;
            minHueR = minHTrackR.Value;
            maxHueR = maxHTrackR.Value;
            minSaturationY = minSTrackY.Value;
            maxSaturationY = maxSTrackY.Value;
            minSaturationR = minSTrackR.Value;
            maxSaturationR = maxSTrackR.Value;
            minValueY = minVTrackY.Value;
            maxValueY = maxVTrackY.Value;
            minValueR = minVTrackR.Value;
            maxValueR = maxVTrackR.Value;
        }
        private void RedIsolation(Mat tempFrameR)
        {
            Mat hsvFrameR = new Mat();
            CvInvoke.CvtColor(tempFrameR, hsvFrameR, Emgu.CV.CvEnum.ColorConversion.Bgr2Hsv);

            //split HSV channels for better control
            Mat[] hsvChannelsR = hsvFrameR.Split();

            //set hue values
            Mat hueFilterR = new Mat();
            CvInvoke.InRange(hsvChannelsR[0], new ScalarArray(minHueR), new ScalarArray(maxHueR), hueFilterR);
            Invoke(new Action(() => { hueFrameR.Image = hueFilterR.Bitmap; }));

            //set saturation values
            Mat saturationFilterR = new Mat();
            CvInvoke.InRange(hsvChannelsR[1], new ScalarArray(minSaturationR), new ScalarArray(maxSaturationR), saturationFilterR);
            Invoke(new Action(() => { saturationFrameR.Image = saturationFilterR.Bitmap; }));

            //set value values
            Mat valueFilterR = new Mat();
            CvInvoke.InRange(hsvChannelsR[2], new ScalarArray(minValueR), new ScalarArray(maxValueR), valueFilterR);
            Invoke(new Action(() => { valueFrameR.Image = valueFilterR.Bitmap; }));

            //combine channels and display in redFrame
            Mat hsvFilterR = new Mat();
            CvInvoke.BitwiseAnd(hueFilterR, saturationFilterR, hsvFilterR);
            CvInvoke.BitwiseAnd(hsvFilterR, valueFilterR, hsvFilterR);
            Invoke(new Action(() => { redFrame.Image = hsvFilterR.Bitmap; }));

            RedPixelCount(hsvFilterR);
        }
        private void RedPixelCount(Mat tempFrameR2)
        {
            Image<Gray, byte> img2 = tempFrameR2.ToImage<Gray, byte>();

            //create box in the lower seventh of the screen to read white pixels
            img2.ROI = new Rectangle(0, 0, tempFrameR2.Width, tempFrameR2.Height * 6/ 7);
            int whitePixelsR = img2.CountNonzero()[0];
            img2.ROI = Rectangle.Empty;

            Invoke(new Action(() =>
            {
                if (!pixelCountPause.Checked)
                {
                    labelBR.Text = "" + (whitePixelsR);
                }
            }));
            if (comPort != "0")
            {
                if (whitePixelsR >= 1400) //if there are at least 1400 pixels, stop the program
                {
                    LMCrobot.Move(Robot.STOP); //doesn't always want to stop, had to spam it lol
                    LMCrobot.Move(Robot.STOP);
                    LMCrobot.Move(Robot.STOP);
                    LMCrobot.Move(Robot.STOP);
                    LMCrobot.Close(); //close communication so it can't lane detect
                }
            }
            
        }
        private void YellowIsolation(Mat tempFrameY)
        {
            Mat hsvFrameY = new Mat();
            CvInvoke.CvtColor(tempFrameY, hsvFrameY, Emgu.CV.CvEnum.ColorConversion.Bgr2Hsv);

            //split HSV channels for better control
            Mat[] hsvChannelsY = hsvFrameY.Split();

            //set hue values
            Mat hueFilterY = new Mat();
            CvInvoke.InRange(hsvChannelsY[0], new ScalarArray(minHueY), new ScalarArray(maxHueY), hueFilterY);
            Invoke(new Action(() => { hueFrameY.Image = hueFilterY.Bitmap; }));

            //set saturation values
            Mat saturationFilterY = new Mat();
            CvInvoke.InRange(hsvChannelsY[1], new ScalarArray(minSaturationY), new ScalarArray(maxSaturationY), saturationFilterY);
            Invoke(new Action(() => { saturationFrameY.Image = saturationFilterY.Bitmap; }));

            //set value values
            Mat valueFilterY = new Mat();
            CvInvoke.InRange(hsvChannelsY[2], new ScalarArray(minValueY), new ScalarArray(maxValueY), valueFilterY);
            Invoke(new Action(() => { valueFrameY.Image = valueFilterY.Bitmap; }));

            //combine channels and display in yellowFrame
            Mat hsvFilterY = new Mat();
            CvInvoke.BitwiseAnd(hueFilterY, saturationFilterY, hsvFilterY);
            CvInvoke.BitwiseAnd(hsvFilterY, valueFilterY, hsvFilterY);
            Invoke(new Action(() => { yellowFrame.Image = hsvFilterY.Bitmap; }));

            YellowPixelCount(hsvFilterY);
        }
        private void YellowPixelCount(Mat tempFrameY2)
        {
            //pixel divisions
            int whitePixelsFFLY = 0; //far far left
            int whitePixelsFLY = 0; //far left
            int whitePixelsLY = 0; //left
            int whitePixelsCY = 0; //center
            int whitePixelsRY = 0; //right
            int whitePixelsFRY = 0; //far right
            int whitePixelsFFRY = 0; //far far right

            Image<Gray, byte> img = tempFrameY2.ToImage<Gray, byte>();

            //pixel counter by sevenths
            for (int yellowFFLx = 0; yellowFFLx < tempFrameY2.Width / 7; yellowFFLx++)
            {
                for (int yellowFFLy = 0; yellowFFLy < tempFrameY2.Height; yellowFFLy++)
                {
                    if (img.Data[yellowFFLy, yellowFFLx, 0] == 255) whitePixelsFFLY++; //count farthest left white pixels
                }
            }
            for (int yellowFLx = tempFrameY2.Width / 7; yellowFLx < (2 * tempFrameY2.Width) / 7; yellowFLx++)
            {
                for (int yellowFLy = 0; yellowFLy < tempFrameY2.Height; yellowFLy++)
                {
                    if (img.Data[yellowFLy, yellowFLx, 0] == 255) whitePixelsFLY++; //count middle left white pixels
                }
            }
            for (int yellowLx = (2 * tempFrameY2.Width) / 7; yellowLx < (3 * tempFrameY2.Width) / 7; yellowLx++)
            {
                for (int yellowLy = 0; yellowLy < tempFrameY2.Height; yellowLy++)
                {
                    if (img.Data[yellowLy, yellowLx, 0] == 255) whitePixelsLY++; //count closest left white pixels
                }
            }
            for (int yellowCx = (3 * tempFrameY2.Width) / 7; yellowCx < (4 * tempFrameY2.Width) / 7; yellowCx++)
            {
                for (int yellowCy = 0; yellowCy < tempFrameY2.Height; yellowCy++)
                {
                    if (img.Data[yellowCy, yellowCx, 0] == 255) whitePixelsCY++; //count center white pixels
                }
            }
            for (int yellowRx = (4 * tempFrameY2.Width) / 7; yellowRx < (5 * tempFrameY2.Width) / 7; yellowRx++)
            {
                for (int yellowRy = 0; yellowRy < tempFrameY2.Height; yellowRy++)
                {
                    if (img.Data[yellowRy, yellowRx, 0] == 255) whitePixelsRY++; //count closest right white pixels
                }
                for (int yellowFRx = (5 * tempFrameY2.Width) / 7; yellowFRx < (6 * tempFrameY2.Width) / 7; yellowFRx++)
                {
                    for (int yellowFRy = 0; yellowFRy < tempFrameY2.Height; yellowFRy++)
                    {
                        if (img.Data[yellowFRy, yellowFRx, 0] == 255) whitePixelsFRY++; //count middle right white pixels
                    }
                }
                for (int yellowFFRx = (6 * tempFrameY2.Width) / 7; yellowFFRx < tempFrameY2.Width; yellowFFRx++)
                {
                    for (int yellowFFRy = 0; yellowFFRy < tempFrameY2.Height; yellowFFRy++)
                    {
                        if (img.Data[yellowFFRy, yellowFFRx, 0] == 255) whitePixelsFFRY++; //count farthest right white pixels
                    }
                }
                Invoke(new Action(() =>
                {
                    if (!pixelCountPause.Checked)
                    {
                        //display white pixel values for the inside 5 sections, don't need the outside two since it should never be that off center
                        labelFLY.Text = "" + (whitePixelsFLY);
                        labelLY.Text = "" + (whitePixelsLY);
                        labelCY.Text = "" + (whitePixelsCY);
                        labelRY.Text = "" + (whitePixelsRY);
                        labelFRY.Text = "" + (whitePixelsFRY);
                    }
                }));
                if (comPort != "0") //if the com port is connected
                {
                    //had to be passed by reference or there would result in a counting error
                    YellowLineDetection(whitePixelsFLY, whitePixelsLY, whitePixelsCY, whitePixelsRY, whitePixelsFRY, whitePixelsFFRY, whitePixelsFFLY);
                }
            }
        }
        private void YellowLineDetection(int wp1, int wp2, int wp3, int wp4, int wp5, int wp6, int wp0)
        {
            if (wp3 >= 200 && wp2 < 100 && wp4 < 100) //if center has at least 200 white pixels, and the left and right of center have less than 100 white pixels 
            {
                LMCrobot.Move(Robot.CENTER); //go straight
            }
            else if (wp4 >= 100 && wp4 < 200) //if right of center detects at least 100 white pixels and less than 200 white pixels
            {
                LMCrobot.Move(Robot.RIGHT); //go right
            }
            else if (wp2 >= 100 && wp2 < 200) //if left of center detects at least 100 white pixels and less than 200 pixels
            {
                LMCrobot.Move(Robot.LEFT); //go left
            }
            else if (wp4 >= 200 || wp5 >= 100 || wp6 >= 100) //if right of center detects at least 200 pixels, or if far right or far far right of center detect at least 100 pixels
            {
                LMCrobot.Move(Robot.FARRIGHT); //go hard right
            }
            else if (wp2 >= 200 || wp1 >= 100 || wp0 >= 100) //if left of center detetcts at least 200 pixels, or if far left or far far left of center detect at least 100 pixels
            {
                LMCrobot.Move(Robot.FARLEFT); //go hard left
            }
            else //idk why I had to add this but sometimes it would act wonky so I defaulted to straight
            {
                LMCrobot.Move(Robot.CENTER);
            }
        }
        private void DisplayWebcam()
        {
            while (capture.IsOpened)
            {
                //import capture
                Mat frame = capture.QueryFrame();

                //resize
                int newHeight = (frame.Size.Height * rawFrame.Size.Width) / frame.Size.Width;
                Size newSize = new Size(rawFrame.Size.Width, newHeight);
                CvInvoke.Resize(frame, frame, newSize);

                //display capture in rawFrame
                Invoke(new Action(() => { rawFrame.Image = frame.Bitmap; }));

                RedIsolation(frame);
                YellowIsolation(frame);
            }
        }
        private void Form1_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            captureThread.Abort();
        }

        //set hue saturation and value to respective trackbar number
        private void minHTrackY_Scroll(object sender, EventArgs e)
        {
            minHueY = minHTrackY.Value;
            minHLabelY.Text = "" + minHueY;
        }

        private void maxHTrackY_Scroll(object sender, EventArgs e)
        {
            maxHueY = maxHTrackY.Value;
            maxHLabelY.Text = "" + maxHueY;
        }

        private void minSTrackY_Scroll(object sender, EventArgs e)
        {
            minSaturationY = minSTrackY.Value;
            minSLabelY.Text = "" + minSaturationY;
        }

        private void maxSTrackY_Scroll(object sender, EventArgs e)
        {
            maxSaturationY = maxSTrackY.Value;
            maxSLabelY.Text = "" + maxSaturationY;
        }

        private void minVTrackY_Scroll(object sender, EventArgs e)
        {
            minValueY = minVTrackY.Value;
            minVLabelY.Text = "" + minValueY;
        }

        private void maxVTrackY_Scroll(object sender, EventArgs e)
        {
            maxValueY = maxVTrackY.Value;
            maxVLabelY.Text = "" + maxValueY;
        }

        private void minHTrackR_Scroll(object sender, EventArgs e)
        {
            minHueR = minHTrackR.Value;
            minHLabelR.Text = "" + minHueR;
        }

        private void maxHTrackR_Scroll(object sender, EventArgs e)
        {
            maxHueR = maxHTrackR.Value;
            maxHLabelR.Text = "" + maxHueR;
        }

        private void minSTrackR_Scroll(object sender, EventArgs e)
        {
            minSaturationR = minSTrackR.Value;
            minSLabelR.Text = "" + minSaturationR;
        }

        private void maxSTrackR_Scroll(object sender, EventArgs e)
        {
            maxSaturationR = maxSTrackR.Value;
            maxSLabelR.Text = "" + maxSaturationR;
        }

        private void minVTrackR_Scroll(object sender, EventArgs e)
        {
            minValueR = minVTrackR.Value;
            minVLabelR.Text = "" + minValueR;
        }

        private void maxVTrackR_Scroll(object sender, EventArgs e)
        {
            maxValueR = maxVTrackR.Value;
            maxVLabelR.Text = "" + maxValueR;
        }

        //connect to com port upon clicking button
        private void comPortConnect_Click(object sender, EventArgs e)
        {
            comPort = comPortInput.Text;
            LMCrobot = new Robot(comPort);
        }
    }
}
