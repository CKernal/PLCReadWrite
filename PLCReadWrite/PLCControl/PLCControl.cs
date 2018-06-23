using HslCommunication;
using System;


namespace PLCReadWrite.PLCControl
{
    /// <summary>
    /// PLC读写控制类，提供批量读写方法
    /// </summary>
    public class PLCControl
    {
        private IPLC m_plc;
        private bool m_isConnected = false;

        /// <summary>
        /// Plc连接状态发生改变时触发
        /// </summary>
        public event EventHandler OnPlcStatusChanged;

        public PLCControl(IPLC plc)
        {
            m_plc = plc;
        }

        /// <summary>
        /// 获取IP地址
        /// </summary>
        public string IpAddress
        {
            get { return m_plc.IpAddress; }
        }

        /// <summary>
        /// 获取端口号
        /// </summary>
        public int Port
        {
            get { return m_plc.Port; }
        }
        /// <summary>
        /// 指示Plc连接状态
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return m_isConnected;
            }
            private set
            {
                if (value != m_isConnected)
                {
                    if (OnPlcStatusChanged != null)
                    {
                        OnPlcStatusChanged.Invoke(this, new PlcStatusArgs(value ? PlcStatus.Connected : PlcStatus.Unconnected));
                    }
                }
                m_isConnected = value;
            }
        }

        /// <summary>
        /// 尝试建立连接
        /// </summary>
        /// <returns></returns>
        public bool Open()
        {
            try
            {
                var connect = m_plc.ConnectServer();
                if (connect.IsSuccess)
                {
                    IsConnected = true;
                    return true;
                }
                else
                {
                    IsConnected = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            if (IsConnected)
            {
                m_plc.ConnectClose();
                IsConnected = false;
            }
        }

        /// <summary>
        /// 批量读取PLC位软元件指定地址的Bool数据
        /// </summary>
        /// <param name="startAddr"></param>
        /// <param name="uSize"></param>
        /// <param name="sData"></param>
        /// <returns></returns>
        public bool ReadBool(string startAddr, ushort uSize, ref bool[] sData)
        {
            if (uSize == 0) { return false; }
            OperateResult<bool[]> read = m_plc.ReadBool(startAddr, uSize);
            IsConnected = read.IsSuccess;
            if (IsConnected)
            {
                sData = read.Content;
            }

            return IsConnected;
        }
        /// <summary>
        /// 批量读取PLC字软元件指定地址的Int16数据
        /// </summary>
        /// <param name="startAddr"></param>
        /// <param name="uSize"></param>
        /// <param name="sData"></param>
        /// <returns></returns>
        public bool ReadInt16(string startAddr, ushort uSize, ref short[] sData)
        {
            if (uSize == 0) { return false; }
            OperateResult<byte[]> read = m_plc.Read(startAddr, uSize);
            IsConnected = read.IsSuccess;
            if (IsConnected)
            {
                short[] tempData = new short[uSize];
                for (int index = 0; index < uSize; index++)
                {
                    tempData[index] = BitConverter.ToInt16(read.Content, index * 2);
                }
                sData = tempData;
            }

            return IsConnected;
        }

        private bool ReadCollectionBit<T>(ref PLCDataCollection<T> plcDataCollection) where T : struct
        {
            string startAddr = plcDataCollection.FullStartAddress;
            ushort uSize = (ushort)plcDataCollection.DataLength;

            OperateResult<byte[]> read = m_plc.Read(startAddr, uSize);
            IsConnected = read.IsSuccess;
            if (IsConnected)
            {
                short[] tempData = new short[uSize];
                for (int index = 0; index < uSize; index++)
                {
                    tempData[index] = m_plc.Transform.TransInt16(read.Content, index * 2);
                }

                byte[] byteData = new byte[uSize * 2];
                for (int index = 0; index < uSize; index++)
                {
                    byte[] tempByte = BitConverter.GetBytes(tempData[index]);
                    byteData[index * 2 + 0] = tempByte[0];
                    byteData[index * 2 + 1] = tempByte[1];
                }

                System.Collections.BitArray bitArray = new System.Collections.BitArray(byteData);
                int sAddr = plcDataCollection.StartAddr;
                Type tType = typeof(T);

                foreach (var d in plcDataCollection)
                {
                    int index = ((d.Addr - sAddr) * 16) + d.Bit;
                    d.Data = (T)(ValueType)bitArray[index];
                }
            }
            return IsConnected;
        }
        private bool ReadCollectionNormal<T>(ref PLCDataCollection<T> plcDataCollection) where T : struct
        {
            string startAddr = plcDataCollection.FullStartAddress;
            ushort uSize = (ushort)plcDataCollection.DataLength;

            OperateResult<byte[]> read = m_plc.Read(startAddr, uSize);
            IsConnected = read.IsSuccess;
            if (IsConnected)
            {
                int sAddr = plcDataCollection.StartAddr;
                DataType dType = plcDataCollection.DataType;
                Type tType = typeof(T);

                foreach (var d in plcDataCollection)
                {
                    //根据数据类型为每个PLCData赋值
                    int index = d.Addr - sAddr;
                    switch (dType)
                    {
                        case DataType.BoolAddress:
                            d.Data = (T)(ValueType)m_plc.Transform.TransBool(read.Content, index);
                            break;
                        case DataType.Int16Address:
                            d.Data = (T)(ValueType)m_plc.Transform.TransInt16(read.Content, index * 2);
                            break;
                        case DataType.Int32Address:
                            d.Data = (T)(ValueType)m_plc.Transform.TransInt32(read.Content, index * 2);
                            break;
                        case DataType.Int64Address:
                            d.Data = (T)(ValueType)m_plc.Transform.TransInt64(read.Content, index * 2);
                            break;
                        case DataType.Float32Address:
                            d.Data = (T)(ValueType)m_plc.Transform.TransSingle(read.Content, index * 2);
                            break;
                        case DataType.Double64Address:
                            d.Data = (T)(ValueType)m_plc.Transform.TransDouble(read.Content, index * 2);
                            break;
                        default:
                            d.Data = default(T);
                            break;
                    }
                }
            }

            return IsConnected;
        }
        /// <summary>
        /// 读取一个PLC数据集
        /// </summary>
        /// <param name="plcDataCollection"></param>
        /// <returns></returns>
        public bool ReadCollection<T>(ref PLCDataCollection<T> plcDataCollection) where T : struct
        {
            if (plcDataCollection.DataLength <= 0
                || plcDataCollection.DataLength > ushort.MaxValue)
            {
                return false;
            }

            if (plcDataCollection.IsBitCollection)
            {
                return ReadCollectionBit(ref plcDataCollection);
            }
            return ReadCollectionNormal(ref plcDataCollection);
        }

        public bool WriteInt16(string startAddr, short sData)
        {
            OperateResult write = m_plc.Write(startAddr, sData);
            IsConnected = write.IsSuccess;
            return IsConnected;
        }
        public bool WriteInt16(string startAddr, short[] sData)
        {
            OperateResult write = m_plc.Write(startAddr, sData);
            IsConnected = write.IsSuccess;
            return IsConnected;
        }

        public bool WriteBool(string startAddr, bool sData)
        {
            OperateResult write = m_plc.Write(startAddr, sData);
            IsConnected = write.IsSuccess;
            return IsConnected;
        }
        public bool WriteBool(string startAddr, bool[] sData)
        {
            OperateResult write = m_plc.Write(startAddr, sData);
            IsConnected = write.IsSuccess;
            return IsConnected;
        }
    }

    #region PLC状态改变的事件参数定义
    public enum PlcStatus
    {
        Connected,
        Unconnected,
    }
    public class PlcStatusArgs : EventArgs
    {
        private PlcStatus plcStatus;

        public PlcStatus Status
        {
            get { return plcStatus; }
            set { plcStatus = value; }
        }

        public PlcStatusArgs(PlcStatus status)
        {
            this.plcStatus = status;
        }
    }
    #endregion
}
