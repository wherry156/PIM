using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;

namespace 互调自动化测试
{
    internal class HX
    {
        public class TESTED_PIM
        {
            public string Band{ get; set; }
           
            public double Number { get; set; }
            public TESTED_PIM()
            {
                Band = "unknow";
                Number = 999;
            }
            
        }
        

    }
}
