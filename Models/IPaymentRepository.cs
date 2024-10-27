using GraphExp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphExp.Models
{
    internal interface IPaymentRepository
    {
        // IEnumerable<PostPiidEvent> GetAll();
        // PostPiidEvent Get(string id);
        bool AddPiidEvent(PostPiidEvent item);
        bool AddTransactionEvent(PostTransactionEvent item);
        // PostPiidEvent Update(PostPiidEvent item);
        List<Vertex> GetAlias(PostPiidEvent alias);
        // PostTransactionEvent GetAlias2(PostTransactionEvent alias);
        // void Remove(string id);
        IEnumerable<string> GetActivePIsBySecureDataId(string secureDataId);
        IEnumerable<string> GetAllPiidAndTxn(string accountId);
    }
}
