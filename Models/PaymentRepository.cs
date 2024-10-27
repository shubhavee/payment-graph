using GraphExp.Models;
using Gremlin.Net;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace GraphExp.Models
{
    public class PaymentRepository : IPaymentRepository
    {
        // private List<PostPiidEvent> Payments = new List<PostPiidEvent>();
        private readonly BidirectionalGraph<Vertex, Edge<Vertex>> _paymentGraph;
        Dictionary<string, string> gremlinQueries;
        GremlinClient client;
        GremlinServer server;

        public PaymentRepository()
        {
            _paymentGraph = new BidirectionalGraph<Vertex, Edge<Vertex>>();

            // AddPiidEvent(new PostPiidEvent { invoiceId = "invoiceId1", piid = "piid1", vpa = "add bse65 image1", secureDataId = "secureDataId1" });
            // AddPiidEvent(new PostPiidEvent { invoiceId = "invoiceId2", piid = "piid2", vpa = "add bse65 image2", secureDataId = "secureDataId2" });
            // AddPiidEvent(new PostPiidEvent { invoiceId = "invoiceId3", piid = "piid3", vpa = "add bse65 image3", secureDataId = "secureDataId2" });
            var vertexAccountId = new Vertex { VertexType = "accountIdType", VertexValue = "accountId1" };
            var vertexSecureDataId = new Vertex { VertexType = "secureDataIdType", VertexValue = "secureDataId1" };
            var vertexPiid = new Vertex { VertexType = "piidType", VertexValue = "piid1" };
            _paymentGraph.AddVertex(vertexAccountId);
            _paymentGraph.AddVertex(vertexSecureDataId);
            _paymentGraph.AddVertex(vertexPiid);
            _paymentGraph.AddEdge(new Edge<Vertex>(vertexAccountId, vertexPiid));
            _paymentGraph.AddEdge(new Edge<Vertex>(vertexPiid, vertexAccountId));
            _paymentGraph.AddEdge(new Edge<Vertex>(vertexPiid, vertexSecureDataId));
            _paymentGraph.AddEdge(new Edge<Vertex>(vertexSecureDataId, vertexPiid));

            // GREMLIN part
            server = new GremlinServer(
                hostname: "localhost",
                port: 65400,
                username: "/dbs/db1/colls/coll1",
                password: "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
            );

                client = new GremlinClient(
                gremlinServer: server,
                new GraphSON2Reader(),
                new GraphSON2Writer(),
                GremlinClient.GraphSON2MimeType);

            gremlinQueries = new Dictionary<string, string>();
            // gremlinQueries.Add("Cleanup", "g.V().drop()");

            // create starter payment graph sample database1
            var secureDataId1 = "secureDataId1";
            var accountId1 = "accountId1";
            var piid1 = "piid1";
            var txn1 = new Dictionary<string, string> { { "txnId", "txnId1" }, { "amount", "20" } };
            gremlinQueries.Add($"AddVertex + {secureDataId1}", $"g.addV('secureDataId').property('value', '{secureDataId1}').property('timestamp','{DateTime.UtcNow.Date}' )");
            gremlinQueries.Add($"AddVertex + {accountId1}", $"g.addV('accountId').property('value', '{accountId1}').property('timestamp','{DateTime.UtcNow.Date}' )");
            gremlinQueries.Add($"AddVertex + {piid1}", $"g.addV('piid').property('value', '{piid1}').property('timestamp','{DateTime.UtcNow.Date}' )");
            gremlinQueries.Add($"AddVertex + {txn1}", $"g.addV('txn').property('value', '{txn1}').property('timestamp','{DateTime.UtcNow.Date}' )");
            gremlinQueries.Add($"AddEdge + between secureDataId1 and piid1", $"g.V('{secureDataId1}').addE('Associated PIID').to(g.V('{piid1}'))");
            gremlinQueries.Add($"AddEdge + between piid1 and accountId1", $"g.V('{piid1}').addE('Associated accountId').to(g.V('{accountId1}'))");

            foreach (var query in gremlinQueries)
            {
                Console.WriteLine(String.Format("Running this query: {0}: {1}", query.Key, query.Value));

                // Create async task to execute the Gremlin query.
                var resultSet = SubmitRequest(client, query.Value).Result;
                outputData(resultSet);
            }

            Console.WriteLine("Output query response\n");
        }

        /*public IEnumerable<PostPiidEvent> GetAll()
        {
            return Payments;
        }*/
        /*public PostPiidEvent Get(string accountId)
        {
            // return Payments.Find(p => string.Equals(p.accountId, accountId));
            return _paymentGraph.Vertices.Where(p => p.VertexType == "accountIdType");

        }*/
        public IEnumerable<string> GetAllPiidAndTxn(string accountId)
        {
            // return Payments.Find(p => string.Equals(p.accountId, accountId));
            var getAccountIdNode = _paymentGraph.Vertices.FirstOrDefault(p => p.VertexType == "accountIdType" && p.VertexValue == accountId);
            var allAccountIds = new List<string>();
            foreach (var edge in _paymentGraph.OutEdges(getAccountIdNode))
            {
                var piidOrTxn = edge.Target;
                allAccountIds.Add(edge.Target.VertexValue);
            }

            return allAccountIds;
        }

        public IEnumerable<string> GetActivePIsBySecureDataId(string secureDataId)
        {
            // return Payments.FindAll(p => string.Equals(p.secureDataId, secureDataId));
            var getSecureIdNode = _paymentGraph.Vertices.FirstOrDefault(p => p.VertexType == "secureDataIdType" && p.VertexValue == secureDataId);
            var allAccountIds = new List<string>();

            // Get all outgoing edges from the given vertex
            foreach (var edge in _paymentGraph.OutEdges(getSecureIdNode))
            {
                var piid = edge.Target;
                foreach (var edge2 in _paymentGraph.OutEdges(piid))
                {
                    if (edge2.Target.VertexType == "accountIdType" && !allAccountIds.Contains(edge2.Target.VertexValue))
                    {
                        allAccountIds.Add(edge2.Target.VertexValue);
                    }
                }
            }
            return allAccountIds;
        }

        public List<Vertex> GetAlias(PostPiidEvent alias)
        {
            // return Payments.Find(p => string.Equals(p.secureDataId, alias.secureDataId) && string.Equals(p.accountId, alias.accountId));
            var getAccountIdNode = _paymentGraph.Vertices.FirstOrDefault(p => p.VertexType == "accountIdType" && p.VertexValue == alias.accountId);
            var connectedNodes = new List<Vertex>();

            // Get all outgoing edges from the given vertex
            if (getAccountIdNode != null) 
            {
                foreach (var edge in _paymentGraph.OutEdges(getAccountIdNode))
                {
                    if (edge.Target.VertexType == "piidType" && edge.Target.VertexValue == alias.piid)
                    {
                        connectedNodes.Add(edge.Target);
                    }
                }
            }

            return connectedNodes;
        }

        public bool AddTransactionEvent(PostTransactionEvent item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            /*if (item.accountId == null)
            {
                item.accountId = Guid.NewGuid().ToString();
            }
            Payments.Add(item);
            return item;
            */
            var sourcePiidVertex = new Vertex();
            var sourceAccountIdVertex = _paymentGraph.Vertices.FirstOrDefault(p => p.VertexType == "accountIdType" && p.VertexValue == item.accountId);
            if (sourceAccountIdVertex == null)
            {
                throw new InvalidOperationException("Source vertex not found");
                /*sourceAccountIdVertex = new Vertex { VertexType = "accountIdType", VertexValue = item.accountId };
                _paymentGraph.AddVertex(sourceAccountIdVertex);
                sourcePiidVertex = new Vertex { VertexType = "piidType", VertexValue = item.piid };
                _paymentGraph.AddVertex(sourcePiidVertex);
                _paymentGraph.AddEdge(new Edge<Vertex>(sourceAccountIdVertex, sourcePiidVertex));*/
            }
            else
            {
                var sourceTransactionVertex = new Vertex { VertexType = "transactionType", VertexValue = JsonConvert.SerializeObject(item.transaction) };
                _paymentGraph.AddVertex(sourceTransactionVertex);
                _paymentGraph.AddEdge(new Edge<Vertex>(sourceAccountIdVertex, sourceTransactionVertex));
                _paymentGraph.AddEdge(new Edge<Vertex>(sourceTransactionVertex, sourceAccountIdVertex));

                sourcePiidVertex = _paymentGraph.Vertices.FirstOrDefault(p => p.VertexType == "piidType" && p.VertexValue == item.piid);
                // if piid is not found, it is like globalPI, it won't have secureDataId
                if (sourcePiidVertex == null)
                {
                    sourcePiidVertex = new Vertex { VertexType = "piidType", VertexValue = item.piid };
                    _paymentGraph.AddVertex(sourcePiidVertex);
                }

                _paymentGraph.AddEdge(new Edge<Vertex>(sourceTransactionVertex, sourcePiidVertex));
                _paymentGraph.AddEdge(new Edge<Vertex>(sourcePiidVertex, sourceTransactionVertex));
            }

            return true;
        }

        public bool AddPiidEvent(PostPiidEvent item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            /*if (item.accountId == null)
            {
                item.accountId = Guid.NewGuid().ToString();
            }
            Payments.Add(item);
            return item;
            */
            var sourcePiidVertex = new Vertex();
            var sourceAccountIdVertex = _paymentGraph.Vertices.FirstOrDefault(p => p.VertexType == "accountId" && p.VertexValue == item.accountId);
            if (sourceAccountIdVertex == null)
            {
                // throw new InvalidOperationException("Source vertex not found");
                sourceAccountIdVertex = new Vertex { VertexType = "accountIdType", VertexValue = item.accountId };
                _paymentGraph.AddVertex(sourceAccountIdVertex);
                sourcePiidVertex = new Vertex { VertexType = "piidType", VertexValue = item.piid };
                _paymentGraph.AddVertex(sourcePiidVertex);
                _paymentGraph.AddEdge(new Edge<Vertex>(sourceAccountIdVertex, sourcePiidVertex));
                _paymentGraph.AddEdge(new Edge<Vertex>(sourcePiidVertex, sourceAccountIdVertex));
            }
            else
            {
                sourcePiidVertex = new Vertex { VertexType = "piidType", VertexValue = item.piid };
                _paymentGraph.AddVertex(sourcePiidVertex);
                _paymentGraph.AddEdge(new Edge<Vertex>(sourceAccountIdVertex, sourcePiidVertex));
                _paymentGraph.AddEdge(new Edge<Vertex>(sourcePiidVertex, sourceAccountIdVertex));
            }

            var sourceSecureDataIdVertex = _paymentGraph.Vertices.FirstOrDefault(p => p.VertexType == "secureDataIdType" && p.VertexValue == item.secureDataId);
            
            if (sourceSecureDataIdVertex == null)
            {
                sourceSecureDataIdVertex = new Vertex { VertexType = "secureDataIdType", VertexValue = item.secureDataId };
            }

            // check logic once
            _paymentGraph.AddVertex(new Vertex { VertexType = "secureDataIdType", VertexValue = item.secureDataId });
            _paymentGraph.AddEdge(new Edge<Vertex>(sourcePiidVertex, sourceSecureDataIdVertex));
            _paymentGraph.AddEdge(new Edge<Vertex>(sourceSecureDataIdVertex, sourcePiidVertex));

            // GREMLIN
            var gremline_test_secureDataId1 = "gremline_test_secureDataId1";
            gremlinQueries.Add($"AddVertex + {gremline_test_secureDataId1}", $"g.addV('secureDataId').property('id', '{gremline_test_secureDataId1}').property('partitionId','{gremline_test_secureDataId1}' )");
            // Create DB
            foreach (var query in gremlinQueries)
            {
                Console.WriteLine(String.Format("Running this query: {0}: {1}", query.Key, query.Value));

                // Create async task to execute the Gremlin query.
                var resultSet = SubmitRequest(client, query.Value).Result;
                outputData(resultSet);
            }
            Console.WriteLine("Output query response\n");

            return true;
        }

        private static void outputData(dynamic resultSet)
        {
            if (resultSet.Count > 0)
            {
                Console.WriteLine("\tResult:");
                foreach (var result in resultSet)
                {
                    // The vertex results are formed as Dictionaries with a nested dictionary for their properties
                    string output = JsonConvert.SerializeObject(result);
                    Console.WriteLine($"\t{output}");
                }
                Console.WriteLine();
            }
        }
        private static Task<ResultSet<dynamic>> SubmitRequest(GremlinClient gremlinClient, string query)
        {
            try
            {
                return gremlinClient.SubmitAsync<dynamic>(query);
            }
            catch (ResponseException e)
            {
                Console.WriteLine("\tRequest Error!");

                // Print the Gremlin status code.
                Console.WriteLine($"\tStatusCode: {e.StatusCode}");

                // Return a completed task with an empty ResultSet
                return Task.FromResult(new ResultSet<dynamic>(new List<dynamic>(), new Dictionary<string, object>()));
            }
        }

        public Vertex FindVertex(string vertexType, string vertexValue)
        {
            return _paymentGraph.Vertices.FirstOrDefault(v => v.VertexType == vertexType && v.VertexValue == vertexValue);
        }

        /*public void AddOutgoingNode(Vertex sourceVertex, Vertex targetVertex)
        {
            if (sourceVertex == null || targetVertex == null)
            {
                throw new ArgumentNullException("Source or target vertex cannot be null");
            }

            _paymentGraph.AddVertex(targetVertex);
            _paymentGraph.AddEdge(new Edge<Vertex>(sourceVertex, targetVertex));
        }*/

        /*public PostTransactionEvent Add2(PostTransactionEvent item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (item.accountId == null)
            {
                item.accountId = Guid.NewGuid().ToString();
            }
            Payments.Add(item);
            return item;
        }

        public void Remove(string id)
        {
            Payments.RemoveAll(p => string.Equals(p.accountId, id));
        }

        public PostPiidEvent Update(PostPiidEvent item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var profile = Payments.Find(p => string.Equals(p.piid, item.piid));

            if (profile == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            UpdateProfileHelper(profile, item);

            return item;
        }

        public static void UpdateProfileHelper(PostPiidEvent oldProfile, PostPiidEvent newProfile)
        {
            oldProfile.tokenExpiry = newProfile.tokenExpiry;
        }
        */
    }
}