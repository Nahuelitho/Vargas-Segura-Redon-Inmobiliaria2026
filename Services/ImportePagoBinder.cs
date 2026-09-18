using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inmobiliaria.Services;

// Los controles HTML number envían punto decimal, incluso con Windows en español.
public class ImportePagoBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var valor = context.ValueProvider.GetValue(context.ModelName);
        if (valor == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, valor);
        var texto = valor.FirstValue?.Trim();
        if (string.IsNullOrEmpty(texto)) context.Result = ModelBindingResult.Success(null);
        else if (decimal.TryParse(texto.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var importe))
            context.Result = ModelBindingResult.Success(importe);
        else context.ModelState.TryAddModelError(context.ModelName, "Ingrese un importe válido sin separadores de miles.");
        return Task.CompletedTask;
    }
}
