using System.Linq.Expressions;

namespace Reviewer.WebAPI.Dtos;

public sealed record CreateProductDto(
    string Name,
    decimal Price);

public abstract class AbstractValidator<TEntity>
    where TEntity : class
{
    private List<Func<TEntity, object>> _getters = new();
    private List<Func<TEntity, ValidationFailure?>> _results { get; } = new();
    public IRuleBuilder<TEntity, TProperty> RuleFor<TProperty>(Expression<Func<TEntity, TProperty>> expression)
    {
        var compile = expression.Compile();
        if (expression.Body is not MemberExpression member)
        {
            throw new ArgumentException();
        }

        string propertyName = member.Member.Name;

        Func<TEntity, object> getter = x => compile(x)!;
        _getters.Add(getter);

        return new RuleBuilder<TEntity, TProperty>(propertyName, compile, _results);
    }

    public ValidationResult Validate(TEntity instance)
    {
        List<ValidationFailure> failures = new();
        foreach (var result in _results)
        {
            var validationFailure = result(instance);
            if (validationFailure is not null)
                failures.Add(validationFailure);
        }
        var res = new ValidationResult();
        res.Errors = failures;
        return res;
    }
}

public interface IRuleBuilder<TEntity, TProperty>
{
    string _propertyName { get; }
    Func<TEntity, TProperty> _getter { get; }
    List<Func<TEntity, ValidationFailure?>> _results { get; }

}
public class RuleBuilder<TEntity, TProperty> : IRuleBuilder<TEntity, TProperty>
{
    public string _propertyName { get; }
    public Func<TEntity, TProperty> _getter { get; }
    public List<Func<TEntity, ValidationFailure?>> _results { get; } = new();
    public RuleBuilder(string propertyName, Func<TEntity, TProperty> getter, List<Func<TEntity, ValidationFailure?>> results)
    {
        _propertyName = propertyName;
        _getter = getter;
        _results = results;
    }
}

public static class Extensions
{
    public static IRuleBuilder<TEntity, string> NotEmpty<TEntity>(this IRuleBuilder<TEntity, string> builder)
    {
        Func<TEntity, ValidationFailure?> func = instance =>
        {
            var value = builder._getter(instance);
            if (string.IsNullOrEmpty(value))
                return new ValidationFailure(builder._propertyName, "NotEmpty", "Value cannot be null or empty");

            return null;
        };

        builder._results.Add(func);
        return builder;
    }

    public static IRuleBuilder<TEntity, decimal> GreaterThan<TEntity>(this IRuleBuilder<TEntity, decimal> builder, decimal maxValue)
    {
        Func<TEntity, ValidationFailure?> func = instance =>
        {
            var propertyValue = builder._getter(instance);
            if (propertyValue < maxValue)
                return new ValidationFailure(builder._propertyName, "GreaterThan", $"Value must be greater than {maxValue}");

            return null;
        };

        builder._results.Add(func);
        return builder;
    }

    public static IRuleBuilder<TEntity, string> WithMessage<TEntity>(this IRuleBuilder<TEntity, string> builder, string message)
    {
        var lastIndex = builder._results.Count - 1;
        var lastRule = builder._results[lastIndex];

        builder._results[lastIndex] = instance =>
        {
            var failure = lastRule(instance);
            if (failure is not null)
            {
                return new ValidationFailure(failure.PropertyName, failure.ErrorCode, message);
            }
            return null;
        };

        return builder;
    }

    public static IRuleBuilder<TEntity, decimal> WithMessage<TEntity>(this IRuleBuilder<TEntity, decimal> builder, string message)
    {
        var lastIndex = builder._results.Count - 1;
        var lastRule = builder._results[lastIndex];

        builder._results[lastIndex] = instance =>
        {
            var failure = lastRule(instance);
            if (failure is not null)
            {
                return new ValidationFailure(failure.PropertyName, failure.ErrorCode, message);
            }
            return null;
        };

        return builder;
    }
}

public sealed class ProductCreateValidator : AbstractValidator<CreateProductDto>
{
    public ProductCreateValidator()
    {
        RuleFor(p => p.Name).NotEmpty().WithMessage("This my custom name error");
        RuleFor(p => p.Price).GreaterThan(10).WithMessage("This my custom price error");
    }
}

public sealed class ProductCreateValidator2 : AbstractValidator<CreateProductDto>
{
    public ProductCreateValidator2()
    {
        RuleFor(p => p.Name).NotEmpty().WithMessage("This my custom name error");
        RuleFor(p => p.Price).GreaterThan(10).WithMessage("This my custom price error");
    }
}

public sealed class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<ValidationFailure> Errors { get; set; } = new();
}

public sealed record ValidationFailure(
    string PropertyName,
    string ErrorCode,
    string ErrorMessage);