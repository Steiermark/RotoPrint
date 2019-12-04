using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Plc
{
    interface IPlc
    {
        int ActXPos { get; set; }
        int ActYPos { get; set; }
        int ActZPos { get; set; }
        int ActRPos { get; set; }
        bool Automatic { get; set; }
        int XSpeed { get; set; }
        int YSpeed { get; set; }
        int ZSpeed { get; set; }
        int RSpeed { get; set; }
        int StatusWord { get; set; }
        int StatusBits { get; set; }
        int StatusAction { get; set; }
        int SignalState { get; set; }
        void StartAll();
        void StopAll();
        void ResetAll();
        bool IsHomed { get; set; }
        bool IsIdle { get; set; }
        void Home();
        void Zero();
        void MoveAbs(int x, int y);
        void Search(int x, int y);
        bool Read(IPAddress ip, int port, byte seg, int address, ref float f);
        bool Write(IPAddress ip, int port, byte seg, int address, float f);
        bool Read(IPAddress ip, int port, byte seg, int address, ref int i);
        bool Write(IPAddress ip, int port, byte seg, int address, int i);
    }
}
