using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebNails.Payment.Models
{
    public class Payment
    {
        public long ID { get; set; }
        public TypePayment TypePayment { get; set; }
        public string Code { get; set; }
        public string Transaction { get; set; }
        public string EmailOwner { get; set; }
        public string EmailBuyer { get; set; }
        public string EmailReceiver { get; set; }
        public int Amount { get; set; }
        public bool IsRefund { get; set; }
        public DateTime CreateDate { get; set; }
    }

    public enum TypePayment
    {
        Paypal = 1
    }
}