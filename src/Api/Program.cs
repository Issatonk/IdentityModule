var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityModule(builder.Configuration);

builder.Services.AddControllers();
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseIdentityModule();

app.Run();
