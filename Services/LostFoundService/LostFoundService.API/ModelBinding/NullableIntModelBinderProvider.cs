using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LostFoundService.Api.ModelBinding
{
    public class NullableIntModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(int?))
                return new NullableIntModelBinder();
            return null;
        }
    }
}
