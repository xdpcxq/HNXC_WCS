﻿using System;
using System.Collections.Generic;
using System.Text;
using THOK.MCP;
using System.Data;
using THOK.XC.Process.Dal;

namespace THOK.XC.Process.Process_02
{
    public class StockOutSeparateProcess : AbstractProcess
    {
        protected override void StateChanged(StateItem stateItem, IProcessDispatcher dispatcher)
        {
            /*  处理事项：
             * 二层出库拆盘完成
             *  stateItem.State ：参数 - 请求的卷烟编码。        
            */

            object[] obj = ObjectUtil.GetObjects(stateItem.State);

            if (obj[0] == null || obj[0].ToString() == "0")
                return;


            string WriteItem="";
            string FromStation = "";
            try
            {
                switch (stateItem.ItemName)
                {
                    case "02_1_372":
                        FromStation = "372";
                        WriteItem = "02_2_372";
                        break;
                    case "02_1_392":
                        FromStation = "392";
                        WriteItem = "02_2_392";
                        break;
                }
                string TaskNo = obj[0].ToString().PadLeft(4, '0');
                TaskDal dal = new TaskDal();
                string[] strValue = dal.GetTaskInfo(TaskNo);
                if (!string.IsNullOrEmpty(strValue[0]))
                {
                    ChannelDal cdal = new ChannelDal();
                    string strChannelNo = cdal.InsertChannel(strValue[0],strValue[1]);//分配缓存道
                    if (strChannelNo != "")
                    {
                        dal.UpdateTaskDetailState(string.Format("TASK_ID='{0}' AND ITEM_NO=4", strValue[0]), "2");
                        WriteToService("StockPLC_02", WriteItem + "_1", int.Parse(strChannelNo));
                        WriteToService("StockPLC_02", WriteItem + "_2", 1);
                        dal.UpdateTaskDetailStation(FromStation, strChannelNo, "1", string.Format("TASK_ID='{0}' AND ITEM_NO=5", strValue[0]));
                    }

                }

            }
            catch (Exception e)
            {
                Logger.Error("THOK.XC.Process.Process_02.StockOutSeparateProcess，原因：" + e.Message);
            }
        }
    }
}
