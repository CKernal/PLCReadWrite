using System;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.Profinet.Melsec;
using HslCommunication.Profinet.Omron;
using System.Net;
using System.Linq;
using System.Threading.Tasks;

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

        void BeginRead(string address, ushort length, Action<OperateResult<byte[]>> readCallback);
        void BeginReadBool(string address, Action<OperateResult<bool>> readCallback);
        void BeginReadBool(string address, ushort length, Action<OperateResult<bool[]>> readCallback);
        void BeginReadInt16(string address, Action<OperateResult<short>> readCallback);
        void BeginReadInt16(string address, ushort length, Action<OperateResult<short[]>> readCallback);

        void BeginWrite(string address, byte[] value, Action<OperateResult> writeCallback);
        void BeginWrite(string address, bool value, Action<OperateResult> writeCallback);
        void BeginWrite(string address, bool[] values, Action<OperateResult> writeCallback);
        void BeginWrite(string address, short value, Action<OperateResult> writeCallback);
        void BeginWrite(string address, short[] values, Action<OperateResult> writeCallback);
    }

    public class MelsecPlcA1E : MelsecA1ENet, IPLC
    {
        public MelsecPlcA1E(string ip, int port) : base(ip, port)
        {
            ConnectTimeOut = 300;
            ReceiveTimeOut = 300;
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
            ConnectTimeOut = 300;
            ReceiveTimeOut = 300;
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
            ConnectTimeOut = 300;
            ReceiveTimeOut = 300;

            /***************************************************************************
             * (SA1) PC网络号，一般为PC IP地址的最后一位（Source node address）
             * (DA1) PLC网络号，一般为PLC IP地址的最后一位（destination node address）
             * (DA2) PLC单元号，通常为0（Destination unit address）
            ***************************************************************************/

            string localIp = GetLocalIpAddress();
            SA1 = GetIpAddressNode(localIp);
            DA1 = GetIpAddressNode(ip);
            DA2 = 0x00; 
        }

        public IByteTransform Transform
        {
            get { return base.ByteTransform; }
        }

        private byte GetIpAddressNode(string ip)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(ip, out ipAddress))
            {
                byte[] tempByte = ipAddress.GetAddressBytes();
                return tempByte[3];
            }
            return default(byte);
        }

        private string GetLocalIpAddress()
        {
            IPAddress localIp = Dns.GetHostAddresses(Dns.GetHostName())
            .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .First();

            return localIp.ToString();
        }
    }
}
