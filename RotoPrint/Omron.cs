using System;
using System.Net;
using System.Net.Sockets;

namespace RotoPrint
{

    class Omron
    {
        byte[] RxData = new byte[2000];
        int NofRxData = 0;

        EndPoint plc = new IPEndPoint(IPAddress.Parse("192.168.10.240"), 9600);

        private byte[] TxData = new byte[]
        {
            // Full UDP packet: 80 00 02 00 00 00 00 05 00 19 01 02 82 00 64 00 00 01 00 01
                // header
                0x80, //0.(ICF) Display frame information: 1000 0001
                0x00, //1.(RSV) Reserved by system: (hex)00
                0x02, //2.(GCT) Permissible number of gateways: (hex)02
                0x00, //3.(DNA) Destination network address: (hex)00, local network
                0x00, //4.(DA1) Destination node address: (hex)00, local PLC unit
                0x00, //5.(DA2) Destination unit address: (hex)00, PLC
                0x00, //6.(SNA) Source network address: (hex)00, local network
                101, //7.(SA1) Source node address: (hex)05, PC's IP is 192.168.10.101
                0x00, //8.(SA2) Source unit address: (hex)00, PC only has one ethernet
                0x19, //9.(SID) Service ID: just give a random number 19

                // command
                0x01, //10.(MRC) Main request code: 01, memory area write
                0x02, //11.(SRC) Sub-request code: 02, memory area write

            // PLC Memory Area
                0x82, //12.Memory area code, 1 byte: 0x82(DM) 0xB0(CIO) 0x55(WA)

            // Address information
                0x00, //13.Write start address (2 bytes) DM100
                0x64, //14

                0x00, //15

                0x00, //16.Words to read/write msb
                0x01, //17.Words to read/write lsb

                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
        };


        public void YposHome(int yAxis)
        {
            FinsWrite(yAxis, 0);
        }

        public void YposSet(int yAxis, int pos)
        {
            FinsWrite(100 + yAxis, pos);
        }

        public int YposGet(int yAxis)
        {
            int pos = 0;
            FinsRead(yAxis, ref pos);
            return pos;
        }

        public void ZposHome()
        {
            FinsWriteBit(0, 0, 1);
        }

        public void ZposSet(double zPos)
        {
            FinsWrite(150, zPos);
        }

        public double ZposGet()
        {
            double zPos = 0;
            FinsRead(100, ref zPos);
            return zPos;
        }


        private int UdpTransceive(byte[] msg, int BytesToSend)
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int i = 0;
                while (true)
                {
                    try
                    {
                        socket.ReceiveTimeout = 200;
                        socket.SendTo(msg, BytesToSend, 0, plc);
                        NofRxData = socket.ReceiveFrom(RxData, ref plc);
                        return NofRxData;
                    }
                    catch (Exception ex)
                    {
                        if (i++ == 3)
                        {
                            return -1;
//                            throw new Exception("Computer not connected to OMRON PLC! Please connect PLC and restart program");
                        }
                    }
                }
            }
        }

        private bool FinsRtx(int Address, bool Write, int type)
        {
            bool Status = false;
            int ErrorCode = 0;
            int nofTxData = 18;

            if (Write) TxData[11] = 0x02; // cmd write
            else TxData[11] = 0x01; // cmd read

            if (type < 10)
                TxData[12] = 0x82; // DM
            else if (type == 10)
                TxData[12] = 0x31; // WA single bit
            else if (type == 11)
                TxData[12] = 0xB1; // WA 16bit


            TxData[9] = Convert.ToByte(new Random().Next() % 128); // node

            TxData[13] = Convert.ToByte((Address >> 8) & 0xff);
            TxData[14] = Convert.ToByte(Address & 0xff);
            if (type != 10) TxData[15] = 0;
            TxData[16] = 0;

            if (type == 1 || type == 11) // int
            {
                TxData[17] = 1;
                if (Write) nofTxData = 20;
                else nofTxData = 18;
            }

            if (type == 2) // float
            {
                TxData[17] = 2;
                if (Write) nofTxData = 22;
                else nofTxData = 18;
            }

            if (type == 4) // double 
            {
                TxData[17] = 4;
                if (Write) nofTxData = 26;
                else nofTxData = 18;
            }

            if (type == 10) // bits
            {
                TxData[17] = 1;
                if (Write) nofTxData = 19;
                else nofTxData = 18;
            }

            NofRxData = UdpTransceive(TxData, nofTxData);

            if (NofRxData < 0)
            {
                return Status;
            }
            else if (NofRxData < 14)
                throw new Exception("Too few data received");
            else
            {
                ErrorCode = RxData[12] << 8 + RxData[13];
                if (RxData[6] != TxData[3] || RxData[7] != TxData[4] || RxData[8] != TxData[5])
                    throw new Exception("Illegal source address error");
                else if (RxData[9] != TxData[9])
                    throw new Exception("Illegal Source ID");
                else if (ErrorCode > 0 && ErrorCode != 0x0040)
                    throw new Exception(String.Format("Received error code in float write {0:X4}", ErrorCode));
                else
                    Status = true;
            }
            return Status;
        }

        public bool FinsWriteBit(int Address, byte BitPos, int i)
        {
            TxData[15] = BitPos;
            byte[] b = BitConverter.GetBytes(i);
            TxData[18] = b[0];
            //        TxData[19] = b[0];
            return FinsRtx(Address, true, 10);
        }


        public bool FinsWriteBits(int Address, int i)
        {
            byte[] b = BitConverter.GetBytes(i);
            TxData[18] = b[1];
            TxData[19] = b[0];
            return FinsRtx(Address, true, 11);
        }

        public bool FinsReadBits(int Address, ref int i)
        {
            bool Status = false;
            if (FinsRtx(Address, false, 11))
            {
                byte[] c = new byte[2];
                c[0] = RxData[14 + 1];
                c[1] = RxData[14 + 0];
                i = BitConverter.ToUInt16(c, 0);
                Status = true;
            }
            return Status;
        }



        public bool FinsWrite(int Address, int i)
        {
            byte[] b = BitConverter.GetBytes(i);
            TxData[18] = b[1];
            TxData[19] = b[0];
            return FinsRtx(Address, true, 1);
        }

        public bool FinsWrite(int Address, float f)
        {
            byte[] b = BitConverter.GetBytes(f);
            TxData[18] = b[1];
            TxData[19] = b[0];
            TxData[20] = b[3];
            TxData[21] = b[2];
            return FinsRtx(Address, true, 2);
        }

        public bool FinsWrite(int Address, double d)
        {

            byte[] b = BitConverter.GetBytes(d);
            TxData[18] = b[1];
            TxData[19] = b[0];
            TxData[20] = b[3];
            TxData[21] = b[2];
            TxData[22] = b[5];
            TxData[23] = b[4];
            TxData[24] = b[7];
            TxData[25] = b[6];

            return FinsRtx(Address, true, 4);
        }

        public bool FinsRead(int Address, ref int i)
        {
            bool Status = false;
            if (FinsRtx(Address, false, 1))
            {
                byte[] c = new byte[2];
                c[0] = RxData[14 + 1];
                c[1] = RxData[14 + 0];
                i = BitConverter.ToUInt16(c, 0);
                Status = true;
            }
            return Status;
        }

        public bool FinsRead(int Address, ref float f)
        {
            bool Status = false;

            if (FinsRtx(Address, false, 2))
            {
                byte[] c = new byte[4];
                c[0] = RxData[14 + 1];
                c[1] = RxData[14 + 0];
                c[2] = RxData[14 + 3];
                c[3] = RxData[14 + 2];
                f = BitConverter.ToSingle(c, 0);
                Status = true;
            }
            return Status;
        }

        public bool FinsRead(int Address, ref double d)
        {
            bool Status = false;

            if (FinsRtx(Address, false, 4))
            {
                byte[] c = new byte[8];
                c[0] = RxData[14 + 1];
                c[1] = RxData[14 + 0];
                c[2] = RxData[14 + 3];
                c[3] = RxData[14 + 2];
                c[4] = RxData[14 + 5];
                c[5] = RxData[14 + 4];
                c[6] = RxData[14 + 7];
                c[7] = RxData[14 + 6];


                d = BitConverter.ToDouble(c, 0);
                Status = true;
            }
            return Status;
        }

    }
}