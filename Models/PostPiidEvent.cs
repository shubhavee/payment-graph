using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GraphExp.Models
{
    public class PostPiidEvent
    {
        public string accountId { get; set; }

        public string invoiceId { get; set; }

        public string piid { get; set; }

        public string vpa { get; set; }

        public string secureDataId { get; set; }

        public string tokenExpiry { get; set; }
    }
}