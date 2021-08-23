using System.Text.Json.Serialization;

namespace TinyUpdate.Github.GraphQL
{
    /// <summary>
    /// Basic class to act as GraphQl Query
    /// </summary>
    public class GraphQlQuery
    {
        public GraphQlQuery(string query, string variables)
        {
            Query = query;
            Variables = variables;
        }

        [JsonPropertyName("query")]
        public string Query { get; }
        
        [JsonPropertyName("variables")]
        public string Variables { get; }
    }
}