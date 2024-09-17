using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QRCoder;
using System.IO;
using System.Drawing;
using WebNails.Payment.Models;
using System.Data.SqlClient;
using System.Configuration;
using Dapper;
using System.Data;

namespace WebNails.Payment.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return Content("");
        }

        public ActionResult Gifts()
        {
            return View();
        }


        public ActionResult GenerateQRCoce(string strCode)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(strCode, QRCodeGenerator.ECCLevel.Q);
            if (!Directory.Exists(Server.MapPath("/Upload/QRCode/")))
            {
                Directory.CreateDirectory(Server.MapPath("/Upload/QRCode/"));
            }
            qrCodeData.SaveRawData(Server.MapPath("/Upload/QRCode/file-" + strCode + ".qrr"), QRCodeData.Compression.Uncompressed);
            QRCode qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(5);
            return View(BitmapToBytes(qrCodeImage));
        }

        public ActionResult GetQRCoce(string strCode)
        {
            QRCodeData qrCodeData = new QRCodeData(Server.MapPath("/Upload/QRCode/file-" + strCode + ".qrr"), QRCodeData.Compression.Uncompressed);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            return View(BitmapToBytes(qrCodeImage));
        }

        private Byte[] BitmapToBytes(Bitmap img)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public ActionResult GetImageQRCode(string strCode)
        {
            return File(Server.MapPath("/Upload/QRCode/file-" + strCode + ".png"), "image/png");
        }

        [HttpPost]
        public ActionResult Login(LoginModel model, string token, string Domain)
        {
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var checklogin = sqlConnect.Query("spUserSite_GetByUsernameAndPassword", new { strUsername = model.Username, strPassword = model.Password, strDomain = Domain }, commandType: CommandType.StoredProcedure).Count() == 1;
                if (checklogin)
                {
                    return Json(new { IsLogin = true });
                }
                else
                {
                    return Json(new { IsLogin = false });
                }
            }
        }

        public ActionResult GetGiftManage(string token, string Domain, int intSkip, int intCountSort, string search = "")
        {
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var param = new DynamicParameters();
                param.Add("@intSkip", intSkip);
                param.Add("@intTake", intCountSort);
                param.Add("@strDomain", Domain);
                param.Add("@strValue", search);
                param.Add("@intTotalRecord", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var objResult = sqlConnect.Query<InfoPaypal>("spInfoPaypal_GetInfoPaypalByNailDomain", param, commandType: CommandType.StoredProcedure);

                var Count = param.Get<int>("@intTotalRecord");

                return Json(new { Count = Count, data = objResult }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult UpdateCompleted(string token, Guid id)
        {
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var objResult = sqlConnect.Execute("spInfoPaypal_UpdateIsUsed", new { strID = id, bitIsUsed = true }, commandType: CommandType.StoredProcedure);

                if (objResult > 0)
                {
                    return Json(objResult);
                }
                else
                {
                    return Json(0);
                }
            }
        }

        [HttpPost]
        public ActionResult SendMail(string token, Guid id)
        {
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var info = sqlConnect.Query<InfoPaypal>("spInfoPaypal_GetInfoPaypalByID", new { strID = id }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                if (info != null)
                {
                    var objResult = sqlConnect.Execute("spInfoPaypal_UpdateStatus", new { strID = id, intStatus = (int)PaymentStatus.Success }, commandType: CommandType.StoredProcedure);

                    return Json(new { Count = objResult, data = info });
                }
                else
                {
                    return Json(new { Count = 0, data = info });
                }
            }
        }

        [HttpPost]
        public ActionResult CheckCodeSaleOff(string token, string Domain, string Code, int Amount)
        {
            var result = false;
            var message = "";
            if (!string.IsNullOrEmpty(Code))
            {
                using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
                {
                    var objNailCodeSale = sqlConnect.Query<NailCodeSale>("spNailCodeSale_GetNailCodeSaleByCode", new { strCode = Code, strDomain = Domain, strDateNow = DateTime.Now }, commandType: CommandType.StoredProcedure).FirstOrDefault();
                    result = objNailCodeSale != null;
                    if (result)
                    {
                        if (Amount < objNailCodeSale.MinAmountSaleOff)
                        {
                            result = false;
                            message = $"Amount payment less than {string.Format("{0:N0}", objNailCodeSale.MinAmountSaleOff)}. Code sale off not available.";
                        }
                    }
                    else
                    {
                        message = "Code sale off incorrect";
                    }
                }
            }
            return Json(new { Status = result, Message = message });
        }

        [HttpPost]
        public ActionResult GetListNailCodeSaleByDomain(string token, string Domain)
        {
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var data = sqlConnect.Query<NailCodeSale>("spNailCodeSale_GetNailCodeSalesByDomain", new { @strDomain = Domain, strDateNow = DateTime.Now }, commandType: CommandType.StoredProcedure).ToList();
                return Json(data);
            }
        }
    }
}