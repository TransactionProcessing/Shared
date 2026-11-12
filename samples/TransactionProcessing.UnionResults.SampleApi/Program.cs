using TransactionProcessing.UnionResults;
using TransactionProcessing.UnionResults.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddUnionResults(options => options.MapCode("Transaction.Duplicate", 409));
builder.Services.AddOpenApi();
var app = builder.Build();
app.UseExceptionHandler();
var api = app.MapGroup("/api").AddUnionResultFilter();
api.MapGet("/items/{id:int}", (int id) => id == 42
    ? Result<Item>.Ok(new Item(id, "Example"))
    : Result<Item>.Fail(new Error("Item.NotFound", "Item does not exist.", ErrorType.NotFound)))
    .ProducesUnionResult<Item>();
api.MapPost("/items", () => Result<Item>.Created(new Item(43, "Created"), "/api/items/43"))
    .ProducesUnionResult<Item>();
api.MapDelete("/items/{id:int}", (int id) => Result.Ok())
    .ProducesUnionResult();
app.MapOpenApi();
app.Run();

internal sealed record Item(int Id, string Name);
