var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

//app.MapPost("/products", (CreateProductDto request) =>
//{
//    ProductCreateValidator validator = new();
//    ValidationResult result = validator.Validate(request);

//    return result.IsValid ? Results.Ok() : Results.InternalServerError(result.Errors);
//});

app.MapControllers();

app.Run();