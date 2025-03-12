var builder = WebApplication.CreateBuilder(args);

// ✅ Add services for Controllers
builder.Services.AddControllers();

var app = builder.Build();

// ✅ Enable Routing and Controllers
app.UseRouting();
app.UseAuthorization();
app.MapControllers(); 

app.Run();