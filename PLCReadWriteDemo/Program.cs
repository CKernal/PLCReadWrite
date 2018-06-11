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
        static void Main(string[] args)
        {
            var collection = new PLCDataCollection("温度数据集合");
            collection.Add("温度数据", "D100");
            collection.Add("温度数据", "D101");
            collection.Add("温度数据", "D102");
            collection.Add("温度数据", "D103");


            foreach (var item in collection)
            {
                Console.WriteLine(item.ToString());
            }

            Console.WriteLine("*************************************");

            IPLC plc = new MelsecPlcA1E("192.168.100.1", 5000);
            PLCControl plcControl = new PLCControl(plc);
            //plcControl.PlcDataCollectionDictionary.AddOrUpdate(0, collection, (oldkey, oldvalue) => collection);

            //var co = plcControl.PlcDataCollectionDictionary[0];

            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //sw.Restart();

            //plcControl.ReadCollection(ref co);

            //sw.Stop();
            //Console.WriteLine("Elapsed.TotalMilliseconds:{0}", sw.Elapsed.TotalMilliseconds);

            //foreach (var item in co)
            //{
            //    Console.WriteLine(item.ToString());
            //}

            Console.WriteLine("*************************************");

            Console.ReadKey();
        }
    }
}
