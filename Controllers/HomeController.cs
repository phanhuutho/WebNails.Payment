using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QRCoder;
using System.IO;
using System.Drawing;
using Newtonsoft.Json;

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


        public ActionResult GenerateQRCoce(string strCode, string strOwner = "", string strBuyer = "", string strReceiver = "", int intAmount = 0)
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
            if (!Directory.Exists(Server.MapPath("/Upload/QRCode/")))
            {
                Directory.CreateDirectory(Server.MapPath("/Upload/QRCode/"));
            }
            qrCodeData.SaveRawData(Server.MapPath("/Upload/QRCode/file-" + strCode + ".qrr"), QRCodeData.Compression.Uncompressed);
            QRCode qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20, Color.Black, Color.White, icon: ConvertToBitmap(Server.MapPath("/Content/images/logo.png")));
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
    }
}