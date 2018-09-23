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
    public class PLCControl : PLCControlBase
    {
        private ConcurrentDictionary<int, PLCDataCollection> m_plcDataCollectionDictionary;

        public PLCControl(IPLC plc) : base(plc)
        {
            m_plcDataCollectionDictionary = new ConcurrentDictionary<int, PLCDataCollection>();
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

        private Task<bool> ReadCollectionBitAsync(PLCDataCollection plcDataCollection)
        {
            string startAddr = plcDataCollection.FullStartAddress;
            ushort uSize = (ushort)plcDataCollection.DataLength;

            var tcs = new TaskCompletionSource<bool>();

            m_plc.BeginReadInt16(startAddr, uSize, read =>
            {
                if (!read.IsSuccess)
                {
                    tcs.SetResult(false);
                    return;
                }

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

                tcs.SetResult(true);
            });

            return tcs.Task;
        }
        private Task<bool> ReadCollectionNormalAsync(PLCDataCollection plcDataCollection)
        {
            string startAddr = plcDataCollection.FullStartAddress;
            ushort uSize = (ushort)plcDataCollection.DataLength;

            var tcs = new TaskCompletionSource<bool>();

            m_plc.BeginRead(startAddr, uSize, read =>
            {
                if (!read.IsSuccess)
                {
                    tcs.SetResult(false);
                    return;
                }

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

                tcs.SetResult(true);
            });

            return tcs.Task;
        }
        /// <summary>
        /// 异步读取一个PLC数据集
        /// </summary>
        /// <param name="plcDataCollection"></param>
        /// <returns></returns>
        public Task<bool> ReadCollectionAsync(PLCDataCollection plcDataCollection)
        {
            if (plcDataCollection.IsBitCollection)
            {
                return ReadCollectionBitAsync(plcDataCollection);
            }
            return ReadCollectionNormalAsync(plcDataCollection);
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
}
