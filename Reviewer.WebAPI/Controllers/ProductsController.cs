using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Reviewer.WebAPI.Dtos;

namespace Reviewer.WebAPI.Controllers;
[Route("api/[controller]")]
[ApiController]
[MyValidation]
public sealed class ProductsController : ControllerBase
{
    [HttpPost]
    public IActionResult Create(CreateProductDto request)
    {
        return Ok();
    }
}

public sealed class MyValidation : Attribute, IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context) { }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var body = context.ActionArguments.First().Value;
        var bodyType = body.GetType();
        var validatorBaseType = typeof(AbstractValidator<>).MakeGenericType(bodyType); //Abstractvalidator<CreateProductDto>

        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes().Where(p =>
        p.IsClass
        && !p.IsAbstract
        && validatorBaseType.IsAssignableFrom(p)
        );

        List<ValidationResult> results = new();
        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type);
            var methodInfo = type.GetMethod("Validate");
            var result = (ValidationResult)methodInfo.Invoke(instance, [body]);

            results.Add(result);
        }

        var res = results.Where(p => !p.IsValid).ToList();

        if (res.Any())
        {
            var errors = res.SelectMany(s => s.Errors).Distinct().ToList();

            context.Result = new ObjectResult(new
            {
                Success = false,
                Message = "Validation errors occurred",
                Errors = errors.Select(e => e.ErrorMessage).ToList()
            })
            {
                StatusCode = 403
            };

            return;
        }
    }
}

public sealed class ValidationException : Exception
{
    public ValidationException(List<ValidationFailure> failures) : base(JsonSerializer.Serialize(failures))
    {

    }
}