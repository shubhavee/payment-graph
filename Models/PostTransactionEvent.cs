using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GraphExp.Models
{
    public class PostTransactionEvent
    {
        public string accountId { get; set; }
        public Dictionary<string, string> transaction { get; set; }
        public string piid { get; set; }

    }
}