using HslCommunication;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCReadWrite.PLCControl.String
{
    /// <summary>
    /// PLC读写控制类，提供批量读写方法
    /// </summary>
    public class PLCControl
    {
        private IPLC m_plc;
        private bool m_isConnected = false;
        private ConcurrentDictionary<int, PLCDataCollection> m_plcDataCollectionDictionary;

        /// <summary>
        /// Plc连接状态发生改变时触发
        /// </summary>
        public event EventHandler OnPlcStatusChanged;

        public PLCControl(IPLC plc)
        {
            m_plc = plc;
            m_plcDataCollectionDictionary = new ConcurrentDictionary<int, PLCDataCollection>();
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
                }
                else
                {
                    IsConnected = false;
                }
                return IsConnected;
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
            OperateResult<short[]> read = m_plc.ReadInt16(startAddr, uSize);
            IsConnected = read.IsSuccess;
            if (IsConnected)
            {
                sData = read.Content;
            }

            return IsConnected;
        }

        private bool ReadCollectionBit(ref PLCDataCollection plcDataCollection)
        {
            string startAddr = plcDataCollection.FullStartAddress;
            ushort uSize = (ushort)plcDataCollection.DataLength;

            OperateResult<short[]> read = m_plc.ReadInt16(startAddr, uSize);
            IsConnected = read.IsSuccess;
            if (IsConnected)
            {
                byte[] byteData = new byte[uSize * 2];
                for (int index = 0; index < uSize; index++)
                {
                    byte[] tempByte = BitConverter.GetBytes(read.Content[index]);
                    byteData[index * 2 + 0] = tempByte[0];
                    byteData[index * 2 + 1] = tempByte[1];
                }

                System.Collections.BitArray bitArray = new System.Collections.BitArray(byteData);
                int sAddr = plcDataCollection.StartAddr;

                foreach (var d in plcDataCollection)
                {
                    int index = ((d.Addr - sAddr) * 16) + d.Bit;
                    d.Data = bitArray[index].ToString();
                }
            }
            return IsConnected;
        }
        private bool ReadCollectionNormal(ref PLCDataCollection plcDataCollection)
        {
            string startAddr = plcDataCollection.FullStartAddress;
            ushort uSize = (ushort)plcDataCollection.DataLength;

            OperateResult<byte[]> read = m_plc.Read(startAddr, uSize);
            IsConnected = read.IsSuccess;
            if (IsConnected)
            {
                int sAddr = plcDataCollection.StartAddr;

                foreach (var d in plcDataCollection)
                {
                    //根据数据类型为每个PLCData赋值
                    int index = d.Addr - sAddr;
                    switch (d.DataType)
                    {
                        case DataType.BoolAddress:
                            d.Data = m_plc.Transform.TransBool(read.Content, index).ToString();
                            break;
                        case DataType.Int16Address:
                            d.Data = m_plc.Transform.TransInt16(read.Content, index * 2).ToString();
                            break;
                        case DataType.Int32Address:
                            d.Data = m_plc.Transform.TransInt32(read.Content, index * 2).ToString();
                            break;
                        case DataType.Int64Address:
                            d.Data = m_plc.Transform.TransInt64(read.Content, index * 2).ToString();
                            break;
                        case DataType.Float32Address:
                            d.Data = m_plc.Transform.TransSingle(read.Content, index * 2).ToString();
                            break;
                        case DataType.Double64Address:
                            d.Data = m_plc.Transform.TransDouble(read.Content, index * 2).ToString();
                            break;
                        case DataType.StringAddress:
                            //PLC中一个字地址为2个字节（2Byte），可储存两个ASCII字符，一个中文字符。需要解码的字节数为：（PLC字地址长度*2）
                            //此处只支持ASCII字符，若想支持中文读取，可使用支持中文的编码如Unicode，PLC端也相应使用Unicode方式写入
                            //d.Data = Encoding.ASCII.GetString(read.Content, index * 2, d.Length * 2);
                            d.Data = m_plc.Transform.TransString(read.Content, index * 2, d.Length * 2, Encoding.ASCII).ToString();
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
        public bool ReadCollection(ref PLCDataCollection plcDataCollection)
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

        #region 内部数据集合操作
        /// <summary>
        /// 获取内部数据集合
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public PLCDataCollection GetCollection(int key)
        {
            if (m_plcDataCollectionDictionary.ContainsKey(key))
            {
                return m_plcDataCollectionDictionary[key];
            }
            return null;
        }

        /// <summary>
        /// 添加或更新内部数据集合
        /// </summary>
        /// <param name="key"></param>
        /// <param name="collection"></param>
        public void AddCollection(int key, PLCDataCollection collection)
        {
            m_plcDataCollectionDictionary.AddOrUpdate(key, collection, (oldkey, oldvalue) => collection);
        }
        #endregion
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
