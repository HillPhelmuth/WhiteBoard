using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Board.Api
{
    public class ImageStorageFunction
    {
        private readonly ILogger _logger;
        private readonly string connectionStringBlob = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") ?? "";
        private readonly string connectionStringCosmos = Environment.GetEnvironmentVariable("AZURE_COSMOS_CONNECTION_STRING") ?? "";
        private readonly CosmosClient _cosmosClient;
        public ImageStorageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ImageStorageFunction>();
            _cosmosClient = new CosmosClient(connectionStringCosmos);
        }

        [Function("PostImage")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "PostImage/{userName}")] 
            HttpRequestData req, string userName)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var reqString = await req.ReadAsStringAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            var imageData = JsonConvert.DeserializeObject<ImageData>(reqString ?? "");
            await using var stream = new MemoryStream(imageData?.ImageBytes ?? Array.Empty<byte>());
            //dynamic result;
            try
            {
                var imgContainer = GetContainer(userName.ToValidContainerName());
                var fileName = $"{imageData?.ImageName}.png";
                var imgBlob = imgContainer.GetBlobClient(fileName);
                await imgBlob.UploadAsync(stream, overwrite: true);
                await response.WriteStringAsync($"Image {imageData?.ImageName} uploaded successfully");
            }
            catch (Exception e)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync($"Error saving file: {e.Message}\r\n{e.StackTrace}");
            }
            

            return response;
        }
        [Function("SaveImage")]
        public async Task<HttpResponseData> SaveImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "SaveImage/{userName}")] HttpRequestData req, string userName, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function SaveImage processed a request.");
            var reqString = await req.ReadAsStringAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            try
            {
                var imageData = JsonConvert.DeserializeObject<ImageData>(reqString);
                await using var stream = new MemoryStream(imageData.ImageBytes);

                var cosmosContainer = _cosmosClient.GetContainer("WhiteboardDb", "Images");
                imageData.Id ??= $"{imageData.UserName}-{imageData.Category}-{imageData.ImageName}";
                imageData.ImageBytes = Array.Empty<byte>();
                imageData.UserName ??= userName;
                log.LogInformation(JsonConvert.SerializeObject(imageData, Formatting.Indented));
                var result = await cosmosContainer.UpsertItemAsync(imageData);
                log.LogInformation($"Results:\r\nStatus code: {result.StatusCode}\r\nContent: {result.Resource}");
                await response.WriteStringAsync($"Image {imageData.ImageName} saved successfully");
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(ex.ToString());
            }

            return response;
        }
        [Function("GetAppImages")]
        public async Task<HttpResponseData> GetAppImages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetAppImages")] HttpRequestData req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function GetAppImages processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            var imageList = new ImageList { Category = "image", Images = new List<ImageData>() };
            var container = GetContainer();
            await foreach (var blob in container.GetBlobsAsync())
            {
                var blobName = blob.Name;
                var client = container.GetBlobClient(blobName);
                await using var stream = await client.OpenReadAsync();
                imageList.Images.Add(new ImageData { ImageName = blobName, ImageBytes = await stream.ReadFully(), CreatedOnDate = blob.Properties.CreatedOn });
            }
            log.LogInformation($"Image data retrieved for {string.Join(", ", imageList.Images.Select(x => x.ImageName))}");
            await response.WriteAsJsonAsync(imageList);
            return response;
        }
        [Function("GetUserImages")]
        public async Task<HttpResponseData> GetUserImages(
               [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetUserImages/{userName}")]
            HttpRequestData req, ILogger log, string userName)
        {
            log.LogInformation("C# HTTP trigger function HandImageGetStorage processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            try
            {
                var cosmosContainer = _cosmosClient.GetContainer("WhiteboardDb", "Images");
                var imageQuery = new List<ImageData>();
                using (var imageIterator = cosmosContainer.GetItemLinqQueryable<ImageData>().ToFeedIterator())
                {
                    while (imageIterator.HasMoreResults)
                    {
                        var resultSet = await imageIterator.ReadNextAsync();
                        imageQuery.AddRange(resultSet.Where(x => x.UserName == userName));
                    }
                }
                var imageList = new ImageList { Category = "general", Images = new List<ImageData>() };
                var container = GetContainer(userName.ToValidContainerName());
                await foreach (var blob in container.GetBlobsAsync())
                {
                    var blobName = blob.Name;
                    var imageMatch = imageQuery.Find(x => x.ImageName == blobName.NoFileExt());
                    if (imageMatch == null) continue;
                    var client = container.GetBlobClient(blobName);
                    await using var stream = await client.OpenReadAsync();
                    imageMatch.CreatedOnDate = blob.Properties.CreatedOn;
                    imageMatch.ImageBytes = await stream.ReadFully();
                    imageList.Images.Add(imageMatch);
                }
                log.LogInformation($"Image data retrieved for {string.Join(", ", imageList.Images.Select(x => x.ImageName))}");
                await response.WriteAsJsonAsync(imageList);
            }
            catch (Exception ex)
            {
                log.LogError($"{ex.Message}\r\n{ex.StackTrace}");
                await response.WriteStringAsync($"{ex.Message}\r\n{ex.StackTrace}");
            }

            return response;
        }
        [Function("GetUserTypeImages")]
        public async Task<HttpResponseData> GetUserTypeImages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetUserTypeImages/{userName}/{category}")]
            HttpRequestData req, ILogger log, string userName, string category)
        {
            log.LogInformation("C# HTTP trigger function HandImageGetStorage processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            var imageQuery = new List<ImageData>();
            var cosmosContainer = _cosmosClient.GetContainer("WhiteboardDb", "Images");
            using (var imageIterator = cosmosContainer.GetItemLinqQueryable<ImageData>().ToFeedIterator())
            {
                while (imageIterator.HasMoreResults)
                {
                    var resultSet = await imageIterator.ReadNextAsync();
                    imageQuery.AddRange(resultSet.Where(x => x.UserName == userName && x.Category == category));
                }
            }
            var imageList = new ImageList { Category = category, Images = new List<ImageData>() };

            var container = GetContainer(userName.ToValidContainerName());
            await foreach (var blob in container.GetBlobsAsync())
            {
                var blobName = blob.Name;
                var imageMatch = imageQuery.Find(x => x.ImageName == blobName.NoFileExt());
                if (imageMatch == null) continue;
                var client = container.GetBlobClient(blobName);
                await using var stream = await client.OpenReadAsync();
                imageMatch.CreatedOnDate = blob.Properties.CreatedOn;
                imageMatch.ImageBytes = await stream.ReadFully();
                imageList.Images.Add(imageMatch);
            }
            log.LogInformation($"Image data retrieved for {string.Join(", ", imageList.Images.Select(x => x.ImageName))}");
            await response.WriteAsJsonAsync(imageList);
            return response;
        }
        #region helper

        private BlobContainerClient GetContainer(string containerName = "appimages")
        {
            var container = new BlobContainerClient(connectionStringBlob, containerName);
            container.CreateIfNotExists();

            return container;
        }

        #endregion
    }
}
