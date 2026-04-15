var builder = DistributedApplication.CreateBuilder(args);

var cacheAndMessages = builder.AddRedis("cacheAndMessages");

var apiService = builder.AddProject<Projects.biblioteket_ApiService>("apiservice")
    .WithReference(cacheAndMessages)
    .WaitFor(cacheAndMessages);

builder.AddProject<Projects.biblioteket_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cacheAndMessages)
    .WaitFor(cacheAndMessages);    

builder.AddProject<Projects.biblioteket_Worker>("worker")
    .WithReference(cacheAndMessages)
    .WaitFor(cacheAndMessages);

builder.Build().Run();