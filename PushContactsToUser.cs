using DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace PushToEMRS
{
    public class PushContactDataToUsers
    {
        private DataTable dataTable;
        private readonly IOperations operations;
        private StringBuilder stringBuilder;

        public PushContactDataToUsers()
        {
            operations = new Operations();
        }
        // NOT IN USEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE
        public bool PushUsers()
        {
            try
            {
                //Console.WriteLine(LogHelper.Log(LogTarget.SQL, "\n--Selecting Users from [table_303] table with Azure id's which are not avail in [Users] table--"));
                stringBuilder = new StringBuilder();
                stringBuilder.Append("select p.email1,p.firstname,p.familyname, temp.UsersObjectId,p.userid,p.vshocuser")
                    .Append(" FROM dbo.table_303 AS p")
                    .Append(" INNER JOIN WHOAzureUsers AS temp ON temp.[UserEmail] = p.[email1]")
                    .Append(" FULL OUTER JOIN Users AS U ON U.name=p.email1")
                    .Append(" where prevdataid=0 and u.name is NULL");

                dataTable = operations.Read( stringBuilder.ToString() , ConfigurationManager.AppSettings.Get("vShocDbConnection"));
                if (dataTable.Rows.Count > 0)
                {
                    //Console.WriteLine(LogHelper.Log(LogTarget.SQL, "\n--Total " + dataTable.Rows.Count + " records found--"));
                    //Console.WriteLine(LogHelper.Log(LogTarget.SQL, "\n--Inserting Users from [table_303] table with Azure id's which are not avail into [Users] table--"));
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        try
                        {
                            stringBuilder = new StringBuilder();
                            Guid guserid = Guid.NewGuid();
                            string email = dataTable.Rows[i].ItemArray[0].ToString();
                            string firstName = Convert.ToString(dataTable.Rows[i].ItemArray[1].ToString()).Replace("'", "''");
                            string familyName = Convert.ToString(dataTable.Rows[i].ItemArray[2].ToString()).Replace("'", "''");
                            string emrsuserid = dataTable.Rows[i].ItemArray[3].ToString();
                            string realName = familyName.ToUpper() + ", " + firstName;
                            DateTime dateTime = Convert.ToDateTime("1900-01-03T00:00:00.000");

                            stringBuilder.Append("INSERT [dbo].[Users] ([name], [password], [primaryemail], [multipleuser], [color], [administrator], [deletable], [failedattempts], [passwordexpiration], [lockoutexpiration], [accountlocked], [mustchangepwd], [salt], [dualcommit], [langlcid], [formatlcid], [timezoneoverride], [timezone], [daylight], [lockoutset], [passwordset], [lastattemptedlogin], [lastlogin], [lcid], [timezoneid], [comments], [realname], [location], [officephone], [mobilephone], [officephoneisdefault], [department], [supervisor], [attachmentid], [organization], [overrideorglocale], [disableuserupdate], [allowinactivityexpiration], [accounttype], [defaultincidentid], [globaluserid], [datecreated], [expirationdate], [supervisoremail], [supervisorphone], [lastpositionid], [emrsuserid], [pushToEMRS])")
                           .Append("VALUES ( '" + email + "', N'', '" + email + "', 0, NULL, 0, 1, 0, NULL, NULL, 0, 0, N'', 0, 0, 0, 0, 60, 1, NULL,'" + dateTime.ToString("yyyy-MM-dd HH:mm:ss") + "', NULL, NULL, NULL, NULL, N' ','" + realName + "', NULL, NULL, NULL, 1, NULL, NULL, 0, NULL, 0, 0, 1, 0, NULL,'" + guserid.ToString() + "', NULL, NULL, NULL, NULL, NULL,'" + emrsuserid + "',NULL)");
                            operations.Insert( stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));
                            //ProgressBar.DrawProgressBar(i + 1, dataTable.Rows.Count);

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
