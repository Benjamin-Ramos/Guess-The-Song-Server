using WebApplicationServidorAdivinaCancion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("ngrok-skip-browser-warning", "true");
    await next();
});

app.UseRouting();

app.UseCors("PermitirTodo");

app.MapHub<GameHub>("/gamehub");

app.MapGet("/", () => "Servidor de Adivina la Canción funcionando correctamente en MonsterASP");

app.Run();