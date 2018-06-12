using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCReadWrite.PLCControl.String
{
    /// <summary>
    /// PLC数据集合（仅支持同一种地址前缀）
    /// </summary>
    public class PLCDataCollection : IEnumerable<PLCData>
    {
        public string Name { get; private set; }
        public string Prefix { get; private set; }
        public int StartAddr { get; private set; }
        public int DataLength { get; private set; }
        public bool IsBitCollection { get; private set; }
        public string FullStartAddress
        {
            get { return string.Format("{0}{1}", Prefix, StartAddr); }
        }

        /// <summary>
        /// 集合内部储存结构
        /// </summary>
        private List<PLCData> m_plcDataList = new List<PLCData>();

        public PLCData this[int index]
        {
            get { return m_plcDataList[index]; }
            set { m_plcDataList[index] = value; }
        }

        public PLCDataCollection(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 清空数据集合
        /// </summary>
        public void Clear()
        {
            m_plcDataList.Clear();
            Update();
        }
        /// <summary>
        /// 清空数据集数据
        /// </summary>
        public void ClearData()
        {
            foreach (var d in m_plcDataList)
            {
                d.Data = "";
                d.OldData = "";
            }
            Update();
        }
        /// <summary>
        /// 向PLC数据集中添加一个地址，仅供类内部使用
        /// </summary>
        /// <param name="plcData"></param>
        /// <returns></returns>
        private bool Add(PLCData plcData)
        {
            if (m_plcDataList.Count == 0)
            {
                this.Prefix = plcData.Prefix;
                this.IsBitCollection = plcData.IsBit;
            }

            if (this.Prefix == plcData.Prefix
                && this.IsBitCollection == plcData.IsBit)
            {
                int matchIndex = -1;

                if (IsBitCollection)
                {
                    matchIndex = m_plcDataList.FindIndex(d =>
                        d.Prefix == plcData.Prefix
                        && d.Addr == plcData.Addr
                        && d.Bit == plcData.Bit);
                }
                else
                {
                    matchIndex = m_plcDataList.FindIndex(d =>
                        d.Prefix == plcData.Prefix
                        && d.Addr == plcData.Addr);
                }

                if (matchIndex < 0)
                {
                    m_plcDataList.Add(plcData);
                    Update();
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 向PLC数据集中添加一个Bit地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <returns></returns>
        public bool AddBit(string name, string addr, string secondName = null)
        {
            if (addr.IndexOf('.') < 0)
            {
                return false;
            }

            string[] splits = addr.Substring(1).Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            PLCData plcData = new PLCData();
            plcData.Name = name;
            plcData.SecondName = secondName;
            plcData.Prefix = addr[0].ToString();
            plcData.Addr = int.Parse(splits[0]);
            plcData.Bit = byte.Parse(splits[1]);
            plcData.DataType = DataType.BoolAddress;
            plcData.Length = 1;
            plcData.IsBit = true;

            return this.Add(plcData);
        }
        /// <summary>
        /// 向PLC数据集中添加多个Bit地址，在原基础地址上自动添加count个Bit地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool AddBit(string name, string addr, int count)
        {
            if (addr.IndexOf('.') < 0)
            {
                return false;
            }

            bool ret = false;
            int baseAddr = 0;
            byte basebit = 0;
            string[] splits = addr.Substring(1).Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            baseAddr = int.Parse(splits[0]);
            basebit = byte.Parse(splits[1]);

            for (int i = 0; i < count; i++)
            {
                int curAddr = baseAddr + i;

                string newSecondName = i.ToString();
                string newAddr = string.Format("{0}{1}.{2}", addr[0], curAddr, basebit);
                ret &= AddBit(name, newAddr, newSecondName);
            }

            return ret;
        }
        /// <summary>
        /// 向PLC数据集中添加一个地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <param name="dataType"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool Add(string name, string addr, DataType dataType = DataType.Int16Address, int length = 1, string secondName = null)
        {
            PLCData plcData = new PLCData();
            plcData.Name = name;
            plcData.SecondName = secondName;
            plcData.Prefix = addr[0].ToString();
            plcData.Addr = int.Parse(addr.Substring(1));
            plcData.DataType = dataType;
            plcData.Length = length;

            return this.Add(plcData);
        }

        /// <summary>
        /// 向PLC数据集中添加多个地址，在原基础地址上自动添加count个地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <param name="dataType"></param>
        /// <param name="length"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool Add(string name, string addr, DataType dataType, int length, int count)
        {
            bool ret = false;
            int baseAddr = 0;
            baseAddr = int.Parse(addr.Substring(1));

            for (int i = 0; i < count; i++)
            {
                int curAddr = baseAddr + (i * length);

                string secondName = i.ToString();
                string newAddr = string.Format("{0}{1}", addr[0], curAddr);
                ret &= Add(name, newAddr, dataType, length, secondName);
            }

            return ret;
        }

        public void Remove(string name)
        {
            m_plcDataList.RemoveAll(d => d.Name == name);
            Update();
        }
        /// <summary>
        /// 更新数据集，仅供类内部使用
        /// </summary>
        private void Update()
        {
            int startAddr = int.MaxValue;
            int endAddr = 0;
            int endUnitLength = 1;

            foreach (var d in m_plcDataList)
            {
                switch (d.DataType)
                {
                    case DataType.BoolAddress:
                        d.Length = 1;
                        break;
                    case DataType.Int16Address:
                        d.Length = 1;
                        break;
                    case DataType.Int32Address:
                        d.Length = 2;
                        break;
                    case DataType.Int64Address:
                        d.Length = 4;
                        break;
                    case DataType.Float32Address:
                        d.Length = 2;
                        break;
                    case DataType.Double64Address:
                        d.Length = 4;
                        break;
                    case DataType.StringAddress:
                        //数据类型为字符串时，长度值由外部传入
                        break;
                }

                if (d.Addr < startAddr) { startAddr = d.Addr; }
                if (d.Addr > endAddr) { endAddr = d.Addr; endUnitLength = d.Length; }
            }

            this.StartAddr = startAddr;
            this.DataLength = (endAddr + endUnitLength) - startAddr;
        }

        public IEnumerator<PLCData> GetEnumerator()
        {
            for (int index = 0; index < m_plcDataList.Count; index++)
            {
                yield return m_plcDataList[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
