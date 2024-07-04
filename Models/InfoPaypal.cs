using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebNails.Payment.Models
{
    public class InfoPaypal
    {
        public Guid ID { get; set; }
        public int Nail_ID { get; set; }
        public string Transactions { get; set; }
        public string Code { get; set; }
        public string Owner { get; set; } //Email Owner
        public float Amount { get; set; }
        public string Stock { get; set; } //Email Receiver
        public string Email { get; set; } //Email Buyer
        public string NameReceiver { get; set; } //Name Receiver
        public string NameBuyer { get; set; } //Name Buyer
        public string Message { get; set; }
        public PaymentStatus Status { get; set; } //0: pending; 1:success
        public bool IsUsed { get; set; }
        public DateTime DateTimeCreate { get; set; }
        public DateTime DateTimeUpdateUsed { get; set; }
        public string CodeSaleOff { get; set; }
        public int AmountReal { get; set; }
        public int ValidCode { get; set; }
        public string DescriptionValidCode { get; set; }
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Success = 1
    }
}