//***************************************************************************************************
//
//                    APX-7400 DLL Wrapper Class Sample
//
//        Copyright (c) 2015 AVAL DATA Corporation All Right Reserved.
//-----------------------------------------------------------------------------
// Module Name :  <APX7400.cpp>
// Comment     :  DLL wrapper class sample
//-----------------------------------------------------------------------------
// [내용]
//  apx7400.dll을 C# 위에서 접근하기 위한 DLL IMPORT와 간단한 클래스·상수를 정의하고 있습니다.
//
// [주의]
//  데이터 전송은 .NET Framework 상의 메모리 영역으로 직접 전송할 수 없음에 유의하십시오.
//  본 샘플에서는 Marshal. Alloc HGlobal에 의해 Win32상으로부터의 메모리 블록을 확보하고 있습니다.
// 
//***************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace apx7400Lib
{
    class apx7400
    {
        public apx7400()
        {
        }
        
        // Error Code
        public enum ErrorCode : int
        {
            APX7400_NO_ERROR           = 0,   // No Error
            APX7400_NO_TARGET_BOARD    = -1,  // Not Board Detected
            APX7400_INVALID_PARAMETER  = -2,  // Invalid Parameter
            APX7400_RESOURCE_ERROR     = -3,  // Resource Allocate Error
            APX7400_LINK_DOWN          = -4,  // Link Down Error
            APX7400_CABLE_DISCONNECT   = -5,  // Cable Disconnect Error
            APX7400_TIME_OUT           = -6,  // Wait Event Timeout or Library Mutex Timeout
            APX7400_INVALID_REQUEST    = -7,  // No Support Action
            APX7400_DEVICE_BUSY        = -8,  // Device Busy... Prease Retry Request
            APX7400_DRIVER_ERROR       = -9,  // Device Driver Internal Error
            APX7400_HARDWARE_ERROR     = -10, // Device Error Detect
            APX7400_BOOT_RECOVERY_FPGA = -11, // FPGA boot from Recovery Version
            APX7400_NODEID_DUPLICATED  = -12, // NodeID duplicated in Link
            APX7400_FIFO_DISCONNECT    = -13  // FIFO communication is disconnected
        }
        
        // Error/Warning Status - apx7400WaitErrorStatus(), apx7400RegisterCallBack()
        public enum BoardStatus : int
        {
            BIT_MSK_ALL_STATUS      = (0x01031F1F), // ALL Status Bit
            BIT_MSK_CH1_STATUS      = (0x0103001F), // OPT Ch1 and Hardware Error Status Bit
            BIT_MSK_CH2_STATUS      = (0x01031F00), // OPT Ch2 and Hardware Error Status Bit
            BIT_ERR_FPGA_TEMP       = (1 <<  24),   // [Error]   FPGA Temprature Alarm         -> ACT LED = Red
            BIT_WRN_NODE_CHANGE     = (1 <<  17),   // [Warning] Nodes Change in Link
            BIT_ERR_NODE_OVERLAP    = (1 <<  16),   // [Error]   NodeID Overlap in Link        -> ERR LED = Red (Overlap Node Only)
            BIT_ERR_COMM_ERROR_CH2  = (1 <<  12),   // [Error]   OPT Ch2 - Communication Error (nealy equal Link Down)
            BIT_WRN_SEND_RETRY_CH2  = (1 <<  11),   // [Warning] OPT Ch2 - Send Retry          -> LNK2 LED = Orange
            BIT_WRN_RECV_RETRY_CH2  = (1 <<  10),   // [Warning] OPT Ch2 - Recv Retry          -> LNK2 LED = Orange
            BIT_WRN_LINK_CHANGE_CH2 = (1 <<   9),   // [Warning] OPT Ch2 - Link Change
            BIT_ERR_LINK_DOWN_CH2   = (1 <<   8),   // [Error]   OPT Ch2 - Link Down           -> LNK2 LED = Red
            BIT_ERR_COMM_ERROR_CH1  = (1 <<   4),   // [Error]   OPT Ch1 - Communication Error (nealy equal Link Down)
            BIT_WRN_SEND_RETRY_CH1  = (1 <<   3),   // [Warning] OPT Ch1 - Send Retry          -> LNK1 LED = Orange
            BIT_WRN_RECV_RETRY_CH1  = (1 <<   2),   // [Warning] OPT Ch1 - Recv Retry          -> LNK1 LED = Orange
            BIT_WRN_LINK_CHANGE_CH1 = (1 <<   1),   // [Warning] OPT Ch1 - Link Change
            BIT_ERR_LINK_DOWN_CH1   = (1 <<   0)    // [Error]   OPT Ch1 - Link Down           -> LNK1 LED = Red
        }
        
        // define
        public uint FPGA_YEAR(uint reg)    { return ((reg & 0xFF000000) >> 24); }
        public uint FPGA_MON(uint reg)     { return ((reg & 0x00FF0000) >> 16); }
        public uint FPGA_DAY(uint reg)     { return ((reg & 0x0000FF00) >>  8); }
        public uint FPGA_REV(uint reg)     { return ((reg & 0x000000FF) >>  0); }
        
        public uint SDK_MAJOR(uint ver)    { return ((ver & 0xFF000000) >> 24); }
        public uint SDK_MINOR(uint ver)    { return ((ver & 0x00FF0000) >> 16); }
        public uint SDK_BUILD(uint ver)    { return ((ver & 0x0000FF00) >>  8); }
        public uint SDK_REV(uint ver)      { return ((ver & 0x000000FF) >>  0); }
        
        // Board Inf.
        public struct APX7400_BOARD_INFO
        {
            public int  BoardType;          // Board type (0:APX-7401, 1:APX-7402, else: not support board)
            public int  OptChNum;           // number of Optical channel
            public int  MemoryAreaNum;      // number of Memory area
            public int  MemoryAreaSize;     // number of Memory size [MB]
            public int  DipSW;              // DipSW(SW3) value
            public int  BoardTemp;          // Board Temperature [degrees Celsius]
            public int  FpgaTemp;           // FPGA  Temperature [degrees Celsius]
            public uint FpgaVer;            // FPGA version
            public uint DriverVer;          // Drivre version
            public uint LibraryVer;         // Library version
        }
        
        // Error callback routone
        public delegate void ERRORCALLBACK(int nBoardIndex, int ErrorBit, IntPtr pArgument);
        
        
        #region DLLImport
        private const string APX7400DLL = "apx7400.dll";
        
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400Initialize();
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400Finalize();
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400GetBoardCount();
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400GetBoardInfo(int BoardIndex, out APX7400_BOARD_INFO BoardInfo);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400SetNodeID(int BoardIndex, int NodeID);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400GetNodeID(int BoardIndex, out int NodeID);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400GetNodeCount(int BoardIndex, int Channel);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400GetLinkNodes(int BoardIndex, out uint NodesCh1, out uint NodesCh2, out uint Overlap);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400WaitErrorStatus(int BoardIndex, int WaitBit, uint Timeout);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400RegisterCallBack(int BoardIndex, int WaitBit, ERRORCALLBACK CallBack, IntPtr Arguments);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400SendMailBox(int BoardIndex, int Channel, int NodeID, int MailBoxNo, uint MailData);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400RecvMailBox(int BoardIndex, int MailBoxNo, out uint MailData, uint Timeout);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400WriteTargetMemory(int BoardIndex, int Channel, uint Nodes, int MemoryAreaNo, uint Offset, IntPtr Buffer, uint Size, int MailBoxNo, uint MailData);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400ReadTargetMemory(int BoardIndex, int Channel, int NodeID, int MemoryAreaNo, uint Offset, IntPtr Buffer, uint Size);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400WriteMyMemory(int BoardIndex, int MemoryAreaNo, uint Offset, IntPtr Buffer, uint Size);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400ReadMyMemory(int BoardIndex, int MemoryAreaNo, uint Offset, IntPtr Buffer, uint Size);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400WriteTargetRegister(int BoardIndex, int Channel, int NodeID, uint Offset, uint Data);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400ReadTargetRegister(int BoardIndex, int Channel, int NodeID, uint Offset, out uint Data);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400RegisterBuffer(int BoardIndex, int MemoryAreaNo, out IntPtr Buffer, uint Size, int Mode);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400UnRegisterBuffer(int BoardIndex, int MemoryAreaNo, int FifoNo);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400ConnectFifo(int BoardIndex, int Channel, int NodeID, int MemoryAreaNo, int Mode, uint Timeout);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400DisconnectFifo(int BoardIndex, int MemoryAreaNo);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400StartFifo(int BoardIndex, int MemoryAreaNo, int BufferNo, uint Size);
        [DllImport(APX7400DLL)]
        public extern static ErrorCode apx7400WaitEndFifo(int BoardIndex, int MemoryAreaNo, out uint Size, uint Timeout);
        
        #endregion
    }
}
