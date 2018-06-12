using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCReadWrite.PLCControl.String
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


    public class PLCData
    {
        private const int TIMEOUT_TICKS = 50000000;

        private string _data = "";
        private string _oldData = "";
        private DateTime m_lastUpdate;

        public string Name { get; set; }
        public string SecondName { get; set; }
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
            get { return _data; }
            set
            {
                _oldData = _data;
                _data = value;
                m_lastUpdate = DateTime.Now;
            }
        }

        public string OldData
        {
            get { return _oldData; }
            set { _oldData = value; }
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

        public override string ToString()
        {
            return string.Format("{0}={1}", FullAddress, Data);
        }
    }
}
