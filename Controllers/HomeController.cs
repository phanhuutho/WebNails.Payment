using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QRCoder;
using System.IO;
using System.Drawing;
using Newtonsoft.Json;
using WebNails.Payment.Models;
using System.Data.SqlClient;
using System.Configuration;
using Dapper;
using System.Data;
using WebNails.Payment.Utilities;

namespace WebNails.Payment.Controllers
{
    public class HomeController : Controller
    {
        private readonly string VirtualData = ConfigurationManager.AppSettings["VirtualData"];
        // GET: Home
        public ActionResult Index()
        {
            return Content("");
        }

        public ActionResult Gifts()
        {
            return View();
        }

        [Token]
        public ActionResult GenerateQRCoce(string token, string Domain, string strCode, string strOwner = "", string strBuyer = "", string strReceiver = "", int intAmount = 0)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            var dataJson = new
            {
                Code = strCode,
                Owner = strOwner,
                Buyer = strBuyer,
                Receiver = strReceiver,
                Amount = intAmount
            };
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(JsonConvert.SerializeObject(dataJson), QRCodeGenerator.ECCLevel.Q);
            if (!Directory.Exists(VirtualData + "/Upload/QRCode/"))
            {
                Directory.CreateDirectory(VirtualData + "/Upload/QRCode/");
            }
            qrCodeData.SaveRawData(VirtualData + "/Upload/QRCode/file-" + strCode + ".qrr", QRCodeData.Compression.Uncompressed);
            QRCode qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20, Color.Black, Color.White, icon: ConvertToBitmap(VirtualData + "/Content/images/logo.png"));
            return View(BitmapToBytes(qrCodeImage));
        }

        [Token]
        public ActionResult GetQRCoce(string token, string Domain, string strCode)
        {
            QRCodeData qrCodeData = new QRCodeData(VirtualData + "/Upload/QRCode/file-" + strCode + ".qrr", QRCodeData.Compression.Uncompressed);
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

        [Token]
        public ActionResult GetImageQRCode(string token, string Domain, string strCode)
        {
            return File(VirtualData + "/Upload/QRCode/file-" + strCode + ".png", "image/png");
        }

        private Bitmap ConvertToBitmap(string fileName)
        {
            Bitmap bitmap;
            using (Stream bmpStream = System.IO.File.Open(fileName, FileMode.Open))
            {
                Image image = Image.FromStream(bmpStream);

                bitmap = new Bitmap(image);

            }
            return bitmap;
        }

        [Token]
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

        [Token]
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

                return Json(new { Count = Count, Data = objResult }, JsonRequestBehavior.AllowGet);
            }
        }

        [Token]
        [HttpPost]
        public ActionResult UpdateCompleted(string token, string Domain, Guid id)
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

        [Token]
        [HttpPost]
        public ActionResult SendMail(string token, string Domain, Guid id)
        {
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var info = sqlConnect.Query<InfoPaypal>("spInfoPaypal_GetInfoPaypalByID", new { strID = id }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                if (info != null)
                {
                    var objResult = sqlConnect.Execute("spInfoPaypal_UpdateStatus", new { strID = id, intStatus = (int)PaymentStatus.Success }, commandType: CommandType.StoredProcedure);

                    return Json(new { Count = objResult, Data = info });
                }
                else
                {
                    return Json(new { Count = 0, Data = info });
                }
            }
        }

        [Token]
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

        [Token]
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