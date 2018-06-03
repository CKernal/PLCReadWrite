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
        /// <summary>
        /// 字符串型，根据字符串长度的不同占用PLC地址
        /// </summary>
        StringAddress
    }

    /// <summary>
    /// PLC数据集合（仅支持同一种地址前缀）
    /// </summary>
    public class PLCDataCollection<T> : IEnumerable where T : struct
    {
        public string Name { get; set; }
        public string Prefix { get; set; }
        public int StartAddr { get; set; }
        public int DataLength { get; set; }
        public bool IsBitCollection { get; set; }
        public DataType DataType { get; set; }
        public string FullStartAddress
        {
            get { return string.Format("{0}{1}", Prefix, StartAddr); }
        }

        /// <summary>
        /// 集合内部储存结构
        /// </summary>
        private List<PLCData<T>> m_plcDataList = new List<PLCData<T>>();

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
        public bool AddBit(string name, string addr, string secondName = null)
        {
            if (addr.IndexOf('.') < 0)
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
        public bool Add(string name, string addr, int length, string secondName = null)
        {
            PLCData<T> plcData = new PLCData<T>();
            plcData.Name = name;
            plcData.SecondName = secondName;
            plcData.Prefix = addr[0].ToString();
            plcData.Addr = int.Parse(addr.Substring(1));
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
        public bool Add(string name, string addr, int length, int count)
        {
            bool ret = false;
            int baseAddr = 0;
            baseAddr = int.Parse(addr.Substring(1));

            for (int i = 0; i < count; i++)
            {
                int curAddr = baseAddr + (i * length);

                string secondName = i.ToString();
                string newAddr = string.Format("{0}{1}", addr[0], curAddr);
                ret &= Add(name, newAddr, length, secondName);
            }

            return ret;
        }


        /// <summary>
        /// 从PLC数据集中移除一个地址
        /// </summary>
        /// <param name="name"></param>
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

            this.m_plcDataList.ForEach(d =>
            {
                switch (DataType)
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
                );
            this.StartAddr = startAddr;
            this.DataLength = (endAddr + endUnitLength) - startAddr;
        }

        public IEnumerator GetEnumerator()
        {
            for (int index = 0; index < m_plcDataList.Count; index++)
            {
                yield return m_plcDataList[index];
            }
        }
    }
}
