using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public sealed class FlexibleDecimalBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var val = context.ValueProvider.GetValue(context.ModelName);
        if (val == ValueProviderResult.None)
            return Task.CompletedTask;

        var raw = val.FirstValue?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return Task.CompletedTask;

        // həm "0,01", həm "0.01" qəbul et
        raw = raw.Replace(',', '.');

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
        {
            context.Result = ModelBindingResult.Success(d);
            return Task.CompletedTask;
        }

        context.ModelState.TryAddModelError(context.ModelName, "Invalid decimal value.");
        return Task.CompletedTask;
    }
}