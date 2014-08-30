using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Open.HttpProxy;

namespace ProxyTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var httpProxy = new HttpProxy();
            httpProxy.Start();
            Console.ReadKey();
        }
    }
}
