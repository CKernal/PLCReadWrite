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
            var collection = new PLCDataCollection<bool>("温度数据集合");
            collection.AddBit("温度数据1", "D1.1",10);


            foreach (var item in collection)
            {
                Console.WriteLine(item.ToString());
            }
            collection.Remove("温度数据1");

            foreach (var item in collection)
            {
                Console.WriteLine(item.ToString());
            }

            Console.ReadKey();
        }
    }
}
