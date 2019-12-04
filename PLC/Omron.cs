using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;
using System.Diagnostics;

namespace Plc
{
    public class Omron
    {
        const int
            x_searchpos1 = 300,      //X Pladekant 1
            y_searchpos1 = 301,      //Y Pladekant 1
            x_searchpos2 = 302,      //X Pladekant 2
            y_searchpos2 = 303,      //Y Pladekant 2

            xy_idle = 320,      //1=true
            xy_homed = 321,      //1=true
            xy_distance = 325,  // Pulse count

            x_homed = 330,      //X er initialiseret
            x_idle = 331,      //X Klar/ikke i brug
            x_actpos = 335,      //X Aktuel position
            x2_actpos = 335,
            x1_actpos = 334,

            y_homed = 340,      //Y er initialiseret
            y_idle = 341,      //Y Klar/ikke i brug
            y_actpos = 345,      //Y Aktuel position

            z_homed = 350,      //Z er initialiseret
            z_idle = 351,      //Z Klar/ikke i brug
            z_actpos = 355,      //Z Aktuel position

            r_homed = 360,      //R er initialiseret
            r_idle = 361,      //R Klar/ikke i brug
            r_actpos = 365,      //R Aktuel position

            automatic = 400,      //1 = der kan køres XY 
            stop_all = 401,      //1 = Stopper alle kørsler

            xy_moveabs = 412,      //1 = Start XY kørsel
            xy_search = 413,      //1" = Start XY kantsøgning

            x_speed = 430,      //Normal drift hastighed
            x_jogspeed = 431,      //Jog og home hastighed
            x_accel = 432,
            x_decel = 433,
            x_abspos1 = 434,      //Pos der skal køres til 1/1000 mm
            x_jogfwd = 4,
            x_jogrev = 436,
            x_home = 437,      //1 = Start X initialisering
            x_zero = 438,      //1 = Start X kørsel til pos 0
            x_moveabs = 439,      //1 = Start X kørsel til x_abspos1
            x_home_torque = 440,     // Negativ værdi - default -460

            y_speed = 450,      //Normal drift hastighed
            y_jogspeed = 451,      //Jog og home hastighed
            y_accel = 452,
            y_decel = 453,
            y_abspos1 = 454,      //Pos der skal køres til 1/1000 mm
            y_jogfwd = 455,
            y_jogrev = 456,
            y_home = 457,      //1 = Start Y initialisering
            y_zero = 458,      //1 = Start Y kørsel til pos 0
            y_moveabs = 459,      //1 = Start Y kørsel til y_abspos1
            y_home_torque = 460,     // Negativ værdi - default -360

            z_speed = 470,      //Normal drift hastighed
            z_jogspeed = 471,      //Jog og home hastighed
            z_accel = 472,
            z_decel = 473,
            z_abspos1 = 474,      //Pos der skal køres til 1/1000 mm
            z_jogfwd = 475,
            z_jogrev = 476,
            z_home = 477,
            z_zero = 478,      //1 = Start Z initialisering
            z_moveabs = 479,      //1 = Start Z kørsel til pos 0
            z_movesensor = 480,      //1 = Start Z kørsel til z_abspos1
            z_home_torque = 481,     // Positiv værdi - default 290
            z_home_offset = 482,     // Postion efter end homing
            z_sensorpos = 483,       //na 
            z_log_high = 407,
            z_log_low = 408,

            z_heightsensor = 404,      //Z Højde
            z_sensoroffset = 405,      //na
            z_print_dist = 406,      //na

            r_speed = 490,      //Normal drift hastighed
            r_jogspeed = 491,      //Jog og home hastighed
            r_accel = 492,
            r_decel = 493,
            r_abspos1 = 494,      //Pos der skal køres til 1/1000 mm
            r_jogfwd = 495,
            r_jogrev = 496,
            r_home = 497,      //1 = Start R initialisering
            r_zero = 498,      //1 = Start R kørsel til pos 0
            r_moveabs = 499,      //1 = Start R kørsel til r_abspos1
            r_home_torque = 500,     // Negativ værdi 
            r_home_offset = 501,     // Movement efter homing

            acttemp1 = 500,
            acttemp2 = 501,
            acttemp3 = 502,
            acttemp4 = 503,

            settemp1 = 510,
            settemp2 = 511,
            settemp3 = 512,
            settemp4 = 513,

            status_word = 900,      //0 = Initializing, 1 = App stopped with no error, 2 = Errors in the system, 3 = App running
            status_bits = 901,      //0 = Alarm flag,  
            status_action = 902,      //0 = Initalizing, 1 = Send reset, 2 = Resetting, 3 = System healthy 
            signal_state = 1000,     // = 907 Start = 1, Stop = 2, Reset = 4

            plc_io_1 = 10,
            plc_io_2 = 11,
            plc_io_3 = 12,
            plc_io_4 = 13;

        const int Node = 7, Cmd = 11, Seg = 12, Adr1 = 13, Adr2 = 14, Fix = 15, Num1 = 16, Num2 = 17, Data = 18;
        const int Ret = 14;
        const int CmdRead = 0x01, CmdWrite = 0x02;
        const byte CIO = 0xB0, VR = 0xF0;

        byte[] msg = { 0x80, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x00, 0x01, 0x01, 0x01, 0x82, 0x00, 0x00, 0x00, 0x00, 0x02 };

        public IPAddress Plc { get; set; }
        public IPAddress Servo { get; set; }
        public int Port { get; set; }
        public int MaxX { get; set; }
        public int MaxY { get; set; }
        public int MaxZ { get; set; }
        public int MinZ { get; set; }
        public int MaxR { get; set; }

        public bool HomeHeightOK = true;
        public int HomeHeight = 0;
        public bool DEBUG = false;

        private int _CIO1 = 0;
        private int CIO1
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return _CIO1;
            }
            set
            {
                _CIO1 = value;
                Write(Plc, Port, plc_io_1, _CIO1);
            }
        }

        private int _CIO2 = 0;
        public int CIO2
        {
            get
            {
                Read(Plc, Port, plc_io_2, ref _CIO2);
                return _CIO2;
            }
            set
            {
                _CIO2 = value;
                Write(Plc, Port, plc_io_2, _CIO2);
            }
        }

        private int _CIO3 = 0;
        public int CIO3
        {
            get
            {
                Read(Plc, Port, plc_io_3, ref _CIO3);
                return _CIO3;
            }
            set
            {
                _CIO3 = value;
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        private int _CIO4 = 0;
        public int CIO4
        {
            get
            {
                Read(Plc, Port, plc_io_4, ref _CIO4);
                return _CIO4;
            }
            set
            {
                _CIO4 = value;
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        private int _Temp1 = 0;
        public int Temp1
        {
            get
            {
                Read(Plc, Port, acttemp1, ref _Temp1);
                return _Temp1;
            }
            set
            {
                _Temp1 = value;
                Write(Plc, Port, settemp1, _Temp1);
            }
        }

        private int _Temp2 = 0;
        public int Temp2
        {
            get
            {
                Read(Plc, Port, acttemp2, ref _Temp2);
                return _Temp2;
            }
            set
            {
                _Temp2 = value;
                Write(Plc, Port, settemp2, _Temp2);
            }
        }

        private int _Temp3 = 0;
        public int Temp3
        {
            get
            {
                Read(Plc, Port, acttemp3, ref _Temp3);
                return _Temp3;
            }
            set
            {
                _Temp3 = value;
                Write(Plc, Port, settemp3, _Temp3);
            }
        }

        private int _Temp4 = 0;
        public int Temp4
        {
            get
            {
                Read(Plc, Port, acttemp4, ref _Temp4);
                return _Temp4;
            }
            set
            {
                _Temp4 = value;
                Write(Plc, Port, settemp4, _Temp4);
            }
        }

        public bool EmergencyStop
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return (_CIO1 & 0x2000) == 0;                                           // 0010 0000 0000 0000
            }
        }

        public bool LightBarrier2
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return (_CIO1 & 0x1000) != 0;                                           // 0001 0000 0000 0000
            }
        }

        public bool LightBarrier1
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return (_CIO1 & 0x0800) != 0;                                           // 0000 1000 0000 0000
            }
        }

        public bool SensorPass
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return (_CIO1 & 0x0400) != 0;                                           // 0000 0100 0000 0000
            }
        }

        public bool HotFeedInkLevelMelt
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return (_CIO1 & 0x0080) != 0;                                           // 0000 0000 1000 0000
            }
        }

        public bool HotFeedInkLevelPot
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return (_CIO1 & 0x0010) != 0;                                           // 0000 0000 0001 0000
            }
        }

        public bool WaterPumpFuse
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return (_CIO1 & 0x0008) != 0;                                           // 0000 0000 0000 1000
            }
        }

        public bool Waterlevel2
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return (_CIO1 & 0x0004) != 0;                                           // 0000 0000 0000 0100
            }
        }

        public bool Waterlevel1
        {
            get
            {
                Read(Plc, Port, plc_io_1, ref _CIO1);
                return (_CIO1 & 0x0002) != 0;                                           // 0000 0000 0000 0010
            }
        }

        public bool Fuse(int i)
        {
            Read(Plc, Port, plc_io_2, ref _CIO2);
            return (_CIO2 & 0x0001 << i) != 0;                                           // 0000 0000 0000 0010
        }

        public bool Meniscus
        {
            get
            {
                return (_CIO3 & 0x0020) != 0;
            }
            set
            {
                if (value)
                    _CIO3 |= 0x0020;                                                    // 0000 0000 0010 0000
                else
                    _CIO3 &= 0xffdf;                                                    // 1111 1111 1101 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool Cooling
        {
            get
            {
                return (_CIO3 & 0x0040) != 0;       
            }
            set
            {
                if (value)
                    _CIO3 |= 0x0040;                                                    // 0000 0000 0100 0000
                else
                    _CIO3 &= 0xffbf;                                                    // 1111 1111 1011 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool Power150V
        {
            get
            {
                return (_CIO3 & 0x0080) == 0;                                           // Inversed 0 -> true                                  
            }
            set
            {
                if (value)
                    _CIO3 &= 0xff7f;                                                    // 1111 1111 0111 1111
                else
                    _CIO3 |= 0x0080;                                                    // 0000 0000 1000 0000
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool Heaters
        {
            get
            {
                return (_CIO3 & 0x0100) != 0;
            }
            set
            {
                if (value)
                    _CIO3 |= 0x0100;                                                    // 0000 0001 0000 0000
                else
                    _CIO3 &= 0xfeff;                                                    // 1111 1110 1111 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool UV1                                                                 
        {                                                                                
            get
            {
                return (_CIO3 & 0x0200) != 0;                                         
            }
            set
            {
                if (value)
                    _CIO3 |= 0x0200;                                                    // 0000 0010 0000 0000
                else
                    _CIO3 &= 0xfdff;                                                    // 1111 1101 1111 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool UV2
        {
            get
            {
                return (_CIO3 & 0x0400) != 0;
            }
            set
            {
                if (value)
                    _CIO3 |= 0x0400;                                                    // 0000 0100 0000 0000
                else
                    _CIO3 &= 0xfbff;                                                    // 1111 1011 1111 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool Purge
        {
            get
            {
                return (_CIO3 & 0x0800) != 0;                                               
            }
            set
            {
                if (value)
                    _CIO3 |= 0x0800;                                                    // 0000 1000 0000 0000
                else
                    _CIO3 &= 0xf7ff;                                                    // 1111 0111 1111 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool InkPump
        {
            get
            {
                return (_CIO3 & 0x1000) != 0;                                               
            }
            set
            {
                if (value)
                    _CIO3 |= 0x1000;                                                    // 0001 0000 0000 0000
                else
                    _CIO3 &= 0xefff;                                                    // 1110 1111 1111 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool Cip
        {
            get
            {
                return (_CIO3 & 0x2000) != 0;
            }
            set
            {
                if (value)
                    _CIO3 |= 0x2000;                                                    // 0010 0000 0000 0000
                else
                    _CIO3 &= 0xdfff;                                                    // 1101 1111 1111 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool Vacuum
        {
            get
            {
                return (_CIO3 & 0x4000) != 0;
            }
            set
            {
                if (value)
                    _CIO3 |= 0x4000;                                                    // 0100 0000 0000 0000
                else
                    _CIO3 &= 0xbfff;                                                    // 1011 1111 1111 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool InkValve
        {
            get
            {
                return (_CIO3 & 0x8000) != 0;
            }
            set
            {
                if (value)
                    _CIO3 |= 0x8000;                                                    // 1000 0000 0000 0000
                else
                    _CIO3 &= 0x7fff;                                                    // 0111 1111 1111 1111
                Write(Plc, Port, plc_io_3, _CIO3);
            }
        }

        public bool HotFeedVent
        {
            get
            {
                return (_CIO4 & 0x0001) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0001;                                                    // 0000 0000 0000 0001
                else
                    _CIO4 &= 0xfffe;                                                    // 1111 1111 1111 1110
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool HotFeedWaterPump
        {
            get
            {
                return (_CIO4 & 0x0002) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0002;                                                    // 0000 0000 0000 0010
                else
                    _CIO4 &= 0xfffd;                                                    // 1111 1111 1111 1101
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool HotFeedInkFeed
        {
            get
            {
                return (_CIO4 & 0x0004) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0004;                                                    // 0000 0000 0000 0100
                else
                    _CIO4 &= 0xfffb;                                                    // 1111 1111 1111 1011
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool HotFeedVacuum
        {
            get
            {
                return (_CIO4 & 0x0008) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0008;                                                    // 0000 0000 0000 1000
                else
                    _CIO4 &= 0xfff7;                                                    // 1111 1111 1111 0111
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool HotFeedMaster
        {
            get
            {
                return (_CIO4 & 0x0010) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0010;                                                    // 0000 0000 0001 0000
                else
                    _CIO4 &= 0xffef;                                                    // 1111 1111 1110 1111
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool RedLight
        {
            get
            {
                return (_CIO4 & 0x0020) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0020;                                                    // 0000 0000 0010 0000
                else
                    _CIO4 &= 0xffdf;                                                    // 1111 1111 1101 1111
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }
        
        public bool GreenLight
        {
            get
            {
                return (_CIO4 & 0x0040) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0040;                                                    // 0000 0000 0100 0000
                else
                    _CIO4 &= 0xffbf;                                                    // 1111 1111 1011 1111
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool BlueLight
        {
            get
            {
                return (_CIO4 & 0x0080) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0080;                                                    // 0000 0000 1000 0000
                else
                    _CIO4 &= 0xff7f;                                                    // 1111 1111 0111 1111 
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool Heater1
        {
            get
            {
                return (_CIO4 & 0x0100) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0100;                                                    // 0000 0001 0000 0000
                else
                    _CIO4 &= 0xfeff;                                                    // 1111 1110 1111 1111
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool Heater2
        {
            get
            {
                return (_CIO4 & 0x0200) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x1000;                                                    // 0001 0000 0000 0000
                else
                    _CIO4 &= 0xefff;                                                    // 1110 1111 1111 1111
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool Heater3
        {
            get
            {
                return (_CIO4 & 0x0400) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0400;                                                    // 0000 0100 0000 0000
                else
                    _CIO4 &= 0xfbff;                                                    // 1111 1011 1111 1111
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public bool Heater4
        {
            get
            {
                return (_CIO4 & 0x0800) != 0;
            }
            set
            {
                if (value)
                    _CIO4 |= 0x0800;                                                    // 0000 1000 0000 0000
                else
                    _CIO4 &= 0xf7ff;                                                    // 1111 0111 1111 1111
                Write(Plc, Port, plc_io_4, _CIO4);
            }
        }

        public int XYDistance
        {
            get
            {
                float f = 0;
                Read(Servo, Port, xy_distance, ref f);
                return Convert.ToInt32(f);
            }
        }

        public int SensorHeight
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_heightsensor, ref f);
                return Convert.ToInt32(f);
            }
        }

        public int SensorHeightPLC
        {
            get
            {
                int i = 0;
                Read(Plc, Port, 620, ref i);
                return i * 10;
            }
        }

        public int SensorPos
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_sensorpos, ref f);
                return Convert.ToInt32(f);
            }
            set
            {
                Write(Servo, Port, z_sensorpos, Convert.ToSingle(value));
            }
        }

        public int SensorLogHigh
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_log_high, ref f);
                return Convert.ToInt32(f);
            }
            set
            {
                float f = value;
                Write(Servo, Port, z_log_high, f);
            }
        }

        public int SensorLogLow
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_log_low, ref f);
                return Convert.ToInt32(f);
            }
            set
            {
                float f = value;
                Write(Servo, Port, z_log_low, f);
            }
        }

        public int SensorOffset
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_sensoroffset, ref f);
                return Convert.ToInt32(f);
            }
            set
            {
                float f = value;
                Write(Servo, Port, z_sensoroffset, f);
            }
        }

        public Rectangle SearchPos
        {
            get
            {
                float X = 0, Y = 0, Width = 0, Height = 0;
                Read(Servo, Port, x_searchpos1, ref X);
                Read(Servo, Port, y_searchpos1, ref Y);
                Read(Servo, Port, x_searchpos2, ref Width);
                Read(Servo, Port, y_searchpos2, ref Height);
                return new Rectangle(Convert.ToInt32(X), Convert.ToInt32(Y), Convert.ToInt32(Width), Convert.ToInt32(Height));
            }
            set
            {
                Write(Servo, Port, x_searchpos1, Convert.ToSingle(value.X));
                Write(Servo, Port, y_searchpos1, Convert.ToSingle(value.Y));
                Write(Servo, Port, x_searchpos2, Convert.ToSingle(value.Width));
                Write(Servo, Port, y_searchpos2, Convert.ToSingle(value.Height));
            }
        }

        public int ActXPos
        {
            get
            {
                float f = 0;
                Read(Servo, Port, x_actpos, ref f);
                return Convert.ToInt32(f);
            }
        }

        public int ActYPos
        {
            get
            {
                float f = 0;
                Read(Servo, Port, y_actpos, ref f);
                return Convert.ToInt32(f);
            }
        }

        public int ActZPos
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_actpos, ref f);
                return Convert.ToInt32(f);
            }
        }

        public int ActRPos
        {
            get
            {
                float f = 0;
                Read(Servo, Port, r_actpos, ref f);
                return Convert.ToInt32(f);
            }
        }

        public bool Automatic
        {
            get
            {
                float f = 0;
                Read(Servo, Port, automatic, ref f);
                return f == 1;
            }

            set
            {
                if (value)
                    Write(Servo, Port, automatic, 1F);
                else
                    Write(Servo, Port, automatic, 0F);
                System.Threading.Thread.Sleep(500);
            }
        }

        private bool _Online = true;
        public bool Online
        {
            get
            {
                float f = 0;
                return Read(Servo, Port, automatic, ref f);
            }
        }

        public int XSpeed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, x_speed, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, x_speed, Convert.ToSingle(value));
            }
        }

        public int YSpeed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, y_speed, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, y_speed, Convert.ToSingle(value));
            }
        }

        public int ZSpeed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_speed, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, z_speed, Convert.ToSingle(value));
            }
        }

        public int RSpeed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, r_speed, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, r_speed, Convert.ToSingle(value));
            }
        }

        public int XJogSpeed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, x_jogspeed, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, x_jogspeed, Convert.ToSingle(value));
            }
        }

        public int YJogSpeed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, y_jogspeed, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, y_jogspeed, Convert.ToSingle(value));
            }
        }

        public int ZJogSpeed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_jogspeed, ref f);
                return Convert.ToInt32(f);
            }
            set
            {
                Write(Servo, Port, z_jogspeed, Convert.ToSingle(value));
            }
        }

        public int RJogSpeed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, r_jogspeed, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, r_jogspeed, Convert.ToSingle(value));
            }
        }

        public int XAccel
        {
            get
            {
                float f = 0;
                Read(Servo, Port, x_accel, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, x_accel, Convert.ToSingle(value));
            }
        }

        public int YAccel
        {
            get
            {
                float f = 0;
                Read(Servo, Port, y_accel, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, y_accel, Convert.ToSingle(value));
            }
        }

        public int ZAccel
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_accel, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, z_accel, Convert.ToSingle(value));
            }
        }

        public int RAccel
        {
            get
            {
                float f = 0;
                Read(Servo, Port, r_accel, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, r_accel, Convert.ToSingle(value));
            }
        }

        public int XDecel
        {
            get
            {
                float f = 0;
                Read(Servo, Port, x_decel, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, x_decel, Convert.ToSingle(value));
            }
        }

        public int YDecel
        {
            get
            {
                float f = 0;
                Read(Servo, Port, y_decel, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, y_decel, Convert.ToSingle(value));
            }
        }

        public int ZDecel
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_decel, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, z_decel, Convert.ToSingle(value));
            }
        }

        public int RDecel
        {
            get
            {
                float f = 0;
                Read(Servo, Port, r_decel, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, r_decel, Convert.ToSingle(value));
            }
        }

        public int XTorque
        {
            get
            {
                float f = 0;
                Read(Servo, Port, x_home_torque, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, x_home_torque, Convert.ToSingle(value));
            }
        }

        public int YTorque
        {
            get
            {
                float f = 0;
                Read(Servo, Port, y_home_torque, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, y_home_torque, Convert.ToSingle(value));
            }
        }

        public int ZTorqueUp { get; set; }
        public int ZTorqueDown { get; set; }

        public int ZTorque
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_home_torque, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, z_home_torque, Convert.ToSingle(value));
            }
        }

        public int RTorque
        {
            get
            {
                float f = 0;
                Read(Servo, Port, r_home_torque, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, r_home_torque, Convert.ToSingle(value));
            }
        }

        public int ZHomeOffset
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_home_offset, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, z_home_offset, Convert.ToSingle(value));
            }
        }

        public int RHomeOffset
        {
            get
            {
                float f = 0;
                Read(Servo, Port, r_home_offset, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, r_home_offset, Convert.ToSingle(value));
            }
        }

        public int StatusWord
        {
            get
            {
                float f = 0;
                Read(Servo, Port, status_word, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, status_word, Convert.ToSingle(value));
            }
        }

        public int StatusBits
        {
            get
            {
                float f = 0;
                Read(Servo, Port, status_bits, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, status_bits, Convert.ToSingle(value));
            }
        }

        public int StatusAction
        {
            get
            {
                float f = 0;
                Read(Servo, Port, status_action, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, status_action, Convert.ToSingle(value));
            }
        }

        public int SignalState
        {
            get
            {
                float f = 0;
                Read(Servo, Port, signal_state, ref f);
                return Convert.ToInt32(f);
            }

            set
            {
                Write(Servo, Port, signal_state, Convert.ToSingle(value));
            }
        }

        public Omron(bool Debug = false)
        {
            DEBUG = Debug;
        }

        public void Start()
        {
            _CIO1 = CIO1;
            _CIO2 = CIO2;
            _CIO3 = CIO3;
            _CIO4 = CIO4;

            CIO3 = CIO3 & 0xffe0;                                                       // Make sure all drives are engaged

            StartAll();
        }

        public void StartAll()
        {
            Write(Servo, Port, signal_state, 0F);
            Write(Servo, Port, signal_state, 1F);
            System.Threading.Thread.Sleep(500);
        }

        public void StopAll()
        {
            Write(Servo, Port, signal_state, 0F);
            Write(Servo, Port, signal_state, 2F);
            System.Threading.Thread.Sleep(500);
        }

        public void ResetAll()
        {
            for (int i = 0; i < 5; i++)
            {
                Write(Servo, Port, signal_state, 4F);
                System.Threading.Thread.Sleep(500);
                Write(Servo, Port, signal_state, 0F);

                float f = 0;
                Read(Servo, Port, status_word, ref f);
                if (f == 3F)
                    break;
            }
        }

        public bool XHomed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, x_homed, ref f);
                return f == 1;
            }
        }

        public bool YHomed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, y_homed, ref f);
                return f == 1;
            }
        }

        public bool ZHomed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_homed, ref f);
                return f == 1;
            }
        }

        public bool RHomed
        {
            get
            {
                float f = 0;
                Read(Servo, Port, r_homed, ref f);
                return f == 1;
            }
        }

        public bool IsHomed
        {
            get
            {
                float x = 0, y = 0, z = 0, r = 0;
                Read(Servo, Port, x_homed, ref x);
                Read(Servo, Port, y_homed, ref y);
                Read(Servo, Port, z_homed, ref z);
                Read(Servo, Port, r_homed, ref r);
                return x == 1 && y == 1 && z == 1; // && HomeHeightOK; //DEBUG && r == 1; 
            }
        }

        public bool XIdle
        {
            get
            {
                float f = 0;
                Read(Servo, Port, x_idle, ref f);
                return f == 1;
            }
        }

        public bool YIdle
        {
            get
            {
                float f = 0;
                Read(Servo, Port, y_idle, ref f);
                return f == 1;
            }
        }

        public bool ZIdle
        {
            get
            {
                float f = 0;
                Read(Servo, Port, z_idle, ref f);
                return f == 1;
            }
        }

        public bool RIdle
        {
            get
            {
                float f = 0;
                Read(Servo, Port, r_idle, ref f);
                return f == 1;
            }
        }

        public bool IsIdle
        {
            get
            {
                float x = 0, y = 0, z = 0, r = 0;
                Read(Servo, Port, x_idle, ref x);
                Read(Servo, Port, y_idle, ref y);
                Read(Servo, Port, z_idle, ref z);
                Read(Servo, Port, r_idle, ref r);
                return x == 1 && y == 1 && z == 1 && r == 1;
            }
        }

        public void Home()
        {
            Thread Homer = new Thread(HomeThread);
            Homer.Start();
        }

        private void HomeThread()
        {
            Write(Servo, Port, z_home_torque, Convert.ToSingle(ZTorqueUp));
            Write(Servo, Port, z_home, 1F);
            Thread.Sleep(500);
            while (!ZIdle) ;
            //DEBUG Write(Servo, Port, r_home, 1F);
            Write(Servo, Port, y_home, 1F);
            Write(Servo, Port, x_home, 1F);

            Thread.Sleep(500);
            while (!(XIdle && YIdle)) ;
        }

        private class Line
        {
            public Point p1, p2;

            public Line(Point point1, Point point2)
            {
                p1 = point1;
                p2 = point2;
            }
        }

        private Point Intersection(Line line1, Line line2)
        {
            int x = 0, y = 0;

            int a1 = 0, b1 = 0;
            if (line1.p2.X - line1.p1.X != 0)
            {
                a1 = (line1.p2.Y - line1.p1.Y) / (line1.p2.X - line1.p1.X);
                b1 = line1.p1.Y - (a1 * line1.p2.X);
            }
            else
                x = line1.p1.X;


            int a2 = 0, b2 = 0;
            if (line2.p2.X - line2.p1.X != 0)
            {
                a2 = (line2.p2.Y - line2.p1.Y) / (line2.p2.X - line2.p1.X);
                b2 = line2.p1.Y - (a2 * line2.p2.X);
            }
            else
                x = line2.p1.X;

            if (x == 0 && a1 != a2)
                x = (b1 - b2) / (a1 - a2);

            if (line1.p2.X - line1.p1.X != 0)
                y = (a1 * x) + b1;
            else
                y = (a2 * x) + b2;
            
            return new Point(x, y);
        }

        public void Measure(ref Rectangle rect)
        {
            
            //Thread Measurer = new Thread(MeasureThread);
            //Measurer.Start(rect);
            //Rectangle rect = (Rectangle)data;
            MoveAbsXY(rect.X + (rect.Width / 4), rect.Y);
            Thread.Sleep(500); while (!IsIdle) ;
            SearchXY(rect.X + (rect.Width / 4), rect.Y + rect.Height);
            /*Thread.Sleep(500); */
            
            //DEBUG
            while (!IsIdle) 
                Trace.WriteLine(String.Format("{0} {1} {2} {3} {4}", DateTime.Now, ActXPos, ActYPos, SensorHeight, SensorHeightPLC));

            Rectangle s1 = SearchPos;

            //DEBUG
            rect = s1;
            return;

            MoveAbsXY(rect.X + ((rect.Width / 4) * 3), rect.Y);
            Thread.Sleep(500); while (!IsIdle) ;
            SearchXY(rect.X + ((rect.Width / 4) * 3), rect.Y + rect.Height);
            Thread.Sleep(500); while (!IsIdle) ;
            Rectangle s2 = SearchPos;

            MoveAbsXY(rect.X, rect.Y + (rect.Height / 4));
            Thread.Sleep(500); while (!IsIdle) ;
            SearchXY(rect.X + rect.Width, rect.Y + (rect.Height / 4));
            Thread.Sleep(500); while (!IsIdle) ;
            Rectangle s3 = SearchPos;

            MoveAbsXY(rect.X, rect.Y + ((rect.Height / 4) * 3));
            Thread.Sleep(500); while (!IsIdle) ;
            SearchXY(rect.X + rect.Width, rect.Y + ((rect.Height / 4) * 3));
            Thread.Sleep(500); while (!IsIdle) ;
            Rectangle s4 = SearchPos;

            MoveAbsXY(0, 0);

            //Rectangle s1 = new Rectangle(200, 100, 200, 500);
            //Rectangle s2 = new Rectangle(400, 100, 400, 500);
            //Rectangle s3 = new Rectangle(100, 200, 500, 200);
            //Rectangle s4 = new Rectangle(100, 400, 500, 400);

            Line line1 = new Line(new Point(s1.X, s1.Y), new Point(s2.X, s2.Y));
            Line line2 = new Line(new Point(s3.X, s3.Y), new Point(s4.X, s4.Y));
            Line line3 = new Line(new Point(s1.Width, s1.Height), new Point(s2.Width, s2.Height));
            Line line4 = new Line(new Point(s3.Width, s3.Height), new Point(s4.Width, s4.Height));

            Point p1 = Intersection(line1, line2);
            Point p2 = Intersection(line2, line3);
            Point p3 = Intersection(line3, line4);
            Point p4 = Intersection(line4, line1);
        }

        private void MeasureThread(object data)
        {
            //Rectangle rect = (Rectangle)data;
            //MoveAbsXY(rect.X + (rect.Width / 4), rect.Y);
            //while (!IsIdle) ;
            //SearchXY(rect.X + (rect.Width / 4), rect.Y + rect.Height);
            //while (!IsIdle) ;
            //Rectangle s1 = SearchPos;

            //MoveAbsXY(rect.X + ((rect.Width / 4) * 3), rect.Y);
            //while (!IsIdle) ;
            //SearchXY(rect.X + ((rect.Width / 4) * 3), rect.Y + rect.Height);
            //while (!IsIdle) ;
            //Rectangle s2 = SearchPos;

            //MoveAbsXY(rect.X, rect.Y + (rect.Height / 4));
            //while (!IsIdle) ;
            //SearchXY(rect.X + rect.Width, rect.Y + (rect.Height / 4));
            //while (!IsIdle) ;
            //Rectangle s3 = SearchPos;

            //MoveAbsXY(rect.X, rect.Y + ((rect.Height / 4) * 3));
            //while (!IsIdle) ;
            //SearchXY(rect.X + rect.Width, rect.Y + ((rect.Height / 4) * 3));
            //while (!IsIdle) ;
            //Rectangle s4 = SearchPos;

            ////Rectangle s1 = new Rectangle(200, 100, 200, 500);
            ////Rectangle s2 = new Rectangle(400, 100, 400, 500);
            ////Rectangle s3 = new Rectangle(100, 200, 500, 200);
            ////Rectangle s4 = new Rectangle(100, 400, 500, 400);

            //Line line1 = new Line(new Point(s1.X, s1.Y), new Point(s2.X, s2.Y));
            //Line line2 = new Line(new Point(s3.X, s3.Y), new Point(s4.X, s4.Y));
            //Line line3 = new Line(new Point(s1.Width, s1.Height), new Point(s2.Width, s2.Height));
            //Line line4 = new Line(new Point(s3.Width, s3.Height), new Point(s4.Width, s4.Height));

            //Point p1 = Intersection(line1, line2);
            //Point p2 = Intersection(line2, line3);
            //Point p3 = Intersection(line3, line4);
            //Point p4 = Intersection(line4, line1);
        }

        public void ItemHeight()
        {
            MoveAbsZ(100000);
            Thread.Sleep(500);
            while (!ZIdle)
            {
            }
            SensorLogHigh = SensorHeight + 2000;
            SensorLogLow = SensorHeight + 2000;
        }

        public void Zero()
        {
            Write(Servo, Port, r_home, 1F);
            Write(Servo, Port, z_home, 1F);
            Write(Servo, Port, y_home, 1F);
            Write(Servo, Port, x_home, 1F);
        }

        public void MoveAbsXY(int x, int y)
        {
            if (x >= 0 && x <= MaxX && y >= 0 && y <= MaxY && ActZPos >= MinZ)
            {
                float fx = x, fy = y;
                Write(Servo, Port, x_abspos1, fx);
                Write(Servo, Port, y_abspos1, fy);
                Write(Servo, Port, xy_moveabs, 1F);
            }
            else
                Trace.WriteLine(String.Format("Error in move coordinates: x={0} y={1}", x, y));  
        }

        public void MoveAbsX(int i)
        {
            if (i >= 0 && i <= MaxX && ActZPos >= MinZ)
            {
                float f = i;
                Write(Servo, Port, x_abspos1, f);
                Write(Servo, Port, x_moveabs, 1F);
            }
            else
                Trace.WriteLine(String.Format("Error in move coordinates: x={0}", i));
        }

        public void MoveAbsY(int i)
        {
            if (i >= 0 && i <= MaxY && ActZPos >= MinZ)
            {
                float f = i;
                Write(Servo, Port, y_abspos1, f);
                Write(Servo, Port, y_moveabs, 1F);
            }
            else
                Trace.WriteLine(String.Format("Error in move coordinates: y={0}", i));
        }

        public void MoveAbsZ(int i)
        {
            if (i >= MinZ && i <= MaxZ)
            {
                float t =  (i < ActZPos) ? ZTorqueDown : ZTorqueUp;
                Write(Servo, Port, z_home_torque, t);

                float f = i;
                Write(Servo, Port, z_abspos1, f);
                Write(Servo, Port, z_moveabs, 1F);
            }
            else
                Trace.WriteLine(String.Format("Error in move coordinates: z={0}", i));
        }

        public void MoveAbsR(int i)
        {
            if (i >= -MaxR && i <= MaxR)
            {
                float f = i;
                Write(Servo, Port, r_abspos1, f);
                Write(Servo, Port, r_moveabs, 1F);
            }
            else
                Trace.WriteLine(String.Format("Error in move coordinates: r={0}", i));
        }

        public void SearchXY(int x, int y)
        {
            SearchPos = new Rectangle(0, 0, 0, 0);
            if (x >= 0 && x <= MaxX && y >= 0 && y <= MaxY)
            {
                float fx = x, fy = y;
                Write(Servo, Port, x_abspos1, fx);
                Write(Servo, Port, y_abspos1, fy);
                Write(Servo, Port, xy_search, 1F);
            }
            else
                Trace.WriteLine(String.Format("Error in search coordinates: x={0} y={1}", x, y));
        }

        Object PlcLock = new Object();

        private bool Read(IPAddress ip, int port, int address, ref float f)
        {
            if (DEBUG)
                return false;

            try
            {
                lock (PlcLock)
                {
                    using (Udp udp = new Udp())
                    {
                        msg[Cmd] = CmdRead;
                        msg[Seg] = VR;
                        msg[Node] = Convert.ToByte(new Random().Next() % 128);
                        msg[Adr1] = Convert.ToByte((address & 0xff00) >> 8);
                        msg[Adr2] = Convert.ToByte(address & 0x00ff);
                        msg[Num1] = 0;
                        msg[Num2] = 2;

                        byte[] r = udp.Message(ip, port, msg);
                        if (r != null)
                        {
                            if (r[4] != msg[Node])
                                throw new Exception("Received wrong node# in read request");
                            if (r[12] + r[13] != 0)
                                throw new Exception(String.Format("Received error code in read request {0:X2}{1:X2}", r[12], r[13]));
                        }
                        else
                            throw new Exception("UDP returned null");

                        byte[] c = new byte[4];
                        c[0] = r[Ret + 3];
                        c[1] = r[Ret + 2];
                        c[2] = r[Ret + 1];
                        c[3] = r[Ret + 0];
                        f = BitConverter.ToSingle(c, 0);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("{0} Error reading SERVO address={1:X4} value={2:X4}: {3}", DateTime.Now, address, Convert.ToInt32(f), ex.Message));
                return false; 
            }
        }

        private bool Write(IPAddress ip, int port, int address, float f)
        {
            if (DEBUG)
                return false;

            try
            {
                lock (PlcLock)
                {
                    using (Udp udp = new Udp())
                    {
                        msg[Cmd] = CmdWrite;
                        msg[Seg] = VR;
                        msg[Node] = Convert.ToByte(new Random().Next() % 128);
                        msg[Adr1] = Convert.ToByte((address & 0xff00) >> 8);
                        msg[Adr2] = Convert.ToByte(address & 0x00ff);
                        msg[Num1] = 0;
                        msg[Num2] = 1;

                        byte[] b = BitConverter.GetBytes(f);
                        byte[] c = new byte[4];
                        c[0] = b[3];
                        c[1] = b[2];
                        c[2] = b[1];
                        c[3] = b[0];

                        byte[] r = udp.Message(ip, port, msg.Concat(c).ToArray());
                        if (r != null)
                        {
                            if (r[4] != msg[Node])
                                throw new Exception("Received wrong node# in write request");
                            if (r[12] + r[13] != 0)
                                throw new Exception(String.Format("Received error code in write request {0:X2}{1:X2}", r[12], r[13]));
                        }
                        else
                            throw new Exception("UDP returned null");

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("{0} Error writing SERVO address={1:X4} value={2:X4} : {3}", DateTime.Now, address, Convert.ToInt32(f), ex.Message));
                return false;
            }
        }

        private bool Read(IPAddress ip, int port, int address, ref int i)
        {
            if (DEBUG)
                return false;

            byte[] r;
            try
            {
                lock (PlcLock)
                {
                    using (Udp udp = new Udp())
                    {
                        msg[Cmd] = CmdRead;
                        msg[Seg] = CIO;
                        msg[Node] = Convert.ToByte(new Random().Next() % 128);
                        msg[Adr1] = Convert.ToByte((address & 0xff00) >> 8);
                        msg[Adr2] = Convert.ToByte(address & 0x00ff);
                        msg[Num1] = 0;
                        msg[Num2] = 1;
                        msg[Node] = Convert.ToByte(new Random().Next() % 128);

                        r = udp.Message(ip, port, msg);
                        if (r != null)
                        {
                            if (r[4] != msg[Node])
                                throw new Exception("Received wrong node# in read request");
                            if (r[12] + r[13] != 0)
                                throw new Exception(String.Format("Received error code in read request {0:X2}{1:X2}", r[12], r[13]));
                        }
                        else
                            throw new Exception("UDP returned null");

                        byte[] c = new byte[2];
                        c[0] = r[Ret + 1];
                        c[1] = r[Ret + 0];
                        i = BitConverter.ToUInt16(c, 0);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("{0} Error reading PLC address {1:X4} value={2:X4} : {3}", DateTime.Now, address, i, ex.Message));
                return false;
            }
        }

        private bool Write(IPAddress ip, int port, int address, int i)
        {
            if (DEBUG)
                return false;

            try
            {
                lock (PlcLock)
                {
                    using (Udp udp = new Udp())
                    {
                        msg[Cmd] = CmdWrite;
                        msg[Seg] = CIO;
                        msg[Node] = Convert.ToByte(new Random().Next() % 128);
                        msg[Adr1] = Convert.ToByte((address & 0xff00) >> 8);
                        msg[Adr2] = Convert.ToByte(address & 0x00ff);
                        msg[Num1] = 0;
                        msg[Num2] = 1;

                        byte[] b = BitConverter.GetBytes(i);
                        byte[] c = new byte[2];
                        c[0] = b[1];
                        c[1] = b[0];
                        
                        byte[] r = udp.Message(ip, port, msg.Concat(c).ToArray());
                        if (r != null)
                        {
                            if (r[4] != msg[Node])
                                throw new Exception("Received wrong node# in write request");
                            if (r[12] + r[13] != 0)
                                if (b.Length >= 14)
                                    throw new Exception(String.Format("Received error code in write request {0:X2}{1:X2}", b[12], b[13]));
                                else
                                    throw new Exception(String.Format("Received error code in write request '{0}'", r.ToString()));
                        }
                        else
                            throw new Exception("UDP returned null");

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("{0} Error writing PLC address {1:X4} value={2:X4} : {3}", DateTime.Now, address, i, ex.ToString()));
                return false;
            }
        }    
    }
}
