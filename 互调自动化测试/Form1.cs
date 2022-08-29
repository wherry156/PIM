using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 互调自动化测试
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        HX.TESTED_PIM[,,] PIM = new HX.TESTED_PIM[30,4 ,3];//string[端口,频段（根据累加）,倾角,0频段标识|1PIM值]
        bool RCU_changed=true;//是否有切换RCU的动作
        int[] LAST_order_time=new int[10];//记录RCU最后一次命令的时间   
        int LAST_TEST_PORT = 0;
        private void Form1_Load(object sender, EventArgs e)
        {

        }

     
        private void cutover(object sender, EventArgs e)
        {
            if (CUTOVER.Text.Contains("单"))
            {
                SOA.Text = SOA.Text.Replace("全", "单");
                SOA.Size = new Size(474, 29);
                CUTOVER.Text = CUTOVER.Text.Replace("单", "全");
                label1.Hide();
                label2.Hide();
                TESTED.Hide();
                TOTEST.Hide();
            }
            else if (CUTOVER.Text.Contains("全"))
            {
                SOA.Text = SOA.Text.Replace("单", "全");
                SOA.Size = new Size(149, 29);
                CUTOVER.Text = CUTOVER.Text.Replace("全", "单");
                label1.Show();
                label2.Show();
                TESTED.Show();
                TOTEST.Show();
            }

        }


        private void RCU_Changed(object sender, EventArgs e)
        {
            RCU_changed = true;
        }

        private void PORT_TEST(object sender, EventArgs e)
        {
            double WORST_PIM = 999;
            string TESTING_band = RPIA.Getband().Substring(5,RPIA.Getband().Length-5),SHOW_TEXT=string.Empty;
            int myband = 1;
            Button button = (Button)sender;
            string[] book =button.Text.Split('\n' );
            int TESTING_PORT = int.Parse( button.Text.Substring(5, 2));
            if ((LAST_TEST_PORT - TESTING_PORT) != (LAST_TEST_PORT / 2 - TESTING_PORT / 2) && RCU_changed == false)
            {
                //提示切换RCU
            }
            if (book[1].Contains("未知") || book[1].Contains(TESTING_band))
            {
                myband = 1;
            }
            else if (book[2].Contains("未知") || book[2].Contains(TESTING_band))
                myband = 2;
            
            else 
            {
                //提示可能频段错误
            }
            book[myband] = TESTING_band + ":开始测试";
            SHOW_TEXT = book[0] + "\n" + book[1]+"\n"+book[2];
            button.Text = SHOW_TEXT;
            Application.DoEvents();

             LAST_TEST_PORT = TESTING_PORT;
        }
    }
}
