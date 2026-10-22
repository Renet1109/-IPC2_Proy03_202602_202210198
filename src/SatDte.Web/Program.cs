using Microsoft.AspNetCore.Http.Features;
using SatDte.Web.Servicios;

var builder = WebApplication.CreateBuilder(args);
// Cada pestaña envía únicamente sus filtros. La API valida los campos de la petición elegida.
builder.Services.AddRazorPages().AddMvcOptions(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 110 * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 110 * 1024 * 1024);
builder.Services.AddHttpClient<ClienteApi>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5001/");
    cliente.Timeout = TimeSpan.FromSeconds(120);
});
var app = builder.Build();
app.UseExceptionHandler("/Error");
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();
