﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PLCReadWrite.PLCControl
{
    /// <summary>
    /// PLC数据集合（仅支持同一种地址前缀）
    /// </summary>
    public class PLCDataCollection<T> : ICollection<PLCData<T>> where T : struct
    {
        /// <summary>
        /// 集合内部储存结构
        /// </summary>
        private List<PLCData<T>> m_plcDataList = new List<PLCData<T>>();

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
        public int Count
        {
            get { return m_plcDataList.Count; }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }
        public PLCData<T> this[int index]
        {
            get { return m_plcDataList[index]; }
            set { m_plcDataList[index] = value; }
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
                    throw new Exception("The DataType is not support!");
            }

        }


        /// <summary>
        /// 清空数据集合数据
        /// </summary>
        public void ClearData()
        {
            foreach (var d in m_plcDataList)
            {
                d.Data = default(T);
                d.OldData = default(T);
            }
            Update();
        }

        /// <summary>
        /// 向PLC数据集中添加一个Bit地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private void AddBit(string name, string addr, uint index = 0)
        {
            if (DataType != DataType.BoolAddress)
            {
                return;
            }

            string[] splits = addr.Substring(1).Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            PLCData<T> plcData = new PLCData<T>()
            {
                Name = name,
                NameIndex = index,
                Prefix = addr[0].ToString(),
                Addr = int.Parse(splits[0]),
                Bit = byte.Parse(splits[1]),
                Length = UnitLength,
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
        private void AddBit(string name, string addr, int count)
        {
            if (DataType != DataType.BoolAddress || count < 0)
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
                string newAddr = string.Format("{0}{1}.{2}", addr[0], curAddr, basebit);
                AddBit(name, newAddr, (uint)i);
            }
        }
        /// <summary>
        /// 向PLC数据集中添加一个地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public void Add(string name, string addr, uint index = 0)
        {
            if (addr.Contains('.'))
            {
                AddBit(name, addr, index);
                return; 
            }
            PLCData<T> plcData = new PLCData<T>()
            {
                Name = name,
                NameIndex = index,
                Prefix = addr[0].ToString(),
                Addr = int.Parse(addr.Substring(1))
            };
            Add(plcData);
        }
        /// <summary>
        /// 向PLC数据集中添加多个地址，在原基础地址上自动添加count个地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="addr"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public void Add(string name, string addr, int count)
        {
            if (addr.Contains('.'))
            {
                AddBit(name, addr, count);
                return;
            }

            if (count < 0)
            {
                return;
            }

            int baseAddr = 0;
            baseAddr = int.Parse(addr.Substring(1));

            for (int i = 0; i < count; i++)
            {
                int curAddr = baseAddr + (i * UnitLength);

                string newAddr = string.Format("{0}{1}", addr[0], curAddr);
                Add(name, newAddr, (uint)i);
            }
        }

        /// <summary>
        /// 更新数据集，添加或移除集合项后调用
        /// </summary>
        public void Update()
        {
            if (m_plcDataList.Count <= 0)
            {
                DataLength = 0;
                return;
            }

            int startAddr = int.MaxValue;
            int endAddr = 0;
            int endUnitLength = 1;

            foreach (var d in m_plcDataList)
            {
                d.Length = UnitLength;
                if (d.Addr < startAddr) { startAddr = d.Addr; }
                if (d.Addr > endAddr) { endAddr = d.Addr; endUnitLength = d.Length; }
            }

            StartAddr = startAddr;
            DataLength = (endAddr + endUnitLength) - startAddr;
        }

        public void ForEach(Action<PLCData<T>> action)
        {
            foreach (var item in m_plcDataList)
            {
                action(item);
            }
        }


        public IEnumerator<PLCData<T>> GetEnumerator()
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
        public void Add(PLCData<T> item)
        {
            if (m_plcDataList.Count == 0)
            {
                this.Prefix = item.Prefix;
                this.IsBitCollection = item.IsBit;
            }

            if (this.Prefix == item.Prefix
                && this.IsBitCollection == item.IsBit)
            {
                m_plcDataList.Add(item);
            }
        }

        public bool Contains(PLCData<T> item)
        {
            return m_plcDataList.Contains(item);
        }

        public void CopyTo(PLCData<T>[] array, int arrayIndex)
        {
            m_plcDataList.CopyTo(array, arrayIndex);
        }

        public bool Remove(PLCData<T> item)
        {
            return m_plcDataList.Remove(item);
        }

    }
}
