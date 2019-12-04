using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;


namespace RotoPrint
{
    class MenuInk
    {
        Omron plc;
        MenuSetup Setup;
        lb lb = new lb();
        TouchKeyboard kb = new TouchKeyboard();

        private Timer InkTimer;
 
        private Double[] AxisActPos = new double[c.NofAxis];
        private int ServoRdyStatus = 0;



        Double yPosPurge = 430;
        Double zPosPurge = 25;
        Double iPosPurge = 15;

        Double yPosWipeEnd = 430-65;


        int clInk2AirTime;
        int clAir2CleanTime;
        int clClean2AirTime;

        int puPurgeTime;


        bool Panel = true;


        // pu cl st dw pr


        enum InkStates { Init, Idle, zHome, zHomeWp, yPos, yPosWp, iPosPurge, iPosPurgeWp, zPosPurge, zPosPurgeWp, InStartPosition, yWipeEnd, yWipeEndWp, zHome2, zHome2Wp, yHome, yHomeWp,
                        clInk2Air, clInk2AirWt, clAir2Clean, clAir2CleanWt, clClean2Air, clClean2AirWt,

            puInk2Air, puInk2AirWt,

        };

        InkStates State = InkStates.Idle;

        enum InkCmds {None,Park,Clean,Flush,Purge,DropWatch,Print };
        InkCmds InkCmd = InkCmds.None;

        static int NofInk = 6;
        Label[] lbInk = new Label[NofInk];
        string[] TxtInk = { "White", "Cyan", "Magenta", "Yellow", "Black", "Vanish" };
        bool[] InkSelected = { false, false, false, false, false, false};


        static int NofInkMenu = 6;
        Label[] lbInkMenu = new Label[NofInkMenu];
        string[] TxtInkMenu = { "Park", "Clean", "Flush", "Purge", "Drop W", "Print" };

        static int NofClParam = 3;
        Label[] lbClParam = new Label[NofClParam];
        Label[] lbClParamTxt = new Label[NofClParam];
        string[] TxtClParam = { "Air Time", "Clean Time", "Air Time"};




        public void Init()
        {
            InkTimer = new Timer();
            InkTimer.Interval = 100;
            InkTimer.Enabled = false;
            InkTimer.Tick += new System.EventHandler(InkTimer_Tick);

            for (int i = 0; i < NofInk; i++)
            {
                lbInk[i] = new Label();
                lb.KeyDefault(lbInk[i], TxtInk[i], i);
                lbInk[i].Location = new Point(180 * i, 120);
                lbInk[i].Size = new Size(179, 59);
                lbInk[i].Click += new EventHandler(lbInk_Click);
            }

            for (int i = 0; i < NofInk; i++)
            {
                lbInkMenu[i] = new Label();
                lb.KeyDefault(lbInkMenu[i], TxtInkMenu[i], i);
                lbInkMenu[i].Location = new Point(180 * i, 240);
                lbInkMenu[i].Size = new Size(179, 59);
                lbInkMenu[i].Click += new EventHandler(lbInkMenu_Click);
            }

            for (int i = 0; i < NofClParam; i++)
            {
                lbClParamTxt[i] = new Label();
                lb.KeyDefault(lbClParamTxt[i], TxtClParam[i], i);
                lbClParamTxt[i].Location = new Point(180, 300+i*120);
                lbClParamTxt[i].Size = new Size(179, 59);

                lbClParam[i] = new Label();
                lb.KeyDefault(lbClParam[i], "", i);
                lbClParam[i].Location = new Point(180 , 360 + i*120);
                lbClParam[i].Size = new Size(179, 59);
                lbClParam[i].Click += new EventHandler(lbClParam_Click);
            }
        }

        private void lbClParam_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            int ParamNr = (int)lbl.Tag;

            kb.HideAkey();
            kb.HideNkey();

            lbClParam[ParamNr].BackColor = lb.bg1;

            TouchKeyboard.lbNkeyHost = (Label)sender;
            kb.ShowNkey(180, 360 + NofClParam * 120, true);
        }

        private void lbInk_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            int InkNr = (int)lbl.Tag;

            if (InkSelected[InkNr])
            {
                lbInk[InkNr].BackColor = lb.bg1;
                InkSelected[InkNr] = false;
            }
            else
            {
                lbInk[InkNr].BackColor = Color.DodgerBlue;
                InkSelected[InkNr] = true;
            }
        }

        private void lbInkMenu_Click(object sender, EventArgs e)
        {
            Label lbl = (Label)sender;
            int InkMenuNr = (int)lbl.Tag;

            if (InkCmd == InkCmds.None)
            {
                lbInkMenu[InkMenuNr].BackColor = Color.DodgerBlue;
//                if (InkMenuNr == 0) InkCmd = InkCmds.Park;
//                if (InkMenuNr == 1) InkCmd = InkCmds.Clean;
//                if (InkMenuNr == 2) InkCmd = InkCmds.Flush;
//                if (InkMenuNr == 3) InkCmd = InkCmds.Purge;
//                if (InkMenuNr == 4) InkCmd = InkCmds.DropWatch;
//                if (InkMenuNr == 5) InkCmd = InkCmds.Print;
            }
        }

        public void ShowMenu(bool show)
        {
            for (int i = 0; i < NofInk; i++)
                lbInk[i].Visible = show;

            for (int i = 0; i < NofInkMenu; i++)
                lbInkMenu[i].Visible = show;

            for (int i = 0; i < NofClParam; i++)
            {
                lbClParamTxt[i].Visible = show;
                lbClParam[i].Visible = show;
            }
        }

        void GetAxisActPosRound(byte AxisNr)
        {
            plc.FinsRead(100 + AxisNr * 4, ref AxisActPos[AxisNr]);
            AxisActPos[AxisNr] = Math.Round(AxisActPos[AxisNr], 1);
        }

        void GotoAxisPos(byte AxisNr,Double Pos)
        {
            plc.FinsWrite(140 + AxisNr * 4, Pos);
            plc.FinsWriteBit(9, AxisNr, 1);  // start move
        }

        void GotoAxisHome(byte AxisNr)
        {
            plc.FinsWriteBit(3, AxisNr, 1);  // Start home
        }

        int TimeOut;

        private void InkTimer_Tick(object sender, EventArgs e)
        {
            bool AllInPos;
            byte AxisNr;


            switch (State)
            {
                case InkStates.Init:
                    Setup.InitMachine();
                    State = InkStates.Idle;
                    break;

                case InkStates.Idle:
                    if (InkCmd != InkCmds.None) State = InkStates.zHome;
                    break;

                case InkStates.zHome:
                    plc.FinsWriteBit(3, 0, 1);  // Start home
                    State = InkStates.zHomeWp;
                    break;

                case InkStates.zHomeWp:
                    GetAxisActPosRound(c.aZ);
                    if (AxisActPos[c.aZ] == 0) State = InkStates.yPos;
                    break;

                case InkStates.yPos:
                    for(int i=0;i < c.NofInk; i++)
                    {
                        AxisNr = (byte)(i + c.aW);
                        if (InkSelected[i] && !Panel)
                            GotoAxisPos(AxisNr, yPosPurge);
                        else
                            GotoAxisHome(AxisNr);
                    }
                    GotoAxisHome(c.aCorona);
                    State = InkStates.yPosWp;
                    break;

                case InkStates.yPosWp:
                    AllInPos = true;
                    for(int i = 0; i < c.NofInk; i++) 
                    {
                        AxisNr = (byte)(i +c.aW);
                        GetAxisActPosRound(AxisNr);

                        if (InkSelected[i] || !Panel)
                            if (AxisActPos[AxisNr] != yPosPurge) AllInPos = false;
                        else
                            if (AxisActPos[AxisNr] != 0) AllInPos = false;
                    }
                    GetAxisActPosRound(c.aCorona);
                    if (AxisActPos[c.aCorona] != 0) AllInPos = false;

                    if (AllInPos)
                    {
                        if (Panel)
                            State = InkStates.InStartPosition;
                        else
                            State = InkStates.iPosPurge;
                    }
                    break;

                case InkStates.iPosPurge:
                    GotoAxisPos(c.aIndex, iPosPurge);
                    State = InkStates.iPosPurgeWp;
                    break;

                case InkStates.iPosPurgeWp:
                    GetAxisActPosRound(c.aIndex);    
                    if (AxisActPos[c.aIndex] == iPosPurge) State = InkStates.zPosPurge;
                    break;

                case InkStates.zPosPurge:
                    GotoAxisPos(c.aZ, zPosPurge);
                    State = InkStates.zPosPurgeWp;
                    break;

                case InkStates.zPosPurgeWp:
                    GetAxisActPosRound(c.aZ);
                    if (AxisActPos[c.aZ] == zPosPurge)
                        State = InkStates.InStartPosition;
                    break;

                case InkStates.InStartPosition:
                    switch (InkCmd)
                    {
                        case InkCmds.Clean:
                            State = InkStates.clInk2Air;
                            break;
                    }
                    break;

                case InkStates.yWipeEnd:
                    for (int i = 0; i < c.NofInk; i++)
                    {
                        if (InkSelected[i] && !Panel)
                        {
                            AxisNr = (byte)(i + c.aW);
                            GotoAxisPos(AxisNr, yPosWipeEnd);    
                        }
                    }
                    State = InkStates.yPosWp;
                    break;

                case InkStates.yWipeEndWp:
                    AllInPos = true;
                    for (int i = 0; i < c.NofInk; i++)
                    {
                        if (InkSelected[i] || Panel)
                        {
                            AxisNr = (byte)(i + c.aW);
                            GetAxisActPosRound(AxisNr);
                            if (AxisActPos[AxisNr] != yPosWipeEnd) AllInPos = false;
                        }
                    }
 
                    if (AllInPos) State = InkStates.zHome2;
                    break;

                case InkStates.zHome2:
                    plc.FinsWriteBit(3, 0, 1);  // Start home
                    State = InkStates.zHomeWp;
                    break;

                case InkStates.zHome2Wp:
                    GetAxisActPosRound(c.aZ);
                    if (AxisActPos[c.aZ] == 0) State = InkStates.yPos;
                    break;

                case InkStates.yHome:
                    for (int i = 0; i < c.NofInk; i++)
                    {
                        AxisNr = (byte)(i + c.aW);
                        GotoAxisHome(AxisNr);
                    }
                    GotoAxisHome(c.aCorona);
                    State = InkStates.yHomeWp;
                    break;

                case InkStates.yHomeWp:
                    AllInPos = true;
                    for (int i = 0; i < c.NofInk; i++)
                    {
                        AxisNr = (byte)(i + c.aW);
                        GetAxisActPosRound(AxisNr);
                        if (AxisActPos[AxisNr] != 0) AllInPos = false;
                    }
                    GetAxisActPosRound(c.aCorona);
                    if (AxisActPos[c.aCorona] != 0) AllInPos = false;
                    InkCmd = InkCmds.None;
                    if (AllInPos) State = InkStates.Idle;
                    break;


                // *******************  CLEAN **************************************************
                case InkStates.clInk2Air:
                    for (byte i = 0; i < c.NofInk; i++)
                    {
                        if (InkSelected[i]) plc.FinsWriteBit(c.vAir_Ink, i, 1); // Open Air
                    }
                    TimeOut = clInk2AirTime;
                    State = InkStates.clInk2AirWt;
                    break;

                case InkStates.clInk2AirWt:
                    if (TimeOut > 0) TimeOut--; else State = InkStates.clAir2Clean;
                    break;

                case InkStates.clAir2Clean:
                    for (byte i = 0; i < c.NofInk; i++)
                    {
                        if (InkSelected[i]) plc.FinsWriteBit(c.vClean_Air, i, 1); // Clean Valve
                    }
                    TimeOut = clAir2CleanTime;
                    State = InkStates.clAir2CleanWt;
                    break;

                case InkStates.clAir2CleanWt:
                    if (TimeOut > 0) TimeOut--; else State = InkStates.clClean2Air;
                    break;

                case InkStates.clClean2Air:
                    for (byte i = 0; i < c.NofInk; i++)
                    {
                        if (InkSelected[i]) plc.FinsWriteBit(c.vClean_Air, i, 0); // Close Clean Valve
                    }
                    TimeOut = clClean2AirTime;
                    State = InkStates.clClean2AirWt;
                    break;

                case InkStates.clClean2AirWt:
                    if (TimeOut > 0) TimeOut--;
                    else
                    {
                        for (byte i = 0; i < c.NofInk; i++)
                        {
 //                           if (InkSelected[i]) plc.FinsWriteBit(c.v, i, 0); // Air
                        }

                        if (!Panel)
                            State = InkStates.yWipeEnd;
                        else
                        {
                            InkCmd = InkCmds.None;
                            State = InkStates.Idle;
                        }
                    }
                    break;
            }
        }

    }
}
