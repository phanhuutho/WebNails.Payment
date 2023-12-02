using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebNails.Payment.Models
{
    public class ClientDomainModel
    {
        public long ID { get; set; }
        public string ClientDomain { get; set; }
        public string Email { get; set; }
        public int MiniumAmount { get; set; } 
    }
}