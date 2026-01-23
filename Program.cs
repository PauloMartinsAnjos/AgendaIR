using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using AgendaIR.Data;
using AgendaIR.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURAÇÃO DO BANCO DE DADOS =====
// Adiciona o Entity Framework com PostgreSQL
// A connection string vem do appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== CONFIGURAÇÃO DE AUTENTICAÇÃO =====
// Configurar autenticação por cookies
// Isso permite que tanto magic link quanto login tradicional funcionem
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Página de login
        options.LogoutPath = "/Auth/Logout"; // Página de logout
        options.AccessDeniedPath = "/Auth/AccessDenied"; // Página de acesso negado
        options.ExpireTimeSpan = TimeSpan.FromHours(12); // Cookie expira em 12 horas
        options.SlidingExpiration = true; // Renova o cookie se usado
    });

// ===== CONFIGURAÇÃO DE AUTORIZAÇÃO =====
builder.Services.AddAuthorization();

// ===== ADICIONAR HttpContextAccessor =====
builder.Services.AddHttpContextAccessor();

// ===== REGISTRO DE SERVIÇOS =====
// Registrar nossos serviços customizados para injeção de dependência
builder.Services.AddScoped<MagicLinkService>();
builder.Services.AddScoped<FileUploadService>();
builder.Services.AddScoped<GoogleCalendarService>();

// ===== CONFIGURAÇÃO DE SESSÃO =====
// Adicionar suporte a sessões para armazenar dados temporários do usuário
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== ADICIONAR MVC =====
builder.Services.AddControllersWithViews();

// ===== ADICIONAR HttpContextAccessor =====
// Permite acessar o contexto HTTP em qualquer lugar da aplicação
// ===== ADICIONAR MVC COM CONFIGURAÇÕES DE UPLOAD =====
builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        // Aumentar limite de upload para 50MB
        options.MaxModelBindingCollectionSize = 1024;
    });

// ===== CONFIGURAR LIMITES DE UPLOAD =====
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB em bytes
    options.ValueLengthLimit = 52428800;
    options.MultipartHeadersLengthLimit = 52428800;
});

var app = builder.Build();

// ===== APLICAR MIGRATIONS AUTOMATICAMENTE =====
// Cria o banco de dados e aplica migrations na primeira execução
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Aplica migrations pendentes
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao aplicar migrations do banco de dados");
    }
}

// ===== CONFIGURAR PIPELINE DE REQUISIÇÕES =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Servir arquivos estáticos (CSS, JS, imagens, uploads)
app.UseStaticFiles();

// Habilitar roteamento
app.UseRouting();

// Habilitar sessão
app.UseSession();

// Habilitar autenticação e autorização
// A ordem é importante: Authentication ANTES de Authorization
app.UseAuthentication();
app.UseAuthorization();

// ===== CONFIGURAR ROTAS =====
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
