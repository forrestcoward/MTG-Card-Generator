using Microsoft.Azure.Cosmos;
using System;

namespace MTG.CardGenerator
{
    internal class QueryDefinitionWrapper
    {
        /// <summary>
        /// The formatted query with parameters substituted.
        /// </summary>
        public string Query { get; private set; }

        /// <summary>
        /// The underlying QueryDefinition object.
        /// </summary>
        public QueryDefinition QueryDefinition { get; private set; }

        public QueryDefinitionWrapper(string query)
        {
            Query = query;
            QueryDefinition = new QueryDefinition(query);
        }

        public QueryDefinitionWrapper WithParameter(string name, object value)
        {
            QueryDefinition = QueryDefinition.WithParameter(name, value);
            if (value is DateTime time)
            {
                Query = Query.Replace(name, $"'{time.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}'");
            }
            else if (value is string)
            {
                Query = Query.Replace(name, $"'{value}'");
            }
            else
            {
                Query = Query.Replace(name, value.ToString());
            }

            return this;
        }
    }
}
