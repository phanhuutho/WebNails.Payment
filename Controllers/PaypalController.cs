using Dapper;
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

namespace WebNails.Payment.Controllers
{
    public class PaypalController : Controller
    {
        // GET: Paypal
        public ActionResult Index()
        {
            return Content("");
        }

        public ActionResult Payment(string img = "")
        {
            ViewBag.Img = img;
            return View();
        }

        [HttpPost]
        public ActionResult Process(string amount, string stock, string email, string message, string name_receiver, string name_buyer, string img = "", string codesale = "")
        {
            var strID = Guid.NewGuid();
            var EmailPaypal = ConfigurationManager.AppSettings["EmailPaypal"];

            using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
            {
                var Domain = Request.Url.Host;
                var Transactions = GenerateUniqueCode();

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

                ViewBag.EmailPaypal = EmailPaypal ?? "";
                ViewBag.Amount = string.Format("{0}", amount) ?? string.Format("{0}", "1");
                ViewBag.Stock = stock ?? "";
                ViewBag.Email = email ?? "";
                ViewBag.NameReceiver = name_receiver ?? "";
                ViewBag.NameBuyer = name_buyer ?? "";
                ViewBag.Img = img;
                ViewBag.Cost = Cost;
                ViewBag.CodeSaleOff = codesale;

                var cookieDataBefore = new HttpCookie("DataBefore");
                cookieDataBefore["Amount"] = string.Format("{0}", amount);
                cookieDataBefore["Email"] = email;
                cookieDataBefore["Stock"] = stock;
                cookieDataBefore["Message"] = message;
                cookieDataBefore["NameReceiver"] = name_receiver;
                cookieDataBefore["NameBuyer"] = name_buyer;
                cookieDataBefore["Img"] = img;
                cookieDataBefore["Guid"] = strID.ToString();
                cookieDataBefore["Cost"] = string.Format("{0:N2}", Cost);
                cookieDataBefore["CodeSaleOff"] = codesale;
                cookieDataBefore.Expires.Add(new TimeSpan(0, 60, 0));
                Response.Cookies.Add(cookieDataBefore);
            }

            return View();
        }

        public ActionResult PaymentResponse()
        {
            var data = new RouteValueDictionary();
            foreach (var key in Request.Form.AllKeys)
            {
                data.Add(key, Request[key]);
            }
            foreach (var key in Request.QueryString.AllKeys)
            {
                data.Add(key, Request[key]);
            }
            foreach (var key in Request.Headers.AllKeys)
            {
                data.Add(key, Request[key]);
            }

            TempData["PayerID"] = Request["PayerID"];
            return RedirectToAction("Finish", data);
        }

        public ActionResult Finish()
        {
            string responseCode;
            string SecureHash;
            var strAmount = string.Empty;
            var strCost = string.Empty;
            var strCodeSaleOff = string.Empty;
            var strEmail = string.Empty;
            var strStock = string.Empty;
            var strNameReceiver = string.Empty;
            var strNameBuyer = string.Empty;
            var strMessage = string.Empty;
            var strImg = string.Empty;
            var strID = new Guid();

            HttpCookie cookieDataBefore = Request.Cookies["DataBefore"];
            if (cookieDataBefore != null)
            {
                strAmount = cookieDataBefore["Amount"];
                strCost = cookieDataBefore["Cost"];
                strCodeSaleOff = cookieDataBefore["CodeSaleOff"];
                strEmail = cookieDataBefore["Email"];
                strStock = cookieDataBefore["Stock"];
                strNameReceiver = cookieDataBefore["NameReceiver"];
                strNameBuyer = cookieDataBefore["NameBuyer"];
                strMessage = cookieDataBefore["Message"];
                strImg = cookieDataBefore["Img"];
                strID = Guid.Parse(cookieDataBefore["Guid"]);
            }

            if (Request.QueryString["PayerID"] != null && Request.QueryString["PayerID"] == string.Format("{0}", TempData["PayerID"]))
            {
                SecureHash = "<font color='blue'><strong>CORRECT</strong></font>";

                responseCode = "0";

                //var strCode = GenerateUniqueCode();
                using (var sqlConnect = new SqlConnection(ConfigurationManager.ConnectionStrings["ContextDatabase"].ConnectionString))
                {
                    var info = sqlConnect.Query<InfoPaypal>("spInfoPaypal_GetInfoPaypalByID", new { strID = strID }, commandType: CommandType.StoredProcedure).FirstOrDefault();

                    if (info != null)
                    {
                        var objResult = sqlConnect.Execute("spInfoPaypal_UpdateStatus", new { strID = strID, intStatus = (int)PaymentStatus.Success }, commandType: CommandType.StoredProcedure);

                        if (objResult > 0)
                        {
                            SendMailToOwner(string.Format("{0:N2}", strAmount), strStock, strEmail, strMessage, info.Code, strNameReceiver, strNameBuyer, strImg, string.Format("{0:N2}", strCost), strCodeSaleOff);
                            SendMailToBuyer(string.Format("{0:N2}", strAmount), strStock, strEmail, strMessage, info.Code, strNameReceiver, strNameBuyer, strImg, string.Format("{0:N2}", strCost), strCodeSaleOff);
                            SendMailToReceiver(strStock, strEmail, string.Format("{0:N2}", strCost), info.Code, strNameReceiver, strNameBuyer, strImg);
                        }
                    }
                }
            }
            else
            {
                SecureHash = "<font color='red'><strong>FAIL</strong></font>";
                responseCode = "-1";
            }

            ViewBag.SecureHash = SecureHash;
            ViewBag.ResponseCode = responseCode;
            return View();
        }

        private void SendMailToOwner(string strAmount, string strStock, string strEmail, string strMessage, string strCode, string strNameReceiver, string strNameBuyer, string img = "", string strCost = "", string strCodeSaleOff = "")
        {
            if (!string.IsNullOrEmpty(strAmount) && !string.IsNullOrEmpty(strStock) && !string.IsNullOrEmpty(strEmail) && !string.IsNullOrEmpty(strMessage))
            {
                var EmailPaypal = ConfigurationManager.AppSettings["EmailPaypal"];
                using (MailMessage mail = new MailMessage(new MailAddress(ConfigurationManager.AppSettings["EmailSystem"], ConfigurationManager.AppSettings["EmailName"], System.Text.Encoding.UTF8), new MailAddress(EmailPaypal)))
                {
                    mail.HeadersEncoding = System.Text.Encoding.UTF8;
                    mail.SubjectEncoding = System.Text.Encoding.UTF8;
                    mail.BodyEncoding = System.Text.Encoding.UTF8;
                    mail.IsBodyHtml = bool.Parse(ConfigurationManager.AppSettings["IsBodyHtmlEmailSystem"]);
                    mail.Subject = "Checkout Paypal Gift Purchase - " + strEmail;
                    mail.Body = $@"<p>Amount pay: <strong>${strAmount} USD</strong></p>
					    {(!string.IsNullOrEmpty(strCost) ? $"<p>Cost: {strCost}</p>" : "")}
					    {(!string.IsNullOrEmpty(strCodeSaleOff) ? $"<p>Code Sale Off: {strCodeSaleOff}</p>" : "")}
					    <p>Receiver name: {strNameReceiver}</p>
					    <p>Receiver email: {strStock}</p>
					    <p>Buyer name: {strNameBuyer}</p>
					    <p>Buyer email: {strEmail}</p>
					    <p>Comment: {strMessage}</p>
                        <p>Code: <strong>{strCode}</strong></p> 
                        <p><img width='320' src='{Url.RequestContext.HttpContext.Request.Url.Scheme + "://" + Url.RequestContext.HttpContext.Request.Url.Authority + img}' width='360px' /></p>";

                    SmtpClient mySmtpClient = new SmtpClient(ConfigurationManager.AppSettings["HostEmailSystem"], int.Parse(ConfigurationManager.AppSettings["PortEmailSystem"]));
                    NetworkCredential networkCredential = new NetworkCredential(ConfigurationManager.AppSettings["EmailSystem"], ConfigurationManager.AppSettings["PasswordEmailSystem"]);
                    mySmtpClient.UseDefaultCredentials = false;
                    mySmtpClient.Credentials = networkCredential;
                    mySmtpClient.EnableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSslEmailSystem"]);
                    mySmtpClient.Send(mail);
                }
            }
        }

        private void SendMailToReceiver(string strEmailReceiver, string strEmailBuyer, string strAmount, string strCode, string strNameReceiver, string strNameBuyer, string img = "")
        {
            if (!string.IsNullOrEmpty(strEmailReceiver) && !string.IsNullOrEmpty(strEmailBuyer))
            {
                using (MailMessage mail = new MailMessage(new MailAddress(ConfigurationManager.AppSettings["EmailSystem"], ConfigurationManager.AppSettings["EmailName"], System.Text.Encoding.UTF8), new MailAddress(strEmailReceiver, strNameReceiver, System.Text.Encoding.UTF8)))
                {
                    mail.HeadersEncoding = System.Text.Encoding.UTF8;
                    mail.SubjectEncoding = System.Text.Encoding.UTF8;
                    mail.BodyEncoding = System.Text.Encoding.UTF8;
                    mail.IsBodyHtml = bool.Parse(ConfigurationManager.AppSettings["IsBodyHtmlEmailSystem"]);
                    mail.Subject = "Gift For You";
                    mail.Body = $@"<p>Hello,</p><br/>
					    <p>You have a gift from <strong>{strNameBuyer} - ({strEmailBuyer})</strong>.</p>
                        <p>Please visit us at <strong>{ViewBag.Name}</strong> - Address: <strong>{ViewBag.Address}</strong> - Phone: <strong>{ViewBag.TextTell}</strong> to redeem your gift.</p>
                        <p>Amount: <strong>${strAmount} USD</strong>.</p>
                        <p>Code: <strong>{strCode}</strong></p><br/>
					    <p>Thank you!</p> 
                        <p><img width='320' src='{Url.RequestContext.HttpContext.Request.Url.Scheme + "://" + Url.RequestContext.HttpContext.Request.Url.Authority + img}' /></p>";

                    SmtpClient mySmtpClient = new SmtpClient(ConfigurationManager.AppSettings["HostEmailSystem"], int.Parse(ConfigurationManager.AppSettings["PortEmailSystem"]));
                    NetworkCredential networkCredential = new NetworkCredential(ConfigurationManager.AppSettings["EmailSystem"], ConfigurationManager.AppSettings["PasswordEmailSystem"]);
                    mySmtpClient.UseDefaultCredentials = false;
                    mySmtpClient.Credentials = networkCredential;
                    mySmtpClient.EnableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSslEmailSystem"]);
                    mySmtpClient.Send(mail);
                }
            }
        }

        private void SendMailToBuyer(string strAmount, string strStock, string strEmail, string strMessage, string strCode, string strNameReceiver, string strNameBuyer, string img = "", string strCost = "", string strCodeSaleOff = "")
        {
            if (!string.IsNullOrEmpty(strAmount) && !string.IsNullOrEmpty(strStock) && !string.IsNullOrEmpty(strEmail) && !string.IsNullOrEmpty(strMessage))
            {
                using (MailMessage mail = new MailMessage(new MailAddress(ConfigurationManager.AppSettings["EmailSystem"], ConfigurationManager.AppSettings["EmailName"], System.Text.Encoding.UTF8), new MailAddress(strEmail)))
                {
                    mail.HeadersEncoding = System.Text.Encoding.UTF8;
                    mail.SubjectEncoding = System.Text.Encoding.UTF8;
                    mail.BodyEncoding = System.Text.Encoding.UTF8;
                    mail.IsBodyHtml = bool.Parse(ConfigurationManager.AppSettings["IsBodyHtmlEmailSystem"]);
                    mail.Subject = "Checkout Paypal Gift Purchase - " + strEmail;
                    mail.Body = $@"<p>Amount pay: {strAmount}</p>
					    {(!string.IsNullOrEmpty(strCost) ? $"<p>Cost: {strCost}</p>" : "")}
					    {(!string.IsNullOrEmpty(strCodeSaleOff) ? $"<p>Code Sale Off: {strCodeSaleOff}</p>" : "")}
					    <p>Receiver name: {strNameReceiver}</p>
					    <p>Receiver email: {strStock}</p>
					    <p>Buyer name: {strNameBuyer}</p>
					    <p>Buyer email: {strEmail}</p>
					    <p>Comment: {strMessage}</p>
                        <p>Code: <strong>{strCode}</strong></p> 
                        <p><img width='320' src='{Url.RequestContext.HttpContext.Request.Url.Scheme + "://" + Url.RequestContext.HttpContext.Request.Url.Authority + img}' /></p>";

                    SmtpClient mySmtpClient = new SmtpClient(ConfigurationManager.AppSettings["HostEmailSystem"], int.Parse(ConfigurationManager.AppSettings["PortEmailSystem"]));
                    NetworkCredential networkCredential = new NetworkCredential(ConfigurationManager.AppSettings["EmailSystem"], ConfigurationManager.AppSettings["PasswordEmailSystem"]);
                    mySmtpClient.UseDefaultCredentials = false;
                    mySmtpClient.Credentials = networkCredential;
                    mySmtpClient.EnableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSslEmailSystem"]);
                    mySmtpClient.Send(mail);
                }
            }
        }

        private static Random random = new Random();
        private string GenerateUniqueCode()
        {
            var strYear = string.Format("{0:yyyy}", DateTime.Now);
            var strDay = string.Format("{0:ddd dd MMM}", DateTime.Now);
            strDay = String.Join("", strDay.Split(new char[] { ' ' }));
            string strReverse = string.Empty;
            for (int i = strDay.Length - 1; i >= 0; i--)
            {
                strReverse += strDay[i];
            }
            var strTimes = string.Format("{0:HHmmss}", DateTime.Now);

            var result = string.Format("{0}{1}{2}", strYear, strReverse, strTimes).ToUpper();
            return result;
        }

        [HttpPost]
        public ActionResult GetDataCode(string SiteName, DateTime StartDate, DateTime EndDate)
        {
            return Json(new List<DataResponseModel>());
        }

        [HttpPost]
        public ActionResult UpdateCodeRefund(string SiteName, string Code)
        {
            return Json("OK");
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

        private void SaveBitmap(Bitmap img, string strCode)
        {
            var filepath = Server.MapPath("/Upload/QRCode/file-" + strCode + ".png");
            if (!Directory.Exists(Server.MapPath("/Upload/QRCode/")))
            {
                Directory.CreateDirectory(Server.MapPath("/Upload/QRCode/"));
            }
            img.Save(filepath);
        }
    }
}