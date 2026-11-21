using TransactionProcessing.UnionResults;
using TransactionProcessing.UnionResults.AspNetCore;
using UnionResults = TransactionProcessing.UnionResults.Results;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddUnionResults();
builder.Services.AddOpenApi();
var app = builder.Build();
app.UseExceptionHandler();
var api = app.MapGroup("/api").AddUnionResultFilter();
api.MapGet("/items/{id:int}", GetItem)
    .ProducesUnionResult<Item>();
api.MapPost("/items", () => TypedResults.Created("/api/items/43", new Item(43, "Created")));
api.MapDelete("/items/{id:int}", (int id) => UnionResults.Ok())
    .ProducesUnionResult();
app.MapOpenApi();
app.Run();

static Result<Item> GetItem(int id) => id == 42
    ? UnionResults.Ok(new Item(id, "Example"))
    : new Error(ErrorType.NotFound, "Item does not exist.");

internal sealed record Item(int Id, string Name);
