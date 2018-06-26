using System;
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
}
