
using System.Reflection;
using Reviewer.WebAPI.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapPost("/products", (CreateProductDto request) =>
{
    ProductCreateValidator validator = new();
    ValidationResult result = validator.Validate(request);

    return result.IsValid ? Results.Ok() : Results.InternalServerError(result.Errors);
}).AddEndpointFilter<ValidationFilter>();

app.MapControllers();

app.Run();

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // T tipindeki argument'ý bul
        var argument = context.Arguments.FirstOrDefault();
        if (argument == null)
        {
            return await next(context);
        }

        var bodyType = argument.GetType();
        var validatorBaseType = typeof(AbstractValidator<>).MakeGenericType(bodyType);
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes().Where(p => p.IsClass && !p.IsAbstract && validatorBaseType.IsAssignableFrom(p));

        List<ValidationResult> results = new();
        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type);
            var methodInfo = type.GetMethod("Validate");
            var result = (ValidationResult)methodInfo?.Invoke(instance, [argument]);
            if (result != null)
                results.Add(result);
        }

        var invalidResults = results.Where(p => !p.IsValid).ToList();
        if (invalidResults.Any())
        {
            var errors = invalidResults.SelectMany(s => s.Errors)
                                    .Select(e => new
                                    {
                                        Field = e.PropertyName,
                                        Message = e.ErrorMessage
                                    })
                                    .Distinct()
                                    .ToList();

            return Results.BadRequest(new
            {
                Title = "Validation Failed",
                Status = 400,
                Errors = errors
            });
        }

        return await next(context);
    }
}
