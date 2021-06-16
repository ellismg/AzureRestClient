using Azure;
using Azure.Core;
using Azure.Core.Serialization;
using Azure.Identity;
using dotenv.net;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Node;

DotEnv.Load();

AzureRestClient client = new AzureRestClient(
    new Uri(Environment.GetEnvironmentVariable("FARMBEATS_ENDPOINT")),
    "2021-03-31-preview",
    new DefaultAzureCredential(),
    "https://farmbeats.azure.net/.default",
    new AzureRestClientOptions()
    {
        ValueSerializer = new JsonObjectSerializer(new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })
    }
);

// Create or Update Farmer.
var farmer_id = "ellismg_test_farmer";

var farmer = new Farmer()
{
    Name = "Contoso Farmer",
    Description = "Your custom farmer description here",
    Status = "Active",
    Properties = new Dictionary<string, object>()
    {
        ["custom"] = "property"
    }
};

client.PatchValue($"/farmers/{farmer_id}", farmer, new ()
{
    ContentType = "application/merge-patch+json"
});

// List Farmers.
foreach (Farmer f in client.GetValues<Farmer>($"/farmers"))
{
    Console.WriteLine($"Farmer: [ id: {f.Id}, name: {f.Name ?? "<none>"} ]");
}


// Casecade Delete Farmer.
// NOTE: Job Ids must start with a letter, and then can contain alphanumerics and a dash, so we don't use just a UUID here.
string jobId = $"ecd-{Guid.NewGuid()}";

Response initial = client.Put($"/farmers/cascade-delete/{jobId}?farmerId={farmer_id}", default(RequestContent));
Operation<JsonNode> o = client.OperationFromResponse(initial, new GetOperationOptions()
{
    FinalStateLocation = OperationFinalStateLocation.UseLocationHeader,
});
Response final = o.WaitForCompletionResponseAsync().GetAwaiter().GetResult();

Console.WriteLine($"Removal Final Status: {final.Status}");
Console.WriteLine($"Removal Completed: {o.HasCompleted}");
Console.WriteLine($"Removal Has Value: {o.HasValue}");
Console.WriteLine($"Removal Final Body: {o.Value}");

// List Farmers, Again.
foreach (Farmer f in client.GetValues<Farmer>($"/farmers"))
{
    Console.WriteLine($"Farmer: [ id: {f.Id}, name: {f.Name ?? "<none>"} ]");
}

class Farmer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}