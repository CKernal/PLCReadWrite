using PLCReadWrite.PLCControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCReadWrite.PLCControl
{
    /// <summary>
    /// 提供PLC读写控制的接口
    /// </summary>
    public interface IPLCControl
    {
        string IpAddress { get; }
        bool IsConnected { get; }
        int Port { get; }

        event EventHandler OnPlcStatusChanged;

        void Close();
        bool Open();
        bool ReadBool(string startAddr, ushort uSize, ref bool[] sData);
        bool ReadInt16(string startAddr, ushort uSize, ref short[] sData);
        bool WriteBool(string startAddr, bool sData);
        bool WriteBool(string startAddr, bool[] sData);
        bool WriteInt16(string startAddr, short sData);
        bool WriteInt16(string startAddr, short[] sData);

    }
}
