﻿using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using QRCoder;
using System.IO;
using System.Drawing;
using WebNails.Payment.Models;
using WebNails.Payment.Utilities;

namespace WebNails.Payment.Controllers
{
    public class PaypalController : Controller
    {
        private readonly string VirtualData = ConfigurationManager.AppSettings["VirtualData"];
        // GET: Paypal
        public ActionResult Index()
        {
            return Content("");
        }

        [Token]
        [HttpPost]
        public ActionResult Process(string token, string Domain, string EmailPaypal, Guid strID, string Transactions, string amount, string stock, string email, string message, string name_receiver, string name_buyer, string codesale = "")
        {
            if (string.IsNullOrEmpty(token))
            {
                return Content("Invalid Token");
            }
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var IsValidCodeSale = false;
                var ValidCode = 0;
                var DescriptionCode = "";
                var Cost = float.Parse(amount);
                if (!string.IsNullOrEmpty(codesale))
                {
                    var objNailCodeSale = sqlConnect.Query<NailCodeSale>("spNailCodeSale_GetNailCodeSaleByCode", new { strCode = codesale, strDomain = Domain, strDateNow = DateTime.Now }, commandType: CommandType.StoredProcedure).FirstOrDefault();
                    if (objNailCodeSale != null)
                    {
                        IsValidCodeSale = float.Parse(amount) >= float.Parse(objNailCodeSale.MinAmountSaleOff.ToString());
                        if (!IsValidCodeSale)
                        {
                            ValidCode = 1;
                            DescriptionCode = $"Amount payment less than {string.Format("{0:N0}", objNailCodeSale.MinAmountSaleOff)}. Code sale off not available.";
                        }
                        else
                        {
                            DescriptionCode = "Code sale off correct";
                            var amount_update = Cost * (100 - objNailCodeSale.Sale) / 100;
                            amount = string.Format("{0:N2}", amount_update);
                        }
                    }
                    else
                    {
                        ValidCode = 2;
                        DescriptionCode = "Code sale off incorrect";
                    }
                }

                var objResult = sqlConnect.Execute("spInfoPaypal_InsertBefore", new
                {
                    strID = strID,
                    strDomain = Domain,
                    strTransactions = Transactions,
                    strCode = Transactions,
                    strOwner = EmailPaypal,
                    strStock = stock,
                    strEmail = email,
                    strNameReceiver = name_receiver,
                    strNameBuyer = name_buyer,
                    intAmount = float.Parse(amount),
                    strMessage = message,
                    strCodeSaleOff = codesale,
                    intAmountReal = Cost,
                    intValidCode = ValidCode,
                    strDescriptionValidCode = DescriptionCode
                }, commandType: CommandType.StoredProcedure);

                return Json(amount);
            }
        }

        [Token]
        public ActionResult Finish(string token, string Domain, string strID)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Content("Invalid Token");
            }
            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var info = sqlConnect.Query<InfoPaypal>("spInfoPaypal_GetInfoPaypalByID", new { strID = strID }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                if (info != null)
                {
                    var objResult = sqlConnect.Execute("spInfoPaypal_UpdateStatus", new { strID = strID, intStatus = (int)PaymentStatus.Success }, commandType: CommandType.StoredProcedure);

                    return Json(new { Count = objResult, Data = info });
                }
                return Json(new { Count = 0, Data = info });
            }
        }

        [Token]
        [HttpPost]
        public ActionResult GetDataCode(string token, string Domain, DateTime StartDate, DateTime EndDate)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Content("Invalid Token");
            }
            return Json(new List<DataResponseModel>());
        }

        [Token]
        [HttpPost]
        public ActionResult UpdateCodeRefund(string token, string Domain, string Code)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Content("Invalid Token");
            }
            return Json("OK");
        }

        [Token]
        public ActionResult GenerateQRCoce(string token, string Domain, string strCode)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Content("Invalid Token");
            }
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(strCode, QRCodeGenerator.ECCLevel.Q);
            if (!Directory.Exists(VirtualData + "/Upload/QRCode/"))
            {
                Directory.CreateDirectory(VirtualData + "/Upload/QRCode/");
            }
            qrCodeData.SaveRawData(VirtualData + "/Upload/QRCode/file-" + strCode + ".qrr", QRCodeData.Compression.Uncompressed);
            QRCode qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(5);
            return View(BitmapToBytes(qrCodeImage));
        }

        [Token]
        public ActionResult GetQRCoce(string token, string Domain, string strCode)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Content("Invalid Token");
            }
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

        private void SaveBitmap(Bitmap img, string strCode)
        {
            var filepath = VirtualData + "/Upload/QRCode/file-" + strCode + ".png";
            if (!Directory.Exists(VirtualData + "/Upload/QRCode/"))
            {
                Directory.CreateDirectory(VirtualData + "/Upload/QRCode/");
            }
            img.Save(filepath);
        }
    }
}