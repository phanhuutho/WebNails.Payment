using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebNails.Payment.Models
{
    public class NailApi
    {
        public int ID { get; set; }
        public string Url { get; set; }
        public Guid? Token { get; set; }
    }
}