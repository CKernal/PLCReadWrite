using System;

namespace PLCReadWrite.PLCControl.String
{

    public class PLCData
    {
        private const int TIMEOUT_TICKS = 50000000;

        private string m_data = string.Empty;
        private string m_oldData = string.Empty;
        private DateTime m_lastUpdate;

        public string Name { get; set; }
        public uint NameIndex { get; set; }
        public string Prefix { get; set; }
        public int Addr { get; set; }
        public byte Bit { get; set; }
        public DataType DataType { get; set; }
        /// <summary>
        /// 此处有长度分两种情况，一种指PLC的字数据长度，另一种指PLC的位数据长度；
        /// 1、PLC的字数据长度，如一个D地址为16位，两个字节,（可对应C#中的数据类型：Int16）
        /// 2、PLC的位数据长度，如一个M地址为1位，（可对应C#中的数据类型：Bool）
        /// </summary>
        public int Length { get; set; }
        public bool IsBit { get; set; }

        public string Data
        {
            get { return m_data; }
            set
            {
                m_oldData = m_data;
                m_data = value;
                m_lastUpdate = DateTime.Now;
            }
        }

        public string OldData
        {
            get { return m_oldData; }
            set { m_oldData = value; }
        }

        public bool Timeout
        {
            get { return (DateTime.Now.Ticks - m_lastUpdate.Ticks) > TIMEOUT_TICKS; }
        }

        public bool IsChanged
        {
            get { return Data != OldData; }
        }

        public string FullAddress
        {
            get
            {
                if (IsBit)
                {
                    return string.Format("{0}{1}.{2}", Prefix, Addr, Bit);
                }
                return string.Format("{0}{1}", Prefix, Addr);
            }
        }

        public string FullName
        {
            get { return string.Format("{0}-{1}", Name, NameIndex); }
        }

        public override string ToString()
        {
            return string.Format("{0}={1}", FullAddress, Data);
        }
    }
}
