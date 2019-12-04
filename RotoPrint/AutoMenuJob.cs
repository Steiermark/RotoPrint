using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using JMPHS;
using System.Net;

namespace RotoPrint
{
    public class AutoMenuJob
    {
        static PHCInterface phc;

        public struct Job_Struct
        {
            public string Name;
            public double Diameter;
            public double Offset;
            public int DpiY;
            public int DpiX;
            public int WidthPx;
            public int HeightPx;
            public double Widthmm;
            public double Heightmm;
            public int NofuSlices;
            public int NofmSlices;
        }

        bool[] PrintImageDone = new bool[] { false, false, false, false, false, false };

        bool[] ColorLoaded = new bool[] { false, false, false, false, false, false };
        Bitmap[] Bmp = new Bitmap[6];
        Bitmap[,] PhcBmp = new Bitmap[6, 60];
        StreamWriter MyLog;

        public SArray[] arrays = new SArray[6];
        public static Job_Struct JobInfo = new Job_Struct();

        public AutoMenuJob()
        {

        }

        private void Slice(Bitmap BmpSrc, int HeadNr, int n)
        {
            int m = BmpSrc.Height / (256 * n);
            if (BmpSrc.Height > 256 * m) m++;
            int NofUint = (BmpSrc.Width + 31) / 32;
            int DstHeight = m * n * 512;
            Bitmap BmpDst = new Bitmap(BmpSrc.Width, DstHeight, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData Src = BmpSrc.LockBits(new Rectangle(0, 0, BmpSrc.Width, BmpSrc.Height), ImageLockMode.ReadWrite, BmpSrc.PixelFormat);
                BitmapData Dst = BmpDst.LockBits(new Rectangle(0, 0, BmpDst.Width, BmpDst.Height), ImageLockMode.ReadWrite, BmpSrc.PixelFormat);

                int Height = BmpSrc.Height;
                int WidthInBytes = Src.Stride;


                uint* SrcStart = (uint*)Src.Scan0;
                uint* DstStart = (uint*)Dst.Scan0;
                int n256 = n * 256;

                int DstSize = (BmpDst.Height * Dst.Stride) / 4;
                for (int i = 0; i < DstSize; i++)
                    DstStart[i] = 0xFFFFFFFF;

                for (int y = 0; y < Height; y++)
                {
                    uint* SrcPos = SrcStart + (y * Src.Stride / 4);
                    int p = (((y / n256) * n256 + (y % n) * 256 + (y % n256) / n) * 2) + 1;
                    uint* DstPos = DstStart + (p * Src.Stride / 4);
                    for (int x = 0; x < WidthInBytes / 4; x++)
                    {
                        DstPos[x] = SrcPos[x];
                    }
                }

                BmpSrc.UnlockBits(Src);
                BmpDst.UnlockBits(Dst);
            }

            for (int i = 0; i < n * m; i++)
            {
                PhcBmp[HeadNr, i] = BmpDst.Clone(new Rectangle(0, i * 512, BmpDst.Width, 512), BmpDst.PixelFormat);
                //            PhcBmp[HeadNr, i].RotateFlip(RotateFlipType.RotateNoneFlipY);
            }

            for (int i = 0; i < n * m; i++)
            {
                PhcBmp[HeadNr, i].Save("slice" + HeadNr + "-" + i + ".bmp");
            }
        }

        public void SendSensorSignal(int PrinterId)
        {
            phc.SetSensorSignal(PrinterId);
        }
        public int GetNumberOfSlices()
        {
            return JobInfo.NofuSlices;
        }
        public Boolean lbUpload_Slice(int SliceIdx)
        {
            if (SliceIdx < JobInfo.NofmSlices)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (ColorLoaded[i])
                    {
                        PrintImageDone[i] = false;
                        phc.SetImage(arrays[i].Id, (Image)PhcBmp[i, SliceIdx], 400, 0); // Upload next slice to PHC
                        System.Threading.Thread.Sleep(2000);
                    }
                    else
                    {
                        PrintImageDone[i] = true;
                    }
                }
                return true;
            }

            return false;
        }

        public void lbFiles_load()
        {
            //Label lbl = (Label)sender;
            //int FileNr = (int)lbl.Tag;

            //string FileMask = "kant";// lbFile[FileNr].Text.Substring(0, 6);
            //string filename = lbFile[FileNr].Text.Substring(7);
            //string filepath = "C:\\rotoprint\\images\\";
            //var FileNamePath = Path.Combine(filepath, filename);

            ColorLoaded[0] = false; // white

            Bitmap TmpBmp = new Bitmap("C:\\rotoprint\\images\\kant_C.bmp");
            Bitmap newBmp = new Bitmap(TmpBmp);
            newBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            Bmp[1] = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format24bppRgb);
            JobInfo.WidthPx = Bmp[1].Width;
            JobInfo.HeightPx = Bmp[1].Height;
            ColorLoaded[1] = true;

            TmpBmp = new Bitmap("C:\\rotoprint\\images\\kant_M.bmp");
            newBmp = new Bitmap(TmpBmp);
            newBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            Bmp[2] = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format24bppRgb);
            JobInfo.WidthPx = Bmp[2].Width;
            JobInfo.HeightPx = Bmp[2].Height;
            ColorLoaded[2] = true;

            TmpBmp = new Bitmap("C:\\rotoprint\\images\\kant_Y.bmp");
            newBmp = new Bitmap(TmpBmp);
            newBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            Bmp[3] = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format24bppRgb);
            JobInfo.WidthPx = Bmp[3].Width;
            JobInfo.HeightPx = Bmp[3].Height;
            ColorLoaded[3] = true;

            TmpBmp = new Bitmap("C:\\rotoprint\\images\\kant_K.bmp");
            newBmp = new Bitmap(TmpBmp);
            newBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            Bmp[4] = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format24bppRgb);
            JobInfo.WidthPx = Bmp[4].Width;
            JobInfo.HeightPx = Bmp[4].Height;
            ColorLoaded[4] = true;

            ColorLoaded[5] = false; // Vanish

            /*
            for (int i = 0; i < 6; i++)
            {
                if (FileMask[i] != ' ')
                {
                    Bitmap TmpBmp = new Bitmap(FileNamePath + FileMask[i] + ".bmp");
                    Bitmap newBmp = new Bitmap(TmpBmp);
                    newBmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    Bmp[i] = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format24bppRgb);
                    JobInfo.WidthPx = Bmp[i].Width;
                    JobInfo.HeightPx = Bmp[i].Height;
                    ColorLoaded[i] = true;
                }
                else
                    ColorLoaded[i] = false;
            }
            */

            //JobInfo.Diameter = Convert.ToDouble(lbParam[1].Text, System.Globalization.CultureInfo.InvariantCulture);
            JobInfo.Diameter = Convert.ToDouble("50.0", System.Globalization.CultureInfo.InvariantCulture);
            
            if (JobInfo.DpiX == 0)
            {
                JobInfo.DpiX = (int)Math.Ceiling(JobInfo.WidthPx / (JobInfo.Diameter * Math.PI / 25.4 * 100)) * 100;
            }

            if (JobInfo.DpiY == 0)
            {
                JobInfo.DpiY = JobInfo.DpiX;
            }

            JobInfo.Widthmm = Math.Round(25.4 * (JobInfo.WidthPx / JobInfo.DpiX));
            JobInfo.Heightmm = Math.Round(25.4 * (JobInfo.HeightPx / JobInfo.DpiY));


            JobInfo.NofuSlices = Convert.ToInt16(JobInfo.DpiY) / 100;
            JobInfo.NofmSlices = JobInfo.HeightPx / (JobInfo.NofuSlices * 256);
            if (JobInfo.HeightPx > JobInfo.NofuSlices * 256) JobInfo.NofmSlices++;

            for (int i = 0; i < 6; i++)
            {
                if (ColorLoaded[i]) Slice(Bmp[i], i, JobInfo.NofuSlices);
            }

            PhcConnect();
        }

        void phc_OnStatus(int PrinterId, int StatusCode, string Message, object Data)
        {
            string s;
            if (Data != null)
                s = string.Format("Status:{0} {1} {2}, Value: {3}", PrinterId, StatusCode, Message, Data.ToString());
            else
                s = string.Format("Status:{0} {1} {2}", PrinterId, StatusCode, Message);

            if (MyLog != null) MyLog.WriteLine(s);

            if (StatusCode == 20001)
            {
                //tb1.Text = s + Environment.NewLine + tb1.Text;
                PrintImageDone[PrinterId] = true;
            }
            else if(StatusCode == 200) // Ready to Print
            {
               // PrintImageDone[PrinterId] = true;
            }
        }

        public bool phc_AllPrintHeadDone()
        {
            for(int i = 0; i < 6; i++)
            {
                if(PrintImageDone[i] == false)
                {
                    return false;
                }
            }

            return true;
        }
        public bool phc_IsPrintDone(int PrinterId)
        {
            return PrintImageDone[PrinterId];
        }
        void phc_OnDebug(int PrinterId, PrinterCommunication.EDebugLevel level, string msg)
        {
            string s = "Debug " + PrinterId.ToString() + " " + msg;
            if (MyLog != null) MyLog.WriteLine(s);
            //tb1.Text = s + Environment.NewLine + tb1.Text;
        }

        void phc_OnError(int PrinterId, int ErrorCode, bool IsSet, string msg, object data)
        {
            string s;

            if (data != null)
                s = string.Format("Error: {0} {1}, Value: {2}", ErrorCode, msg, data.ToString());
            else
                s = string.Format("Error: {0} {1}", ErrorCode, msg);

            if (MyLog != null) MyLog.WriteLine(s);

            //tb1.Text = s + Environment.NewLine + tb1.Text;
        }

        void phc_OnConnect(bool Success)
        {
            string s;

            if (Success)
                s = "Connection established";
            else
                s = "Connection Failed";

            if (MyLog != null) MyLog.WriteLine(s);
            //tb1.Text = s + Environment.NewLine + tb1.Text;
        }

        void phc_OnInformation(int PrinterId, SPrinterInformation pi)
        {
            String s = "Info " + PrinterId.ToString();
            if (pi.ImageLoaded) s = s + " Image Loaded";

            if (MyLog != null) MyLog.WriteLine(s);
            //tb1.Text = s + Environment.NewLine + tb1.Text;

            /*
              public bool Valid;
                  public int PrinterId;
                  public DateTime LastUpdated;
                  public bool ImageLoaded;
                  public List<int> Errors;
                  public bool NursingEnabled;
                  public bool ReadyToPrint;
                  public bool ReadyToPrintWithoutSensorIgnore;
                  public bool EncoderRunning;
                  public bool SensorIgnore;
                  public uint SensorSignalsTotal;
                  public uint SensorSignalsValid;
                  public uint PrintsTotal;
                  public uint PrintsImage;
            */

        }

        public void PhcConnect()
        {
            MyLog = new StreamWriter("Mylog2.txt");
            phc = new PHCInterface();
            phc.OnDebug += new PHCInterface.DebugDelegate(phc_OnDebug);
            phc.OnError += new PHCInterface.ErrorDelegate(phc_OnError);
            phc.OnConnect += new PHCInterface.ConnectDelegate(phc_OnConnect);
            phc.OnStatus += new PHCInterface.StatusDelegate(phc_OnStatus);

            //            phc.OnTemperature += new PHCInterface.TemperatureDelegate(phc_OnTemperature);
            phc.OnInformation += new PHCInterface.InformationDelegate(phc_OnInformation);
            //            phc.OnCreateImageJob += new PHCInterface.CreateImageJobDelegate(phc_OnCreateImageJob);


            //            IPAddress ip = IPAddress.Parse("192.168.10.101");
            IPAddress ip = IPAddress.Parse("192.168.1.200");
            int port = 46654;
            //tb1.Text = "Trying to connect to PHC server" + Environment.NewLine + tb1.Text;
            if (!phc.Connect(ip, port))
            {
                //MessageBox.Show(String.Format("Cannot connect to PHC server {0} port {1}", ip, port));
            }
            else
            {
                //tb1.Text = "Successfully connected to PHC server" + Environment.NewLine + tb1.Text;

                SPrinter[] printers = phc.GetPrinters();
                foreach (SPrinter p in printers)
                {

                }

                arrays = phc.GetArrays();

                //               phc.SetSensorSignal(d.Id); // Tell PHC to print
                //                phc.SetSensorIgnore(arrays[0].Devices[0].Id, false);
                //                while (Notprinting) ;
                //                phc.SetSensorIgnore(arrays[0].Devices[0].Id, true);
            }
        }


    }
}
