using GraphExp.Models;
using Gremlin.Net;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Structure;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            // add passwork below
        server = new GremlinServer(
             hostname: "graph-exp.gremlin.cosmosdb.azure.com",
             port: 443,
             username: "/dbs/db1/colls/graph1",
             enableSsl: true
            );

        client = new GremlinClient(server, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);

        /*server = new GremlinServer(
            hostname: "localhost",
            port: 65400,
            username: "/dbs/db1/colls/coll1",
        );

            client = new GremlinClient(
            gremlinServer: server,
            new GraphSON2Reader(),
            new GraphSON2Writer(),
            GremlinClient.GraphSON2MimeType);*/
        
        gremlinQueries = new Dictionary<string, string>();
        // gremlinQueries.Add("Cleanup", "g.V().drop()");

            /*
        var secureDataId1 = "secureDataId1";
        var accountId1 = "accountId1";
        var piid1 = "piid1";
        var txn1 = new Dictionary<string, string> { { "txnId", "txnId1" }, { "amount", "20" } };
        gremlinQueries.Add($"AddVertex + {secureDataId1}", $"g.addV('secureDataId').property('id', '{secureDataId1}').property('value', '{secureDataId1}').property('timestamp','{DateTime.UtcNow.Date}' )");
        gremlinQueries.Add($"AddVertex + {accountId1}", $"g.addV('accountId').property('id', '{accountId1}').property('value', '{accountId1}').property('timestamp','{DateTime.UtcNow.Date}' )");
        gremlinQueries.Add($"AddVertex + {piid1}", $"g.addV('piid').property('id', '{piid1}').property('value', '{piid1}').property('timestamp','{DateTime.UtcNow.Date}' )");
        gremlinQueries.Add($"AddVertex + {txn1}", $"g.addV('txn').property('id', '{txn1}').property('txnId', '{txn1["txnId"]}').property('amount', '{txn1["amount"]}').property('timestamp','{DateTime.UtcNow.Date}' )");
        gremlinQueries.Add($"AddEdge + between secureDataId1 and piid1", $"g.V('{secureDataId1}').addE('Associated PIID').to(g.V('{piid1}'))");
        gremlinQueries.Add($"AddEdge + between piid1 and accountId1", $"g.V('{piid1}').addE('Associated accountId').to(g.V('{accountId1}'))");
        gremlinQueries.Add($"AddEdge + between accountid1 and txn1", $"g.V('{accountId1}').addE('Associated txn').to(g.V('{txn1}'))");
        gremlinQueries.Add($"AddEdge + between accountid1 and piid1", $"g.V('{accountId1}').addE('Associated piid').to(g.V('{piid1}'))");

        foreach (var query in gremlinQueries)
        {
            Console.WriteLine(String.Format("Running this query: {0}: {1}", query.Key, query.Value));

            // Create async task to execute the Gremlin query.
            var resultSet = SubmitRequest(client, query.Value).Result;
            outputData(resultSet);
        }

        Console.WriteLine("Output query response\n");*/
    }

    public IEnumerable<string> GetAllPiidAndTxn(string accountId)
        {
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
            /*var getSecureIdNode = _paymentGraph.Vertices.FirstOrDefault(p => p.VertexType == "secureDataIdType" && p.VertexValue == secureDataId);
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
            }*/

            // GREMLIN
            var queryScript = $"g.V().hasLabel('secureDataId').has('id', '{secureDataId}').outE('Associated PIID').inV().hasLabel('piid').outE('Associated accountId').inV()";
            var queryRes = SubmitRequest(client, queryScript).Result;
            outputData(queryRes);
            var response = JsonConvert.SerializeObject(queryRes);
            List<string> responseList = new List<string>();
            responseList.Add(response);

            // return allAccountIds;
            return responseList;
        }

        public List<Vertex> GetAlias(PostPiidEvent alias)
        {
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

            // GREMLIN
            /*var queryScript = $"g.V().hasLabel('accountId').has('id', '{alias.accountId}').outE('Associated PIID').inV().hasLabel('piid').has('id', '{alias.piid}')";
            var queryRes = SubmitRequest(client, queryScript).Result;
            outputData(queryRes);
            var response = JsonConvert.SerializeObject(queryRes);
            List<string> responseList = new List<string>();*/

        }

        public bool AddTransactionEvent(PostTransactionEvent item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var sourcePiidVertex = new Vertex();
            var sourceAccountIdVertex = _paymentGraph.Vertices.FirstOrDefault(p => p.VertexType == "accountIdType" && p.VertexValue == item.accountId);
            if (sourceAccountIdVertex == null)
            {
                throw new InvalidOperationException("Source vertex not found");
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
                _paymentGraph.AddVertex(sourceSecureDataIdVertex);
            }

            // _paymentGraph.AddVertex(new Vertex { VertexType = "secureDataIdType", VertexValue = item.secureDataId });
            _paymentGraph.AddEdge(new Edge<Vertex>(sourcePiidVertex, sourceSecureDataIdVertex));
            _paymentGraph.AddEdge(new Edge<Vertex>(sourceSecureDataIdVertex, sourcePiidVertex));

            // GREMLIN
            // var addPiidEvent_secureDataId1 = "gremline_test_secureDataId1";
            /*gremlinQueries = new Dictionary<string, string>();
            var accountId = "tobefilled";
            var piid = item.piid;

            gremlinQueries.Add($"Find if account exists already + {item}", $"g.V().hasLabel('accountId').has('value._value', {item.accountId}).inV().hasLabel('piid').outE('Associated Account').inV()");

            foreach (var query in gremlinQueries)
            {
                Console.WriteLine(String.Format("Running this query: {0}: {1}", query.Key, query.Value));

                // Create async task to execute the Gremlin query.
                var resultSet = SubmitRequest(client, query.Value).Result;
                outputData(resultSet);
            }
            Console.WriteLine("Output query response\n");*/

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
    }
}