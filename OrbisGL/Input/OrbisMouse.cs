using OrbisGL.GL2D;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Xml.Schema;
using Orbis.Internals;

namespace OrbisGL.Input
{
    public unsafe class OrbisMouse : IMouse
    {
        int CurrentX;
        int CurrentY;

        int MouseHandle = 0;
        int CurrentUserID = 0;
        
        MouseButtons CurrentButtons = 0;
        OrbisMouseData* CurrentData = null;
        const int bulkMouseData = 8;
        
        public void RefreshData(long Tick)
        {
            int Count = sceMouseRead(MouseHandle, CurrentData, bulkMouseData);
            if (Count > Constants.ORBIS_OK)
            {
                CurrentButtons = 0;
                for (int i = 0; i < Count; i++)
                {
                    if ((CurrentData[i].buttons & Constants.ORBIS_MOUSE_BUTTON_PRIMARY) != 0)
                        CurrentButtons |= MouseButtons.Left;
                    
                    if ((CurrentData[i].buttons & Constants.ORBIS_MOUSE_BUTTON_SECONDARY) != 0)
                        CurrentButtons |= MouseButtons.Right;
                    
                    if ((CurrentData[i].buttons & Constants.ORBIS_MOUSE_BUTTON_OPTIONAL) != 0)
                        CurrentButtons |= MouseButtons.Middle;

                    CurrentX += CurrentData[i].xAxis;
                    CurrentY += CurrentData[i].yAxis;
                }

                if (CurrentX < 0)
                    CurrentX = 0;

                if (CurrentY < 0)
                    CurrentY = 0;

                if (CurrentX >= Coordinates2D.Width)
                    CurrentX = Coordinates2D.Width;

                if (CurrentY >= Coordinates2D.Height)
                    CurrentY = Coordinates2D.Height;
            }
            
        }

        public MouseButtons GetMouseButtons()
        {
            return CurrentButtons;
        }

        public Vector2 GetPosition()
        {
            return new Vector2(CurrentX, CurrentY);
        }

        public bool Initialize(int UserID = Constants.ORBIS_USER_SERVICE_USER_ID_ALL_USERS)
        {
            CurrentUserID = UserID;
            
            if (sceMouseInit() != Constants.ORBIS_OK)
                return false;

            var OpenParam = new OrbisMouseOpenParam();
            OpenParam.behaviorFlag = 0; //Constants.ORBIS_MOUSE_OPEN_PARAM_MERGED;

            MouseHandle = sceMouseOpen(CurrentUserID, Constants.ORBIS_MOUSE_PORT_TYPE_STANDARD, 0, OpenParam);

            CurrentX = Coordinates2D.Width / 2;
            CurrentY = Coordinates2D.Height / 2;

            var MouseData = new OrbisMouseData();
            var pMouseData = Marshal.AllocHGlobal(sizeof(OrbisMouseData) * bulkMouseData);
            CurrentData = (OrbisMouseData*)pMouseData.ToPointer();
            for (int i = 0; i < bulkMouseData; i++)
            {
                CurrentData[i] = MouseData;
            }
            
            return true;
        }

        public void Dispose()
        {
            sceMouseClose();
        }


        [DllImport("libSceMouse.sprx")]
        static extern int sceMouseInit();

        [DllImport("libSceMouse.sprx")]
        static extern int sceMouseClose();

        [DllImport("libSceMouse.sprx")]
        static extern int sceMouseOpen(int UserID, int Type, int Index, OrbisMouseOpenParam pParam);

        [DllImport("libSceMouse.sprx")]
        static extern int sceMouseRead(int Handle, OrbisMouseData* pData, int num);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct OrbisMouseOpenParam
        {
            public byte behaviorFlag;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            public byte[] reserve;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct OrbisMouseData
        {
            public ulong timestamp;
            public int connected;
            public uint buttons;
            public int xAxis;
            public int yAxis;
            public int wheel;
            public int tilt;
            public fixed byte reserve[8];
        }

    }
}
