using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using GraphExp.Models;
using RouteAttribute = System.Web.Http.RouteAttribute;

namespace GraphExp.Controllers
{
    public class PaymentController : System.Web.Http.ApiController
    {
        static readonly IPaymentRepository repository = new PaymentRepository();

        [System.Web.Http.HttpPost]
        [Route("api/payment/postPiidEvent")]
        public HttpResponseMessage PostPiidEvent([FromBody] PostPiidEvent item)
        {
            List<Vertex> itemExists = repository.GetAlias(item);

            if (itemExists != null && itemExists.Any())
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            bool piidAdded = repository.AddPiidEvent(item);
            var response = Request.CreateResponse<PostPiidEvent>(HttpStatusCode.Created, item);

            // why add location of newly created resource in header response?(When the server creates a resource, it should include the URI of the new resource in the Location header of the response.)
            // string uri = Url.Link("DefaultApi", new { id = item.userId });
            // response.Headers.Location = new Uri(uri);
            return response;
        }

        [System.Web.Http.HttpPost]
        [Route("api/payment/postTransactionEvent")]
        public HttpResponseMessage PostTransactionEvent([FromBody] PostTransactionEvent item)
        {
            // PostTransactionEvent itemExists = repository.GetAlias(item);
            /*if (itemExists != null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }*/
            var result = repository.AddTransactionEvent(item);
            var response = Request.CreateResponse<PostTransactionEvent>(HttpStatusCode.Created, item);

            // why add location of newly created resource in header response?(When the server creates a resource, it should include the URI of the new resource in the Location header of the response.)
            // string uri = Url.Link("DefaultApi", new { id = item.userId });
            // response.Headers.Location = new Uri(uri);
            return response;
        }

        public IEnumerable<string> GetBySecureDataId(string secureDataId)
        {
            IEnumerable<string> item = repository.GetActivePIsBySecureDataId(secureDataId);
            if (item == null)
            {
                // throw new HttpResponseException(HttpStatusCode.NotFound);
                return new List<string>();
            }
            return item;
        }

        public IEnumerable<string> GetAllPiidAndTxn(string accountId)
        {
            IEnumerable<string> item = repository.GetAllPiidAndTxn(accountId);
            if (item == null)
            {
                // throw new HttpResponseException(HttpStatusCode.NotFound);
                return new List<string>();
            }
            return item;
        }


       /* public IEnumerable<PostPiidEvent> GetAllProfiles()
        {
            return repository.GetAll();
        }

        public PostPiidEvent GetPaymentInstrument(string accountId)
        {
            PostPiidEvent item = repository.Get(accountId);
            if (item == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            return item;
        }


        [System.Web.Http.HttpPost]
        [Route("api/payment/deletePiid")]
        public void DeletePiid([FromBody] string piid)
        {
            PostPiidEvent item = repository.Get(piid);
            if (item == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            repository.Remove(piid);
        }
       */
    }
}