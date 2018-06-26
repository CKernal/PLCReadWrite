using HslCommunication;
using System;
using System.Collections.Concurrent;

namespace PLCReadWrite.PLCControl
{
    /// <summary>
    /// PLC读写控制类，提供批量读写方法
    /// </summary>
    public class PLCControl : PLCControlBase
    {
        private ConcurrentDictionary<int, object> m_plcDataCollectionDictionary;

        public PLCControl(IPLC plc) : base(plc)
        {
            m_plcDataCollectionDictionary = new ConcurrentDictionary<int, object>();
        }

        private bool ReadCollectionBit<T>(ref PLCDataCollection<T> plcDataCollection) where T : struct
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


        #region 内部数据集合操作
        /// <summary>
        /// 获取内部数据集合
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public PLCDataCollection<T> GetCollection<T>(int key) where T : struct
        {
            if (m_plcDataCollectionDictionary.ContainsKey(key))
            {
                return (PLCDataCollection<T>)m_plcDataCollectionDictionary[key];
            }
            return null;
        }

        /// <summary>
        /// 添加或更新内部数据集合
        /// </summary>
        /// <param name="key"></param>
        /// <param name="collection"></param>
        public void AddCollection<T>(int key, PLCDataCollection<T> collection) where T : struct
        {
            m_plcDataCollectionDictionary.AddOrUpdate(key, collection, (oldkey, oldvalue) => collection);
        }
        #endregion
    }

}
