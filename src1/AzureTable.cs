using System;
using System.Threading.Tasks;
using System.Threading;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Web;

namespace AIMS360.Azure
{
    public class AzureTable
    {


        // * * * * * * * * * * Private Fields * * * * * * * * * *
        private TableClient tableClient;
        private string DefaultPartitionKey;
        private string DefaultEntityKey;
        private ILogger log;

        // * * * * * * * * * * Construtors * * * * * * * * * *
        public AzureTable(string ConnectionString, string TableName, string DefaultPartitionKey = null, string DefaultEntityKey = null, ILogger log = null)
        {
            this.tableClient = new TableClient(ConnectionString, TableName);
            this.DefaultPartitionKey = DefaultPartitionKey;
            this.DefaultEntityKey = DefaultEntityKey;
            this.log = log;
        }


















        // * * * * * * * * * * Getter Functions * * * * * * * * * *

        /**
         * Fetches a "row" from the table using PartitionKey and RowKey and returns the value
         * of that entity using the key in EntityKey. Returns null if the entity key is not 
         * present and throws an exception if the entity key does not exist. If a value is not set, uses
         * the corresponding default value set in the constructor.
         */
        public async Task<string> GetTableEntityValue(string RowKey, string PartitionKey = null, string EntityKey = null)
        {
            var tableEntity = await GetTableEntity(RowKey, PartitionKey);
            
            if(tableEntity == null)
                return null;

            // see if the entity key is in the entity
            EntityKey = (EntityKey == null ? DefaultEntityKey : EntityKey);
            if(!tableEntity.ContainsKey(EntityKey))
                return null;  // entity key does not exist
            else
                return (string)tableEntity[EntityKey]; // entity key does exist

        }


        /**
         * Gets the entity with the corresponding ParititionKey value,
         * using the defaults specified in the constructor if none were given
         */
        public async Task<TableEntity> GetTableEntity(string RowKey, string PartitionKey = null)
        {
            // set keys to default values if null
            PartitionKey    = (PartitionKey == null ? DefaultPartitionKey : PartitionKey);
            PartitionKey    = HttpUtility.UrlEncode(PartitionKey);
            RowKey          = HttpUtility.UrlEncode(RowKey);
            // get response from azure and convert into TableEntity
            int maxAttempts = 3;
            TableEntity azureTableEntity = null;
            for(int i = 1; i <= maxAttempts; i++)
            {
                try{
                    // get response and get the entity from inside the Azure.Response
                    var azureTableResponse = await tableClient.GetEntityAsync<TableEntity>(PartitionKey, RowKey);
                    azureTableEntity = azureTableResponse.Value;
                    return azureTableEntity;
                } catch (RequestFailedException e) when (e.Status == 404){
                    return null; // entity does not exist

                } catch (Exception e) {
                    LinearBackoff(i, maxAttempts, "Query Azure for TableEntity", e);
                }
            }

            return azureTableEntity;
        }

        













        // * * * * * * * * * * Table Modification Functions * * * * * * * * * *


        /**
         * Simple rewrite to allow passing one key/value to the partition,
         * where key is the entityKey specified in the last argument
         * and row specified, of course using default values if keys are null.
         */
        public async Task<Response> AddEntitySimple(string RowKey, object value, string PartitionKey = null, string EntityKey = null)
        {

            EntityKey = (EntityKey == null ? DefaultEntityKey : EntityKey);

            var keyValuePairs = new (string, object)[]{(EntityKey, value)};
            return await AddEntityKeyValuePairs(RowKey, keyValuePairs, PartitionKey);
        }


        /**
         * Converts the key value pairs into a tableEntity
         * then calls AddEntity to add the resulting entity
         * into the Table specified in the constructor.
         */
        public async Task<Response> AddEntityKeyValuePairs(string RowKey, (string, object)[] EntityKeyValuePairs, string PartitionKey = null)
        {
            // create table entity and add all key/value pairs to it
            var tableEntity = new TableEntity(PartitionKey, RowKey);
            foreach((string key, object value) in EntityKeyValuePairs)
                tableEntity.Add(key, value);

            return await AddEntity(RowKey, tableEntity, PartitionKey);
        }


        /**
         * Adds the key/value pairs in the first parameter to a table
         * entity and adds that to this table client. Uses default
         * values specified in constructor if Keys are not set.
         */
        public async Task<Response> AddEntity(string RowKey, TableEntity tableEntity, string PartitionKey = null)
        {
            tableEntity.PartitionKey    = (PartitionKey == null ? DefaultPartitionKey : PartitionKey);
            UrlEncodeEntity(tableEntity);

            // add entity to azure
            int maxAttempts = 3;
            for(int i = 1; i <= maxAttempts; i++)
            {
                try{
                    // add entity to azure
                    var azureTableResponse = await tableClient.AddEntityAsync<TableEntity>(tableEntity);
                    return azureTableResponse;
                } catch (RequestFailedException e) when (e.Status == 409){
                    return null;
                } catch (Exception e) {
                    LinearBackoff(i, maxAttempts, "add entity to table", e);
                }
            }

            return null; // shouldn't be here.
        }


        /**
         * Updates the entity specified with PartitionKey and RowKey
         * using a TableEntity with a single key/value pair (EntityKey, value)
         * and default Replace mode Replace
         */
        public async Task<Response> UpdateEntitySimple(string RowKey, object value, string EntityKey = null, string PartitionKey = null, TableUpdateMode mode = TableUpdateMode.Replace)
        {
            if(value is string)
                value = HttpUtility.UrlEncode((string)value);

            // set key value
            EntityKey = (EntityKey == null ? DefaultEntityKey : EntityKey);

            // update
            var keyValuePairs = new (string, object)[]{(EntityKey, value)};
            return await UpdateEntityKeyValuePairs(RowKey, keyValuePairs, PartitionKey, mode);
        }


        /**
         * Updates the entity specified with RowKey/PartitionKey with
         * the values in the KeyValuePairs array, with default mode Replace
         */
        public async Task<Response> UpdateEntityKeyValuePairs(string RowKey, (string, object)[] EntityKeyValuePairs, string PartitionKey = null, TableUpdateMode mode = TableUpdateMode.Replace)
        {
            // create table entity and add all key/value pairs to it
            var tableEntity = new TableEntity(PartitionKey, RowKey);
            foreach((string key, object value) in EntityKeyValuePairs)
                tableEntity.Add(key, value);

            return await UpdateEntity(RowKey, tableEntity, PartitionKey, mode);
        }


        /**
         * Updates the value with the given partition/row key pairs,
         * using default values if null, with the values in EntityKeyValuePairs
         */
        public async Task<Response> UpdateEntity(string RowKey, TableEntity newEntity, string PartitionKey = null, TableUpdateMode mode = TableUpdateMode.Replace)
        {
            newEntity.PartitionKey = (PartitionKey == null ? DefaultPartitionKey : PartitionKey);
            newEntity.RowKey       = RowKey;
            UrlEncodeEntity(newEntity);

            // update using linear backoff
            int maxAttempts = 3;
            for(int i = 1; i <= maxAttempts; i++)
            {
                try{
                    // update entity
                    var response = await tableClient.UpdateEntityAsync<TableEntity>(newEntity, ETag.All, mode);
                    return response;
                } catch(Exception e) {
                    LinearBackoff(i, maxAttempts, $"updating table entry {RowKey}", e);
                }
            }

            return null; // shouldn't get here
        }


















        // * * * * * * * * * * Helper Functions * * * * * * * * * *
        private void LogError(string data)
        {
            if(log == null) return;
            lock(log)
            {
                log.LogError(data);
            }
        }

        private void LogInformation(string data)
        {
            if(log == null) return;
            lock(log)
            {
                log.LogInformation(data);
            }
        }

        private void LinearBackoff(int i, int maxAttempts, string action, Exception e)
        {
            // on limit
            if(i == maxAttempts)
            {
                LogError($"Reached maximum number of attempts {action}. Terminating...");
                throw e;
            }
            // if we still have more retries available
            LogError($"Failed to {action}, making attempt {i + 1} out of {maxAttempts}...");
            Thread.Sleep(500 * i); // linear backoff
        }

        private TableEntity UrlEncodeEntity(TableEntity Entity)
        {
            Entity.PartitionKey = HttpUtility.UrlEncode(Entity.PartitionKey);
            Entity.RowKey = HttpUtility.UrlEncode(Entity.RowKey);

            return Entity;
        }
    }
}