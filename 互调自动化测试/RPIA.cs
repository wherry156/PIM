using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace 互调自动化测试
{
    internal class RPIA
    {
        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Auto)]
        private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);
        //切换窗体显示
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        [DllImport("User32.dll ")]
        public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childe, string strclass, string FrmText);
        #region PIM值文本
        /// <summary>
        /// 返回值为互调值
        /// </summary>
        /// <returns></returns>
        public static double GetPIM()
        {
            IntPtr roson = FindWindow(null, "RPIA 无源互调测量");
            if (roson == IntPtr.Zero)
                roson = FindWindow(null, "RPIA for PIM Measurement");
            IntPtr roson01 = FindWindowEx(roson, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            IntPtr reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(reson02, roson01, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            int i = 0;
            while (true)
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                i++;
                if (i >= 13)
                    break;

            }
            StringBuilder str = new StringBuilder();
            GetWindowText(roson01, str, 9999);
            double k;
            string str2 = str.ToString();
            if (!double.TryParse(str2, out k))
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                GetWindowText(roson01, str, 9999);
                str2 = str.ToString();
                return double.Parse(str2);
            }
            else
                return double.Parse(str2);
        }
        #endregion
        /// <summary>
        /// 参数为互调软件句柄，返回值为互调值
        /// </summary>
        /// <param name="PIM"></param>
        /// <returns></returns>
        public static double GetPIM(IntPtr PIM)
        {

            IntPtr roson01 = FindWindowEx(PIM, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            IntPtr reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(reson02, roson01, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            int i = 0;
            while (true)
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                i++;
                if (i >= 13)
                    break;

            }
            StringBuilder str = new StringBuilder();
            GetWindowText(roson01, str, 9999);
            double k;
            string str2 = str.ToString();
            if (!double.TryParse(str2, out k))
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                GetWindowText(roson01, str, 9999);
                str2 = str.ToString();
                return double.Parse(str2);
            }
            else
                return double.Parse(str2);
        }
        #region 互调频段文本
        /// <summary>
        /// 直接获取互调测试频段的文本信息
        /// </summary>
        /// <returns></returns>
        public static string Getband()
        {
            IntPtr roson = FindWindow(null, "RPIA 无源互调测量");
            if (roson == IntPtr.Zero)
                roson = FindWindow(null, "RPIA for PIM Measurement");
            IntPtr roson01 = FindWindowEx(roson, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            IntPtr reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(reson02, roson01, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            int i = 0;
            while (true)
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                i++;
                if (i >= 5)
                    break;

            }
            StringBuilder str = new StringBuilder();

            GetWindowText(roson01, str, 9999);
            string str2 = str.ToString();
            ;
            if (str2.Contains("Band"))
                return str2;
            else
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                GetWindowText(roson01, str, 9999);
                str2 = str.ToString();
                return str2;
            }
        }
        #endregion
        #region 开始测试按钮
        /// <summary>
        /// 直接获取开始测试按钮的句柄
        /// </summary>
        /// <returns></returns>
        public static IntPtr begin_button()
        {
            IntPtr roson = FindWindow(null, "RPIA 无源互调测量");
            if (roson == IntPtr.Zero)
                roson = FindWindow(null, "RPIA for PIM Measurement");
            IntPtr roson01 = FindWindowEx(roson, IntPtr.Zero, null, null);//roson1=cb6
            IntPtr reson02 = roson01;//reson2=cb6
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);//1=bca
            roson01 = FindWindowEx(reson02, roson01, null, null);//1=cf8
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);//1=b20
            reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);//1=c42
            int i = 0;
            while (true)
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                i++;
                if (i >= 3)
                    break;

            }
            ShowWindow(roson, 3);
            SetForegroundWindow(roson);
            SetForegroundWindow(roson01);
            return roson01;
        }
        #endregion
        /// <summary>
        /// 参数值为互调软件句柄，返回值为开始按钮句柄。
        /// </summary>
        /// <param name="PIM"></param>
        /// <returns></returns>
        public static IntPtr begin_button(IntPtr PIM)
        {

            IntPtr roson01 = FindWindowEx(PIM, IntPtr.Zero, null, null);//roson1=cb6
            IntPtr reson02 = roson01;//reson2=cb6
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);//1=bca
            roson01 = FindWindowEx(reson02, roson01, null, null);//1=cf8
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);//1=b20
            reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);//1=c42
            int i = 0;
            while (true)
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                i++;
                if (i >= 3)
                    break;

            }
            ShowWindow(PIM, 3);
            SetForegroundWindow(PIM);
            SetForegroundWindow(roson01);
            return roson01;
        }
       
      /// <summary>
      /// 参数为互调测试软件句柄，返回值为BAND：互调频段
      /// </summary>
      /// <param name="PIM"></param>
      /// <returns></returns>
        public static string Getband(IntPtr PIM)
        {
            IntPtr roson01 = FindWindowEx(PIM, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            IntPtr reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            roson01 = FindWindowEx(reson02, roson01, null, null);
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            reson02 = roson01;
            roson01 = FindWindowEx(roson01, IntPtr.Zero, null, null);
            int i = 0;
            while (true)
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                i++;
                if (i >= 5)
                    break;

            }
            StringBuilder str = new StringBuilder();

            GetWindowText(roson01, str, 9999);
            string str2 = str.ToString();
            ;
            if (str2.Contains("Band"))
                return str2;
            else
            {
                roson01 = FindWindowEx(reson02, roson01, null, null);
                GetWindowText(roson01, str, 9999);
                str2 = str.ToString();
                return str2;
                IntPtr p = (IntPtr)Convert.ToInt64(str2);
            }
        }
        /// <summary>
        /// 返回一个数组，打开多个互调窗口时，记录每一个互调测试窗口句柄及对应的测试频段
        /// </summary>
        /// <returns></returns>
        public static string[,] ptr_and_band()
        {
            string[,] strings = new string[2, 6];
            int i = 0;
            IntPtr PIM = IntPtr.Zero;
            while (true)
            {
                PIM = FindWindowEx(IntPtr.Zero, PIM, null, "RPIA for PIM Measurement");//
                if (PIM != IntPtr.Zero)
                {
                    strings[0, i] = PIM.ToString();
                    strings[1, i] = Getband(PIM);
                    i++;
                }
                else
                    break;
            }
            while (true)
            {
                PIM = FindWindowEx(IntPtr.Zero, PIM, null, "RPIA 无源互调测量");
                if (PIM != IntPtr.Zero)
                {
                    strings[0, i] = PIM.ToString();
                    strings[1, i] = Getband(PIM);
                    i++;
                }
                else
                    break;
            }
            return strings;
        }
        public static void RIGHT_click(IntPtr button)
        {
            SendMessage(button, 0x0201, IntPtr.Zero, null);
            Thread.Sleep(30);
            SendMessage(button, 0x0202, IntPtr.Zero, null);
            Thread.Sleep(30); 
        }
    }
}
