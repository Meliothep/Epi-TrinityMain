
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var dbUsername = builder.AddParameter("DbUsername", secret: true);
var dbPassword = builder.AddParameter("DbPassword", secret: true);

var kafka = builder.AddKafka("kafka",9092);

var seq = builder.AddSeq("seq")
                 .ExcludeFromManifest();

var postgres = builder.AddPostgres("customerPostgres", dbUsername, dbPassword, 5432);

if(builder.Environment.IsDevelopment()){
        postgres.WithPgAdmin();
}


var postgresdb = postgres.AddDatabase("customersDB");

var migrationService = builder.AddProject<Projects.Trinity_MigrationService>("migrations")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

builder.AddProject<Projects.Customers>("customer")
        .WithReference(kafka)
        .WaitFor(kafka)
        .WithReference(postgresdb)
        .WaitFor(postgresdb)
        .WithReference(seq)
        .WaitFor(migrationService)
        .WaitFor(seq);


postgres = builder.AddPostgres("inventoryPostgres", dbUsername, dbPassword, 5433);
postgresdb = postgres.AddDatabase("inventoryDB");

migrationService.WithReference(postgresdb).WaitFor(postgresdb);

builder.AddProject<Projects.Inventory>("inventory")
        .WithReference(kafka)
        .WaitFor(kafka)
        .WithReference(postgresdb)
        .WaitFor(postgresdb)
        .WithReference(seq)
        .WaitFor(migrationService)
        .WaitFor(seq);


builder.Build().Run();
