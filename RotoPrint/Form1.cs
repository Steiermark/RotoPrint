using System;
using System.Drawing;
using System.Windows.Forms;
using JMPHS;

namespace RotoPrint
{

   public partial class Form1 : Form
   {
      private lb lb;
      private Error er;
      private MenuPrint mp;
      private MenuJob mj;
      private MenuInk mi;
      private MenuDebug md;
      private MenuSetup ms;

      private TouchKeyboard kb;
      private Omron plc;
      private PHCInterface phc;

      private const int NofMenu = 5;
      public Label[] lbMenu = new Label[NofMenu];
      string[] TxtMenu = new string[] { "Print", "Job", "Ink", "Setup", "Debug" };
      public int MenuNr = 0;


      public Form1()
      {
         InitializeComponent();
      }

      private void Form1_Load(object sender, EventArgs e)
      {
         this.Size = new Size(1080, 1920);
         this.BackColor = Color.FromArgb(255, 50, 50, 50);
         this.Location = new Point(0, 0);

         lb = new lb(this);
         er = new Error();
         kb = new TouchKeyboard();
         mp = new MenuPrint();
         mj = new MenuJob(this);
         mi = new MenuInk();
         md = new MenuDebug();
         ms = new MenuSetup();

         er.Init();
         kb.Init();
         mp.Init();
         mj.Init();
         mi.Init();
         md.Init();
         Init();
      }

      public void Init()
      {
         for (int i = 0; i < NofMenu; i++)
         {
            lbMenu[i] = new Label();
            lb.MenuDefault(lbMenu[i], 0, TxtMenu[i], i);
            lbMenu[i].Click += new System.EventHandler(this.lbMenu_Click);
            lbMenu[i].Visible = true;
         }
         lbMenu[0].ForeColor = Color.White;
         lbMenu[0].BackColor = Color.DodgerBlue;
         ShowMainMenu(true);
         mp.ShowMenu(true);
      }


        public void ShowMainMenu(bool Show)
      {
         for (int i = 0; i < NofMenu; i++)
            lbMenu[i].Visible = Show;
      }

      private void lbMenu_Click(object sender, EventArgs e)
      {
         Label lbl = (Label)sender;
         MenuNr = (int)lbl.Tag;

         //            _theForm.timer1.Enabled = false;
         kb.HideAkey();
         kb.HideNkey();

         bool[] MenuActive = new bool[5] { false, false, false, false, false };
         MenuActive[MenuNr] = true;

         for (int i = 0; i < 5; i++)
         {
            if (i == MenuNr)
            {
               lbMenu[MenuNr].ForeColor = Color.White;
               lbMenu[MenuNr].BackColor = Color.DodgerBlue;
            }
            else
            {
               lbMenu[i].ForeColor = Color.DodgerBlue;
               lbMenu[i].BackColor = Color.Black;
            }
         }

         mp.ShowMenu(MenuActive[0]);
         mj.ShowMenu(MenuActive[1]);
         mi.ShowMenu(MenuActive[2]);
         md.ShowMenu(MenuActive[4]);
      }
   }
}



