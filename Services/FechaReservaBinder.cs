using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Inmobiliaria.Services;

public class FechaReservaBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var valor = context.ValueProvider.GetValue(context.ModelName);
        if (valor == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, valor);
        var texto = valor.FirstValue?.Trim();
        if (string.IsNullOrEmpty(texto) && context.ModelType == typeof(DateTime?))
            context.Result = ModelBindingResult.Success(null);
        else if (DateTime.TryParseExact(texto, new[] { "dd/MM/yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha) && fecha.Year >= 1000)
            context.Result = ModelBindingResult.Success(fecha);
        else
            context.ModelState.TryAddModelError(context.ModelName, "Ingrese una fecha válida en formato dd/mm/aaaa.");
        return Task.CompletedTask;
    }
}
