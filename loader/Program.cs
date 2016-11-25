using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreData;

namespace loader
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataStorage = new DataStorage();
            if (!dataStorage.Init())
                return;
            string[] csvFiles = Directory.GetFiles(@"data", "*.csv");
            foreach (var filename in csvFiles)
            {
                dataStorage.HandleCsv(filename);
            }
        }
    }
}
