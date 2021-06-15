using Azure.Core;
using Azure.Core.Serialization;
using Azure.Identity;
using dotenv.net;
using System;
using System.Collections.Generic;
using System.Text.Json;

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

var farmer_id = "test_farmer";

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

foreach (Farmer f in client.GetValues<Farmer>($"/farmers"))
{
    Console.WriteLine($"Farmer: [ id: {f.Id}, name: {f.Name ?? "<none>"} ]");
}

client.Delete($"/farmers/{farmer_id}");

class Farmer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}