using System.Text;
using api_preven_email_service.DAO;
using api_preven_email_service.DAO.SysParametro;
using api_preven_email_service.Helper;
using api_preven_email_service.Model;
using api_preven_email_service.Model.SysParametro;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ** Configurar Serilog antes de construir la app **
Serilog.Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.File("logs/api-log.txt", 
        rollingInterval: RollingInterval.Day, 
        fileSizeLimitBytes: 5 * 1024 * 1024, 
        rollOnFileSizeLimit: true, 
        retainedFileCountLimit: 10, 
        shared: true, 
        flushToDiskInterval: TimeSpan.FromSeconds(1),
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"))
    .CreateLogger();

// Reemplazar el proveedor de logs con Serilog
builder.Host.UseSerilog();

var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");
if (connectionString != null)
{
    var postreSQLConnectionConfiguration = new PostgreSQLConfiguracion(connectionString);
    builder.Services.AddSingleton(postreSQLConnectionConfiguration);
}

builder.Services.AddScoped<PostgreSQLInterface, PostgreSQLConection>();
builder.Services.AddScoped<SysParametroDAO>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ** Registrar la clase Log en el contenedor de dependencias con Serilog **
builder.Services.AddSingleton<LoggerService>(new LoggerService(Serilog.Log.Logger));

if (connectionString == null) connectionString = "";
string key = await KeyConsulta(connectionString, builder.Services);

builder.Services.AddAuthentication("Bearer").AddJwtBearer(opt =>
{
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature);

    opt.RequireHttpsMetadata = false;
    opt.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateAudience = false,
        ValidateIssuer = false,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ** Configuración de CORS **
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyOrigin() // Permite cualquier origen (IP)
               .AllowAnyMethod() // Permite cualquier método (GET, POST, PUT, DELETE, etc.)
               .AllowAnyHeader(); // Permite cualquier encabezado
    });
});

// Configuración de Kestrel para escuchar en un puerto específico (5023)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5023); // Escucha en el puerto 5023
});

var app = builder.Build();

// Middleware para registrar solicitudes con Serilog
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthorization();
app.MapControllers();

// ** Asegurar que Serilog se cierre correctamente al apagar la app **
try
{
    Log.Information("Iniciando la aplicación...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló al iniciar.");
}
finally
{
    Log.Information("Cerrando la aplicación...");
    Log.CloseAndFlush(); 
}


async Task<string> KeyConsulta(string connectionString, IServiceCollection services)
{
    #pragma warning disable ASP0000
    var serviceProvider = services.BuildServiceProvider();
    #pragma warning restore ASP0000

    var sysParametroDAO = serviceProvider.GetRequiredService<SysParametroDAO>();
    await using var session = new NpgsqlConnection(connectionString);
    await session.OpenAsync();

    ResponseGetModel vRespuesta = await sysParametroDAO.SysParametroConsulta(uuid: Guid.NewGuid(), id_sys_parametro: 1, session: session);
    SysParametroModel vResultado = (SysParametroModel)vRespuesta.entidad!;
    return vResultado!.valor;
}
