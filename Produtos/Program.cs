using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IBlob, Blob>();
builder.Services.AddAntiforgery();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/produtos/foto", async (IFormFile file, IBlob blob) =>
{
    await blob.Upload(file);
}).DisableAntiforgery();

app.MapGet("/documentos/lista", async (IBlob blob) =>
{
    var documentos =await blob.GetDocuments();
    return documentos;

});
app.MapGet("/authorization", async (IBlob blob) =>
{
    var sas = await blob.CreateServiceSASBlob();
    return sas;

});

app.UseAntiforgery();
app.Run();

public interface IBlob
{
    Task Upload(IFormFile file);
    Task<List<string>> GetDocuments();
    Task<Uri> CreateServiceSASBlob(string storedPolicyName=null);
}

public class Blob : IBlob
{
    private readonly IConfiguration _configuration;

    public Blob(IConfiguration configuration)
    {
        this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task Upload(IFormFile file)
    {
        using var stream = new MemoryStream();
        file.CopyTo(stream);
        stream.Position = 0;
        var container = new BlobContainerClient(_configuration["Blob:ConnectionString"], _configuration["Blob:ContainerName"]);
        await container.UploadBlobAsync(file.FileName, stream);

    }
    public  async Task<List<string>> GetDocuments()
    {
        var container = new BlobContainerClient(_configuration["Blob:ConnectionString"], _configuration["Blob:ContainerName"]);
        var resultSegment = container.GetBlobsByHierarchyAsync( prefix:"",delimiter:"/")
            .AsPages(default);
        var arrayDocuments = new List<string>();
        await foreach (Page<BlobHierarchyItem> blobPage in resultSegment)
        {
            // A hierarchical listing may return both virtual directories and blobs.
            foreach (BlobHierarchyItem blobhierarchyItem in blobPage.Values)
            {
                if (blobhierarchyItem.IsPrefix)
                {
                    arrayDocuments.Add("Diretório: "+blobhierarchyItem.Prefix);
                }
                else
                {
                    // Write out the name of the blob.
                    arrayDocuments.Add("Blob: " + blobhierarchyItem.Blob.Name);

                }
            }

        }
        return arrayDocuments;
    }

    public  async Task<Uri> CreateServiceSASBlob(
    string storedPolicyName = null)
    {
        string accountName = "devstoreaccount1";
        string accountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
        StorageSharedKeyCredential storageSharedKeyCredential =
        new(accountName, accountKey);
        BlobServiceClient blobServiceClient = new BlobServiceClient(
            new Uri($"http://127.0.0.1:10000/{accountName}"),
            storageSharedKeyCredential);
        BlobClient blobClient = blobServiceClient
        .GetBlobContainerClient("pgm")
        .GetBlobClient("PPMU/identidade.txt");
        // Check if BlobContainerClient object has been authorized with Shared Key
        if (blobClient.CanGenerateSasUri)
        {
            // Create a SAS token that's valid for one day
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
                BlobName = blobClient.Name,
                Resource = "b"
            };

            if (storedPolicyName == null)
            {
                sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddDays(1);
                sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
            }
            else
            {
                sasBuilder.Identifier = storedPolicyName;
            }

            Uri sasURI = blobClient.GenerateSasUri(sasBuilder);

            return sasURI;
        }
        else
        {
            // Client object is not authorized via Shared Key
            return null;
        }
    }  

}
