using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace RotoPrint
{
    class MenuDebug
    {
        Omron plc = new Omron();
        MenuSetup Setup;
        TouchKeyboard kb = new TouchKeyboard();
        lb lb = new lb();
        private Timer DbgTimer;


        Color bg1 = Color.FromArgb(255, 80, 80, 80);

        static int NofAxis = 10;
        public Label[] lbAxisName = new Label[NofAxis];
        public Label[] lbAxisPos = new Label[NofAxis];
        public Label[] lbAxisGoto = new Label[NofAxis];
        public Label[] lbAxisHome = new Label[NofAxis];


        string[] TxtAxisName = new string[] { "Z Axis", "Index", "Rod", "Corona", "White", "Cyan", "Magenta", "Yellow", "Black", "Vanish" };

        static int NofPanels = 6;
        string[] TxtPanelName = new string[] { "White", "Cyan", "Magenta", "Yellow", "Black", "Vanish" };

        public Label[] lbPanelName = new Label[NofPanels];

        static int NofPanelInkLeds = 30;
        public Label[] lbPanelInkLed = new Label[NofPanelInkLeds];

        static int NofPanelInkKeys = 84;
        public Label[] lbPanelInkKey = new Label[NofPanelInkKeys];
        string[] TxtPanelInkKey = new string[] { "Power", "Speed", "Reset", "In", "Out", "Down", "Up", "Park", "Clean", "Flush", "Purge", "Drop Watch", "Print", "Vacuum" };


        static int NofTest = 5;
        public Label[] lbTest = new Label[NofTest];
        string[] TxtTest = new string[] { "Emergency STOP", "Enable Z", "Quit", "Scan", "Disable I" };


        public void Init()
        {
            DbgTimer = new Timer();
            DbgTimer.Interval = 100;
            DbgTimer.Enabled = false;
            DbgTimer.Tick += new System.EventHandler(DbgTimer_Tick);

            for (int i = 0; i < NofAxis; i++)
            {
                lbAxisName[i] = new Label();
                lb.AxisDefault(lbAxisName[i], 0, TxtAxisName[i], i);

                lbAxisPos[i] = new Label();
                lb.AxisDefault(lbAxisPos[i], 108, "---.--", i);
                lbAxisPos[i].TextAlign = ContentAlignment.MiddleRight;


                lbAxisGoto[i] = new Label();
                lb.AxisDefault(lbAxisGoto[i], 216, "", i);
                lbAxisGoto[i].TextAlign = ContentAlignment.MiddleRight;
                lbAxisGoto[i].Click += new System.EventHandler(lbAxisGoto_Click);
                lbAxisGoto[i].PreviewKeyDown += new PreviewKeyDownEventHandler(lbAxisGoto_PreviewKeyDown);


                lbAxisHome[i] = new Label();
                lb.AxisDefault(lbAxisHome[i], 324, "Home", i);
                lbAxisHome[i].Click += new System.EventHandler(lbAxisHome_Click);
            }

            int[] LedPos = new int[] { 0, 2, 7, 12, 13 };

            for (int i = 0; i < NofPanels; i++)
            {
                lbPanelName[i] = new Label();
                lb.PanelDefault(lbPanelName[i], 0, TxtPanelName[i], i);
                lbPanelName[i].Location = new Point(180 * i, 660);    // 135
                lbPanelName[i].Size = new Size(179, 59);              //134    
                lbPanelName[i].TextAlign = ContentAlignment.MiddleCenter;
            }

            for (int i = 0; i < NofPanelInkLeds; i++)
            {
                lbPanelInkLed[i] = new Label();
                lb.PanelDefault(lbPanelInkLed[i], 0, "", i);
                lbPanelInkLed[i].Location = new Point((i / 5) * 180, 720 + LedPos[(i % 5)] * 60);
                lbPanelInkLed[i].Size = new Size(59, 59);
                lbPanelInkLed[i].Click += new System.EventHandler(lbPanelInkLed_Click);
            }

            for (int i = 0; i < NofPanelInkKeys; i++)
            {
                lbPanelInkKey[i] = new Label();
                lb.PanelDefault(lbPanelInkKey[i], 0, TxtPanelInkKey[i % 14], i);
                lbPanelInkKey[i].Location = new Point(60 + (i / 14) * 180, 720 + (i % 14) * 60);
                lbPanelInkKey[i].Size = new Size(119, 59);
            }

            for (int i = 0; i < NofTest; i++)
            {
                lbTest[i] = new Label();
                lb.TestDefault(lbTest[i], 0, TxtTest[i], i);
                lbTest[i].Size = new Size(107, 59);
                lbTest[i].Location = new Point(972, 60 + i * 60);
                lbTest[i].Click += new System.EventHandler(lbTest_Click);
            }
            lbTest[0].BackColor = Color.Red;


        }

        private void lbPanelInkLed_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            int n = (int)lbl.Tag;
            byte PanelNr = (byte)(n / 5);
            int LedNr = n % 5;
            int Leds = 0;
            plc.FinsReadBits(40 + LedNr, ref Leds);

            if (LedNr == 1)
            {
                int Tmp = (Leds >> PanelNr) & 0x101;

                if (Tmp == 0)
                {
                    plc.FinsWriteBit(40 + LedNr, PanelNr, 1);
                    plc.FinsWriteBit(40 + LedNr, (byte)(PanelNr + 8), 0);
                    lbPanelInkLed[n].BackColor = Color.Green;
                }
                else if (Tmp == 0x001)
                {
                    plc.FinsWriteBit(40 + LedNr, PanelNr, 0);
                    plc.FinsWriteBit(40 + LedNr, (byte)(PanelNr + 8), 1);
                    lbPanelInkLed[n].BackColor = Color.Red;
                }
                else if (Tmp == 0x100)
                {
                    plc.FinsWriteBit(40 + LedNr, PanelNr, 1);
                    plc.FinsWriteBit(40 + LedNr, (byte)(PanelNr + 8), 1);
                    lbPanelInkLed[n].BackColor = Color.Yellow;
                }
                else
                {
                    plc.FinsWriteBit(40 + LedNr, PanelNr, 0);
                    plc.FinsWriteBit(40 + LedNr, (byte)(PanelNr + 8), 0);
                    lbPanelInkLed[n].BackColor = bg1;
                }
            }
            else
            {
                if ((Leds & (1 << PanelNr)) > 0)
                {
                    plc.FinsWriteBit(40 + LedNr, PanelNr, 0);
                    lbPanelInkLed[n].BackColor = bg1;
                }
                else
                {
                    plc.FinsWriteBit(40 + LedNr, PanelNr, 1);
                    lbPanelInkLed[n].BackColor = Color.Green;
                }
            }
        }

        private void lbAxisGoto_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            byte AxisNr = (byte)(int)lbl.Tag;
            kb.HideNkey();
            TouchKeyboard.lbNkeyHost = (Label)sender;
            kb.ShowNkey(540, 60, true);
        }

        private void lbAxisGoto_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            Label lbl = TouchKeyboard.lbNkeyHost;
            byte AxisNr = (byte)(int)lbl.Tag;
            kb.HideNkey();

            if (e.KeyCode == Keys.Enter)
            {
                Double Pos = Convert.ToDouble(lbl.Text, System.Globalization.CultureInfo.InvariantCulture);
                if (Pos >= 0.0 && Pos <= MenuSetup.AxisMaxPos[AxisNr])
                {
                    plc.FinsWrite(140 + AxisNr * 4, Pos);
                    plc.FinsWriteBit(5, AxisNr, 1);  // enable
                    plc.FinsWriteBit(9, AxisNr, 1);  // start move z
                }
            }
        }

        private void lbAxisHome_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            byte AxisNr = (byte)(int)lbl.Tag;

            plc.FinsWrite(340 + AxisNr * 4, MenuSetup.AxisMomentHome[AxisNr]);
            plc.FinsWrite(420 + AxisNr * 4, MenuSetup.AxisSpeedHome[AxisNr]);
            plc.FinsWriteBit(5, AxisNr, 1);  // enable
            plc.FinsWriteBit(3, AxisNr, 1);  // Start home
        }



        private void lbTest_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            int TestNr = (int)lbl.Tag;


            if (TestNr == 0)
            {
                plc.FinsWriteBits(0, 0x3ff);  // Block all
                                              //                plc.FinsWriteBits(5, 0x3ff);  // Disable all
            }

            if (TestNr == 1)
            {
                plc.FinsWriteBit(5, 0, 1);  // enable
            }


            if (TestNr == 2)
            {
                Application.Exit();
            }

            if (TestNr == 3)
            {
                if (DbgTimer.Enabled == true)
                {
                    DbgTimer.Enabled = false;
                    lbTest[3].BackColor = bg1;
                }
                else
                {
                    DbgTimer.Enabled = true;
                    lbTest[3].BackColor = Color.Green;
                }
            }
            if (TestNr == 4)
            {
                plc.FinsWriteBit(5, 1, 0);  // disable index enable
            }

        }

        private void FileOpen()
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\RotaJet\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;


                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    //Read the contents of the file into a stream
                    var fileStream = openFileDialog.OpenFile();

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }

            MessageBox.Show(fileContent, "File Content at path: " + filePath, MessageBoxButtons.OK);
        }

        public void ShowMenu(bool Show)
        {
            for (int i = 0; i < NofAxis; i++)
            {
                lbAxisName[i].Visible = Show;
                lbAxisPos[i].Visible = Show;
                lbAxisGoto[i].Visible = Show;
                lbAxisHome[i].Visible = Show;
            }

            for (int i = 0; i < NofPanels; i++)
                lbPanelName[i].Visible = Show;

            for (int i = 0; i < NofPanelInkLeds; i++)
                lbPanelInkLed[i].Visible = Show;

            for (int i = 0; i < NofPanelInkKeys; i++)
                lbPanelInkKey[i].Visible = Show;

            for (int i = 0; i < NofTest; i++) lbTest[i].Visible = Show;
        }

        int[] OldKey = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        int AniCount = 0;

        private void DbgTimer_Tick(object sender, EventArgs e)
        {
            int[] Key = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            if (!Error.Show())
            {
                for (int j = 0; j < 14; j++)
                {
                    plc.FinsReadBits(20 + j, ref Key[j]);
                    if (Key[j] != OldKey[j])
                    {
                        OldKey[j] = Key[j];

                        for (int i = 0; i < 6; i++)
                        {
                            if ((Key[j] & (1 << i)) > 0)
                                lbPanelInkKey[i * 14 + j].BackColor = Color.Green;
                            else
                                lbPanelInkKey[i * 14 + j].BackColor = bg1;
                        }
                    }
                    if (Key[j] > 0) plc.FinsWriteBits(20 + j, 0);
                }

                int ServoRdyStatus = 0;
                plc.FinsReadBits(7, ref ServoRdyStatus);
                for (int i = 0; i < 10; i++)
                {
                    if ((ServoRdyStatus & (0x0001 << i)) > 0)
                        lbAxisName[i].BackColor = Color.Green;
                    else
                        lbAxisName[i].BackColor = Color.Red;
                }

                double AxisActPos = 0;

                for (int j = 0; j < 10; j++)
                {
                    plc.FinsRead(100 + j * 4, ref AxisActPos);
                    lbAxisPos[j].Text = String.Format("{0:0.00}", AxisActPos);
                }

                AniCount++;
                if (AniCount >= 5)
                {
                    AniCount = 0;
                    if (lbTest[3].BackColor == bg1)
                        lbTest[3].BackColor = Color.Gray;
                    else
                        lbTest[3].BackColor = bg1;
                }
            }
        }
    }
}