using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;


namespace RotoPrint
{
   static class c
   {
      public const int NofAxis = 11;
      public const int NofInk = 6;

      public const byte aZ = 0;
      public const byte aIndex = 1;
      public const byte aRod = 2;
      public const byte aCorona = 3;
      public const byte aW = 4;
      public const byte aC = 5;
      public const byte aM = 6;
      public const byte aY = 7;
      public const byte aK = 8;
      public const byte aV = 9;

      public const int vClean_Air = 51; //  6 = Clean Tank presssure       
      public const int vAir_Ink = 52;  // 6 = master air             7 = air print head
      public const int vPurge = 35;

   }

   public class lb
   {
      private static Form1 _theForm;

      public delegate void KeyDownType(object sender, PreviewKeyDownEventArgs e);
      public delegate void ClickType(object sender, EventArgs e);


      public Color bg1 = Color.FromArgb(255, 80, 80, 80);

      public void MenuDefault(Label l, int ypos, string Txt, int n)
      {
         l.Tag = n;
         l.Location = new Point(216 * n, ypos);
         l.Size = new Size(215, 59);
         l.Text = Txt;
         l.ForeColor = Color.DodgerBlue;
         l.BackColor = Color.Black;
         l.AutoSize = false;
         l.TextAlign = ContentAlignment.MiddleLeft;
         l.FlatStyle = FlatStyle.Flat;
         l.Font = new Font(l.Font.FontFamily, 24);
         l.Visible = false;
         _theForm.Controls.Add(l);
      }

      public void KeyDefault(Label l, string Txt, int n)
      {
         l.Tag = n;
         l.Size = new Size(107, 59);
         l.Text = Txt;
         l.ForeColor = Color.White;
         l.BackColor = bg1;
         l.AutoSize = false;
         l.TextAlign = ContentAlignment.MiddleCenter;
         l.FlatStyle = FlatStyle.Flat;
         l.Font = new Font(l.Font.FontFamily, 24);
         l.Visible = false;
         _theForm.Controls.Add(l);
      }

      public void TxtDefault(Label l, string Txt, int n)
      {
         KeyDefault(l, Txt, n);
         l.TextAlign = ContentAlignment.MiddleLeft;
      }

      public void TxtDefault(Label l, string Txt, int xpos, int ypos, int width, int height, int n)
      {
         KeyDefault(l, Txt, n);
         l.Location = new Point(xpos, ypos);
         l.Size = new Size(width, height);
         l.TextAlign = ContentAlignment.MiddleLeft;
      }


      public void NumDefault(Label l, string Txt, int n)
      {
         KeyDefault(l, Txt, n);
         l.TextAlign = ContentAlignment.MiddleRight;
      }

      public void NumDefault(Label l, string Txt, int xpos, int ypos, int width, int height, ClickType Click, KeyDownType KeyDown, int n)
      {
         KeyDefault(l, Txt, n);
         l.Location = new Point(xpos, ypos);
         l.Size = new Size(width, height);
         l.TextAlign = ContentAlignment.MiddleRight;
         if (Click != null) l.Click += new System.EventHandler(Click);
         if (KeyDown != null) l.PreviewKeyDown += new PreviewKeyDownEventHandler(KeyDown);
      }


      public void TestDefault(Label l, int xpos, string txt, int n)
      {
         l.Tag = n;
         l.Location = new Point(xpos, 60 + n * 60);
         l.Size = new Size(134, 59);
         l.Text = txt;
         l.ForeColor = Color.White;
         l.BackColor = bg1;
         l.Font = new Font(l.Font.FontFamily, 16);
         l.TextAlign = ContentAlignment.MiddleCenter;
         l.Visible = false;
         _theForm.Controls.Add(l);
      }

      public void AxisDefault(Label l, int xpos, string txt, int n)
      {
         l.Tag = n;
         l.Location = new Point(xpos, 60 + n * 60);
         l.Size = new Size(107, 59);
         l.Text = txt;
         l.ForeColor = Color.White;
         l.BackColor = bg1;
         l.Font = new Font(l.Font.FontFamily, 16);
         l.TextAlign = ContentAlignment.MiddleLeft;
         l.Visible = false;
         _theForm.Controls.Add(l);
      }

      public void PanelDefault(Label l, int xpos, string txt, int n)
      {
         l.Tag = n;
         l.Location = new Point(xpos, 60 + n * 60);
         l.Size = new Size(134, 59);
         l.Text = txt;
         l.ForeColor = Color.White;
         l.BackColor = bg1;
         l.Font = new Font(l.Font.FontFamily, 16);
         l.TextAlign = ContentAlignment.MiddleLeft;
         l.Visible = false;
         _theForm.Controls.Add(l);
      }

      public void pbDefault(PictureBox p)
      {
         _theForm.Controls.Add(p);
      }

      public void tbDefault(TextBox t)
      {
         _theForm.Controls.Add(t);
      }

      public lb(Form1 theForm)
      {
         _theForm = theForm;
      }
      public lb()
      {
      }

   }


   public class MenuSetup
   {
//      Omron plc;
        Omron plc = new Omron();
        //                                                                Z   Index  Rod  Coro    W     C     M     Y     K  Vanish

        public static double[] AxisMaxPos = new double[c.NofAxis]       { 21, 9999, 9999, 330, 250, 250, 250, 250, 250, 250, 250 };

        public static double[] AxisSpeedMax = new double[c.NofAxis]     { 10, 80, 100, 3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000 };
        public static double[] AxisSpeedNormal = new double[c.NofAxis]  { 10, 30, 400, 100, 100, 100, 100, 100, 100, 100,  10 };
        public static double[] AxisSpeedHome = new double[c.NofAxis]    { 10, 15, 100, 20, 20, 20, 20, 20, 20, 20, 20 };

        public static double[] AxisMomentMax = new double[c.NofAxis]    { -100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 };
        public static double[] AxisMomentMin = new double[c.NofAxis]    { -100, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
        public static double[] AxisMomentHome = new double[c.NofAxis]   { -100, -10, -10, -30, -30, -30, -30, -30, -30, -30, -30 };
        public static double[] AxisMomentNormal = new double[c.NofAxis] { -100, 50, 20, 20, 20, 20, 20, 20, 20, 20,  20 };

        public static double[] AxisStartPos = new double[c.NofAxis]     { 5/*75*/, 0, 0, 330, 250, 250, 250, 250, 250, 250, 250 };

        public static double[] AxisActPos = new double[c.NofAxis]       { 0, 0, 0, 330, 275, 0, 0, 0, 0, 0, 0 };
        //public static double[] AxisActPos = new double[c.NofAxis] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


        public void InitMachine()
        {
            for (byte i = 0; i < 10; i++)
            {
                plc.FinsWrite(300 + i * 4, AxisMomentNormal[i]);
                plc.FinsWrite(340 + i * 4, AxisMomentHome[i]);
                plc.FinsWrite(380 + i * 4, AxisSpeedNormal[i]);
                plc.FinsWrite(420 + i * 4, AxisSpeedHome[i]);
                plc.FinsWriteBit(5, i, 1);  // enable
                System.Threading.Thread.Sleep(1000);
            }
        }

        public void AllAxisHome()
        {
            byte AxisNr = 0;

            for (AxisNr = 0; AxisNr < 10; AxisNr++)
            {
                if(AxisNr == 1)//Index
                {

                }
                else if (AxisNr == 2)//Rod
                {

                }
                else
                {
                    plc.FinsWrite(340 + AxisNr * 4, AxisMomentHome[AxisNr]);
                    plc.FinsWrite(420 + AxisNr * 4, AxisSpeedHome[AxisNr]);
                    plc.FinsWriteBit(5, AxisNr, 1);  // enable
                    if (AxisNr == 0) // Z
                    {
                        System.Threading.Thread.Sleep(2000);

                    }
                    plc.FinsWriteBit(3, AxisNr, 1);  // Start home
                    if (AxisNr == 0) // Z
                    {
                        System.Threading.Thread.Sleep(2000);
                    }

                }

            }

        }
        void Init()
      {


      }
   }
}
