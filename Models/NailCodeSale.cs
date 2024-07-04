using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebNails.Payment.Models
{
    public class NailCodeSale
    {
        public string Code { get; set; }
        public int Sale { get; set; }
        public bool Status { get; set; }
        public bool IsDelete { get; set; }
        public DateTime ExpireDateFrom { get; set; }
        public DateTime ExpireDateTo { get; set; }
        public int MinAmountSaleOff { get; set; }
    }
}