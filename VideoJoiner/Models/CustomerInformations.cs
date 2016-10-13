using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VideoJoiner.Models
{
    public class CustomerInformations
    {
        public string NewlyRegistered { get; set; }
        public string SubscribedCustomers { get; set; }
        public string TopRatedCustomers { get; set; }
        public string PendingToApprove { get; set; }
    }
}