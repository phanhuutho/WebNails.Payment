using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebNails.Payment.Models;

namespace WebNails.Payment.Utilities
{
    public class TokenAttribute : ActionFilterAttribute
    {
        private readonly string TokenKeyAPI = ConfigurationManager.AppSettings["TokenKeyAPI"];
        private readonly string SaltKeyAPI = ConfigurationManager.AppSettings["SaltKeyAPI"];
        private readonly string VectorKeyAPI = ConfigurationManager.AppSettings["VectorKeyAPI"];
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext != null && !string.IsNullOrEmpty(filterContext.HttpContext.Request["token"]) && !string.IsNullOrEmpty(filterContext.HttpContext.Request["Domain"]))
            {
                var strEncryptToken = filterContext.HttpContext.Request["token"];
                var strDecryptToken = Sercurity.DecryptFromBase64(strEncryptToken, TokenKeyAPI, SaltKeyAPI, VectorKeyAPI);
                var objToken = JsonConvert.DeserializeObject<TokenResult>(strDecryptToken);

                if (objToken == null || string.IsNullOrEmpty(objToken.Token) && !CheckTokenAPI(objToken.Token) || string.IsNullOrEmpty(objToken.Domain) || !CheckDomainInServer(objToken.Domain) || objToken.TimeExpire == null || objToken.TimeExpire.Value < DateTime.Now)
                {
                    filterContext.Result = new RedirectResult("/");
                }
            }
            else
            {
                filterContext.Result = new RedirectResult("/");
            }
        }

        private bool CheckDomainInServer(string Domain)
        {
            var result = false;
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var objNail = sqlConnect.Query<Nail>(@"spNail_GetNailByDomain", new { strDomain = Domain }, commandType: CommandType.StoredProcedure).DefaultIfEmpty(new Nail()).FirstOrDefault();
                result = objNail != null && objNail.ID > 0;
            }    
            return result;
        }

        private bool CheckTokenAPI(string TokenAPI)
        {
            var result = false;
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var objNailApi = sqlConnect.Query<NailApi>(@"spNailApi_GetNailApiByToken", new { strToken = TokenAPI }, commandType: CommandType.StoredProcedure).DefaultIfEmpty(new NailApi()).FirstOrDefault();
                result = objNailApi != null && objNailApi.ID > 0;
            }   
            return result;
        }
    }
}