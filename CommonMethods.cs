using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using DataAccess;
using System.Configuration;
using System.IO;
using System.Diagnostics;
using CustomLogs;

namespace PushToEMRS
{
    public class CommonMethods
    {   
        /// <summary>
        /// NOT IN USE
        /// </summary>
        private StringBuilder stringBuilder;
        private readonly IOperations operations;
        private bool writeToLogFile;
        public CommonMethods()
        {
            operations = new DataAccess.Operations();
            writeToLogFile = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("WriteToLogFile"));
        }
        public void InsertErrorCode(int contextId, string APIendpoint, string methodtype, int dataid, string tablename, string calljson, string response, string exception, string stacktrace)
        {
            stringBuilder = new StringBuilder();
            stringBuilder.Append("INSERT tbl_emrs_sync_errors (calldate,contextId,APIendpoint,methodtype,dataid_PK,tablename,calljson,responsejson,exception,stacktrace) ")
           .Append("VALUES ( '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + contextId.ToString() + "', '" + APIendpoint + "','" + methodtype + "','" + dataid.ToString() + "','" + tablename + "','" + calljson + "','" + response + "','" + exception + "','" + stacktrace + "')");
            operations.Insert(stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));

        }
        public void UpdatePushEMRSOnError(int dataid)
        {
            stringBuilder = new StringBuilder();
            stringBuilder.Append("UPDATE table_303 set pushToEMRS = null where dataid  = " + dataid.ToString());
            operations.Update(stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));

        }
        public void UpdatePushEMRSOnJobCompletion()
        {
            stringBuilder = new StringBuilder();
            stringBuilder.Append("UPDATE t3 set pushToEMRS = getdate() from dbo.TABLE_303 t3 where pushToEMRS = null and ")
             .Append(" dataid not in (select dataid_PK from tbl_emrs_sync_errors where CAST(calldate AS date) = CAST(getdate() AS date)) ");
            operations.Update(stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));

        }

        public void InsertJobStatus(string stepname, string errormessage)
        {
            stringBuilder = new StringBuilder();
            stringBuilder.Append("Insert [tbl_SqlJobStatus] ( JobName,[StepName] ,[StartTime] ,[EndTime] ,[ErrorLine] , [ErrorMessage]  ) ");
            stringBuilder.Append(" values ( 'VshocEMRSDataSync_ConsoleAPP', '" + stepname + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',N'', N'','" + errormessage + "')");            
           
            operations.Insert(stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));

        }
        // createa a method to insert ols and new values in history table
        public void writeconsole(  string msg , bool isSQL)
        {
            if ((writeToLogFile) )
            {
                if ( (isSQL))
                Console.WriteLine(CustomLogs.LogHelper.Log(LogTarget.SQL, msg));
                else
                  Console.WriteLine(CustomLogs.LogHelper.Log(LogTarget.API, msg));
            }


        }
       
        public string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }


    }
}
