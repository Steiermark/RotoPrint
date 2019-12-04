using System;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using JMPHS;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;

namespace RotoPrint
{

   class MenuJob
   {
      private static Form1 _theForm;
      lb lb = new lb();
      TouchKeyboard kb = new TouchKeyboard();
      private PHCInterface phc;

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

      public static Job_Struct JobInfo = new Job_Struct();

      /*
          // job calculated
          NofPrint = roundup(ImageY / DpiY) * (DpiY/100);

          double ZposPrint        = AxisStartPos[0] - (Diameter / 2);

          double CoronaYposStart  = AxisStartPos[3];
          double CoronaYposEnd    = AxisStartPos[3] - Offset - ((ImageY / DpiY) * 25.4);

      */

      bool[] ColorLoaded = new bool[]{false,false,false,false,false,false};
      StreamWriter MyLog;
      Bitmap[] Bmp = new Bitmap[6];
      Bitmap[,] PhcBmp = new Bitmap[6,60];
//      List<Bitmap> Slices;



      public SArray[] arrays = new SArray[6];

      string[] FileNames = new string[1024];
      int NofFilesFound = 0;
      int FileShowOffset = 0;

      public const int NofMenu = 5;
      public Label[] lbMenu = new Label[NofMenu];
      string[] TxtMenu = new string[] { "Open", "New", "Edit", "Save", "Delete" };

      public const int NofFileNav = 4;
      public Label[] lbFileNav = new Label[NofFileNav];
      string[] TxtFileNav = new string[] { "Prev", "", "Next", "Cancel" };

      public const int NofFile = 10;
      public Label[] lbFile = new Label[NofFile];

      public const int NofParam = 10;
      public Label[] lbParam = new Label[NofParam];
      public Label[] lbParamTxt = new Label[NofParam];
      string[] TxtParam = new string[] { "Job Name", "Diameter", "Offset(mm)", "DPI X", "DPI Y", "Width(px)", "Height(px)", "Width(mm)", "Height(mm)", "Colors files" };

      public Label lbImport = new Label();

      public const int NofUpload = 8;
      public Label[] lbUpload = new Label[NofUpload];


      PictureBox pb1 = new PictureBox();
      public TextBox tb1 = new TextBox();

      public MenuJob(Form1 theForm)
      {
         _theForm = theForm;
      }

      public MenuJob()
      {
      }

      public void Init()
      {
         for (int i = 0; i < NofMenu; i++)
         {
            lbMenu[i] = new Label();
            lb.MenuDefault(lbMenu[i], 60, TxtMenu[i], i);
            lbMenu[i].Click += new System.EventHandler(this.lbMenu_Click);
         }

         for (int i = 0; i < NofFileNav; i++)
         {
            lbFileNav[i] = new Label();
            lb.MenuDefault(lbFileNav[i], 60, TxtFileNav[i], i);
            lbFileNav[i].Click += new System.EventHandler(this.lbFileNav_Click);
         }

         for (int i = 0; i < NofFile; i++)
         {
            lbFile[i] = new Label();
            lb.TxtDefault(lbFile[i], "", i);
            lbFile[i].Font = new Font("Courier New", lbFile[i].Font.Size, FontStyle.Bold);
            lbFile[i].Location = new Point(0, 120 + 60 * i);
            lbFile[i].Size = new Size(864, 59);
            lbFile[i].Click += new System.EventHandler(this.lbFile_Click);
         }

         for (int i = 0; i < NofParam; i++)
         {
            lbParam[i] = new Label();
            lb.NumDefault(lbParam[i], "", 216, 120 + i * 60, 107, 59, lbParam_Click,lbParam_PreviewKeyDown, i);

            lbParamTxt[i] = new Label();
            lb.TxtDefault(lbParamTxt[i], TxtParam[i], 0, 120 + i * 60, 215, 59,i);
         }

         lbParam[0].Size = new Size(864, 59);
         lbParam[0].TextAlign = ContentAlignment.MiddleLeft;
         lbParam[9].Size = new Size(165, 59);
         lbParam[9].TextAlign = ContentAlignment.MiddleLeft;

         lbImport = new Label();
         lb.MenuDefault(lbImport, 0, "Import Graphics", 7);
         lbImport.Location = new Point(216, 120 + NofParam * 60);
         lbImport.Size = new Size(250, 59);
         lbImport.Click += new System.EventHandler(this.lbImport_Click);

         for (int i = 0; i < NofUpload; i++)
         {
            lbUpload[i] = new Label();
            lb.MenuDefault(lbUpload[i], 0, "Upload " + i, i);
            lbUpload[i].Location = new Point(0, 720 + i * 60);
            lbUpload[i].Size = new Size(215, 59);
            lbUpload[i].Click += new System.EventHandler(this.lbUpload_Click);
         }


         pb1 = new PictureBox();
         lb.pbDefault(pb1);
         pb1.Location = new Point(0, 180 + NofParam * 60);
         //           pb1.Location = new Point(0, 180);
         pb1.Size = new Size(1080, 1080);

         tb1 = new TextBox();
         lb.tbDefault(tb1);
         tb1.Location = new Point(400, 180);
         tb1.Size = new Size(680, 1080);
         tb1.Multiline = true;
         tb1.Visible = false;

      }

      public void ShowMenu(bool show)
      {
         for (int i = 0; i < NofMenu; i++)
            lbMenu[i].Visible = show;

         for (int i = 0; i < NofParam; i++)
         {
            lbParam[i].Visible = show;
            lbParamTxt[i].Visible = show;
         }

         tb1.Visible = show;
         pb1.Visible = show;
         if (!show)
         {
            for (int i = 0; i < NofFileNav; i++)
               lbFileNav[i].Visible = show;

            for (int i = 0; i < NofFile; i++)
               lbFile[i].Visible = show;

            lbImport.Visible = show;
         }
      }

      private void Slice(Bitmap BmpSrc,int HeadNr, int n)
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

            int DstSize = (BmpDst.Height * Dst.Stride)/ 4;
            for (int i = 0; i < DstSize; i++)
               DstStart[i] = 0xFFFFFFFF;

            for (int y = 0; y < Height; y++)
            {
               uint* SrcPos = SrcStart + (y * Src.Stride / 4);
               int p = (((y / n256) * n256 + (y % n) * 256 + (y % n256) / n) * 2) +1;
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
            PhcBmp[HeadNr, i].Save("slice" + HeadNr +"-"+ i + ".bmp");
         }
      }

      void phc_OnStatus(int PrinterId, int StatusCode, string Message, object Data)
      {
         string s;
         if (Data != null)
            s = string.Format("Status:{0} {1} {2}, Value: {3}",PrinterId, StatusCode, Message, Data.ToString());
         else
            s = string.Format("Status:{0} {1} {2}", PrinterId, StatusCode, Message);

         if (MyLog != null) MyLog.WriteLine(s);

         if (StatusCode == 20001)
         {
           tb1.Text = s + Environment.NewLine + tb1.Text;
         }
      }

      void phc_OnDebug(int PrinterId, PrinterCommunication.EDebugLevel level, string msg)
      {
         string s = "Debug " + PrinterId.ToString() + " " + msg;
         if (MyLog != null) MyLog.WriteLine(s);
         tb1.Text = s + Environment.NewLine + tb1.Text;
      }

      void phc_OnError(int PrinterId, int ErrorCode, bool IsSet, string msg, object data)
      {
         string s;

         if (data != null)
            s = string.Format("Error: {0} {1}, Value: {2}", ErrorCode, msg, data.ToString());
         else
            s = string.Format("Error: {0} {1}", ErrorCode, msg);

         if (MyLog != null) MyLog.WriteLine(s);

         tb1.Text = s + Environment.NewLine + tb1.Text;
      }

      void phc_OnConnect(bool Success)
      {
         string s;

         if (Success)
            s = "Connection established";
         else
            s = "Connection Failed";

         if (MyLog != null) MyLog.WriteLine(s);
         tb1.Text = s + Environment.NewLine + tb1.Text;
      }

      void phc_OnInformation(int PrinterId, SPrinterInformation pi)
      {
         String s = "Info " + PrinterId.ToString();
         if (pi.ImageLoaded) s = s + " Image Loaded";

         if (MyLog != null) MyLog.WriteLine(s);
         tb1.Text = s + Environment.NewLine + tb1.Text;

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
         MyLog = new StreamWriter("Mylog.txt");
         phc = new PHCInterface(_theForm);
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
         tb1.Text = "Trying to connect to PHC server" + Environment.NewLine + tb1.Text;
         if (!phc.Connect(ip, port))
         {
            MessageBox.Show(String.Format("Cannot connect to PHC server {0} port {1}", ip, port));
         }
         else
         {
            tb1.Text = "Successfully connected to PHC server" + Environment.NewLine + tb1.Text;

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




      private void lbUpload_Click(object sender, EventArgs e)
      {
         Label lbl = (Label)sender;
         int UploadNr = (int)lbl.Tag;
         for (int i=0;i<6;i++)
         if (ColorLoaded[i]) phc.SetImage(arrays[i].Id, (Image)PhcBmp[i, UploadNr], 400,0); // Upload next slice to PHC
      }

      public Boolean lbUpload_Slice(int SliceIdx)
      {
          if(SliceIdx < JobInfo.NofmSlices)
            {
                for (int i = 0; i < 6; i++)
                    if (ColorLoaded[i]) phc.SetImage(arrays[i].Id, (Image)PhcBmp[i, SliceIdx], 400, 0); // Upload next slice to PHC
                return true;
            }

            return false; 
        }
      private void lbFile_Click(object sender, EventArgs e)
      {
         Label lbl = (Label)sender;
         int FileNr = (int)lbl.Tag;
//         var swl = Stopwatch.StartNew();

         string FileMask = lbFile[FileNr].Text.Substring(0, 6);
         string filename = lbFile[FileNr].Text.Substring(7);
         string filepath = "C:\\rotoprint\\images\\";
         var FileNamePath = Path.Combine(filepath, filename);

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

         JobInfo.Diameter = Convert.ToDouble(lbParam[1].Text, System.Globalization.CultureInfo.InvariantCulture);

         if (JobInfo.DpiX == 0)
         {
            JobInfo.DpiX = (int)Math.Ceiling(JobInfo.WidthPx / (JobInfo.Diameter * Math.PI / 25.4 * 100)) * 100;
         }

         if (JobInfo.DpiY == 0)
         {
            JobInfo.DpiY = JobInfo.DpiX;
         }

         JobInfo.Widthmm  = Math.Round(25.4 * (JobInfo.WidthPx / JobInfo.DpiX));
         JobInfo.Heightmm = Math.Round(25.4 * (JobInfo.HeightPx / JobInfo.DpiY));


         lbParam[3].Text = JobInfo.DpiX.ToString();
         lbParam[4].Text = JobInfo.DpiY.ToString();

         lbParam[5].Text = JobInfo.WidthPx.ToString();
         lbParam[6].Text = JobInfo.HeightPx.ToString();

         lbParam[7].Text = JobInfo.Widthmm.ToString();
         lbParam[8].Text = JobInfo.Heightmm.ToString();

         lbParam[9].Text = lbFile[FileNr].Text.Substring(0, 6);

         JobInfo.NofuSlices = Convert.ToInt16(JobInfo.DpiY) / 100;
         JobInfo.NofmSlices = JobInfo.HeightPx / (JobInfo.NofuSlices * 256);
         if (JobInfo.HeightPx > JobInfo.NofuSlices * 256) JobInfo.NofmSlices++;

//         var swl = Stopwatch.StartNew();

         for (int i = 0; i < 6; i++)
         {
            if (ColorLoaded[i]) Slice(Bmp[i], i, JobInfo.NofuSlices);
         }
         //        swl.Stop();


         for (int i = 0; i < NofFile; i++)
            lbFile[i].Visible = false;

         for (int i = 0; i < NofUpload; i++)
            lbUpload[i].Visible = true;

         for (int i = 0; i < NofParam; i++)
         {
            lbParam[i].Visible = true;
            lbParamTxt[i].Visible = true;
            lbImport.Visible = true;
         }
         lbImport.Visible = false;
         PhcConnect();
      }

      private void lbParam_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
      {
         if (e.KeyCode == Keys.Enter)
         {
            MessageBox.Show("Enter key pressed");
         }
         if (e.KeyCode == Keys.Escape)
         {
            MessageBox.Show("Escape key pressed");
         }
      }

      private void lbParam_Click(object sender, EventArgs e)
      {
         Label lbl = (Label)sender;
         int ParamNr = (int)lbl.Tag;

         kb.HideAkey();
         kb.HideNkey();

         if (ParamNr == 0)
         {
            TouchKeyboard.lbAkeyHost = (Label)sender;
            kb.ShowAkey(120 + NofParam * 60);
         }

         if (ParamNr == 1 || ParamNr == 2)
         {
            TouchKeyboard.lbNkeyHost = (Label)sender;
            kb.ShowNkey(216, 120 + NofParam * 60, true);
         }

         if (ParamNr == 3 || ParamNr == 4)
         {
            TouchKeyboard.lbNkeyHost = (Label)sender;
            kb.ShowNkey(216, 120 + NofParam * 60, false);
         }

         if (ParamNr == 7)
         {
         }
      }

      private void lbFileNav_Click(object sender, EventArgs e)
      {
         Label lbl = (Label)sender;
         int MenuNr = (int)lbl.Tag;

         if (MenuNr == 0 && FileShowOffset > 0) FileShowOffset -= NofFile;
         if (MenuNr == 2 && ((FileShowOffset + NofFile) < NofFilesFound)) FileShowOffset += NofFile;

         if (MenuNr == 0 || MenuNr == 2)
         {
            if (FileShowOffset > 0) lbFileNav[0].Visible = true;
            else lbFileNav[0].Visible = false;

            if (NofFilesFound > (FileShowOffset + NofFile)) lbFileNav[2].Visible = true;
            else lbFileNav[2].Visible = false;

            for (int i = 0; i < NofFile; i++)
            {
               if ((FileShowOffset + i) < NofFilesFound)
               {
                  lbFile[i].Text = FileNames[FileShowOffset + i];
                  lbFile[i].Visible = true;
               }
               else
                  lbFile[i].Visible = false;
            }
            int n = FileShowOffset + NofFile;
            if (n > NofFilesFound) n = NofFilesFound;
            lbFileNav[1].Text = (FileShowOffset + 1) + "-" + n;
         }

         if (MenuNr == 3)
         {
            for (int i = 0; i < NofFile; i++) lbFile[i].Visible = false;
            for (int i = 0; i < NofFileNav; i++) lbFileNav[i].Visible = false;
            for (int i = 0; i < NofMenu; i++) lbMenu[i].Visible = true;
            for (int i = 0; i < NofParam; i++) lbParam[i].Visible = true;
            for (int i = 0; i < NofParam; i++) lbParamTxt[i].Visible = true;
         }
      }

      private void lbMenu_Click(object sender, EventArgs e)
      {
         Label lbl = (Label)sender;
         int MenuNr = (int)lbl.Tag;

         kb.HideAkey();
         kb.HideNkey();
         //           MenujobNr = MenuNr;

         if (MenuNr == 0 || MenuNr == 4)
         {
            for (int i = 0; i < NofParam; i++)
            {
               lbParam[i].Visible = false;
               lbParamTxt[i].Visible = false;
               lbImport.Visible = false;
            }
            lbImport.Visible = false;

            DirectoryInfo d = new DirectoryInfo(@"C:\rotoprint\jobs\");//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files

            NofFilesFound = 0;
            foreach (FileInfo file in Files)
            {
               FileNames[NofFilesFound] = file.Name;
               NofFilesFound++;
            }

            int FilesToShow = NofFilesFound;
            if (NofFilesFound > NofFile)
            {
               FilesToShow = NofFile;
               lbFileNav[2].Visible = true;
            }

            lbFileNav[1].Visible = true;
            lbFileNav[1].Text = "1-" + FilesToShow;

            lbFileNav[3].Visible = true;

            for (int i = 0; i < FilesToShow; i++)
            {
               lbFile[i].Text = FileNames[i];
               lbFile[i].Visible = true;
            }
            for (int i = 0; i < NofMenu; i++) lbMenu[i].Visible = false;
         }

         if (MenuNr == 1)
         {
            for (int i = 0; i < NofParam; i++)
            {
               lbParam[i].Visible = true;
               lbParamTxt[i].Visible = true;
            }
            lbParam[0].Text = "";
            lbParam[1].Text = "50.0";
            lbParam[2].Text = "0";
            lbParam[3].Text = "100";
            lbParam[4].Text = "100";
            lbParam[5].Text = "0";
            lbParam[6].Text = "0";
            lbImport.Visible = true;

            for (int i = 0; i < NofMenu; i++) lbMenu[i].Visible = true;
         }

         if (MenuNr == 2)
         {
            /*            for (int i = 0; i < NofParam; i++)
                        {
                           lbParam[i].Visible = true;
                           lbParamTxt[i].Visible = true;
                        }
                        for (int i = 0; i < NofMenu; i++) lbMenu[i].Visible = true;
            */

         }

         if (MenuNr == 3)
         {
            phc.SetSensorSignal(arrays[1].Id); // Tell PHC to print
         }
      }

      private void lbImport_Click(object sender, EventArgs e)
      {
         Label lbl = (Label)sender;
         int FileNr = (int)lbl.Tag;
         StringBuilder sb;
         string[] kk = new string[100];
         string[] mask = new string[100];

         DirectoryInfo d = new DirectoryInfo(@"C:\rotoprint\images\");
         FileInfo[] Files = d.GetFiles("*.bmp"); //Getting Text files
         int NofGfiles = 0;
         foreach (FileInfo file in Files)
         {
            int len = file.Name.Length;
            string FileName = file.Name.Substring(0, len - 5);
            string type = file.Name.Substring(len - 5, 1);

            int i = 0;
            bool Found = false;
            while (i < NofGfiles && !Found)
            {
               if (FileName == kk[i])
               {
                  sb = new StringBuilder(mask[i]);
                  if (type == "C") sb[0] = 'C';
                  if (type == "M") sb[1] = 'M';
                  if (type == "Y") sb[2] = 'Y';
                  if (type == "K") sb[3] = 'K';
                  if (type == "V") sb[4] = 'V';
                  if (type == "W") sb[5] = 'W';
                  mask[i] = sb.ToString();
                  Found = true;
               }
               i++;
            }

            if (!Found)
            {
               kk[NofGfiles] = FileName;
               sb = new StringBuilder("      ");
               if (type == "C") sb[0] = 'C';
               if (type == "M") sb[1] = 'M';
               if (type == "Y") sb[2] = 'Y';
               if (type == "K") sb[3] = 'K';
               if (type == "V") sb[4] = 'V';
               if (type == "W") sb[5] = 'W';
               mask[i] = sb.ToString();
               NofGfiles++;
            }
         }

         for (int i = 0; i < NofParam; i++)
         {
            lbParam[i].Visible = false;
            lbParamTxt[i].Visible = false;
            lbImport.Visible = false;
         }
         lbImport.Visible = false;


         for (int i = 0; i < NofGfiles; i++)
         {
            lbFile[i].Text = mask[i] + " " + kk[i];
            lbFile[i].Visible = true;
         }
      }
   }
}