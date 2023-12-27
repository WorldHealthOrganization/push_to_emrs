using DataAccess;
using EMRSAPI;
using EMRSAPI.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomLogs;


namespace PushToEMRS
{
    public class PushUsersToEMRS
    {
        private static readonly string EMS2ClientId = ConfigurationManager.AppSettings["EMS2ClientId"].ToString();
        private static readonly string EMS2ClientSecret = ConfigurationManager.AppSettings["EMS2ClientSecret"].ToString();
        private static readonly string EMS2Scope = ConfigurationManager.AppSettings["EMS2Scope"].ToString();
        private static readonly string APITokenURL = ConfigurationManager.AppSettings["APITokenURL"].ToString();
        private static EMRSAPI.Interface.ITokenService tokenService;
        private StringBuilder stringBuilder;
        private DataTable dataTable;
        private readonly IOperations operations;
        private EMRSAPI.Interface.IEMRSAPIs eMRSAPIs;
        private readonly CommonFunctions commonfunction;
        private readonly SendEmail sendEmail;

        public PushUsersToEMRS()
        {
            tokenService = new EMRSTokenService(EMS2ClientId, EMS2ClientSecret, APITokenURL, EMS2Scope);
            operations = new Operations();
            eMRSAPIs = new EMRSAPIServices();
            commonfunction = new CommonFunctions();
            sendEmail = new SendEmail();

        }

        public async Task PushUsers(string azureid,bool isInsert,bool isDelete,int dataid, int contextid)
        {
            try
            {   commonfunction.InsertJobStatus("consoleApp_InsertUpdateDeactivateUser_PushUser", null);
                commonfunction.writeconsole("\n---Preparing to push users to EMRS", false);
                commonfunction.writeconsole("\n---Selecting users from vPushUserstoEMRS", false);               
                commonfunction.writeconsole("\n---Selecting users from vPushUserstoEMRS", true);

                stringBuilder = new StringBuilder();

                stringBuilder.Append(" select  * from vPushUserstoEMRS where emrsuserid='" + azureid + "' order by EmailAddress");
                commonfunction.writeconsole(stringBuilder.ToString(), true);

                dataTable = operations.Read(stringBuilder.ToString(), ConfigurationManager.AppSettings.Get("vShocDbConnection"));
                if (dataTable.Rows.Count > 0)
                {
                    string token;
                    commonfunction.writeconsole("\n--Total " + dataTable.Rows.Count + " user found--", true);
                   
                    
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        try
                        {
                            if (isDelete) //Deactivate
                            {
                                commonfunction.writeconsole("\n--Prepareing to deactivate the user " + Convert.ToString(dataTable.Rows[i]["EmailAddress"]) + "  to EMRS--", false);
                                UsersUpdateRequest usersUpdateModel = new UsersUpdateRequest();
                                UserUpdate user = new UserUpdate()
                                {
                                    FirstName = Convert.ToString(dataTable.Rows[i]["FirstName"]),
                                    LastName = Convert.ToString(dataTable.Rows[i]["LastName"]),
                                    OrgPath = Convert.ToString(dataTable.Rows[i]["OrgPath"]),
                                    CountryId = dataTable.Rows[i]["CountryId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dataTable.Rows[i]["CountryId"]),
                                    RegionId = dataTable.Rows[i]["RegionId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dataTable.Rows[i]["RegionId"]),
                                    LocationTypeId = dataTable.Rows[i]["LocationTypeId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dataTable.Rows[i]["LocationTypeId"]),
                                    InternalExternalId = Convert.ToInt32(dataTable.Rows[i]["InternalExternalId"]),
                                    Agency = Convert.ToString(dataTable.Rows[i]["Agency"]),
                                    IsActive = false
                                };
                                usersUpdateModel.Users.Add(user);

                                token = await tokenService.GetToken();

                                commonfunction.writeconsole("\n--Request Parameters: "+ Newtonsoft.Json.JsonConvert.SerializeObject(usersUpdateModel),false);
                                //LogHelper.Log(LogTarget.API, "Request Parameters :" + Newtonsoft.Json.JsonConvert.SerializeObject(usersModel));
                                 await eMRSAPIs.PushContactsDataToEMRSAsync<UserUpdate>(user, System.Configuration.ConfigurationManager.AppSettings.Get("UsersAPI") + "/" + azureid, token, isInsert, isDelete, dataid,azureid);
                                
                            }
                            else
                            {
                                if (isInsert)
                                {
                                    UsersCreateRequest usersCreateModel = new UsersCreateRequest();
                                    UserCreate user = new UserCreate()
                                    {
                                        UserId = Convert.ToString(azureid),
                                        FirstName = Convert.ToString(dataTable.Rows[i]["FirstName"]),
                                        LastName = Convert.ToString(dataTable.Rows[i]["LastName"]),
                                        OrgPath = Convert.ToString(dataTable.Rows[i]["OrgPath"]),
                                        CountryId = dataTable.Rows[i]["CountryId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dataTable.Rows[i]["CountryId"]),
                                        RegionId = dataTable.Rows[i]["RegionId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dataTable.Rows[i]["RegionId"]),
                                        LocationTypeId = dataTable.Rows[i]["LocationTypeId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dataTable.Rows[i]["LocationTypeId"]),
                                        InternalExternalId = Convert.ToInt32(dataTable.Rows[i]["InternalExternalId"]),
                                        Agency = Convert.ToString(dataTable.Rows[i]["Agency"]),
                                        EmailAddress = Convert.ToString(dataTable.Rows[i]["EmailAddress"]).Trim()

                                    };
                                    usersCreateModel.Users.Add(user);

                                    token = await tokenService.GetToken();
                                    //LogHelper.Log(LogTarget.API, "Request Parameters :" + Newtonsoft.Json.JsonConvert.SerializeObject(usersModel));
                                      await eMRSAPIs.PushContactsDataToEMRSAsync<UsersCreateRequest>(usersCreateModel, System.Configuration.ConfigurationManager.AppSettings.Get("UsersAPI"), token, isInsert, isDelete, dataid,azureid);
                                    

                                }
                                else
                                {

                                    UsersUpdateRequest usersUpdateModel = new UsersUpdateRequest();
                                    UserUpdate user = new UserUpdate()
                                    {
                                        FirstName = Convert.ToString(dataTable.Rows[i]["FirstName"]),
                                        LastName = Convert.ToString(dataTable.Rows[i]["LastName"]),
                                        OrgPath = Convert.ToString(dataTable.Rows[i]["OrgPath"]),
                                        CountryId = dataTable.Rows[i]["CountryId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dataTable.Rows[i]["CountryId"]),
                                        RegionId = dataTable.Rows[i]["RegionId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dataTable.Rows[i]["RegionId"]),
                                        LocationTypeId = dataTable.Rows[i]["LocationTypeId"] == DBNull.Value ? null : (int?)Convert.ToInt32(dataTable.Rows[i]["LocationTypeId"]),
                                        InternalExternalId = Convert.ToInt32(dataTable.Rows[i]["InternalExternalId"]),
                                        Agency = Convert.ToString(dataTable.Rows[i]["Agency"]),
                                        IsActive = true
                                    };
                                    usersUpdateModel.Users.Add(user);

                                    token = await tokenService.GetToken();
                                    //LogHelper.Log(LogTarget.API, "Request Parameters :" + Newtonsoft.Json.JsonConvert.SerializeObject(usersModel));
                                    await eMRSAPIs.PushContactsDataToEMRSAsync<UserUpdate>(user, System.Configuration.ConfigurationManager.AppSettings.Get("UsersAPI") + "/" + azureid, token, isInsert, isDelete, dataid,azureid);
                                     

                                }
                            }


                        }
                        catch (Exception ex)
                        {

                            commonfunction.InsertErrorCode(contextid, null, null, dataid, "Users", null, null, commonfunction.RemoveSpecialCharacters(ex.Message), commonfunction.RemoveSpecialCharacters(ex.StackTrace));
                            commonfunction.writeconsole("\n--Error has occured while preparing data for insert/update/deactivating in users [Method PushUsers] --" + commonfunction.RemoveSpecialCharacters(ex.Message), true);
                            commonfunction.InsertJobStatus("consoleApp_InsertUpdateDeactivateUser_PushUsers", commonfunction.RemoveSpecialCharacters(ex.Message));

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                commonfunction.InsertErrorCode(contextid, null, null, dataid, "Users", null, null, ex.Message, ex.StackTrace);
                commonfunction.writeconsole("\n--Error has occured by insert/update/deactivating in users --" + ex.Message, true);
                commonfunction.InsertJobStatus("consoleApp_InsertUpdateDeactivateUser_PushUsers", ex.Message);
            }

        }
    }
    
}
