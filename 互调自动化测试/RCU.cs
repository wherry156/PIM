using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;


namespace 互调自动化测试
{
    class RCU
    {
        #region
        string sRCUType = "RCUC2D6A01";

        SerialPort mySerialPort;
        string comPortName = "COM1";
        /// <summary>
        /// 设置串口通信主串口
        /// </summary>
        public bool setCom(string portname = "COM1")
        {
            comPortName = portname;
            string strCmd = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(comPortName))
                {
                    strCmd = "通信配置中的主串口未配置，请设置与RCU相连接的串口。";
                    return false;
                }

                mySerialPort = new SerialPort();
                mySerialPort.PortName = comPortName;
                mySerialPort.BaudRate = 9600;
                mySerialPort.Parity = (Parity)Enum.Parse(typeof(Parity), "None");  //校验位
                mySerialPort.DataBits = 8;  //数据位             
                mySerialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "One");  //停止位    
                mySerialPort.ReceivedBytesThreshold = 1;
                mySerialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(com_DataReceived);
                mySerialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                strCmd = "主串口打开失败，请检查串口是否被其它软件占用。";
                return false;
            }

        }
        //定位点1
        byte sendPacketByte = 0x10;
        byte refPacketByte = 0x10;

        byte sendPacketByte2 = 0x10;
        byte refPacketByte2 = 0x10;

        byte sendPacketByte3 = 0x10;
        byte refPacketByte3 = 0x10;

        byte sendPacketByte4 = 0x10;
        byte refPacketByte4 = 0x10;

        byte sendPacketByte5 = 0x10;
        byte refPacketByte5 = 0x10;

        byte sendPacketByte6 = 0x10;
        byte refPacketByte6 = 0x10;

        byte sendPacketByte7 = 0x10;
        byte refPacketByte7 = 0x10;

        byte sendPacketByte8 = 0x10;
        byte refPacketByte8 = 0x10;

        bool setPollStart = false;

        public void ResetVal()
        {
            sendPacketByte = 0x10;
            refPacketByte = 0x10;

            sendPacketByte2 = 0x10;
            refPacketByte2 = 0x10;

            sendPacketByte3 = 0x10;
            refPacketByte3 = 0x10;

            sendPacketByte4 = 0x10;
            refPacketByte4 = 0x10;

            sendPacketByte5 = 0x10;
            refPacketByte5 = 0x10;

            sendPacketByte6 = 0x10;
            refPacketByte6 = 0x10;

            sendPacketByte7 = 0x10;
            refPacketByte7 = 0x10;

            sendPacketByte8 = 0x10;
            refPacketByte8 = 0x10;
        }

        /// <summary>
        ///在指定帧中查找指定字节
        /// </summary>
        private bool FindByte(byte aValue, byte[] aFrame, int aStart, out int aFindPos)
        {
            bool result;
            aFindPos = 0;

            result = false;

            for (int i = aStart - 1; i < aFrame.Length; i++)
            {
                if (aFrame[i] == aValue)
                {
                    aFindPos = i + 1;
                    result = true;
                    break;
                }
            }

            return result;
        }
        /// <summary>
        ///在指定帧中取出指定位置字节
        /// </summary>
        private void MidFrame(byte[] aFrame, int aStart, int aLen, out byte[] result)
        {
            result = new byte[aLen];

            for (int i = 0; i < aLen; i++)
            {
                result[i] = aFrame[aStart - 1 + i];
            }
        }
        /// <summary>
        ///生成CRC判断byte
        /// </summary>
        private bool CalCRC16(byte[] buf, int n, out byte h, out byte l)
        {
            UInt16 CRC16;

            CRC16 = crc_cal(ref buf, n);
            l = (byte)(CRC16 & 0xff);
            h = (byte)(CRC16 >> 8);

            return true;
        }
        /// <summary>
        /// 判断校验位正确性
        /// </summary>
        private bool CheckCRC16(byte[] buf, int n, byte h, byte l)
        {
            byte th, tl;
            bool result;

            result = false;
            if (CalCRC16(buf, n, out th, out tl))
                if ((th == h) && (tl == l))
                    result = true;

            return result;
        }
        //生成CRC效验位；
        /// <summary>
        ///生成CRC校验位和停止位
        /// </summary>
        public UInt16 crc_cal(ref byte[] buffer, int len)
        {
            int i = 0, j;
            UInt16 current_crc_value = 0;
            if (len >= 2)
            {
                current_crc_value = 0xFFFF;
                for (i = 1; i < len - 3; i++)
                {
                    current_crc_value = Convert.ToUInt16(current_crc_value ^ (buffer[i]));
                    for (j = 0; j < 8; j++)
                    {
                        if ((current_crc_value & 0x0001) == 1)
                        {
                            current_crc_value = Convert.ToUInt16((current_crc_value >> 1) ^ 0x8408);
                        }
                        else
                        {
                            current_crc_value = Convert.ToUInt16((current_crc_value >> 1));
                        }
                    }
                }
            }
            current_crc_value = Convert.ToUInt16(current_crc_value ^ 0xFFFF);
            buffer[i] = (byte)(current_crc_value & 0xff);
            buffer[i + 1] = (byte)(current_crc_value >> 8);
            buffer[i + 2] = 0x7E;

            return current_crc_value;
        }
        //获得转义帧；
        /// <summary>
        /// 获得转义帧，针对性处理）0X7E，0X7D
        /// </summary>
        public byte[] getOrgFrame(byte[] vTmpFrame)
        {
            int j = 0, aFindPos = 0;
            byte[] OrgFrame;
            int TmpFrameLen = vTmpFrame.Length;

            for (int i = 1; i < TmpFrameLen - 1; i++)
            {
                if ((vTmpFrame[i] == 0x7D) || (vTmpFrame[i] == 0x7E))
                {
                    aFindPos++;
                }
            }

            OrgFrame = new byte[TmpFrameLen + aFindPos];

            for (int i = 0; i < TmpFrameLen; i++)
            {
                if (i == 0 || i == TmpFrameLen - 1)
                {
                    OrgFrame[j] = vTmpFrame[i];
                }
                else
                {
                    if (vTmpFrame[i] == 0x7D)
                    {
                        OrgFrame[j] = 0x7D;
                        j++;
                        OrgFrame[j] = 0x5D;
                    }
                    else if (vTmpFrame[i] == 0x7E)
                    {
                        OrgFrame[j] = 0x7D;
                        j++;
                        OrgFrame[j] = 0x5E;
                    }
                    else
                    {
                        OrgFrame[j] = vTmpFrame[i];
                    }
                }
                j++;
            }

            return OrgFrame;
        }

        bool setFail = false;
        bool setOpen = false;
        StringBuilder comOutput = new StringBuilder();
        byte[] recBuff;
        int comFlag = 0;
        private bool hasReceivedData = true;
        bool hasReceived7E = false;
        private string currentOperation = string.Empty;


        /// <summary>
        /// 接收数据后一些状态位的更新
        /// </summary>
        private void com_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!setOpen)
                return;
            int t_start = 0, iStart, iEnd, len, pos;
            string outStr, subStr;
            t_start = System.Environment.TickCount;
            while ((System.Environment.TickCount - t_start) < 100)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            byte[] temrecBuff = new byte[mySerialPort.ReadBufferSize];
            int dataLength = 0;
            try
            {
                dataLength = mySerialPort.Read(temrecBuff, 0, temrecBuff.Length);
                if (dataLength > 0)
                {
                    hasReceivedData = true;

                    if (!FindByte(0x7E, temrecBuff, 1, out pos))
                    {
                        if (!hasReceived7E)
                        {
                            comOutput.Remove(0, comOutput.Length);
                            return;
                        }
                    }
                    else
                    {
                        hasReceived7E = true;
                    }
                }
                else
                    return;

            }
            catch (Exception ex)
            {
                return;
            }
            for (int i = 0; i < dataLength; i++)
                comOutput.Append(temrecBuff[i].ToString("X2"));

            outStr = comOutput.ToString();
            if (string.IsNullOrEmpty(outStr))
                return;
            len = outStr.Length / 2;
            recBuff = new byte[len];

            for (int i = 0; i < len; i++)
            {
                subStr = outStr.Substring(i * 2, 2);
                recBuff[i] = Convert.ToByte(subStr, 16);
            }

            if (IsProtrol(recBuff, recBuff.Length, out iStart, out iEnd))
            {
                outStr = "";
                comOutput.Remove(0, comOutput.Length);
                comFlag = 1;
                hasReceived7E = false;
            }
            else
            {
                comFlag = 0;
            }
        }
        //定位点2
        public bool Set_titl(int RCUnum, double To_set)//设置角度
        {
            return true;
        }
        public double Get_now_titl(int RCUnum)//读角度
        { return 0; }
        public double Get_max_titl(int RCUnum)//获取最大角度
        { return 0; }
        public double Get_min_titl(int RCUnum)//获取最小角度
        { return 0; }

        //设备扫描；
        /// <summary>
        ///扫描所有RCU设备
        /// </summary>
        public bool GetRCUDevice(ref string[] strSN, out bool hasSetSN, int RCUCount)
        {
            int rtn = 0, n = 0, getRcu = 0;
            byte[] SN, senddata = null;
            string newSN = string.Empty, lastSN = string.Empty, strCmd = string.Empty, subSN = string.Empty;

            hasSetSN = false;

            List<string> hasGetSN = new List<string>();
            for (int i = 0; i <= 12; i++)
            {
                for (int j = 0; j < i + 1; j++)
                {
                    rtn = EnCodeRCUScanFrame(j, i, out senddata);

                    if (rtn != 0)
                    {
                        return false;
                    }
                    rtn = SendAndRecv("搜索RCU", senddata, 1, out SN, 500);
                    if (rtn == 0)
                    {
                        newSN = System.Text.Encoding.Default.GetString(SN);
                        //if (newSN.Substring(0, 2) != "DZ")
                        //    continue;
                        if (newSN.CompareTo(lastSN) == 0)
                            continue;

                        if (hasGetSN.Contains(newSN))
                            continue;
                        else
                            hasGetSN.Add(newSN);

                        subSN = newSN.Substring(2, newSN.Length - 2);

                        for (int k = 0; k < RCUCount; k++)
                        {
                            if (subSN.Contains(strSN[k]))
                            {
                                getRcu++;
                                break;
                            }
                        }
                        n++;
                        lastSN = newSN;

                    }

                    if (getRcu == RCUCount)
                    {
                        hasSetSN = true;
                        break;
                    }
                }

                if (getRcu == RCUCount)
                {
                    hasSetSN = true;
                    break;
                }
            }
            strSN = hasGetSN.ToArray();
            return true;
        }
        //设置地址
        /// <summary>
        ///设置RCU地址
        /// </summary>
        public bool SetRCUAddr(string[] array)
        {
            int rtn = 0, currAddress;
            byte[] SN, senddata = null;

            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>设备扫码开始，请等待......");
            //_logger.Debug("设备扫码开始");
            for (int i = 0; i < array.Length; i++)
            {
                //array[i] = "HX" + array[i];
                SN = Encoding.UTF8.GetBytes(array[i]);
                //anySN = System.Text.Encoding.Default.GetString(SN);

                #region
                currAddress = i + 1;
                //_logger.Debug("地址编号为 " + currAddress);
                rtn = EnCodeRCUAddFrame(out senddata, SN, (byte)currAddress);
                //_logger.DebugFormat("EnCodeRCUAddFrame() -> Result:{0}", rtn);
                rtn = SendAndRecv("设置地址", senddata, 1, out SN, 500);
                //_logger.DebugFormat("SendAndRecv() -> Result:{0}", rtn);
                if (rtn != 0)
                {
                    //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>无法扫描到 " + array[i] + "，请检查内部SN是否正确");
                    //_logger.Debug("序号" + (i + 1) + "扫码完成后设置地址失败！");
                    return false;
                }
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>扫码 " + array[i] + " 成功！");

                #endregion
            }
            return true;
        }

        //设置1号RCU校准；
        /// <summary>
        ///RCU停转
        /// </summary>
        public void setRCUStop(int testAdd)
        {
            int nRet = 0;
            string strInfo = string.Empty;
            byte[] decodeBuff, senddata;

            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>" + testAdd + "号RCU停止转动...");
            //_logger.Debug(testAdd + "号RCU停止转动");
            nRet = EnCodeStopFrame(testAdd, out senddata);
            //_logger.DebugFormat("EnCodeStopFrame() -> Result:{0}", nRet);
            nRet = SendAndRecv("RCU断电重启", senddata, testAdd, out decodeBuff, 500);
            //_logger.Debug("SendAndRecv() -> result:" + nRet);

            if (nRet != 0)
            {
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>" + testAdd + "号RCU断电重启失败！");
                //_logger.Info(testAdd + "号RCU断电重启失败！");
                //isStoped = false;
            }
            else
            {
                //_logger.Debug(testAdd + "号RCU断电重启完成");
                //isStoped = true;
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>......设置完成");
            }
        }

        //设置1号RCU校准；
        /// <summary>
        ///设置RCU校准
        /// </summary>
        public void setRCUCal(Object addr)
        {
            //lock (syncRoot)
            //{
            //    threadIsRunning = true;
            //}
            int testaddress = (int)addr;
            try
            {
                int nRet = 0;
                string strInfo = string.Empty;
                byte[] decodeBuff, senddata;

                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>" + testaddress + "号RCU开始转动角度......");
                //_logger.Debug(testaddress + "号RCU开始转动角度");
                nRet = EnCodeCalFrame(testaddress, out senddata);
                //_logger.DebugFormat("EnCodeCalFrame() -> Result:{0}", nRet);
                nRet = SendAndRecv("RCU校准", senddata, testaddress, out decodeBuff, 500);
                //_logger.Debug("SendAndRecv() -> result:" + nRet);

                if (nRet != 0)
                {
                    //MessageBox.Show(nRet.ToString());
                    //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>" + testaddress + "号RCU角度转动失败！");
                    //_logger.Info(testaddress + "号RCU角度转动失败！");
                    setFail = true;
                    //_logger.Debug("setFail = true;");
                    return;
                }

                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>......设置完成");
                //_logger.Debug(testaddress + "号RCU设置完成");
            }
            catch (Exception ex)
            {
                //_logger.Warn("setRCU1Cal() failed:" + ex.Message, ex);
            }
            finally
            {
                //lock (syncRoot)
                //{
                //    threadIsRunning = false;
                //}
            }
        }
        /// <summary>
        ///连接某个RCU
        /// </summary>
        public bool ConnectRCU(int testAdd)
        {
            try
            {
                byte[] decodeBuff, senddata;
                GenConnectFrame(testAdd, out senddata);
                var nRet = SendAndRecv("建立连接", senddata, testAdd, out decodeBuff, 500);
                //_logger.DebugFormat("SendAndRecv() -> Result:{0}", nRet);
                if (nRet != 0)
                {
                    //_logger.Info("连接RCU[" + testAdd + "]失败！");
                    //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>连接RCU[" + testAdd + "]失败！");
                    //MessageBox.Show("连接RCU[" + testAdd + "]失败！");
                    return false;
                }
                else
                {
                    //_logger.Info("连接RCU[" + testAdd + "]");
                    //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>连接RCU[" + testAdd + "]");
                }

                return true;
            }
            catch (Exception ex)
            {
                //_logger.Info("连接RCU[" + testAdd + "]失败！");
                //MessageBox.Show(ex.Message);
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>连接RCU[" + testAdd + "]失败！");
                return false;
            }
        }
        /// <summary>
        ///断开连接某个RCU
        /// </summary>
        public bool DisconnectRCU(int testAdd)
        {
            try
            {
                byte[] decodeBuff, senddata;
                GenDisconnectFrame(testAdd, out senddata);
                var nRet = SendAndRecv("断开连接", senddata, testAdd, out decodeBuff, 500);
                //_logger.DebugFormat("SendAndRecv() -> Result:{0}", nRet);
                if (nRet != 0)
                {
                    //_logger.Info("断开连接RCU[" + testAdd + "]失败！");
                    //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>断开连接RCU[" + testAdd + "]失败！");
                    //MessageBox.Show("断开连接RCU[" + testAdd + "]失败！");
                    return false;
                }
                else
                {
                    //_logger.Info("断开连接RCU[" + testAdd + "]");
                    //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>断开连接RCU[" + testAdd + "]");
                }

                return true;
            }
            catch (Exception ex)
            {
                //_logger.Info("断开连接RCU[" + testAdd + "]失败！");
                ////MessageBox.Show(ex.Message);
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>断开连接RCU[" + testAdd + "]失败！");
                return false;
            }
        }
        /// <summary>
        ///某号RCU配置文件名称设置
        /// </summary>
        public bool setRCUFileCfg(int testAdd, string filename)
        {
            try
            {
                int nRet = -1;
                byte[] decodeBuff, senddata = null;
                string strInfo = string.Empty;
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>设置RCU正式配置文件......");
                //_logger.Debug("设置RCU正式配置文件");
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>......获取file：" + fileno + "\\" + filename);
                //_logger.Debug("获取file：" + fileno + "\\" + filename);
                string file = filename;
                byte[] fileBytes;

                try
                {
                    // HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>......获取file：" + file);
                    FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                    fileBytes = new byte[fs.Length];
                    fs.Read(fileBytes, 0, (int)fs.Length);
                    if (fs != null)
                    {
                        fs.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("文件[" + file + "]访问失败:" + ex.Message, "提示");
                    //_logger.Info("文件[" + file + "]访问失败:" + ex.Message, ex);
                    return false;
                }
                int count = fileBytes.Length / 70;
                int last = fileBytes.Length % 70;
                //  MessageBox.Show(count + "," + last);
                for (int i = 0; i < count + 1; i++)
                {
                    if (i == count)
                    {
                        if (last != 0)
                        {
                            byte[] sendfile = new byte[last];
                            for (int j = 0; j < last; j++)
                            {
                                sendfile[j] = fileBytes[count * 70 + j];
                            }
                            nRet = EnCodeConfigFrame2(testAdd, out senddata, sendfile, last);
                            //_logger.DebugFormat("EnCodeConfigFrame2() -> Result:{0}", nRet);
                        }
                    }
                    else
                    {
                        byte[] sendfile = new byte[70];
                        for (int j = 0; j < 70; j++)
                        {
                            sendfile[j] = fileBytes[i * 70 + j];
                        }
                        nRet = EnCodeConfigFrame2(testAdd, out senddata, sendfile, 70);
                        //_logger.DebugFormat("EnCodeConfigFrame2() -> Result:{0}", nRet);
                    }
                    //  MessageBox.Show(senddata.ToString());
                    nRet = SendAndRecv("配置文件", senddata, testAdd, out decodeBuff, 500);
                    //_logger.DebugFormat("SendAndRecv() -> Result:{0}", nRet);
                    if (nRet != 0)
                    {
                        //_logger.Info("设置RCU测试用配置文件失败！");
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>设置RCU测试用配置文件失败！");
                        MessageBox.Show("设置RCU测试用配置文件失败！");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                //_logger.Info("设置RCU测试用配置文件失败！" + ex.Message, ex);
                MessageBox.Show(ex.Message);
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>设置RCU测试用配置文件失败！");
                return false;
            }
            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>......设置RCU正式配置文件完成");
            //_logger.Debug("设置RCU正式配置文件完成");
            return true;
        }

        //设置RCU序列号；
        /// <summary>
        ///设置RCU序列号；
        /// </summary>
        public bool setRCUSN(int num, string strSN)
        {
            int nRet = -1;
            byte[] SN, decodeBuff, senddata;
            string strInfo = string.Empty;

            byte[] Txbuf = new byte[3];
            byte[] endByte = new byte[1];

            Txbuf[0] = (byte)'D'; //H 0x48
            Txbuf[1] = (byte)'Z'; //X 0x58
            Txbuf[2] = (byte)sRCUType.Length;
            endByte[0] = 0x11;

            string str = System.Text.Encoding.Default.GetString(Txbuf) + sRCUType +
                System.Text.Encoding.Default.GetString(endByte) + strSN;

            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>设置RCU序列号" + num + "：" + str + "......");

            //_logger.Debug("设置RCU序列号" + num + "：" + str);
            decodeBuff = System.Text.Encoding.ASCII.GetBytes(str);


            nRet = EnCodeSetSNFrame1(num, decodeBuff, out senddata);

            nRet = SendAndRecv("设置序列号1", senddata, num, out SN, 500);
            //_logger.Debug("SendAndRecv -> Result:" + nRet);
            // HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>读取RCU序列号" + num + "：" + System.Text.Encoding.Default.GetString(SN) + "," + SN.Length);
            if (nRet != 0)
            {
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>设置地址" + num + "序列号" + strSN + "失败！");
                //_logger.Info("设置地址" + num + "序列号" + strSN + "失败");
                return false;
            }

            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>......设置地址" + num + "序列号" + strSN + "完成");
            //_logger.Debug("设置地址" + num + "序列号" + strSN + "完成");
            return true;
        }

        //发送数据帧；
        /// <summary>
        /// 发送数据帧和接收数据
        /// </summary>
        private int SendAndRecv(string operation, byte[] sendBuff, int testAdd, out byte[] decodeBuff, long delay)
        {
            //_logger.DebugFormat("Parameter -> operation:{0}", operation);
            currentOperation = operation;
            hasReceivedData = true;
            int rst = -1, sendTimes = 0, maxTimes = 1, pollCount = 0;
            int t_start;
            int nCmdRspTime = 2000;
            bool bRecvOver = false;
            StringBuilder strOutput;
            string strTemp = string.Empty, strFrame = string.Empty;

            decodeBuff = null;

            if ((operation == "设置地址") || (operation == "配置文件") || (operation == "配置文件") || (operation.Contains("获取最") ) || (operation.Contains("读角度")))
                maxTimes = 10;
            if (operation == "RCU校准" || operation == "设置序列号1" || operation.Contains("设置"))
                nCmdRspTime = 4000;
            else if (operation == "搜索RCU")
                nCmdRspTime = 300;

            //协议默认命令指针发送三次
            while (sendTimes < maxTimes)
            {
                //if (isFinalizing == false)//在RunTP的finally代码段里面会执行RCU重启, 这种情况下应该让此方法执行完成
                //{
                //    lock (syncRoot)
                //    {
                //        if (cancelTokenSrc.IsCancellationRequested)
                //        {
                //            _logger.Info("CancellationRequested");
                //            return -1;
                //        }
                //    }
                //}

                //_logger.Debug("sendTimes:" + sendTimes);
                hasReceivedData = false;
                comFlag = 0;
                bRecvOver = false;
                rst = -1;
                hasReceived7E = false;

                strOutput = new StringBuilder();
                for (int i = 0; i < sendBuff.Length; i++)
                {
                    strOutput.Append(sendBuff[i].ToString("X2") + " ");
                }
                strFrame = strOutput.ToString();

                //if (strFrame.Length > 0)
                //    HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>发送指针：");

                while (strFrame.Length > 90)
                {
                    strTemp = strFrame.Substring(0, 90);
                    //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=   " + strTemp);
                    strFrame = strFrame.Substring(90, strFrame.Length - 90);
                }
                if (strFrame.Length > 0)
                {
                    //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=   " + strFrame);
                }

                #region 如果是"RCU断电重启"就等待2秒
                if (operation.CompareTo("RCU断电重启") == 0)
                {
                    t_start = System.Environment.TickCount;
                    while ((System.Environment.TickCount - t_start) < 2000)
                    {
                        Application.DoEvents();
                        Thread.Sleep(1);
                    }
                }
                #endregion
                //---组帧并发送    
                setOpen = true;
                try
                {
                    mySerialPort.Write(sendBuff, 0, sendBuff.Length);
                }
                catch (Exception ex)
                {
                    //_logger.Warn("串口写数据失败:" + ex.Message, ex);
                    return -1;
                }
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < sendBuff.Length; i++)
                    sb.Append(sendBuff[i].ToString("X2"));
                //_logger.Debug("SerialPort Write:" + sb.ToString());
                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>发送指针：" + sb.ToString());
                //等待超时时间,目前都是5000毫秒，为发现有什么问题;没未
                t_start = System.Environment.TickCount;
                while ((System.Environment.TickCount - t_start) < nCmdRspTime)
                {
                    if (comFlag == 1)
                    {
                        setOpen = false;
                        bRecvOver = true;
                        break;
                    }
                    Application.DoEvents();
                    Thread.Sleep(1);
                }

                setOpen = false;
                //if (hideSomeLog == false)
                //    _logger.DebugFormat("bRecvOver:{0}", bRecvOver);
                #region 接收到有效数据
                if (bRecvOver)
                {
                    Thread.Sleep(3);

                    comFlag = 0;

                    if (setPollStart == true)
                    {
                        if (operation.CompareTo("RCU校准") == 0)
                        {
                            /*
                            if (testAdd == 1)
                                GetSendPacketNo(refPacketByte, 0, out sendPacketByte);
                            else
                                GetSendPacketNo(refPacketByte2, 0, out sendPacketByte2);
                            */

                            if (testAdd == 1)
                                GetSendPacketNo(refPacketByte, 0, out sendPacketByte);
                            else
                                GetSendPacketNo(refPacketByte2, 0, out sendPacketByte2);


                            //if (hideSomeLog == false)
                            //    _logger.Debug("GetSendPacketNo()");
                        }
                        setPollStart = false;
                        pollCount = 0;
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>...轮询停止");
                        //_logger.Info("轮询停止");
                    }

                    strOutput = new StringBuilder();
                    for (int i = 0; i < recBuff.Length; i++)
                    {
                        strOutput.Append(recBuff[i].ToString("X2") + " ");
                    }

                    strFrame = strOutput.ToString();

                    //if (strFrame.Length > 0)
                    //    HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>接收指针：");

                    while (strFrame.Length > 90)
                    {
                        strTemp = strFrame.Substring(0, 90);
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=   " + strTemp);
                        strFrame = strFrame.Substring(90, strFrame.Length - 90);
                    }
                    if (strFrame.Length > 0)
                    {
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=   " + strFrame);
                    }

                    //---帧头 反转义 分析
                    //if (hideSomeLog == false)
                    //    _logger.DebugFormat("DecodeProtocolFrame() ->operation:{0}", operation);
                    rst = DecodeProtocolFrame(operation, recBuff, testAdd, out decodeBuff);
                    //if (hideSomeLog == false)
                    //    _logger.DebugFormat("DecodeProtocolFrame() -> result:{0}", rst);

                    if (rst == 0)
                    {
                        break;
                    }
                    else if (rst == 1)
                    {
                        //if (hideSomeLog == false)
                        //    _logger.Debug("EnCodeMidFrame()");
                        EnCodeMidFrame(testAdd, out sendBuff);
                    }
                    else if (rst == 2)
                    {
                        if (operation == "RCU校准")
                        {
                            pollCount++;

                            if (pollCount <= 10)
                            {
                                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>#####启动轮询" + pollCount + "####");
                                //_logger.Debug("启动轮询" + pollCount);
                                setPollStart = true;
                                //if (hideSomeLog == false)
                                //    _logger.Debug("EnCodeMidFrame()");
                                EnCodePollFrame(testAdd, out sendBuff);
                            }
                            else
                            {
                                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>#####轮询10次未收到正确应答####");
                                //_logger.Info("轮询10次未收到正确应答");
                                rst = -1;
                                comOutput.Remove(0, comOutput.Length);
                                sendTimes++;
                            }
                        }
                    }
                    else
                    {
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>#####收到RCU应答数据不正确####");
                        //_logger.Info("收到RCU应答数据不正确");
                        rst = -1;
                        comOutput.Remove(0, comOutput.Length);
                        sendTimes++;
                    }
                }
                #endregion
                #region 没有接收到数据
                else
                {
                    //_logger.Info("规定时间内未收到校准应答");

                    if (operation == "RCU校准")
                    {
                        pollCount++;

                        if (pollCount <= 10)
                        {
                            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>#####启动轮询" + pollCount + "####");
                            //_logger.Debug("启动轮询" + pollCount);
                            setPollStart = true;
                            //if (hideSomeLog == false)
                            //    _logger.Debug("EnCodePollFrame()");
                            EnCodePollFrame(testAdd, out sendBuff);
                        }
                        else
                        {
                            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>#####规定时间内未收到校准应答####");
                            rst = -1;
                            comOutput.Remove(0, comOutput.Length);
                            sendTimes++;
                        }

                    }
                    else
                    {
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>>#####规定时间内未收到校准应答####");
                        rst = -1;
                        comOutput.Remove(0, comOutput.Length);
                        sendTimes++;
                    }

                }
                #endregion
            }

            return rst;
        }

        //组设置序列号的帧;
        /// <summary>
        /// g
        /// </summary>
        /// <param name="num"></param>
        /// <param name="SN"></param>
        /// <param name="sendFrame"></param>//
        /// <returns></returns>
        private int EnCodeSetSNFrame1(int num, byte[] SN, out byte[] sendFrame)
        {

            //MessageBox.Show(SN.Length.ToString());
            ///
            byte[] addFrame = new byte[SN.Length + 10];
            //byte[] addFrame = new byte[49];

            int i = 0;

            addFrame[i] = 0x7E;
          
            i++; addFrame[i] = 0xEA;
            i++; addFrame[i] = 0xEB;
            i++; addFrame[i] = 0x21;
            //i++; addFrame[i] = Convert.ToByte(SN.Length + 2);
            i++; addFrame[i] = 0x00;
            i++; addFrame[i] = 0x02;
            i++;
            for (int j = 0; j < SN.Length; j++)
            {
                addFrame[i + j] = SN[j];
            }
            i = i + SN.Length;

            addFrame[i] = (byte)num;

            crc_cal(ref addFrame, addFrame.Length);
            sendFrame = getOrgFrame(addFrame);

            return 0;
        }

        //组设置配置文件的指令；
        private int EnCodeConfigFrame2(int testAdd, out byte[] sendFrame, byte[] files, int len)
        {
            sendFrame = null;
            byte[] RCUFrame = new byte[9 + len];
            int i;//数据帧计数       


            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 1, out sendPacketByte);
            else if (testAdd == 2)
                GetSendPacketNo(refPacketByte2, 1, out sendPacketByte2);
            else if (testAdd == 3)
                GetSendPacketNo(refPacketByte3, 1, out sendPacketByte3);
            else if (testAdd == 4)
                GetSendPacketNo(refPacketByte4, 1, out sendPacketByte4);
            else if (testAdd == 5)
                GetSendPacketNo(refPacketByte5, 1, out sendPacketByte5);
            else if (testAdd == 6)
                GetSendPacketNo(refPacketByte6, 1, out sendPacketByte6);
            else if (testAdd == 7)
                GetSendPacketNo(refPacketByte7, 1, out sendPacketByte7);
            else if (testAdd == 8)
                GetSendPacketNo(refPacketByte8, 1, out sendPacketByte8);

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            //RCU地址；
            //i++; RCUFrame[i] = 0x01;
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = sendPacketByte;
            else if (testAdd == 2)
                RCUFrame[i] = sendPacketByte2;
            else if (testAdd == 3)
                RCUFrame[i] = sendPacketByte3;
            else if (testAdd == 4)
                RCUFrame[i] = sendPacketByte4;
            else if (testAdd == 5)
                RCUFrame[i] = sendPacketByte5;
            else if (testAdd == 6)
                RCUFrame[i] = sendPacketByte6;
            else if (testAdd == 7)
                RCUFrame[i] = sendPacketByte7;
            else if (testAdd == 8)
                RCUFrame[i] = sendPacketByte8;

            //具体命令；
            i++; RCUFrame[i] = 0X32;
            //帧内容长度；
            i++; RCUFrame[i] = (byte)len;
            //帧内容长度；
            i++; RCUFrame[i] = 0X00;
            i++;
            for (int j = 0; j < files.Length; j++)
            {
                RCUFrame[i + j] = files[j];
            }
            crc_cal(ref RCUFrame, RCUFrame.Length);
            sendFrame = getOrgFrame(RCUFrame);

            return 0;
        }
        private int GetSendPacketNo(byte refPacketNo, int type, out byte senPacketNo)
        {
            senPacketNo = 0x10;
            if (refPacketNo == 0x10)
                return 0;
            else
            {
                byte h, l;
                h = (byte)(refPacketNo >> 4);
                l = (byte)(refPacketNo % 16);
                switch (type)
                {
                    case 0:
                        h = (byte)((h + 14) % 16);//设置中间帧高位-2                        
                        break;
                    case 1:
                        l = (byte)((l + 2) % 16);//设置帧+2
                        break;
                    default:
                        return -1;
                }
                senPacketNo = (byte)(h * 16 + l);
            }

            return 0;
        }
        //设置命令： 读取命令：1
        private int GetRefPacketNo(byte senPacketNo, int type, out byte refPacketNo)
        {
            refPacketNo = 0x11;
            byte h, l;

            h = (byte)(senPacketNo >> 4);
            l = (byte)(senPacketNo % 16);

            switch (type)
            {
                case 0:
                    l = (byte)(1);//接收中间帧低位为1
                    break;
                case 1:
                    //接收帧低位不变
                    break;//
                case 2:
                    l = (byte)((h + 2 - 3) % 16);
                    break;
                default:
                    return -1;
            }

            h = (byte)((h + 2) % 16);
            refPacketNo = (byte)(h * 16 + l);

            return 0;
        }
        //组配置文件轮询指令针；
        private int EnCodeConfPollFrame(int testAdd, out byte[] sendFrame)                                                       
        {
            byte[] RCUFrame = new byte[6];
            int i;//数据帧计数

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = sendPacketByte;
            else
                RCUFrame[i] = sendPacketByte2;


            crc_cal(ref RCUFrame, RCUFrame.Length);
            sendFrame = getOrgFrame(RCUFrame);
            return 0;
        }
        //协议判断 
        private bool getRightPro(byte[] buf, out byte[] OrgFrame)
        {
            int tFrameStart = -1, tFrameEnd = -1;
            int OrgFrameLen;
            byte HeadByte;

            OrgFrame = null;
            HeadByte = 0x7E;

            //***查找帧头
            if (!FindByte(HeadByte, buf, 1, out tFrameStart))
            {
                //if (hideSomeLog == false)
                //    _logger.Info("FindByte() -> Result:False");
                return false;
            }
            else
            {
                //if (hideSomeLog == false)
                //    _logger.Debug("FindByte() -> Result:True");
            }

            //***找到帧头后，查找帧尾位置
            if (!FindByte(HeadByte, buf, tFrameStart + 1, out tFrameEnd))
            {
                //if (hideSomeLog == false)
                //    _logger.Info("FindByte() -> Result:False");
                return false;
            }
            else
            {
                //if (hideSomeLog == false)
                //    _logger.Debug("FindByte() -> Result:True");
            }

            //取帧头帧尾间数据            
            OrgFrameLen = tFrameEnd - tFrameStart + 1;
            OrgFrame = new byte[OrgFrameLen];

            MidFrame(buf, tFrameStart, OrgFrameLen, out OrgFrame);
            #region 处理转义字符
            int countOf7D = 0;
            for (int i = 0; i < OrgFrame.Length; i++)
            {
                if (OrgFrame[i] == 0x7d)
                    countOf7D++;
            }
            if (countOf7D > 0)
            {
                var originalFrame = new byte[OrgFrame.Length - countOf7D];
                originalFrame[0] = OrgFrame[0];

                for (int c = 1, d = 1; c < OrgFrame.Length; c++)
                {
                    if (OrgFrame[c] == 0x7d)
                    {
                        if (c >= OrgFrame.Length - 2)//0x7d不能是最后两个
                        {
                            //_logger.Info("转义字符0x7d的位置有误,不可以是最后两位");
                            return false;
                        }
                        c++;
                        originalFrame[d] = (byte)(0x20 ^ OrgFrame[c]);
                    }
                    else
                    {
                        originalFrame[d] = OrgFrame[c];
                    }
                    d++;
                }
                OrgFrame = originalFrame;
            }
            #endregion
            return true;
        }
        //解析返回的数据帧；
        /// <summary>
        ///解析接收数据帧
        /// </summary>
        private int DecodeProtocolFrame(string operation, byte[] recBuff, int portName,out byte[] decodeBuff)
        {
            int result = 0, calFlag = 0;
            decodeBuff = null;

            switch (operation)
            {
                case "搜索RCU":
                    if (getRightPro(recBuff, out decodeBuff))
                    {
                        //if (hideSomeLog == false)
                        //    _logger.Info("getRightPro() -> Result:True");
                        if (decodeBuff.Length == 37)
                        {
                            MidFrame(decodeBuff, 9, 19, out decodeBuff);
                            result = 0;
                        }
                        else
                        {
                            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                            //_logger.Info("接收到的数据不符合长度和格式要求，测试终止(decodeBuff.Length < 37)");
                            result = -1;
                        }
                    }
                    else
                    {
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                        //_logger.Info("接收到的数据不符合长度和格式要求，测试终止");
                        result = -1;
                    }

                    break;
                case "设置地址":
                    if (getRightPro(recBuff, out decodeBuff))
                    {
                        if (decodeBuff.Length == 33)
                        {
                            MidFrame(decodeBuff, decodeBuff.Length - 3, 1, out decodeBuff);
                            result = 0;
                        }
                        else
                        {
                            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                            //_logger.Info("接收到的数据不符合长度和格式要求，测试终止(decodeBuff.Length not in 33,34)");
                            result = -1;
                        }
                    }
                    else
                    {
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                        //_logger.Info("接收到的数据不符合长度和格式要求，测试终止");
                        result = -1;
                    }
                    break;
                case "发送轮询":
                    if (recBuff.Length == 6)
                    {
                        result = DeCodePollFrame(portName, recBuff);
                        //if (hideSomeLog == false)
                        //    _logger.DebugFormat("DeCodePollFrame() -> Result:{0}", result);
                    }
                    else
                    {
                        //_logger.Info("recBuff的长度不为6");
                        result = -1;
                    }
                    break;
                case "RCU校准":
                    if (getRightPro(recBuff, out decodeBuff))
                    {
                        //if (hideSomeLog == false)
                        //    _logger.Debug("getRightPro() -> Result:True");
                        if (decodeBuff.Length == 6)
                        {
                            calFlag = DnCodeMidFrame(portName, decodeBuff);
                            if (calFlag == 1)
                            {
                                result = 1;
                            }
                            else
                            {
                                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###收到错误数据，开启轮询###");
                                //_logger.Info("收到错误数据，开启轮询");
                                result = 2;
                            }

                        }
                        else if (decodeBuff.Length == 10)
                        {
                            calFlag = DeCodeCalFrame(portName, decodeBuff);
                            //if (hideSomeLog == false)
                            //    _logger.DebugFormat("DeCodeCalFrame() -> Result:{0}", calFlag);
                            if (calFlag == 0)
                            {
                                result = 0;
                            }
                            else
                            {
                                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###收到错误的校准结果数据，测试终止###");
                                //_logger.Info("收到错误的校准结果数据，测试终止");
                                result = -1;
                            }

                        }
                        else
                        {
                            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                            //_logger.Info("接收到的数据不符合长度和格式要求，测试终止");
                            result = -1;
                        }
                    }
                    else
                    {
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                        //_logger.Info("接收到的数据不符合长度和格式要求，测试终止");
                        result = -1;
                    }

                    break;
                case "RCU断电重启":
                    if (getRightPro(recBuff, out decodeBuff))
                    {
                        //if (hideSomeLog == false)
                        //    _logger.Info("getRightPro() -> Result:True");
                        if (decodeBuff.Length == 11)
                        {
                            result = DeCodeStopFrame(portName, decodeBuff);
                            //if (hideSomeLog == false)
                            //    _logger.DebugFormat("DeCodeStopFrame() -> Result:{0}", result);
                        }
                        else
                        {
                            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度要求，测试终止###");
                            //_logger.Info("接收到的数据不符合长度要求，测试终止");
                            result = -1;
                        }
                    }
                    else
                    {
                        //if (hideSomeLog == false)
                        //    _logger.Info("getRightPro() -> Result:False");
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                        //_logger.Info("接收到的数据不符合长度和格式要求，测试终止");
                        result = -1;
                    }
                    break;
                    case "配置文件":
                    if (getRightPro(recBuff, out decodeBuff))
                    {
                            //if (hideSomeLog == false)
                            //    _logger.Info("getRightPro() -> Result:True");
                        if (decodeBuff.Length == 6)
                        {
                            calFlag = DnCodeMidFrame(portName, decodeBuff);
                            //_logger.DebugFormat("DnCodeMidFrame() -> Result:{0}", calFlag);

                            if (calFlag == 1)
                            {
                                result = 1;
                            }
                            else
                            {
                                //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合格式要求，测试终止###");
                                //_logger.Info("接收到的数据不符合长度和格式要求，测试终止");
                                result = -1;
                            }
                        }
                        else if (decodeBuff.Length == 10)
                        {
                            result = DeCodeConfigFrame(portName, decodeBuff);
                            //_logger.DebugFormat("DeCodeConfigFrame -> Result:{0}", result);
                        }
                        else
                        {
                            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                            //_logger.Info("接收到的数据不符合长度和格式要求，测试终止(decodeBuff.Length not in 10,11)");
                            result = -1;
                        }
                    }
                    else
                    {
                        //if (hideSomeLog == false)
                        //    _logger.Info("getRightPro() -> Result:False");
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                        //_logger.Info("接收到的数据不符合长度和格式要求，测试终止");
                        result = -1;
                    }
                    break;
                case "设置序列号1":
                    //if (isv3Ver)
                        MidFrame(recBuff, 6 + sRCUType.Length, 19, out decodeBuff);
                    //else
                    //    MidFrame(recBuff, 9 + sRCUType.Length, 19, out decodeBuff);
                    break;
                case "设置序列号2":
                    MidFrame(recBuff, recBuff.Length - 3, 1, out decodeBuff);
                    break;
                case "读RCU信息":
                    result = DeCodeGetStasFrame(portName, recBuff, out decodeBuff);
                    break;
                case "读天线型号":
                    //result = DeCodeGetTXTypeFrame(portName, recBuff, out decodeBuff);
                    break;
                case "断开连接":
                case "建立连接":
                    if (getRightPro(recBuff, out decodeBuff))
                    {
                        //if (hideSomeLog == false)
                        //    _logger.Info("getRightPro() -> Result:True");
                        if (decodeBuff.Length == 6 && decodeBuff[2] == 0x73)
                        {
                            result = 0;
                            //_logger.DebugFormat("DeCodeStopFrame() -> Result:{0}", result);
                        }
                        else
                        {
                            //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度要求，测试终止###");
                            //_logger.Info("接收到的数据不符合长度要求，测试终止");
                            result = -1;
                        }
                    }
                    else
                    {
                        //if (hideSomeLog == false)
                        //    _logger.Info("getRightPro() -> Result:False");
                        //HxBus.SendCommand(hTCS, hOwnerInstr, "调试信息=>> ###接收到的数据不符合长度和格式要求，测试终止###");
                        //_logger.Info("接收到的数据不符合长度和格式要求，测试终止");
                        result = -1;
                    }
                    break;
                default:
                    result = -1;
                    break;
            }

            return result;
        }
        //协议判断 
        private bool IsProtrol(byte[] buf, int iLen, out int iStart, out int iEnd)
        {
            int i, j;
            int tFrameStart = -1, tFrameEnd = -1;
            int TmpFrameLen, OrgFrameLen;
            byte h, l, HeadByte;
            byte[] OrgFrame, vTmpFrame, CRCFrame;
            bool result;

            iStart = 0;
            iEnd = 0;
            result = false;
            HeadByte = 0x7E;

            //***帧最短长度校验
            if (iLen < 6)
            {
                result = false;
                return result;
            }

            //***查找帧头
            result = FindByte(HeadByte, buf, 1, out tFrameStart);
            //if (hideSomeLog == false)
            //    _logger.InfoFormat("FindByte() first -> Result:{0}, tFrameStart:{1}", result, tFrameStart);
            if (result == false)
                return result;

            //***找到帧头后，查找帧尾位置
            result = FindByte(HeadByte, buf, tFrameStart + 1, out tFrameEnd);
            //if (hideSomeLog == false)
            //    _logger.InfoFormat("FindByte() second -> Result:{0}, tFrameEnd{1}", result, tFrameEnd);
            if (result == false)
                return result;

            //***找到帧尾后，根据帧格式特征检查帧头、帧尾间数据是否为协议数据格式，并进行相应处理
            //最短帧长度校验
            if (tFrameEnd - tFrameStart < 5)
            {
                //_logger.Info("tFrameEnd - tFrameStart < 5");
                result = false;
                return result;
            }

            //取帧头帧尾间数据
            OrgFrameLen = 0;
            TmpFrameLen = tFrameEnd - tFrameStart + 1;
            vTmpFrame = new byte[TmpFrameLen];
            OrgFrame = new byte[TmpFrameLen];

            if (tFrameStart > TmpFrameLen)
            {
                result = false;
                return result;
            }

            if (tFrameStart + TmpFrameLen - 1 > buf.Length)
            {
                //_logger.Info("tFrameStart + TmpFrameLen - 1 > buf.Length");
                result = false;
                return result;
            }

            MidFrame(buf, tFrameStart, TmpFrameLen, out vTmpFrame);

            //还原转义字节
            i = 0;
            j = 0;
            while (i < TmpFrameLen)
            {
                if (i > (vTmpFrame.Length) - 1)
                    break;
                if ((vTmpFrame[i] == 0x7D) && (vTmpFrame[i + 1] == 0x5D))
                {
                    OrgFrame[j] = 0x7D;
                    i++;
                }
                else if ((vTmpFrame[i] == 0x7D) && (vTmpFrame[i + 1] == 0x5E))
                {
                    OrgFrame[j] = 0x7E;
                    i++;
                }
                else
                {
                    OrgFrame[j] = vTmpFrame[i];
                }

                i++;
                j++;
            }

            OrgFrameLen = j;
            //判断CRC是否正确，不正确则认为不是协议帧
            MidFrame(OrgFrame, 1, OrgFrameLen, out CRCFrame);                
            h = OrgFrame[(OrgFrameLen - 1) - 1];
            l = OrgFrame[(OrgFrameLen - 1) - 2];
            if (!CheckCRC16(CRCFrame, OrgFrameLen, h, l))
            {
                //_logger.Info("CheckCRC16() -> Result:False");
                result = false;
                return result;
            }
            else
            {
                iStart = tFrameStart;
                iEnd = tFrameEnd;
                result = true;
            }

            return result;
        }
        //组设置命令中间帧；
        private void EnCodeMidFrame(int testAdd, out byte[] sendFrame)
        {
            byte[] RCUFrame = new byte[6];                                                                                      
            int i = 0;
            //byte midSendPacketByte = 0X00;

            /*
            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 0, out sendPacketByte);
            else
                GetSendPacketNo(refPacketByte2, 0, out sendPacketByte2);
            */
            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 0, out sendPacketByte);
            else
                GetSendPacketNo(refPacketByte2, 0, out sendPacketByte2);


            i = 0; RCUFrame[i] = 0x7E;
            //i++; RCUFrame[i] = 0x01;
            i++; RCUFrame[i] = (byte)testAdd;
            i++;
            //RCUFrame[i] = midSendPacketByte;
            if (testAdd == 1)
                RCUFrame[i] = sendPacketByte;
            else
                RCUFrame[i] = sendPacketByte2;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            sendFrame = getOrgFrame(RCUFrame);
        }
        private void GenConnectFrame(int testAdd, out byte[] sendFrame)
        {
            byte[] RCUFrame = new byte[6];
            int i = 0;
            //byte midSendPacketByte = 0X00;

            /*
            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 0, out sendPacketByte);
            else
                GetSendPacketNo(refPacketByte2, 0, out sendPacketByte2);
            */

            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 0, out sendPacketByte);
            else if (testAdd == 2)
                GetSendPacketNo(refPacketByte2, 0, out sendPacketByte2);
            else if (testAdd == 3)
                GetSendPacketNo(refPacketByte3, 0, out sendPacketByte3);                                                    
            else if (testAdd == 4)
                GetSendPacketNo(refPacketByte4, 0, out sendPacketByte4);
            else if (testAdd == 5)
                GetSendPacketNo(refPacketByte5, 0, out sendPacketByte5);
            else if (testAdd == 6)
                GetSendPacketNo(refPacketByte6, 0, out sendPacketByte6);
            else if (testAdd == 7)
                GetSendPacketNo(refPacketByte7, 0, out sendPacketByte7);
            else if (testAdd == 8)
                GetSendPacketNo(refPacketByte8, 0, out sendPacketByte8);

            i = 0; RCUFrame[i] = 0x7E;
            //i++; RCUFrame[i] = 0x01;
            i++; RCUFrame[i] = (byte)testAdd;
            i++; RCUFrame[i] = 0x93;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            sendFrame = getOrgFrame(RCUFrame);
        }
        /// <summary>
        /// 断开连接帧
        /// </summary>
        private void GenDisconnectFrame(int testAdd, out byte[] sendFrame)
        {
            byte[] RCUFrame = new byte[6];
            int i = 0;
            //byte midSendPacketByte = 0X00;

            /*
            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 0, out sendPacketByte);
            else
                GetSendPacketNo(refPacketByte2, 0, out sendPacketByte2);
            */

            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 0, out sendPacketByte);
            else if (testAdd == 2)
                GetSendPacketNo(refPacketByte2, 0, out sendPacketByte2);
            else if (testAdd == 3)
                GetSendPacketNo(refPacketByte3, 0, out sendPacketByte3);
            else if (testAdd == 4)
                GetSendPacketNo(refPacketByte4, 0, out sendPacketByte4);
            else if (testAdd == 5)
                GetSendPacketNo(refPacketByte5, 0, out sendPacketByte5);
            else if (testAdd == 6)
                GetSendPacketNo(refPacketByte6, 0, out sendPacketByte6);
            else if (testAdd == 7)
                GetSendPacketNo(refPacketByte7, 0, out sendPacketByte7);
            else if (testAdd == 8)
                GetSendPacketNo(refPacketByte8, 0, out sendPacketByte8);


            i = 0; RCUFrame[i] = 0x7E;
            //i++; RCUFrame[i] = 0x01;
            i++; RCUFrame[i] = (byte)testAdd;
            i++; RCUFrame[i] = 0x53;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            sendFrame = getOrgFrame(RCUFrame);
        }
        //解析设置命令中间帧；
        private int DnCodeMidFrame(int testAdd, byte[] refFrame)
        {
            byte[] RCUFrame = new byte[6];
            int i = 0;

            if (testAdd == 1)
                GetRefPacketNo(sendPacketByte, 0, out refPacketByte);
            else
                GetRefPacketNo(sendPacketByte2, 0, out refPacketByte2);

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            //RCU地址；
            //i++; RCUFrame[i] = 0x01;
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = refPacketByte;
            else
                RCUFrame[i] = refPacketByte2;


            crc_cal(ref RCUFrame, RCUFrame.Length);
            byte[] sendFrame = getOrgFrame(RCUFrame);

            for (int k = 0; k < refFrame.Length; k++)
            {
                if (sendFrame[k] != refFrame[k])
                {
                    return sendFrame[5];
                    //MessageBox.Show(sendFrame[2].ToString());
                    //MessageBox.Show(refFrame[2].ToString());
                    //return -1;
                }
            }

            return 1;
        }
        //组断电重启指令；
        private int EnCodeStopFrame(int testAdd, out byte[] sendFrame)
        {
            byte[] RCUFrame = new byte[11];
            int i;//数据帧计数

            //if (testAdd == 1)
            //    GetSendPacketNo(refPacketByte, 1, out sendPacketByte);
            //else
            //    GetSendPacketNo(refPacketByte2, 1, out sendPacketByte2);

            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 1, out sendPacketByte);
            else if (testAdd == 2)
                GetSendPacketNo(refPacketByte2, 1, out sendPacketByte2);
            else if (testAdd == 3)
                GetSendPacketNo(refPacketByte3, 1, out sendPacketByte3);
            else if (testAdd == 4)
                GetSendPacketNo(refPacketByte4, 1, out sendPacketByte4);
            else if (testAdd == 5)
                GetSendPacketNo(refPacketByte5, 1, out sendPacketByte5);
            else if (testAdd == 6)
                GetSendPacketNo(refPacketByte6, 1, out sendPacketByte6);
            else if (testAdd == 7)
                GetSendPacketNo(refPacketByte7, 1, out sendPacketByte7);
            else if (testAdd == 8)
                GetSendPacketNo(refPacketByte8, 1, out sendPacketByte8);

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            //RCU地址；
            //i++;   RCUFrame[i] = 0x01;
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            //if (testAdd == 1)
            //    RCUFrame[i] = sendPacketByte;
            //else
            //    RCUFrame[i] = sendPacketByte2;
            RCUFrame[i] = 0xBF;
            //具体命令；
            i++; RCUFrame[i] = 0x81;
            //帧内容长度；
            i++; RCUFrame[i] = 0xF0;
            //帧内容；
            i++; RCUFrame[i] = 0x02;
            i++; RCUFrame[i] = 0x07;
            i++; RCUFrame[i] = 0x00;

            crc_cal(ref RCUFrame, RCUFrame.Length);//
            sendFrame = getOrgFrame(RCUFrame);
            return 0;
        }
        //解析校准指令；
        private int DeCodeStopFrame(int testAdd, byte[] refFrame)
        {
            byte[] RCUFrame = new byte[11];
            int i;//数据帧计数

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            //RCU地址；            
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++; RCUFrame[i] = 0xBF;
            //具体命令；
            i++; RCUFrame[i] = 0x81;
            //帧内容长度；
            i++; RCUFrame[i] = 0xF0;
            //帧内容；
            i++; RCUFrame[i] = 0x02;
            i++; RCUFrame[i] = 0x07;
            i++; RCUFrame[i] = 0x00;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            byte[] sendFrame = getOrgFrame(RCUFrame);

            for (int k = 0; k < refFrame.Length; k++)
            {
                if (sendFrame[k] != refFrame[k])
                {
                    //return sendFrame[5];
                    return -1;
                }
            }

            return 0;
        }
        //组校准指令；
        private int EnCodeCalFrame(int testAdd, out byte[] sendFrame)
        {
            byte[] RCUFrame = new byte[9];
            int i;//数据帧计数

            /*
            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 1, out sendPacketByte);
            else
                GetSendPacketNo(refPacketByte2, 1, out sendPacketByte2);
            */

            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 1, out sendPacketByte);
            else if (testAdd == 2)
                GetSendPacketNo(refPacketByte2, 1, out sendPacketByte2);
            else if (testAdd == 3)
                GetSendPacketNo(refPacketByte3, 1, out sendPacketByte3);
            else if (testAdd == 4)
                GetSendPacketNo(refPacketByte4, 1, out sendPacketByte4);
            else if (testAdd == 5)
                GetSendPacketNo(refPacketByte5, 1, out sendPacketByte5);
            else if (testAdd == 6)
                GetSendPacketNo(refPacketByte6, 1, out sendPacketByte6);
            else if (testAdd == 7)
                GetSendPacketNo(refPacketByte7, 1, out sendPacketByte7);
            else if (testAdd == 8)
                GetSendPacketNo(refPacketByte8, 1, out sendPacketByte8);

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            //RCU地址；
            //i++;   RCUFrame[i] = 0x01;
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = sendPacketByte;
            else if (testAdd == 2)
                RCUFrame[i] = sendPacketByte2;
            else if (testAdd == 3)
                RCUFrame[i] = sendPacketByte3;
            else if (testAdd == 4)
                RCUFrame[i] = sendPacketByte4;
            else if (testAdd == 5)
                RCUFrame[i] = sendPacketByte5;
            else if (testAdd == 6)
                RCUFrame[i] = sendPacketByte6;
            else if (testAdd == 7)
                RCUFrame[i] = sendPacketByte7;
            else if (testAdd == 8)
                RCUFrame[i] = sendPacketByte8;


            //具体命令；
            i++; RCUFrame[i] = 0X31;
            //帧内容长度；
            i++; RCUFrame[i] = 0X00;
            //帧内容；
            i++; RCUFrame[i] = 0X00;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            sendFrame = getOrgFrame(RCUFrame);
            return 0;
        }
        //解析校准指令；
        private int DeCodeCalFrame(int testAdd, byte[] refFrame)
        {
            byte[] RCUFrame = new byte[10];
            int i;//数据帧计数

            if (testAdd == 1)
                GetRefPacketNo(sendPacketByte, 2, out refPacketByte);
            else
                GetRefPacketNo(sendPacketByte2, 2, out refPacketByte2);

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            //RCU地址；
            //i++; RCUFrame[i] = 0x01;
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = refPacketByte;
            else
                RCUFrame[i] = refPacketByte2;

            //具体命令；
            i++; RCUFrame[i] = 0X31;
            //帧内容长度；
            i++; RCUFrame[i] = 0X01;
            //帧内容；
            i++; RCUFrame[i] = 0X00;
            //帧内容；
            i++; RCUFrame[i] = 0X00;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            byte[] sendFrame = getOrgFrame(RCUFrame);

            for (int k = 0; k < refFrame.Length; k++)
            {
                if (sendFrame[k] != refFrame[k])
                {
                    return sendFrame[5];
                    //return -1;
                }
            }

            return 0;
        }
        //组轮询指令针；
        private int EnCodePollFrame(int testAdd, out byte[] sendFrame)
        {
            byte[] RCUFrame = new byte[6];
            int i;//数据帧计数

            if (testAdd == 1)
                sendPacketByte = (byte)((refPacketByte & 0xF0) + 0x01);
            else if (testAdd == 2)
                sendPacketByte2 = (byte)((refPacketByte2 & 0xF0) + 0x01);

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = sendPacketByte;
            else
                RCUFrame[i] = sendPacketByte2;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            sendFrame = getOrgFrame(RCUFrame);
            return 0;
        }
        //解析轮询指令针；
        private int DeCodePollFrame(int testAdd, byte[] refFrame)
        {
            byte[] RCUFrame = new byte[6];
            int i;//数据帧计数

            if (testAdd == 1)
                refPacketByte = sendPacketByte;
            else
                refPacketByte2 = sendPacketByte2;

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = refPacketByte;
            else
                RCUFrame[i] = refPacketByte2;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            byte[] sendFrame = getOrgFrame(RCUFrame);

            for (int k = 0; k < refFrame.Length; k++)
            {
                if (sendFrame[k] != refFrame[k])
                {
                    return -1;
                }
            }

            return 1;
        }
        //组读信息指令；
        private int EnCodeGetStasFrame(int testAdd, out byte[] sendFrame)
        {
            byte[] RCUFrame = new byte[9];
            int i;//数据帧计数

            /*
            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 1, out sendPacketByte);
            else if (testAdd == 2)
                GetSendPacketNo(refPacketByte2, 1, out sendPacketByte2);
            */

            if (testAdd == 1)
                GetSendPacketNo(refPacketByte, 1, out sendPacketByte);
            else if (testAdd == 2)
                GetSendPacketNo(refPacketByte2, 1, out sendPacketByte2);

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            //RCU地址；
            //i++;   RCUFrame[i] = 0x01;	
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = sendPacketByte;
            else
                RCUFrame[i] = sendPacketByte2;

            //具体命令；
            i++; RCUFrame[i] = 0X05;
            //帧内容长度；
            i++; RCUFrame[i] = 0X00;
            //帧内容；
            i++; RCUFrame[i] = 0X00;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            sendFrame = getOrgFrame(RCUFrame);

            return 0;
        }
        //解析读信息指令；
        private int DeCodeGetStasFrame(int testAdd, byte[] refFrame, out byte[] RCUInfo)
        {
            //strInfo = string.Empty;
            byte[] RCUFrame = new byte[7];
            int i;//数据帧计数
            RCUInfo = null;

            if (testAdd == 1)
                GetRefPacketNo(sendPacketByte, 1, out refPacketByte);
            else
                GetRefPacketNo(sendPacketByte2, 1, out refPacketByte2);

            //帧头；
            i = 0; RCUFrame[i] = 0x7E;
            //RCU地址；            
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = refPacketByte;
            else
                RCUFrame[i] = refPacketByte2;

            //具体命令；
            i++; RCUFrame[i] = 0X05;
            //具体命令；
            i++; RCUFrame[i] = 0X2C;
            //具体命令；
            i++; RCUFrame[i] = 0X00;
            i++; RCUFrame[i] = 0X00;

            for (int k = 0; k < RCUFrame.Length; k++)                                                              
            {
                if (RCUFrame[k] != refFrame[k])
                {
                    return -1;
                }
            }

            RCUInfo = new byte[refFrame.Length - RCUFrame.Length - 3];
            for (int k = 0; k < refFrame.Length - RCUFrame.Length - 3; k++)                             
            {
                RCUInfo[k] = refFrame[RCUFrame.Length + k];
            }

            //strInfo = Encoding.UTF8.GetString(RCUInfo);

            //crc_cal(ref RCUFrame, RCUFrame.Length);
            //byte[] sendFrame = getOrgFrame(RCUFrame);//

            return 0;
        }
        //解析下载文件指令；
        private int DeCodeConfigFrame(int testAdd, byte[] refFrame)
        {
            byte[] RCUFrame = new byte[10];
            int i;//数据帧计数

            if (testAdd == 1)
                GetRefPacketNo(sendPacketByte, 2, out refPacketByte);
            else
                GetRefPacketNo(sendPacketByte2, 2, out refPacketByte2);


            //帧头； 
            i = 0; RCUFrame[i] = 0x7E;
            //RCU地址；            
            i++; RCUFrame[i] = (byte)testAdd;
            //序号包；
            i++;
            if (testAdd == 1)
                RCUFrame[i] = refPacketByte;
            else
                RCUFrame[i] = refPacketByte2;

            //具体命令；
            i++; RCUFrame[i] = 0X32;
            //帧内容长度；
            i++; RCUFrame[i] = 0X01;
            //帧内容；
            i++; RCUFrame[i] = 0X00;
            //帧内容；
            i++; RCUFrame[i] = 0X00;

            crc_cal(ref RCUFrame, RCUFrame.Length);
            byte[] sendFrame = getOrgFrame(RCUFrame);

            for (int k = 0; k < refFrame.Length; k++)
            {
                if (sendFrame[k] != refFrame[k])
                {
                    //return sendFrame[5];
                    return -1;
                }
            }

            return 0;
        }
        //组搜索RCU的帧;
        /// <summary>
        ///搜索RCU指令
        /// </summary>
        /// <param name="count"></param>
        /// <param name="num"></param>
        /// <param name="sendFrame"></param>
        /// <returns></returns>
        private int EnCodeRCUScanFrame(int count, int num, out byte[] sendFrame)
        {
            byte[] RCUFrame = new byte[51];

            int ObjLen, i = 0;

            RCUFrame[i] = 0x7E;
            i++; RCUFrame[i] = 0xFF;
            i++; RCUFrame[i] = 0xBF;
            i++; RCUFrame[i] = 0x81;
            i++; RCUFrame[i] = 0xF0;

            ObjLen = 42;

            i++; RCUFrame[i] = (byte)ObjLen;

            for (int j = 0; j < ObjLen / 2; j++)
            {
                if (j == 0)
                {
                    i++;
                    RCUFrame[i] = 0x01;
                }
                else if (j == 1)
                {
                    i++;
                    RCUFrame[i] = 0x13;
                }
                else if (j == ObjLen / 2 - 1)
                {
                    i++;
                    RCUFrame[i] = (byte)count;
                }
                else
                {
                    i++;
                    RCUFrame[i] = 0x00;
                }
            }

            for (int j = 0; j < ObjLen / 2; j++)
            {
                if (j == 0)
                {
                    i++;
                    RCUFrame[i] = 0x03;
                }
                else if (j == 1)
                {
                    i++;
                    RCUFrame[i] = 0x13;
                }
                else if (j == ObjLen / 2 - 1)
                {
                    i++;
                    RCUFrame[i] = (byte)num;
                }
                else
                {
                    i++;
                    RCUFrame[i] = 0x00;
                }
            }

            crc_cal(ref RCUFrame, RCUFrame.Length);

            sendFrame = getOrgFrame(RCUFrame);

            return 0;
        }
        //组设置RCU地址的帧;
        private int EnCodeRCUAddFrame(out byte[] sendFrame, byte[] SN, byte address)
        {
            byte[] addFrame = new byte[33];

            int ObjLen, i = 0;

            addFrame[i] = 0x7E;
            i++; addFrame[i] = 0xFF;
            i++; addFrame[i] = 0xBF;
            i++; addFrame[i] = 0x81;
            i++; addFrame[i] = 0xF0;
            ObjLen = 24;
            i++; addFrame[i] = (byte)ObjLen;
            i++; addFrame[i] = 0x01;
            i++; addFrame[i] = 0x13;

            for (int j = 1; j <= SN.Length; j++)
            {
                addFrame[i + j] = SN[j - 1];
            }

            i++; addFrame[i + SN.Length] = 0x02;
            i++; addFrame[i + SN.Length] = 0x01;
            i++; addFrame[i + SN.Length] = address;

            crc_cal(ref addFrame, addFrame.Length);
            sendFrame = getOrgFrame(addFrame);

            return 0;
        }
        #endregion
    }
}
