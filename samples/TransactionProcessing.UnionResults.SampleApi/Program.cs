using TransactionProcessing.UnionResults;
using TransactionProcessing.UnionResults.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddUnionResults(options => options.MapCode("Transaction.Duplicate", 409));
var app = builder.Build();
app.UseExceptionHandler();
var api = app.MapGroup("/api").AddUnionResultFilter();
api.MapGet("/items/{id:int}", (int id) => id == 42
    ? Result<Item>.Ok(new Item(id, "Example"))
    : Result<Item>.Fail(new Error("Item.NotFound", "Item does not exist.", ErrorType.NotFound)));
api.MapPost("/items", () => Result<Item>.Created(new Item(43, "Created"), "/api/items/43"));
app.Run();

internal sealed record Item(int Id, string Name);
