using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace RotoPrint
{

    class MenuPrint
    {
        lb lb = new lb();
       // Omron plc;
        Omron plc = new Omron();

        MenuSetup Setup = new MenuSetup();

        AutoMenuJob Amj = new AutoMenuJob();
        int PrintJobCntTotal = 0;
        int PrintJobCnt = 0;

            private Timer PrintTimer;

        private byte aZ = 0;
        private byte aIndex = 1;
        private byte aRod = 2;
        private byte aCorona = 3;

        private Double[] AxisActPos = new double[c.NofAxis];
        private int ServoRdyStatus = 0;
        private int RodStatus = 0;
        private int IndexStatus = 0;

        private enum ProdStates { Stopped, Running, Paused, WaitToPause, WaitToStop };
        enum PrintStates { Init, WaitForZhome, WaitForZprint, NextIndex, WaitForIndex, StartRod, WaitForRod, PrintStart, Print, WaitForOperator, PrintPaused, PrintStopping, WaitForStopZhome, WaitForStopYhome, DrivesNotReady };

        private PrintStates PrintState = PrintStates.Init;
        private ProdStates ProdState = ProdStates.Stopped;


        private int RodCountDown;
        private int PrintSimTimer;
        private int OperatorTimer;
        private int HomeTimer;

        private Double AxisIndexSetPos;
        private Double PosCoronaEnd = 190;

        private const int NofKey = 4;
        private Label[] lbKey = new Label[NofKey];

        const int NofParam = 16;
        private Label[] lbParam = new Label[NofParam];
        private Label[] lbParamTxt = new Label[NofParam];
        private string[] TxtParam = new string[] { "State", "Z-Axiz", "Index", "Rod", "Corona", "White", "Cyan", "Magenta", "Yellow", "Black", "Curing", "", "", "", "", "" };

        const int NofStatusNav = 3;
        private Label[] lbStatusNav = new Label[NofStatusNav];
        private string[] TxtStatusNav = new string[] { "Prev", "Status", "Next" };

        private const int NofStatus = 20;
        private Label[] lbStatus = new Label[NofStatus];

        private static byte CurrentHeadPos = 0;
        
        public void Init()
        {
            AutoMenuJob Amj = new AutoMenuJob();
            Amj.lbFiles_load(); // Upload images and slice them
            PrintJobCntTotal = Amj.GetNumberOfSlices();

            //plc.FinsWriteBit(600, 1, 1); // Corona safety on;
            plc.FinsWriteBit(80, 0, 1); // Power on;
            plc.FinsWriteBit(80, 1, 1); // Intermittent on;
            plc.FinsWriteBit(80, 9, 0); // Timein off;

            PrintJobCnt = 0;
            CurrentHeadPos = 0;
            Setup.AllAxisHome();
            PrintTimer = new Timer();
            PrintTimer.Interval = 100;
            PrintTimer.Enabled = false;
            PrintTimer.Tick += new System.EventHandler(PrintTimer_Tick);
            
            for (int i = 0; i < NofStatusNav; i++)
            {
                lbStatusNav[i] = new Label();
                lb.MenuDefault(lbStatusNav[i], 60, TxtStatusNav[i], i);
                lbStatusNav[i].Location = new Point(432 + i * 216, 60);
                lbStatusNav[i].Click += new System.EventHandler(this.lbStatusNav_Click);
            }

            for (int i = 0; i < NofStatus; i++)
            {
                lbStatus[i] = new Label();
                lb.TxtDefault(lbStatus[i], "", i);
                lbStatus[i].Location = new Point(432, 120 + i * 60);
                lbStatus[i].Size = new Size(657, 59);
                //            lbPrintStatus[i].Click += new System.EventHandler(this.lbPrintStatus_Click);
            }

            for (int i = 0; i < NofParam; i++)
            {
                lbParamTxt[i] = new Label();
                lb.TxtDefault(lbParamTxt[i], TxtParam[i], i);
                lbParamTxt[i].Location = new Point(0, 120 + i * 60);
                lbParamTxt[i].Size = new Size(215, 59);

                lbParam[i] = new Label();
                lb.NumDefault(lbParam[i], "", i);
                lbParam[i].Location = new Point(216, 120 + i * 60);
                lbParam[i].Size = new Size(215, 59);
            }

            string[] TxtKey = new string[] { "Emergency STOP", "Step", "Pause", "Start" };

            for (int i = 0; i < NofKey; i++)
            {
                lbKey[i] = new Label();
                lb.TxtDefault(lbKey[i], TxtKey[i], i);

                lbKey[i].Location = new Point(864, 1440 + i * 120);
                lbKey[i].Size = new Size(216, 120);
//                lbKey[i].BackColor = Color.DodgerBlue;
                lbKey[i].Visible = true;
                lbKey[i].Click += new System.EventHandler(this.lbKey_Click);
            }
            lbKey[0].BackColor = Color.Red;
            //        btnMenuPrint[2].Visible = false;
            lbKey[2].Visible = false;
            lbKey[3].BackColor = Color.Green;

     
        }


        private void GetAxisActPos()
        {
            for (int i = 0; i < c.NofAxis; i++)
            {
                plc.FinsRead(100 + i * 4, ref AxisActPos[i]);
                AxisActPos[i] = Math.Round(AxisActPos[i], 1);
            }
            plc.FinsReadBits(7, ref ServoRdyStatus);
        }

        private void ShowAxisActPos()
        {
            for (int i = 0; i < 10; i++) lbParam[i + 1].Text = AxisActPos[i].ToString();
            lbParam[11].Text = ServoRdyStatus.ToString("X4");
        }

        private void CheckHome()
        {
        }

        // FliFlop Corona
        static bool CoronaFlipFlop = false;

        private void StartCorona()
        {
            if(CoronaFlipFlop == false)
            {
                plc.FinsWriteBit(80, 9, 1); // Timein on;
                plc.FinsWrite(140 + aCorona * 4, PosCoronaEnd);
                plc.FinsWriteBit(5, aCorona, 1);  // enable
                plc.FinsWriteBit(9, aCorona, 1);  // start move 
                //CoronaFlipFlop = true;
            }
            else
            {
                plc.FinsWriteBit(80, 9, 1); // Timein on;
                plc.FinsWrite(140 + aCorona * 4, MenuSetup.AxisStartPos[3]);
                plc.FinsWriteBit(5, aCorona, 1);  // enable
                plc.FinsWriteBit(9, aCorona, 1);  // start move 
                CoronaFlipFlop = false;
            }
        }

        private void StopCorona()
        {
            plc.FinsRead(100 + aCorona * 4, ref AxisActPos[aCorona]);
            AxisActPos[aCorona] = Math.Round(AxisActPos[aCorona], 1);
            if (AxisActPos[aCorona] == PosCoronaEnd)
            {
                plc.FinsWriteBit(80, 9, 0); // Timein off;
            }

        }

        private void PrintTimer_Tick(object sender, EventArgs e)
        {
            if (ProdState == ProdStates.WaitToPause)
                lbKey[1].BackColor = Color.White;
            if (ProdState == ProdStates.WaitToStop)
                lbKey[2].BackColor = Color.White;


            lbParam[0].Text = PrintState.ToString();
            switch (PrintState)
            {
                case PrintStates.Init:
                    Setup.InitMachine();
                    //Ms.InitMachine();
                    GetAxisActPos();
                    ShowAxisActPos();

                    bool HomeAll = true;
                    if (AxisActPos[aZ] != 0) HomeAll = false;
                    for (int i = 3; i < 10; i++) if (AxisActPos[i] != 0) HomeAll = false;
                    //if ((ServoRdyStatus & 0b1111111001) != 0b1111111001) HomeAll = false;
                    if ((ServoRdyStatus & 0b1111100111) != 0b1111100111) HomeAll = false;
                    HomeAll = true;
                    if (HomeAll == false)
                    {
                        if ((ServoRdyStatus & 1) == 0 || AxisActPos[aZ] != 0)
                        {
                            //                                plc.FinsWriteBit(5, aZ, 1);  // enable
                           // plc.FinsWriteBit(3, aZ, 1);  // Start home
                            PrintState = PrintStates.WaitForZhome;

                            plc.FinsWrite(140 + aZ * 4, MenuSetup.AxisStartPos[aZ]);
                            //                           plc.FinsWriteBit(5, aZ,1);  // enable
                            plc.FinsWriteBit(5, aZ, 1);  // enable
                            System.Threading.Thread.Sleep(1000);
                            plc.FinsWriteBit(3, aZ, 1);  // Start home
                           // System.Threading.Thread.Sleep(1000);
                        }
                        else
                        {
                            if (HomeTimer > 0)
                                HomeTimer--;
                            else
                            {
                                for (byte i = 3; i < 10; i++)  // TO DO index rod
                                {
                                    if ((ServoRdyStatus & (1 << i)) == 0 || AxisActPos[i] != 0)
                                    {
                                        //                                            plc.FinsWriteBit(5, i, 1);  // enable
                                        plc.FinsWriteBit(3, i, 1);  // Start home
                                        if((10+i) < 16)
                                            lbParam[10 + i].Text = i + " Homed";
                                    }
                                }
                                HomeTimer = 10;
                            }
                        }
                    }
                    else
                    {
                        //Zpos
                        plc.FinsWrite(140 + aZ * 4, MenuSetup.AxisStartPos[aZ]);
                        plc.FinsWriteBit(5, aZ,1);  // enable
                        System.Threading.Thread.Sleep(1000);
                        plc.FinsWriteBit(9, aZ, 1);  // start move z
                        PrintState = PrintStates.WaitForZprint;
                        System.Threading.Thread.Sleep(1000);

                    }
                    break;

                case PrintStates.WaitForZhome:
                    plc.FinsRead(100 + aZ * 4, ref AxisActPos[aZ]);
                    AxisActPos[aZ] = Math.Round(AxisActPos[aZ], 1);

                    plc.FinsReadBits(7, ref ServoRdyStatus);
                    if ((ServoRdyStatus & 1) == 1 && AxisActPos[aZ] == 0)
                            PrintState = PrintStates.Init;
                    break;

                case PrintStates.WaitForZprint:
                    plc.FinsRead(100 + aZ * 4, ref AxisActPos[aZ]);
                    AxisActPos[aZ] = Math.Round(AxisActPos[aZ], 3);
                    plc.FinsReadBits(7, ref ServoRdyStatus);
                    if ((ServoRdyStatus & 1) == 1 && AxisActPos[aZ] == MenuSetup.AxisStartPos[aZ])
                            PrintState = PrintStates.NextIndex;

                    break;

                case PrintStates.NextIndex:
                    GetAxisActPos();
                    ShowAxisActPos();

                    if ((ServoRdyStatus & 0b111111001) == 0b111111001)
                    //if ((ServoRdyStatus & 0b1111100111) == 0b1111100111)
                    {
                        AxisIndexSetPos = AxisActPos[aIndex];

                        AxisIndexSetPos += 30;
                        AxisIndexSetPos %= 360;
                        plc.FinsWrite(140 + aIndex * 4, AxisIndexSetPos);
                        plc.FinsWriteBit(9, aIndex, 1);

                        /*
                        for (int i = 3; i < 10; i++) plc.FinsWrite(140 + i * 4, MenuSetup.AxisStartPos[i]);
                        plc.FinsWriteBits(5, 0b1111111111);  // enable
                        plc.FinsWriteBits(9, 0b1111111010);  // start move all axis exept z and rod
                        */

                        PrintState = PrintStates.WaitForIndex;
                    }
                    //else
                      //  PrintState = PrintStates.DrivesNotReady;
                    break;

                case PrintStates.WaitForIndex:
                    GetAxisActPos();
                    ShowAxisActPos();

                    plc.FinsReadBits(9, ref IndexStatus);

                    if((IndexStatus & 0x02) == 0) // Index finshed to move ?
                    {
                        lbParam[12].Text = AxisIndexSetPos.ToString();

                        bool AllPositionsReached = true;
                        //if (AxisActPos[aIndex] != AxisIndexSetPos) AllPositionsReached = false;
                        //for (int i = 3; i < 10; i++) if (AxisActPos[i] != MenuSetup.AxisStartPos[i]) AllPositionsReached = false;
                        for (int i = 3; i < 10; i++)
                        {
                            if (AxisActPos[i] != MenuSetup.AxisStartPos[i])
                            {
                                AllPositionsReached = false;
                            }
                        }
                        //AllPositionsReached = true;
                        // Move print-heads to start position   
                        for (int i = 3; i < 10; i++)
                        {
                            if ((MenuSetup.AxisStartPos[i] <= 250) ||
                                (MenuSetup.AxisStartPos[i] <= 330 && i == 3))    // MAx check
                            {
                                MenuSetup.AxisActPos[i] = MenuSetup.AxisStartPos[i];
                                plc.FinsWrite(140 + i * 4, MenuSetup.AxisStartPos[i]);
                                //plc.FinsWriteBit(5, (byte)i, 1);  // enable
                                plc.FinsWriteBit(9, (byte)i, 1);  // start move 
                                //System.Threading.Thread.Sleep(1000);
                            }
                            else
                            {
                                PrintState = PrintStates.DrivesNotReady;
                            }
                        }
                        CurrentHeadPos = 0;
                        System.Threading.Thread.Sleep(1000);

                        // Zero position the Rod
                        plc.FinsWriteBit(5, aRod, 0);  // disable
                        System.Threading.Thread.Sleep(1000);
                        plc.FinsWriteBit(5, 2, 0); // Set Zero position Rod
                        plc.FinsWriteBit(5, 2, 1); // Enable Rod
                        System.Threading.Thread.Sleep(1000);
                        
                        if (AllPositionsReached) PrintState = PrintStates.StartRod;

                    }

                    break;

                case PrintStates.StartRod:
                    
                    plc.FinsReadBits(9, ref RodStatus);

                    if(RodStatus == 0)
                    {
                        plc.FinsReadBits(7, ref ServoRdyStatus);
                        if ((ServoRdyStatus & 0b1111111001) == 0b1111111001)
                        {
                            //plc.FinsWriteBit(5, aRod, 0);  // stop Rod
                            // plc.FinsWriteBit(0, aRod, 1);  // Block Run
                            //                        plc.FinsWriteBit(9, aRod, 0);  // rod stop
                            //plc.FinsWrite(140 + aRod * 4, AxisActPos[aRod] + 10000);
                            AxisActPos[aRod] = 3600; // PLC handle reset of Rod position... 
                            plc.FinsWrite(140 + aRod * 4, (double)(AxisActPos[aRod]));//(360*5* PrintJobCntTotal)));

                            plc.FinsWriteBit(5, aRod, 1);  // enable
                            plc.FinsWriteBit(0, aRod, 0);  // Run Block off
                            plc.FinsWriteBit(9, aRod, 1);  // rod
                            RodCountDown = 10;

                            if (CurrentHeadPos == 0)
                                StartCorona();

                            PrintState = PrintStates.WaitForRod;
                        }
                        else
                        {
                            PrintState = PrintStates.DrivesNotReady;
                        }

                    }

                    break;

                case PrintStates.WaitForRod:

                    plc.FinsReadBits(9, ref RodStatus);
                    if ((RodStatus & 0x04) == 0) // Rod running, wait for it to stop
                    {
                        PrintState = PrintStates.PrintStart;
                    }
                    /*
                    if (RodCountDown > 0)
                        RodCountDown--;
                    else
                        PrintState = PrintStates.PrintStart;
                        */
                    break;

                case PrintStates.PrintStart:
                    plc.FinsReadBits(7, ref ServoRdyStatus);
                    if ((ServoRdyStatus & 0b1111111001) == 0b1111111001)
                    {
                        /*
                        plc.FinsWriteBit(49, 4, 1); // Corona On;
                        plc.FinsWrite(140 + aCorona * 4, PosCoronaEnd);
                        plc.FinsWriteBit(5, aCorona, 1);  // enable
                        plc.FinsWriteBit(9, aCorona, 1);  // start move 
                                                          //                       for (int i=0;i< 6;i++) PhcPrintImage(i,0);
                                                          */
                        Amj.lbUpload_Slice(PrintJobCnt);
                        PrintJobCnt++;

                        for (int i= 0; i < 6; i++)
                        {
                            Amj.SendSensorSignal(i);
                            System.Threading.Thread.Sleep(100);
                        }
                        
                        PrintSimTimer = 40;
                        PrintState = PrintStates.Print;
                    }
                    else
                            PrintState = PrintStates.DrivesNotReady;
                    break;

                case PrintStates.Print:
                    StopCorona();

                    if(Amj.phc_AllPrintHeadDone())
                    {
                        CurrentHeadPos++;
                        OperatorTimer = 40;

                        if (PrintJobCnt == PrintJobCntTotal)
                        {
                            PrintJobCnt = 0;
                            PrintState = PrintStates.WaitForOperator;
                        }
                        else // Print slice handle ?
                        {
                            double pos = 0;
                            // Move each print-head 1 pixel in Y direction
                            for (int i = 4; i < 10; i++)
                            {
                                pos = MenuSetup.AxisActPos[i];

                                if(CurrentHeadPos < 4)
                                {
                                    pos -= 0.0635;// 0.1; // TBD Move one pixel
                                }
                                else
                                {
                                    
                                    pos -= ((0.0635 * 1024) +(0.0635 * 3));
                                }
                                if(pos > 50)
                                {
                                    MenuSetup.AxisActPos[i] = pos;
                                    plc.FinsWrite(140 + i * 4, pos);
                                    //plc.FinsWriteBit(5, (byte)i, 1);  // enable
                                    plc.FinsWriteBit(9, (byte)i, 1);  // start move 
                                    System.Threading.Thread.Sleep(1000);

                                }
                            }

                            if (CurrentHeadPos >= 4)
                                CurrentHeadPos = 0;
                            if (pos > 50)
                            {
                                // Zero position the Rod
                                plc.FinsWriteBit(5, 2, 0); // Set Zero position Rod
                                System.Threading.Thread.Sleep(1000);
                                plc.FinsWriteBit(5, 2, 1); // Enable Rod
                                System.Threading.Thread.Sleep(1000);
                                PrintState = PrintStates.StartRod;
                            }
                            else
                                PrintState = PrintStates.DrivesNotReady;
                        }

                    }

                    if (PrintSimTimer > 0)
                    {
                        PrintSimTimer--;
                        lbParam[13].Text = PrintSimTimer.ToString();
                    }
                    else
                    {
                        OperatorTimer = 40;
                        //plc.FinsWriteBit(5, aRod, 0);  // stop Rod
                        //plc.FinsWriteBit(0, aRod, 1);  // Block Run
                        PrintState = PrintStates.WaitForOperator;
                    }
                    break;

                case PrintStates.WaitForOperator:
                    plc.FinsReadBits(9, ref RodStatus);
                    if ((RodStatus & 0b100) == 0) // Rod running, wait for it to stop
                    {
                        if (ProdState == ProdStates.WaitToPause)
                        {
                            PrintState = PrintStates.PrintPaused;
                            StateMachine(11);
                        }
                        if (ProdState == ProdStates.WaitToStop)
                        {
                            PrintState = PrintStates.PrintStopping;
                        }
                        if (ProdState == ProdStates.Running)
                        {
                            if (OperatorTimer > 0)
                            {
                                OperatorTimer--;
                                lbParam[14].Text = OperatorTimer.ToString();
                            }
                            else
                            {
                                plc.FinsReadBits(9, ref RodStatus);
                                if ((ServoRdyStatus & 0b100) == 0) // Rod running, wait for it to stop
                                {
                                    PrintState = PrintStates.NextIndex;
                                }
                                else
                                {
                                    OperatorTimer = 40;
                                }

                            }
                        }

                    }
                    else
                    {
                        lbParam[14].Text = OperatorTimer.ToString();

                    }

                    break;

                case PrintStates.PrintPaused:

                    if (ProdState == ProdStates.Running) PrintState = PrintStates.NextIndex;
                    if (ProdState == ProdStates.WaitToStop) PrintState = PrintStates.PrintStopping;
                    break;

                case PrintStates.PrintStopping:
                    plc.FinsWriteBit(3, aZ, 1);  // Start home
                    PrintState = PrintStates.WaitForStopZhome;
                    break;

                case PrintStates.WaitForStopZhome:
                    plc.FinsRead(100 + aZ * 4, ref AxisActPos[aZ]);
                    AxisActPos[aZ] = Math.Round(AxisActPos[aZ], 1);
                    if (AxisActPos[aZ] == 0)
                    {
                        for (byte i = 3; i < 10; i++)
                            plc.FinsWriteBit(3, i, 1);  // Start all y home
                        PrintState = PrintStates.WaitForStopYhome;
                    }
                    break;

                case PrintStates.WaitForStopYhome:
                    AllAxisHome();
                    StateMachine(10);
                    PrintState = PrintStates.Init;
                    lbParam[0].Text = PrintState.ToString();
                    PrintTimer.Enabled = false;
                    /*
                    bool AllYhome = true;
                    for (byte i = 3; i < 10; i++)
                    {
                        plc.FinsRead(100 + i * 4, ref AxisActPos[i]);
                        AxisActPos[i] = Math.Round(AxisActPos[i], 1);
                        if (AxisActPos[i] != 0) AllYhome = false;
                    }

                    if (AllYhome)
                    {
                        StateMachine(10);
                        PrintState = PrintStates.Init;
                        PrintTimer.Enabled = false;
                    }
                    */
                    break;
                case PrintStates.DrivesNotReady:

                    break;
            }
        }

        public void StateMachine(int Event)
        {
            switch (ProdState)
            {
                case ProdStates.Stopped:
                    if (Event == 3)// startStop
                    {
                        //                        ShowMainMenu(false);
                        
                        lbKey[0].Visible = false;

                        lbKey[1].BackColor = Color.Yellow;
                        lbKey[1].Text = "Pause";
                        lbKey[1].Visible = true;

                        lbKey[3].BackColor = Color.Red;
                        lbKey[3].Text = "Stop";

                        PrintTimer.Enabled = true;

                        ProdState = ProdStates.Running;
                    }
                    break;
                case ProdStates.Running:
                    if (Event == 1) // Pause/Resume
                    {
                        ProdState = ProdStates.WaitToPause;
                    }

                    if (Event == 3) // Start/Stop
                    {
                        lbKey[0].Visible = false;
                        lbKey[1].Visible = false;

                        ProdState = ProdStates.WaitToStop;
                    }
                    break;

                case ProdStates.Paused:
                    if (Event == 1)  // Pause/Resume
                    {
                        lbKey[0].Visible = false;

                        lbKey[1].BackColor = Color.Yellow;
                        lbKey[1].Text = "Pause";
                        lbKey[1].Visible = true;

                        lbKey[3].BackColor = Color.Red;
                        lbKey[3].Text = "Stop";

                        ProdState = ProdStates.Running;
                    }
                    if (Event == 3)  // Run/Stop
                    {
                        lbKey[1].Visible = false;
                        ProdState = ProdStates.WaitToStop;
                    }

                    break;
                case ProdStates.WaitToStop:
                    if (Event == 10) // Stopped
                    {
 //                       ShowMainMenu(true);

                        lbKey[0].Visible = true;
                        lbKey[1].Visible = false;
                        lbKey[3].BackColor = Color.Green;
                        lbKey[3].Text = "Start";
                        ProdState = ProdStates.Stopped;
                    }
                    break;
                case ProdStates.WaitToPause:
                    if (Event == 11) // Paused
                    {
                        lbKey[0].Visible = false;

                        lbKey[1].BackColor = Color.Green;
                        lbKey[1].Text = "Resume";
                        lbKey[1].Visible = true;

                        lbKey[3].BackColor = Color.Red;
                        lbKey[3].Text = "Stop";
                        ProdState = ProdStates.Paused;
                    }
                    break;
            }

        }

        public void AllAxisHome()
        {
            byte AxisNr = 0;

            for (AxisNr = 0; AxisNr < 10; AxisNr++)
            {
                if (AxisNr == 0)//Z
                {
                    System.Threading.Thread.Sleep(1000);
                }
                else if (AxisNr == 1)//Index
                {

                }
                else if (AxisNr == 2)//Rod
                {

                }
                else
                {
                   // plc.FinsWriteBit(5, AxisNr, 0);  // disble
                   // System.Threading.Thread.Sleep(500);
                    plc.FinsWrite(340 + AxisNr * 4, MenuSetup.AxisMomentHome[AxisNr]);
                    plc.FinsWrite(420 + AxisNr * 4, MenuSetup.AxisSpeedHome[AxisNr]);
                    plc.FinsWriteBit(5, AxisNr, 1);  // enable
                    System.Threading.Thread.Sleep(1000);
                    plc.FinsWriteBit(3, AxisNr, 1);  // Start home
                    //System.Threading.Thread.Sleep(1000);
                }

            }

        }

        public void ShowMenu(bool show)
        {
            for (int i = 0; i < NofKey; i++)
                lbKey[i].Visible = show;

            lbKey[2].Visible = false;

            for (int i = 0; i < NofParam; i++)
            {
                lbParamTxt[i].Visible = show;
                lbParam[i].Visible = show;
            }
            for (int i = 0; i < NofStatusNav; i++)
                lbStatusNav[i].Visible = show;

            for (int i = 0; i < NofStatus; i++)
                lbStatus[i].Visible = show;
        }

        private void lbStatusNav_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            int KeyNr = (int)lbl.Tag;
        }

        private void lbKey_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            int KeyNr = (int)lbl.Tag;
            StateMachine(KeyNr);
        }

    }
}

