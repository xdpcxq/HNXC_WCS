﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using THOK.Util;

namespace THOK.XC.Process.Dao
{
    public  class TaskDao : BaseDao
    {
        /// <summary>
        /// 根据出库任务生成Task_Detail，并返回。
        /// </summary>
        /// <returns></returns>
        public DataTable TaskOutToDetail()
        {
            //处理一楼出库，生成Task_Detail。
            DataTable dtCraneTask = CraneOutTask("TASK_TYPE=12");

            string strBillNo = "";
            string strSQL = "SELECT * FROM WCS_TASK_DETAIL LEFT JOIN WCS_TASK ON WCS_TASK_DETAIL.TASK_ID=WCS_TASK.TASK_ID WHERE TASK_TYPE=22 AND CRANE_NO IS NOT NULL AND WCS_TASK_DETAIL.STATE IN (1,2)";
            DataTable dt = ExecuteQuery(strSQL).Tables[0];
            if (dt.Rows.Count == 0)
            {
                strSQL = "SELECT top 1 WCS_TASK.BILL_NO  FROM WCS_TASK " +
                         "INNER JOIN WMS_BILL_MASTER  ON WCS_TASK.BILL_NO=WMS_BILL_MASTER.BILL_NO " +
                         "WHERE WMS_BILL_MASTER.STATE=3  and WCS_TASK.STATE=0 " +
                         "ORDER BY WMS_BILL_MASTER.SCHEDULE_NO,WMS_BILL_MASTER.SCHEDULE_ITEMNO,WCS_TASK.TASK_LEVEL, WCS_TASK.TASK_DATE,WCS_TASK.BILL_NO ";

                dt = ExecuteQuery(strSQL).Tables[0];
            }
            if (dt.Rows.Count > 0)
            {
                strBillNo = dt.Rows[0]["BILL_NO"].ToString();
            }


            string strWhere = string.Format("PRODUCT_CODE<>'0000' and TASK_TYPE=22 and BILL_NO ='{1}'", "22", strBillNo);
            dt = CraneOutTask(strWhere);
            dtCraneTask.Merge(dt);
            return dtCraneTask;
        }
        /// <summary>
        /// 出入库，插入明细。如果明细已经存在则不进行重新插入， 返回TaskNo
        /// </summary>
        /// <param name="task_id"></param>
        /// <returns></returns>
        public string InsertTaskDetail(string TaskID)
        {
            string strTaskDetailNo = "";

            string strSQL = string.Format("SELECT TASK_NO FROM  WCS_TASK_DETAIL WHERE TASK_ID='{0}' ", TaskID);
            DataTable dt = ExecuteQuery(strSQL).Tables[0];
            if (dt.Rows.Count > 0)
            {
                strTaskDetailNo = dt.Rows[0][0].ToString();
            }
            else
            {
                strTaskDetailNo = GetTaskDetailNo();
                using (PersistentManager pm = new PersistentManager())
                {

                    strSQL = string.Format("INSERT INTO WCS_TASK_DETAIL(TASK_ID,ITEM_NO,TASK_NO,ASSIGNMENT_ID,STATE,DESCRIPTION,BILL_NO) " +
                             "SELECT  WCS_TASK.TASK_ID,SYS_TASK_ROUTE.ITEM_NO,'{1}','{2}','0'," +
                             "SYS_TASK_ROUTE.DESCRIPTION,WCS_TASK.BILL_NO " +
                             "FROM WCS_TASK  " +
                             "LEFT JOIN SYS_TASK_ROUTE ON WCS_TASK.TASK_TYPE=SYS_TASK_ROUTE.TASK_TYPE " +
                             "WHERE TASK_ID='{0}' " +
                             "ORDER BY SYS_TASK_ROUTE.ITEM_NO ", TaskID, strTaskDetailNo, "0000" + strTaskDetailNo);
                    ExecuteNonQuery(strSQL);
                }
            }
            return strTaskDetailNo;
        }

     

        /// <summary>
        /// 获取已经插入Task_Detail 中，堆垛机调度程序。
        /// </summary>
        /// <returns></returns>
        public DataTable TaskCraneDetail(string strWhere)
        {
            string where = strWhere;
            if (strWhere.Trim() == "")
                where = "1=1";
            string strSQL = "SELECT TASK.TASK_ID,'' AS TASK_NO,SYS_TASK_ROUTE.ITEM_NO，''AS  ASSIGNMENT_ID,CMD_SHELF.CRANE_NO, '30'||TASK.cell_code||'01' AS FROM_STATION,SYS_STATION.CRANE_POSITION TO_STATION ,SYS_TASK_ROUTE.ITEM_NO ,'0' AS STATE,TASK.BILL_NO," +
                           "TASK.PRODUCT_CODE,TASK.CELL_CODE,TASK.TASK_TYPE,TASK.TASK_LEVEL,TASK.TASK_DATE,STYLE.SORT_LEVEL,TASK.IS_MIX,PRODUCT.STYLE_NO,SYS_STATION.SERVICE_NAME,SYS_STATION.ITEM_NAME_1," +
                           "SYS_STATION.ITEM_NAME_2,TASK.PRODUCT_BARCODE,TASK.PALLET_CODE,'' AS SQUENCE_NO,TASK.TARGET_CODE,SYS_STATION.STATION_NO,SYS_STATION.MEMO,TASK.PRODUCT_TYPE " +
                            "FROM WCS_TASK_DETAIL DETAIL " +
                            "LEFT JOIN WCS_TASK TASK  ON DETAIL.TASK_ID=TASK.TASK_ID " +
                            "LEFT JOIN CMD_PRODUCT  PRODUCT ON TASK.PRODUCT_CODE=PRODUCT.PRODUCT_CODE " +
                            "LEFT JOIN CMD_PRODUCT_STYLE STYLE ON STYLE.STYLE_NO=PRODUCT.STYLE_NO " +
                            "LEFT JOIN SYS_STATION ON DETAIL.CRANE_NO=SYS_STATION.CRANE_NO AND SYS_STATION.STATION_TYPE=TASK.TASK_TYPE " +
                            "WHERE DETAIL.CRANE_NO<>'' AND " + where +
                            "ORDER BY TASK.TASK_LEVEL,TASK.TASK_DATE,TASK.BILL_NO, TASK.IS_MIX,TASK.PRODUCT_CODE,TASK_ID";

            return ExecuteQuery(strSQL).Tables[0];
        }

        /// <summary>
        /// 根据Task获取出库信息
        /// </summary>
        /// <param name="strWhere"></param>
        /// <returns></returns>
        public DataTable CraneOutTask(string strWhere)
        {
            string where = strWhere;
            if (strWhere == "")
                where = "1=1";
            string strSQL = "SELECT TASK.TASK_ID,'' AS TASK_NO,SYS_TASK_ROUTE.ITEM_NO，''AS  ASSIGNMENT_ID,CMD_SHELF.CRANE_NO, '30'||TASK.cell_code||'01' AS FROM_STATION,SYS_STATION.CRANE_POSITION TO_STATION ,SYS_TASK_ROUTE.ITEM_NO ,'0' AS STATE,TASK.BILL_NO," +
                           "TASK.PRODUCT_CODE,TASK.CELL_CODE,TASK.TASK_TYPE,TASK.TASK_LEVEL,TASK.TASK_DATE,STYLE.SORT_LEVEL,TASK.IS_MIX,PRODUCT.STYLE_NO,SYS_STATION.SERVICE_NAME,SYS_STATION.ITEM_NAME_1," +
                           "SYS_STATION.ITEM_NAME_2,TASK.PRODUCT_BARCODE,TASK.PALLET_CODE,'' AS SQUENCE_NO,TASK.TARGET_CODE,SYS_STATION.STATION_NO,SYS_STATION.MEMO,TASK.PRODUCT_TYPE " +
                           "FROM WCS_TASK TASK " +
                           "LEFT JOIN CMD_CELL on CMD_CELL.CELL_CODE=TASK.CELL_CODE " +
                           "LEFT JOIN CMD_SHELF on CMD_CELL.SHELF_CODE=CMD_SHELF.SHELF_CODE " +
                           "LEFT JOIN SYS_TASK_ROUTE on SYS_TASK_ROUTE.TASK_TYPE=TASK.TASK_TYPE and SYS_TASK_ROUTE.ITEM_NO=1 " +
                           "LEFT JOIN SYS_STATION SYS_STATION on SYS_STATION.STATION_TYPE=TASK.TASK_TYPE and SYS_STATION.CRANE_NO=cmd_shelf.CRANE_NO " +
                           "LEFT JOIN CMD_PRODUCT  PRODUCT ON TASK.PRODUCT_CODE=PRODUCT.PRODUCT_CODE " +
                           "LEFT JOIN CMD_PRODUCT_STYLE STYLE ON STYLE.STYLE_NO=PRODUCT.STYLE_NO " +
                           "WHERE  STATE=0  AND " + where;
            return ExecuteQuery(strSQL).Tables[0];
        }

        /// <summary>
        /// 根据Task获取入库，起始位置，目标位置，及堆垛机编号
        /// </summary>
        /// <param name="strWhere"></param>
        /// <returns></returns>
        public DataTable TaskInCraneStation(string strWhere)
        {
            string where = strWhere;
            if (strWhere == "")
                where = "1=1";
            string strSQL = "SELECT '30'||TASK.cell_code||'01' AS TO_STATION,SYS_STATION.CRANE_POSITION AS FROM_STATION,CMD_SHELF.CRANE_NO FROM WCS_TASK TASK " +
                           "LEFT JOIN CMD_CELL on CMD_CELL.CELL_CODE=TASK.CELL_CODE " +
                           "LEFT JOIN CMD_SHELF on CMD_CELL.SHELF_CODE=CMD_SHELF.SHELF_CODE " +
                           "LEFT JOIN SYS_STATION SYS_STATION on SYS_STATION.STATION_TYPE=TASK.TASK_TYPE and SYS_STATION.CRANE_NO=CMD_SHELF.CRANE_NO" +
                           "WHERE  " + where;
            return ExecuteQuery(strSQL).Tables[0];
        }

        private string GetTaskDetailNo()
        {
            return "";
        }

        
        /// <summary>
        /// 更新任务状态Task
        /// </summary>
        /// <param name="TaskID"></param>
        /// <param name="state"></param>
        public void UpdateTaskState(string TaskID, string state)
        {
            string strSql = string.Format("UPDATE WCS_TASK SET STATE='{0}' WHERE TASK_ID='{1}'", state, TaskID);
            ExecuteNonQuery(strSql);
        }
        /// <summary>
        /// 更新堆垛机顺序号。
        /// </summary>
        /// <param name="TaskID"></param>
        /// <param name="Squenceno"></param>
        public void UpdateCraneQuenceNo(string TaskID, string Squenceno)
        {

        }

        /// <summary>
        /// 更新 货物到达小车站台 完成标志。 起始地址，目的地址
        /// </summary>
        /// <param name="TaskID"></param>
        public void UpdateStockOutToStationState(string TaskID,string ItemName)
        {
            
 
        }
        /// <summary>
        /// 根据条件，返回小车任务明细。
        /// </summary>
        /// <param name="strWhere"></param>
        /// <returns></returns>
        public DataTable TaskCarDetail(string strWhere)
        {
            string where = "1=1";
            if (!string.IsNullOrEmpty(strWhere))
                where = strWhere;
            string strSQL = "SELECT WCS_TASK.TASK_ID,CMD_CELL.CELL_CODE,STATION.STATION_NO,STATION.IN_STATION,ADDRESS1.CAR_ADDRESS STATION_NO_ADDRESS,ADDRESS2.CAR_ADDRESS IN_STATION_ADDRESS,CMD_SHELF.CRANE_NO,DETAIL.TASK_NO," +
                            "WCS_TASK.TASK_TYPE,DETAIL.CAR_NO,DETAIL.ITEM_NO,STATION.OUT_STATION_1,ADDRESS3.CAR_ADDRESS  OUT_STATION_1_ADDRESS, STATION.OUT_STATION_2,ADDRESS4.CAR_ADDRESS  OUT_STATION_2_ADDRESS,'' WRITEITEM,WCS_TASK.TARGET_CODE " +
                            "FROM WCS_TASK " +
                            "LEFT JOIN CMD_CELL ON WCS_TASK.CELL_CODE=CMD_CELL.CELL_CODE " +
                            "LEFT JOIN CMD_SHELF ON CMD_CELL.SHELF_CODE=CMD_SHELF.SHELF_CODE " +
                            "LEFT JOIN SYS_CAR_STATION STATION ON CMD_SHELF.CRANE_NO=STATION.CRANE_NO AND STATION.STATION_TYPE=WCS_TASK.TASK_TYPE " +
                            "LEFT JOIN SYS_CAR_ADDRESS ADDRESS1 ON STATION.STATION_NO=ADDRESS1.STATION_NO " +
                            "LEFT JOIN SYS_CAR_ADDRESS ADDRESS2 ON STATION.IN_STATION=ADDRESS2.STATION_NO " +
                            "LEFT JOIN SYS_CAR_ADDRESS ADDRESS3 ON STATION.OUT_STATION_1=ADDRESS3.STATION_NO "+
                            "LEFT JOIN SYS_CAR_ADDRESS ADDRESS4 ON STATION.OUT_STATION_2=ADDRESS4.STATION_NO "+ 
                            "INNER JOIN WCS_TASK_DETAIL DETAIL ON WCS_TASK.TASK_ID=DETAIL.TASK_ID " +
                            "WHERE " + strWhere;

            return ExecuteQuery(strSQL).Tables[0];
        }


        /// <summary>
        /// 获取堆垛机最大流水号
        /// </summary>
        /// <returns></returns>
        public string GetMaxSQUENCENO()
        {
            return "";
        }

       

        /// <summary>
        ///  更新任务明细状态
        /// </summary>
        /// <param name="TaskID"></param>
        /// <param name="TaskType"></param>
        public void UpdateTaskDetailState(string strWhere, string State)
        {
            string where = "1=1";
            if (!string.IsNullOrEmpty(strWhere))
                where = strWhere;
            string strSQL = string.Format("UPDATE WCS_TASK_DETAIL SET STATE='{0}' WHERE {1}", State, where);
            ExecuteNonQuery(strSQL);

        }
        /// <summary>
        /// 更新起始位置，目标位置
        /// </summary>
        /// <param name="FromStation"></param>
        /// <param name="ToStation"></param>
        /// <param name="strWhere"></param>
        public void UpdateTaskDetailStation(string FromStation, string ToStation, string State, string strWhere)
        {
            string where = "1=1";
            if (!string.IsNullOrEmpty(strWhere))
                where = strWhere;

            string strSQL = string.Format("UPDATE WCS_TASK_DETAIL SET  FROM_STATION='{0}',TO_STATION='{1}',STATE='{2}' WHERE {3}", new string[] { FromStation, ToStation, State, where });
            ExecuteNonQuery(strSQL);
        }

        /// <summary>
        /// 给小车安排任务，更新任务明细表小车编号，起始位置，结束位置
        /// </summary>
        /// <param name="CarNo"></param>
        public void UpdateTaskDetailCar(string FromStation, string ToStation, string state, string CarNo,string strWhere)
        {
            string where = "1=1";
            if (!string.IsNullOrEmpty(strWhere))
                where = strWhere;

            string strSQL = string.Format("UPDATE WCS_TASK_DETAIL SET  FROM_STATION='{0}',TO_STATION='{1}',STATE='{2}',CAR_NO='{3}'  WHERE {4}", new string[] { FromStation, ToStation, state, CarNo, where });
            ExecuteNonQuery(strSQL);

        }
        /// <summary>
        /// 给小车安排任务，更新任务明细表小车编号，起始位置，结束位置
        /// </summary>
        /// <param name="CarNo"></param>
        public void UpdateTaskDetailCrane(string FromStation, string ToStation, string state, string CraneNo, string strWhere)
        {
            string where = "1=1";
            if (!string.IsNullOrEmpty(strWhere))
                where = strWhere;

            string strSQL = string.Format("UPDATE WCS_TASK_DETAIL SET  FROM_STATION='{0}',TO_STATION='{1}',STATE='{2}',CRANE_NO='{3}' WHERE {4}", new string[] { FromStation, ToStation, state, CraneNo, where });
            ExecuteNonQuery(strSQL);

        }

        /// <summary>
        /// 分配货位,返回 0:TaskID，1:任务号，2:货物到达入库站台的目的地址--平面号,3:堆垛机入库站台，4:货位，5:堆垛机编号
        /// </summary>
        /// <param name="strWhere"></param>
        public string [] AssignCell(string strWhere)
        {
            string[] strValue = new string[6];
            string where = "1=1";
            if (!string.IsNullOrEmpty(strWhere))
                where = strWhere;
            string strSQL = "SELECT * FROM WCS_TASK WHERE " + where;
            DataTable dt = ExecuteQuery(strSQL).Tables[0];
            string TaskID = dt.Rows[0]["TASK_ID"].ToString();
           
            string billNo = dt.Rows[0]["BILL_NO"].ToString();
            string ProductCode = dt.Rows[0]["PRODUCT_CODE"].ToString();
            string VCell = "";
            if (dt.Rows[0]["CELL_CODE"].ToString() == "")
            {
                StoredProcParameter parameters = new StoredProcParameter();
                parameters.AddParameter("VBILLNO", billNo);
                parameters.AddParameter("VPRODUCTCODE", ProductCode);
                parameters.AddParameter("VCELL", "", DbType.String, ParameterDirection.Output);
                ExecuteNonQuery("APPLYCELL", parameters);
                VCell = parameters["VCELL"].ToString();
            }
            else
            {
                VCell = dt.Rows[0]["CELL_CODE"].ToString();
            }


            strSQL = string.Format("UPDATE CMD_CELL SET IS_LOCK='1' WHERE CELL_CODE='{0}'", VCell);
            ExecuteNonQuery(strSQL);

            strSQL = string.Format("UPDATE WCS_TASK SET CELL_CODE='{0}' WHERE {1}", VCell, where);
            ExecuteNonQuery(strSQL);


            SysStationDao sysdao = new SysStationDao();

            dt = sysdao.GetSationInfo(VCell, "11");


             ;
             string TaskNo = InsertTaskDetail(TaskID);

            strValue[0] = TaskID;
            strValue[1] = TaskNo;
            strValue[2] = dt.Rows[0]["STATION_NO"].ToString();
            strValue[3] = dt.Rows[0]["CRANE_POSITION"].ToString();
            strValue[4] = VCell;
            strValue[5] = dt.Rows[0]["CRANE_NO"].ToString();

            return strValue;

        }
        public string[] GetTaskInfo(string TaskNo)
        {
            string strSQL = string.Format("SELECT DISTINCT TASK.TASK_ID,TASK.BILL_NO FROM WCS_TASK_DETAIL DETAIL " +
                            "LEFT JOIN WCS_TASK TASK ON DETAIL.TASK_ID=TASK.TASK_ID " +
                            "WHERE DETAIL.TASK_NO='{0}'", TaskNo);
            DataTable dt = ExecuteQuery(strSQL).Tables[0];
            string[] str = new string[2];
            str[0] = dt.Rows[0]["TASK_ID"].ToString();
            str[1] = dt.Rows[0]["BILL_NO"].ToString();
            return str;
        }
        /// <summary>
        /// 二楼分配货位,返回 table 
        /// </summary>
        /// <param name="strWhere"></param>
        public void AssignCellTwo(string strWhere) //
        {
            string where = "1=1";
            if (!string.IsNullOrEmpty(strWhere))
                where = strWhere;
            string strSQL = "SELECT * FROM WCS_TASK WHERE " + where;
            DataTable dt = ExecuteQuery(strSQL).Tables[0];
            string TaskID = dt.Rows[0]["TASK_ID"].ToString();

            string billNo = dt.Rows[0]["BILL_NO"].ToString();
            string ProductCode = dt.Rows[0]["PRODUCT_CODE"].ToString();
            string VCell = "";
            if (dt.Rows[0]["CELL_CODE"].ToString() == "")
            {
                StoredProcParameter parameters = new StoredProcParameter();
                parameters.AddParameter("VBILLNO", billNo);
                parameters.AddParameter("VPRODUCTCODE", ProductCode);
                parameters.AddParameter("VCELL", "", DbType.String, ParameterDirection.Output);
                ExecuteNonQuery("APPLYCELL", parameters);
                VCell = parameters["VCELL"].ToString();
            }
            else
            {
                VCell = dt.Rows[0]["CELL_CODE"].ToString();
            }


            strSQL = string.Format("UPDATE CMD_CELL SET IS_LOCK='1' WHERE CELL_CODE='{0}'", VCell);
            ExecuteNonQuery(strSQL);

            strSQL = string.Format("UPDATE WCS_TASK SET CELL_CODE='{0}' WHERE {1}", VCell, where);
            ExecuteNonQuery(strSQL);

            InsertTaskDetail(TaskID);

            TaskDao dao = new TaskDao();
            dao.UpdateTaskState(TaskID, "1");//更新任务开始执行
            ProductStateDao StateDao = new ProductStateDao();
            StateDao.UpdateProductCellCode(TaskID, VCell); //更新Product_State 货位
            dao.UpdateTaskDetailStation("", "359", "2", string.Format("TASK_ID='{0}' AND ITEM_NO=1", TaskID)); //更新货位申请起始地址及目标地址。
        }
        /// <summary>
        /// 返回任务信息
        /// </summary>
        /// <param name="strWhere"></param>
        /// <returns></returns>
        public DataTable TaskInfo(string strWhere)
        {
            string where = "1=1";
            if (!string.IsNullOrEmpty(strWhere))
                where = strWhere;
            string strSQL = "SELECT * FROM WCS_TASK WHERE " + where;


            return ExecuteQuery(strSQL).Tables[0];
        }

        /// <summary>
        /// 根据单号，返回任务数量
        /// </summary>
        /// <param name="BillNo"></param>
        /// <returns></returns>
        public int TaskCount(string BillNo)
        {

            string strSQL = string.Format("SELECT COUNT(*) FROM WCS_TASK WHERE BILL_NO='{0}'", BillNo);
            DataTable dt=ExecuteQuery(strSQL).Tables[0];
            return int.Parse(dt.Rows[0][0].ToString());
        }

    }
}
