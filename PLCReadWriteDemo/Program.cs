using PLCReadWrite;
using PLCReadWrite.PLCControl;
using PLCReadWrite.PLCControl.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCReadWriteDemo
{
    class Program
    {
        private static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        static void Main(string[] args)
        {
            var collection = new PLCDataCollection<bool>("温度数据集合");
            sw.Restart();
            collection.Add("温度数据", "D100",800);
            sw.Stop();
            Console.WriteLine("Elapsed.TotalMilliseconds:{0}", sw.Elapsed.TotalMilliseconds);

            //foreach (var item in collection)
            //{
            //    Console.WriteLine(item.ToString());
            //}
            var query = collection.Where(p => p.FullAddress == "D102").ToList();

            Console.WriteLine("*************************************");

            IPLC plc = new MelsecPlcA1E("192.168.100.1", 5000);
            PLCControl plcControl = new PLCControl(plc);


            sw.Restart();
            plcControl.ReadCollection(ref collection);
            sw.Stop();
            Console.WriteLine("Elapsed.TotalMilliseconds:{0}", sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            plcControl.ReadCollection(ref collection);
            sw.Stop();
            Console.WriteLine("Elapsed.TotalMilliseconds:{0}", sw.Elapsed.TotalMilliseconds);

            //foreach (var item in collection)
            //{
            //    Console.WriteLine(item.ToString());
            //}

            Console.WriteLine("*************************************");
            Console.ReadKey();
        }
    }
}
