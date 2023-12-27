using CustomLogs;
using DataAccess;
using GraphWebAPI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


namespace PushToEMRS
{
    public class PushContactsToEMRS
    {
        private DataTable dataTable;
        private readonly IOperations operations;
        private StringBuilder stringBuilder;
        // private bool writeToLogFile;
       
        private readonly CommonFunctions commonfunction;
        private readonly SendEmail sendEmail;
       

        public PushContactsToEMRS()
        {
            operations = new Operations();           
            commonfunction = new CommonFunctions();
            sendEmail = new SendEmail();
            
        }

        public async Task StartProcessAsync()
        {
            try
            {
                commonfunction.InsertJobStatus("consoleApp_Createcontacts", null);
                commonfunction.writeconsole("------------------Start Create Contacts -------------------", true);
                commonfunction.writeconsole("------------------Start Create Contacts -------------------", false);


                await MigrateAsync(true, false); //create


                commonfunction.writeconsole("------------------END Create Contacts -------------------", true);
                commonfunction.writeconsole("------------------END Create Contacts -------------------", false);
                commonfunction.writeconsole("------------------Start Update Contacts -------------------", true);
                commonfunction.writeconsole("------------------Start Update Contacts -------------------", false);
                commonfunction.InsertJobStatus("consoleApp_Updatecontacts", null);

                await MigrateAsync(false, false); // update

                commonfunction.writeconsole("------------------END Update Contacts -------------------", true);
                commonfunction.writeconsole("------------------END Update Contacts -------------------", false);
                commonfunction.writeconsole("------------------Start Delete Contacts -------------------", true);
                commonfunction.writeconsole("------------------Start Delete Contacts -------------------", false);
                commonfunction.InsertJobStatus("consoleApp_Deletecontacts", null);


                await MigrateAsync(false, true); // Delete


                commonfunction.writeconsole("------------------END Delete Contacts -------------------", true);
                commonfunction.writeconsole("------------------END Delete Contacts -------------------", false);
                commonfunction.writeconsole("-----------------Start-Update EMRS on Job completion -------------------", false);
                commonfunction.writeconsole("----------------Start--Update EMRS on Job completion -------------------", true);
                commonfunction.InsertJobStatus("consoleApp_UpdatePushEMRSCol", null);
                commonfunction.UpdatePushEMRSOnJobCompletion();

                commonfunction.writeconsole("----------------END--Update EMRS on Job completion -------------------", false);
                commonfunction.writeconsole("---------------END---Update EMRS on Job completion -------------------", true);

                commonfunction.InsertJobStatus("Calling EMail Function", null);
                commonfunction.writeconsole("\n--Calling EMail Function", false);
                sendEmail.SendMail(string.Empty);
                commonfunction.writeconsole("\n--EMail Function Ends", false);
                commonfunction.InsertJobStatus("Ends EMail Function", null);
            }
            catch (Exception ex)
            {
                commonfunction.InsertErrorCode(0, null, null, 0, "StartProcess", null, null, commonfunction.RemoveSpecialCharacters(ex.Message), commonfunction.RemoveSpecialCharacters(ex.StackTrace));
                commonfunction.writeconsole("\n--Error has occured by insert/update/deactivating in users --" + commonfunction.RemoveSpecialCharacters(ex.Message), true);
                commonfunction.InsertJobStatus("consoleApp_InsertUpdateDeactivateUser_MigrateAsync", commonfunction.RemoveSpecialCharacters(ex.Message));

            }

        }

        /// <summary>
        /// This method fetches all new users created, checks if the AzureId exists-if so inserts the data into vShoc users table and pushes the same to EMRS. 
        /// Pre Requisite: Modify the users table to accomodate emrsuserid.
        /// </summary>
        private async Task MigrateAsync(bool isNew,bool isDelete)
        {
            int contextid = 0;
            int dataid = 0;
            try
            {
                commonfunction.InsertJobStatus("consoleApp_InsertUpdateDeactivateUser_MigrateAsync", null);
                commonfunction.writeconsole("\n--Selecting contacts from table_303 pusheMRS is equal to current date and isinsert=1 or 0---", true);
                stringBuilder = new StringBuilder();
                
                if (isDelete)
                {
                    stringBuilder.Append("select email1,Firstname,FamilyName,0,  ");
                    stringBuilder.Append(" dataid, prevdataid ,contactguid from table_303 t inner join users u on email1 = name " );
                    stringBuilder.Append(" where prevdataid != 0 and CAST(isnull(pushtoEMRS,getdate()) AS date) = CAST(getdate() AS date) and name is not null and isnull(accountlocked,0)=0 ");
                }
                else // for insert (isnew = true)  and update (isnew = false)
                {
                    if (isNew)
                    {
                        stringBuilder.Append("select email1, Firstname, FamilyName, case when(name is null and emrsuserid is null) then 1 when(name is not null and emrsuserid is null) then 0 ");
                        stringBuilder.Append(" when  (name is not null and emrsuserid is not null) then 0  end ,dataid, prevdataid ,contactguid from table_303 t left join users u on email1 = name "
                           + " and isnull(accountlocked,0)=0 and isnull(u.synctoemrs, 0) = " + Convert.ToInt32(!isNew).ToString());
                        stringBuilder.Append(" where prevdataid = 0 and CAST(isnull(pushtoEMRS,getdate()) AS date) = CAST(getdate() AS date) and isnull(isInsert,0)=" + Convert.ToInt32(isNew).ToString());
                        stringBuilder.Append(" order by email1");
                    }
                    else
                    {
                        stringBuilder.Append("select email1, Firstname, FamilyName, case when(name is null and emrsuserid is null) then 1 when(name is not null and emrsuserid is null) then 0 ");
                        stringBuilder.Append(" when  (name is not null and emrsuserid is not null) then 0  end ,dataid, prevdataid ,contactguid from table_303 t left join users u on email1 = name ");
                        stringBuilder.Append(" where prevdataid = 0 and CAST(isnull(pushtoEMRS,getdate()) AS date) = CAST(getdate() AS date) and isnull(isInsert,0)=" + Convert.ToInt32(isNew).ToString());
                        stringBuilder.Append(" and isnull(accountlocked, 0) = 0 and isnull(u.synctoemrs, 0) = " + Convert.ToInt32(!isNew).ToString()) ;
                            stringBuilder.Append(" order by email1");

                    }
                    
                }


                dataTable = operations.Read(stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));
                if (dataTable.Rows.Count > 0)
                {
                    commonfunction.writeconsole("\n--Total " + dataTable.Rows.Count + " records found--", true);
                    commonfunction.writeconsole("\n--Insert/Update/Deactivate records in Users table--", true);
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        commonfunction.writeconsole("\n--Started process for EmailID" + dataTable.Rows[i].ItemArray[0].ToString(), false);
                        try
                        {
                            string User = dataTable.Rows[i].ItemArray[0].ToString();
                            GraphWebAPI.GetAzureUserDetails objGAID = new GetAzureUserDetails();
                            string _azureID = objGAID.GetAzureUserID(User);
                            if (!string.IsNullOrEmpty(_azureID))
                            {
                               
                                stringBuilder = new StringBuilder();

                                Guid guserid = Guid.NewGuid();
                                string email = dataTable.Rows[i].ItemArray[0].ToString();
                                string firstName = Convert.ToString(dataTable.Rows[i].ItemArray[1].ToString()).Replace("'", "''");
                                string familyName = Convert.ToString(dataTable.Rows[i].ItemArray[2].ToString()).Replace("'", "''");
                                Boolean isCreateUser = Convert.ToBoolean(dataTable.Rows[i].ItemArray[3]);
                                string emrsuserid = _azureID;
                                string realName = familyName.ToUpper() + ", " + firstName;
                                DateTime dateTime = Convert.ToDateTime("1900-01-03T00:00:00.000");
                                dataid = Convert.ToInt32(dataTable.Rows[i].ItemArray[4]);
                                int prevdataid = Convert.ToInt32(dataTable.Rows[i].ItemArray[5]);
                                string contactguid = Convert.ToString(dataTable.Rows[i].ItemArray[6].ToString()).Replace("'", "''");

                                if (isDelete)
                                {
                                    commonfunction.writeconsole("\n--Preparing to deactivate user in vShoc with email ID" + email, false);
                                    contextid = 6;
                                    stringBuilder.Append(" Update [dbo].[Users] set accountlocked =1 where name like '" + email + "'");
                                    operations.Update(stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));
                                    commonfunction.writeconsole("\n-- Deactivated user in vShoc with email ID" + email, false);
                                }
                                else
                                {
                                     if ((isNew) &&  (isCreateUser))
                                    {

                                        contextid = 4;
                                        commonfunction.writeconsole("\n--Preparing to create user in vShoc with email ID" + email, false);
                                        commonfunction.writeconsole("\n--Preparing to Create user in vShoc with email ID" + email, true);
                                        stringBuilder.Append("INSERT [dbo].[Users] ([name], [password], [primaryemail], [multipleuser], [color], [administrator], [deletable], [failedattempts], [passwordexpiration], [lockoutexpiration], [accountlocked], [mustchangepwd], [salt], [dualcommit], [langlcid], [formatlcid], [timezoneoverride], [timezone], [daylight], [lockoutset], [passwordset], [lastattemptedlogin], [lastlogin], [lcid], [timezoneid], [comments], [realname], [location], [officephone], [mobilephone], [officephoneisdefault], [department], [supervisor], [attachmentid], [organization], [overrideorglocale], [disableuserupdate], [allowinactivityexpiration], [accounttype], [defaultincidentid], [globaluserid], [datecreated], [expirationdate], [supervisoremail], [supervisorphone], [lastpositionid], [emrsuserid])")
                                       .Append("VALUES ( '" + email + "', N'', '" + email + "', 0, NULL, 0, 1, 0, NULL, NULL, 0, 0, N'', 0, 0, 0, 0, 60, 1, NULL,'" + dateTime.ToString("yyyy-MM-dd HH:mm:ss") + "', NULL, NULL, NULL, NULL, N' ','" + realName + "', NULL, NULL, NULL, 1, NULL, NULL, 0, NULL, 0, 0, 1, 0, NULL,'" + guserid.ToString() + "', NULL, NULL, NULL, NULL, NULL,'" + emrsuserid + "')");
                                        operations.Insert(stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));
                                        commonfunction.writeconsole("\n-- created user in vShoc with email ID" + email, false);
                                    }
                                    else
                                    {
                                        contextid = 3;
                                        commonfunction.writeconsole("\n--Preparing to Update user in vShoc with email ID" + email, false);
                                        commonfunction.writeconsole("\n--Preparing to Update user in vShoc with email ID" + email, true);
                                        stringBuilder.Append("Update [dbo].[Users] set [emrsuserid] ='" + _azureID + "' where name = '" + email + "'");
                                        operations.Update(stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));
                                        commonfunction.writeconsole("\n-- Updated user in vShoc with email ID" + email, false);
                                    }
                                }

                                commonfunction.writeconsole(stringBuilder.ToString(), true);

                                PushUsersToEMRS pushUsers = new PushUsersToEMRS();
                                await pushUsers.PushUsers(_azureID, isNew, isDelete,dataid,contextid);
                                 
                            }
                           
                        }
                        catch (Exception ex)
                        {
                            commonfunction.InsertErrorCode(contextid, null, null, dataid, "Users", null, null,commonfunction.RemoveSpecialCharacters(ex.Message), commonfunction.RemoveSpecialCharacters(ex.StackTrace));
                            commonfunction.writeconsole("\n--Error has occured by insert/update/deactivating in users --" + commonfunction.RemoveSpecialCharacters(ex.Message), true);
                            commonfunction.InsertJobStatus("consoleApp_InsertUpdateDeactivateUser_MigrateAsync", commonfunction.RemoveSpecialCharacters(ex.Message));
                           
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                commonfunction.InsertErrorCode(contextid, null, null, dataid, "Users", null, null, ex.Message, ex.StackTrace);
                commonfunction.writeconsole("\n--Error has occured by insert/update/deactivating in users --" + ex.Message, true);
                commonfunction.InsertJobStatus("consoleApp_InsertUpdateDeactivateUser_MigrateAsync", ex.Message);
            }

        }

        
    }
}







