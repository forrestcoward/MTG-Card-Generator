using Microsoft.Azure.Cosmos;
using System;

namespace MTG.CardGenerator
{
    internal class QueryDefinitionWrapper(string query)
    {
        /// <summary>
        /// Gets the formatted query with parameters substituted.
        /// </summary>
        public string Query { get; private set; } = query;

        /// <summary>
        /// Gets the underlying QueryDefinition object.
        /// </summary>
        public QueryDefinition QueryDefinition { get; private set; } = new QueryDefinition(query);

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
