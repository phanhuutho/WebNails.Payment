using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebNails.Payment.Models
{
    public class DataResponseModel
    {
        public string Code { get; set; }
        public string EmailOwner { get; set; }
        public string EmailBuyer { get; set; }
        public string EmailReceiver { get; set; }
        public int Amount { get; set; }
        public bool IsRefund { get; set; }
    }
}