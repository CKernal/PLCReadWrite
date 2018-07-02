using System;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Profinet.Melsec;
using HslCommunication.Profinet.Omron;

namespace PLCReadWrite
{
    public interface IPLC : HslCommunication.Core.IReadWriteNet
    {
        /// <summary>
        /// 获取或设置IP地址
        /// </summary>
        string IpAddress { get; set; }

        /// <summary>
        /// 获取或设置端口号
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// 获得当前PLC的数据变换机制
        /// </summary>
        IByteTransform Transform { get; }

        /// <summary>
        /// 切换短连接模式到长连接模式，后面的每次请求都共享一个通道
        /// </summary>
        /// <returns>返回连接结果，如果失败的话（IsSuccess为False），包含失败信息</returns>
        OperateResult ConnectServer();

        /// <summary>
        /// 在长连接模式下，断开服务器的连接，并切换到短连接模式
        /// </summary>
        /// <returns>关闭连接，不需要查看IsSuccess属性查看</returns>
        OperateResult ConnectClose();

        /// <summary>
        /// 在读取数据之前可以调用本方法将客户端设置为长连接模式，相当于跳过了ConnectServer的结果验证
        /// </summary>
        void SetPersistentConnection();

        OperateResult<bool> ReadBool(string address);
        OperateResult<bool[]> ReadBool(string address, ushort length);

        OperateResult Write(string address, bool value);
        OperateResult Write(string address, bool[] values);
    }

    public class MelsecPlcA1E : MelsecA1ENet, IPLC
    {
        public MelsecPlcA1E(string ip, int port) : base(ip, port)
        {
            ConnectTimeOut = 100;
            ReceiveTimeOut = 100;
        }

        public IByteTransform Transform
        {
            get { return base.ByteTransform; }
        }
    }

    public class MelsecPlcMc : MelsecMcNet, IPLC
    {
        public MelsecPlcMc(string ip, int port) : base(ip, port)
        {
            ConnectTimeOut = 100;
            ReceiveTimeOut = 100;
        }

        public IByteTransform Transform
        {
            get { return base.ByteTransform; }
        }
    }

    public class OmronPlcFins : OmronFinsNet, IPLC
    {
        public OmronPlcFins(string ip, int port) : base(ip, port)
        {
            ConnectTimeOut = 100;
            ReceiveTimeOut = 100;
            //SA1 = 0x0C; // PC网络号，（Source node address）
            //DA1 = 0x0B; // PLC网络号，（destination node address）
            //DA2 = 0x00; // PLC单元号，通常为0（Destination unit address）
        }

        public IByteTransform Transform
        {
            get { return base.ByteTransform; }
        }
    }
}
