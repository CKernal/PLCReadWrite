using PLCReadWrite.PLCControl;
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
            PLCDataCollection<int> collection = new PLCDataCollection<int>();
            collection.Add("温度数据集合", "D1", 1);
            collection.Add("温度数据集合", "D2", 1);
            collection.Add("温度数据集合", "D3", 1);
            collection.Add("温度数据集合", "D4", 1);
            collection.Add("温度数据集合", "D5", 1);
            collection.Add("温度数据集合", "D6", 1);


            foreach (PLCData<int> item in collection)
            {
                Console.WriteLine(item.Data.ToString());
            }

            Console.ReadKey();
        }
    }
}
