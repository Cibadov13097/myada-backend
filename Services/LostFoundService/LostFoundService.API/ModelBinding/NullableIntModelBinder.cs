using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LostFoundService.Api.ModelBinding
{
    /// <summary>
    /// Binds empty string or "null" to null for int? (e.g. form field LocationId=).
    /// </summary>
    public class NullableIntModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(int?))
            {
                return Task.CompletedTask;
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            if (int.TryParse(value, out var parsed))
            {
                bindingContext.Result = ModelBindingResult.Success(parsed);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"The value '{value}' is not valid for {bindingContext.ModelName}.");
            return Task.CompletedTask;
        }
    }
}
