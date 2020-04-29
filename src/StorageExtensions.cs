using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AlejoF.Netlify.Contact
{
    public static class Storage
    {
        public const string ConnectionStringSetting = "StorageConnectionString";
    }

    public static class ServiceExtensions
    {
        public static IServiceCollection AddTableStorage(this IServiceCollection services)
        {
            // Azure Storage
            var connectionString = System.Environment.GetEnvironmentVariable(Storage.ConnectionStringSetting, EnvironmentVariableTarget.Process);

            services.AddSingleton(svc =>
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                return storageAccount.CreateCloudTableClient();
            });

            return services;
        }
    }

    public static class CloudTableExtensions
    {
        public static async Task<TEntity> RetrieveAsync<TEntity>(this CloudTable table, string partitionKey, string rowKey)
            where TEntity : TableEntity, new()
        {
            var retrieveOperation = TableOperation.Retrieve<TEntity>(partitionKey, rowKey);
            var result = await table.ExecuteAsync(retrieveOperation);

            return result.Result as TEntity;
        }

        public static async Task<List<TEntity>> ScanAsync<TEntity>(this CloudTable table, string partitionKey)
            where TEntity : TableEntity, new()
        {
            var query = new TableQuery<TEntity>()
                .Where($"PartitionKey eq '{partitionKey}'");
                
            var segment = await table.ExecuteQuerySegmentedAsync(query, null);
            return segment.ToList();
        }

        public static async Task<List<TEntity>> QueryAsync<TEntity>(this CloudTable table, string partitionKey, string filter)
            where TEntity : TableEntity, new()
        {
            var query = new TableQuery<TEntity>()
                .Where($"PartitionKey eq '{partitionKey}' and {filter}");

            var segment = await table.ExecuteQuerySegmentedAsync(query, null);
            return segment.ToList();
        }

        public static async Task<bool> InsertAsync(this CloudTable table, ITableEntity entity)
        {
            var operation = TableOperation.Insert(entity);
            var result = await table.ExecuteAsync(operation);

            return result.IsSuccess();
        }

        public static async Task<bool> ReplaceAsync(this CloudTable table, ITableEntity entity, bool insertIfNotFound = false)
        {
            var operation = insertIfNotFound ?
                TableOperation.InsertOrReplace(entity)
                : TableOperation.Replace(entity);

            var result = await table.ExecuteAsync(operation);

            return result.IsSuccess();
        }

        public static async Task<bool> DeleteAsync(this CloudTable table, ITableEntity entity)
        {
            var operation = TableOperation.Delete(entity);
            var result = await table.ExecuteAsync(operation);

            return result.IsSuccess();
        }

        public static bool IsSuccess(this TableResult result) => result.HttpStatusCode >= 200 && result.HttpStatusCode < 300;
    }
}