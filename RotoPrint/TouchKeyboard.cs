using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace RotoPrint
{
   public class TouchKeyboard
   {
      lb lb = new lb();

      public static Label lbAkeyHost = new Label();
      public static Label lbNkeyHost = new Label();

      static Label[] lbAkey = new Label[40];
      static Label[] lbNkey = new Label[16];

      private string[] TxtAkey = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "A", "S", "D", "F", "G", "H", "J", "K", "L", "Esc", "Shift", "Z", "X", "C", "V", "B", "N", "M", "Del", "Ok" };
      private string[] TxtNkey = new string[] { "1", "2", "3", "Esc", "4", "5", "6", "", "7", "8", "9", "Del", "", "0", ".", "Ok" };

      public void Init()
      {
         for (int i = 0; i < 40; i++)
         {
            lbAkey[i] = new Label();
            lb.KeyDefault(lbAkey[i], TxtAkey[i], i);
            lbAkey[i].Click += new System.EventHandler(this.lbAkey_Click);
         }
         lbAkey[29].BackColor = Color.Red;
         lbAkey[39].BackColor = Color.Green;

         for (int i = 0; i < 16; i++)
         {
            lbNkey[i] = new Label();
            lb.KeyDefault(lbNkey[i], TxtNkey[i], i);
            lbNkey[i].Click += new System.EventHandler(this.lbNkey_Click);
         }
         lbNkey[3].BackColor = Color.Red;
         lbNkey[15].BackColor = Color.Green;
      }

      public void ShowAkey(int y)
      {
         lbAkeyHost.BackColor = Color.DodgerBlue;
         for (int i = 0; i < 40; i++)
         {
            lbAkey[i].Location = new Point((i % 10) * 108, y + (i / 10) * 60);
            lbAkey[i].Visible = true;
         }
      }

      public void HideAkey()
      {
         lbAkeyHost.BackColor = Color.FromArgb(255, 80, 80, 80);
         for (int i = 0; i < 40; i++)
         {
            lbAkey[i].Visible = false;
         }
      }

      public void ShowNkey(int x, int y, bool Deci)
      {
         lbNkeyHost.BackColor = Color.DodgerBlue;
         if (Deci) lbNkey[14].Text = "."; else lbNkey[14].Text = "";
         for (int i = 0; i < 16; i++)
         {
            lbNkey[i].Location = new Point(x + (i % 4) * 108, y + (i / 4) * 60);
            lbNkey[i].Visible = true;
         }
      }

      public void HideNkey()
      {
         lbNkeyHost.BackColor = Color.FromArgb(255, 80, 80, 80);
         for (int i = 0; i < 16; i++)
         {
            lbNkey[i].Visible = false;
         }
      }

      private void lbAkey_Click(object sender, EventArgs e)
      {
         Label lbl = (Label)sender;
         int Key = (int)lbl.Tag;
         if (Key == 29 || Key == 30 || Key == 38 || Key == 39)
         {
            if (Key == 38)
            {
               int len = lbAkeyHost.Text.Length;
               if (len > 0)
                  lbAkeyHost.Text = lbAkeyHost.Text.Substring(0, len - 1);
            }
            if (Key == 39)
            {
               HideAkey();
               lbAkeyHost.Focus();
               SendKeys.Send("{ENTER}");
            }
            if (Key == 29)
            {
               HideAkey();
               lbAkeyHost.Focus();
               SendKeys.Send("{ESCAPE}");
            }
         }
         else
            lbAkeyHost.Text = lbAkeyHost.Text + TxtAkey[Key];
      }

      private void lbNkey_Click(object sender, EventArgs e)
      {
         Label lbl = (Label)sender;
         int Key = (int)lbl.Tag;

         if (Key == 3 || Key == 11 || Key == 15)
         {
            if (Key == 3) // ESC
            {
               HideNkey();
               lbAkeyHost.Focus();
               SendKeys.Send("{ESCAPE}");
            }

            if (Key == 11) // DEL
            {
               int len = lbNkeyHost.Text.Length;
               if (len > 0)
                  lbNkeyHost.Text = lbNkeyHost.Text.Substring(0, len - 1);
            }

            if (Key == 15) // Ok
            {
               HideNkey();
               lbNkeyHost.Focus();
               SendKeys.Send("{ENTER}");
            }

         }
         else
            lbNkeyHost.Text = lbNkeyHost.Text + TxtNkey[Key];
      }
   }
}
