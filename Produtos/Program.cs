using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.Run();

public interface IBlob
{
    Task Upload(IFormFile file);
}

public class Blob : IBlob
{
    public async Task Upload(IFormFile file)
    {
        var container = new BlobContainerClient();
        throw new NotImplementedException();
    }
}