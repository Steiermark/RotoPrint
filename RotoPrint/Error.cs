using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.IO;


namespace RotoPrint
{
    class Error
    {
        lb lb = new lb();
        public static Label ErrorMessage = new Label();

        public enum Errors
        {
            EzAxis_Driver, EiAxix_Driver, ErAxis_Driver,
            WInk_White_Low, WInk_Cyan_Low,
            Last
        };

        private struct ErrStruct
        {
            public string Text;
            public bool IsError;
            public int Count;
            public bool Active;
        }

        static int ErrorMax = (int)Errors.Last;
        private static ErrStruct[] ErrTable = new ErrStruct[ErrorMax];
        private static int NofErrors = 0;

        private static int NofActiveErrors = 0;
        private static int NofActiveWarnings = 0;

        private static int LatestError;

        private void ErrTableAdd(string s,bool IsError)
        {
            ErrTable[NofErrors].Text = s;
            ErrTable[NofErrors].IsError = IsError;
            ErrTable[NofErrors].Active = false;
            ErrTable[NofErrors].Count = 0;
            NofErrors++; 
        }

        public static bool Show()
        {
            bool Error = false;     
            if (NofActiveErrors > 0)
            {
                ErrorMessage.Text = ErrTable[LatestError].Text;
                ErrorMessage.Visible = true;
                Error = true;
            }
            else
            {
                ErrorMessage.Visible = false;
            }
            return Error;
        }

        public static void Set(int ErrNo)
        {
            if (!ErrTable[ErrNo].Active)
            {
                ErrTable[ErrNo].Active = true;
                ErrTable[ErrNo].Count++;
                if (ErrTable[ErrNo].IsError)
                {
                    NofActiveErrors++;
                    LatestError = ErrNo;
                }
                else
                    NofActiveWarnings++;
            }
        }

        public static void Clear(int ErrNo)
        {
            if (ErrTable[ErrNo].Active)
            {
                ErrTable[ErrNo].Active = false;
                ErrTable[ErrNo].Count--;
                if (ErrTable[ErrNo].IsError)
                    NofActiveErrors--;
                else
                    NofActiveWarnings--;
            }
        }


        public void Init()
        {
            Errors e;
            String s;

            lb.AxisDefault(ErrorMessage, 216, "Error", 0);
            ErrorMessage.Location = new Point(340, 760);
            ErrorMessage.Size = new Size(400, 400);
            ErrorMessage.BackColor = Color.Red;
            ErrorMessage.Visible = false;


            for (int i = 0; i < ErrorMax; i++)
            {
                e = (Errors)i;
                s = e.ToString();
                ErrTable[i].IsError = (s[0] == 'E'); 
                ErrTable[i].Text = s.Replace('_', ' ').Substring(1);
            }
        }
    }
}
