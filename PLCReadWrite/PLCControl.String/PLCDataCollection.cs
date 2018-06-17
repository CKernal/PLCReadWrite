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
    public class PLCDataCollection : ICollection<PLCData>
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
        public int Count
        {
            get { return m_plcDataList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public PLCDataCollection(string name)
        {
            Name = name;
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
        }
        /// <summary>
        /// 向PLC数据集中添加一个地址，仅供类内部使用
        /// </summary>
        /// <param name="plcData"></param>
        /// <returns></returns>
        private bool AddItem(PLCData plcData)
        {
            if (m_plcDataList.Count == 0)
            {
                this.Prefix = plcData.Prefix;
                this.IsBitCollection = plcData.IsBit;
            }

            if (this.Prefix == plcData.Prefix
                && this.IsBitCollection == plcData.IsBit)
            {
                var matchCount = m_plcDataList.Where(d =>
                {
                    bool bret = false;
                    bret = d.Prefix == plcData.Prefix && d.Addr == plcData.Addr;
                    if (IsBitCollection)
                    {
                        bret &= d.Bit == plcData.Bit;
                    }
                    return bret;

                }).Count();

                if (matchCount <= 0)
                {
                    m_plcDataList.Add(plcData);
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
        public void AddBit(string name, string addr, string petName = null)
        {
            if (!addr.Contains('.'))
            {
                return;
            }

            string[] splits = addr.Substring(1).Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            PLCData plcData = new PLCData()
            {
                Name = name,
                PetName = petName,
                Prefix = addr[0].ToString(),
                Addr = int.Parse(splits[0]),
                Bit = byte.Parse(splits[1]),
                DataType = DataType.BoolAddress,
                Length = 1,
                IsBit = true
            };
            Add(plcData);
        }
        /// <summary>
        /// 向PLC数据集中添加多个Bit地址，在原基础地址上自动添加count个Bit地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public void AddBit(string name, string addr, int count)
        {
            if (!addr.Contains('.'))
            {
                return;
            }

            int baseAddr = 0;
            byte basebit = 0;
            string[] splits = addr.Substring(1).Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            baseAddr = int.Parse(splits[0]);
            basebit = byte.Parse(splits[1]);

            for (int i = 0; i < count; i++)
            {
                int curAddr = baseAddr + i;

                string newPetName = i.ToString();
                string newAddr = string.Format("{0}{1}.{2}", addr[0], curAddr, basebit);
                AddBit(name, newAddr, newPetName);
            }
        }
        /// <summary>
        /// 向PLC数据集中添加一个地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <param name="dataType"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public void Add(string name, string addr, DataType dataType = DataType.Int16Address, int length = 1, string petName = null)
        {
            PLCData plcData = new PLCData()
            {
                Name = name,
                PetName = petName,
                Prefix = addr[0].ToString(),
                Addr = int.Parse(addr.Substring(1)),
                DataType = dataType,
                Length = length
            };
            Add(plcData);
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
        public void Add(string name, string addr, DataType dataType, int length, int count)
        {
            int baseAddr = 0;
            baseAddr = int.Parse(addr.Substring(1));

            for (int i = 0; i < count; i++)
            {
                int curAddr = baseAddr + (i * length);

                string petName = i.ToString();
                string newAddr = string.Format("{0}{1}", addr[0], curAddr);
                Add(name, newAddr, dataType, length, petName);
            }
        }

        /// <summary>
        /// 更新数据集，添加或移除集合项后调用
        /// </summary>
        public void Update()
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

        public void Clear()
        {
            m_plcDataList.Clear();
        }

        public void Add(PLCData item)
        {
            m_plcDataList.Add(item);
        }

        public bool Contains(PLCData item)
        {
            return m_plcDataList.Contains(item);
        }

        public void CopyTo(PLCData[] array, int arrayIndex)
        {
            m_plcDataList.CopyTo(array, arrayIndex);
        }

        public bool Remove(PLCData item)
        {
            return m_plcDataList.Remove(item);
        }
    }

}
