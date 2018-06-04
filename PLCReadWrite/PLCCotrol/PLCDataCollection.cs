using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCReadWrite.PLCControl
{
    public enum DataType
    {
        /// <summary>
        /// 布尔型，占1个PLC位地址
        /// </summary>
        BoolAddress,
        /// <summary>
        /// 短整型，占1个PLC字地址
        /// </summary>
        Int16Address,
        /// <summary>
        /// 整型，占2个PLC字地址
        /// </summary>
        Int32Address,
        /// <summary>
        /// 长整型，占4个PLC字地址
        /// </summary>
        Int64Address,
        /// <summary>
        /// 浮点型，占2个PLC字地址
        /// </summary>
        Float32Address,
        /// <summary>
        /// 双精度浮点型，占4个PLC字地址
        /// </summary>
        Double64Address,
    }
    /// <summary>
    /// PLC数据集合（仅支持同一种地址前缀）
    /// </summary>
    public class PLCDataCollection<T> : IEnumerable<PLCData<T>> where T : struct
    {
        public string Name { get; private set; }
        public string Prefix { get; private set; }
        public int StartAddr { get; private set; }
        public int DataLength { get; private set; }
        public bool IsBitCollection { get; private set; }
        public DataType DataType { get; private set; }
        public byte UnitLength { get; private set; }
        public string FullStartAddress
        {
            get { return string.Format("{0}{1}", Prefix, StartAddr); }
        }

        /// <summary>
        /// 集合内部储存结构
        /// </summary>
        private List<PLCData<T>> m_plcDataList = new List<PLCData<T>>();

        public PLCData<T> this[int index]
        {
            get { return m_plcDataList[index]; }
            set { m_plcDataList[index] = value; }
        }

        /// <summary>
        /// 获取集合中数据个数
        /// </summary>
        public int Count
        {
            get { return m_plcDataList.Count; }
        }

        public PLCDataCollection(string name)
        {
            Name = name;

            Type dataType = typeof(T);
            switch (dataType.Name)
            {
                case "Boolean":
                    DataType = DataType.BoolAddress;
                    UnitLength = 1;
                    break;
                case "Int16":
                    DataType = DataType.Int16Address;
                    UnitLength = 1;
                    break;
                case "Int32":
                    DataType = DataType.Int32Address;
                    UnitLength = 2;
                    break;
                case "Int64":
                    DataType = DataType.Int64Address;
                    UnitLength = 4;
                    break;
                case "Float32":
                    DataType = DataType.Float32Address;
                    UnitLength = 2;
                    break;
                case "Double64":
                    DataType = DataType.Double64Address;
                    UnitLength = 4;
                    break;
                default:
                    DataType = DataType.Int16Address;
                    UnitLength = 1;
                    break;
            }

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
        /// 清空数据集合数据
        /// </summary>
        public void ClearData()
        {
            m_plcDataList.ForEach(d =>
            {
                d.Data = default(T);
                d.OldData = default(T);
            });
            Update();
        }
        /// <summary>
        /// 向PLC数据集中添加一个地址，仅供类内部使用
        /// </summary>
        /// <param name="plcData"></param>
        /// <returns></returns>
        private bool Add(PLCData<T> plcData)
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
        private bool AddBit(string name, string addr, string secondName = null)
        {
            if (DataType != DataType.BoolAddress)
            {
                return false;
            }

            string[] splits = addr.Substring(1).Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            PLCData<T> plcData = new PLCData<T>();
            plcData.Name = name;
            plcData.SecondName = secondName;
            plcData.Prefix = addr[0].ToString();
            plcData.Addr = int.Parse(splits[0]);
            plcData.Bit = byte.Parse(splits[1]);
            plcData.Length = UnitLength;
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
        private bool AddBit(string name, string addr, int count)
        {
            if (DataType != DataType.BoolAddress)
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
        /// <returns></returns>
        public bool Add(string name, string addr, string secondName = null)
        {
            if (addr.Contains('.'))
            {
                return AddBit(name, addr, secondName);
            }
            PLCData<T> plcData = new PLCData<T>();
            plcData.Name = name;
            plcData.SecondName = secondName;
            plcData.Prefix = addr[0].ToString();
            plcData.Addr = int.Parse(addr.Substring(1));

            return this.Add(plcData);
        }

        /// <summary>
        /// 向PLC数据集中添加多个地址，在原基础地址上自动添加count个地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public bool Add(string name, string addr, int count)
        {
            if (addr.Contains('.'))
            {
                return AddBit(name, addr, count);
            }

            bool ret = false;
            int baseAddr = 0;
            baseAddr = int.Parse(addr.Substring(1));

            for (int i = 0; i < count; i++)
            {
                int curAddr = baseAddr + (i * UnitLength);

                string secondName = i.ToString();
                string newAddr = string.Format("{0}{1}", addr[0], curAddr);
                ret &= Add(name, newAddr, secondName);
            }

            return ret;
        }


        /// <summary>
        /// 从PLC数据集中移除一个地址
        /// </summary>
        /// <param name="name"></param>
        public void Remove(PLCData<T> plcData)
        {
            m_plcDataList.Remove(plcData);
            Update();
        }
        /// <summary>
        /// 更新数据集，仅供类内部使用
        /// </summary>
        private void Update()
        {
            if (m_plcDataList.Count <= 0)
            {
                DataLength = 0;
                return;
            }

            int startAddr = int.MaxValue;
            int endAddr = 0;
            int endUnitLength = 1;

            m_plcDataList.ForEach(d =>
            {
                d.Length = UnitLength;
                if (d.Addr < startAddr) { startAddr = d.Addr; }
                if (d.Addr > endAddr) { endAddr = d.Addr; endUnitLength = d.Length; }
            });

            StartAddr = startAddr;
            DataLength = (endAddr + endUnitLength) - startAddr;
        }


        //public void ForEach(Action<PLCData<T>> action)
        //{
        //    foreach (var item in m_plcDataList)
        //    {
        //        action(item);
        //    }
        //}

        #region IEnumerable接口实现
        public IEnumerator<PLCData<T>> GetEnumerator()
        {
            for (int index = 0; index < m_plcDataList.Count; index++)
            {
                yield return m_plcDataList[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
