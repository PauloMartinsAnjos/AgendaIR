# AgendaIR - Sistema de Agendamento de Declaração de IR

Sistema completo de agendamento para serviços de Imposto de Renda desenvolvido em **ASP.NET Core MVC** com integração ao Google Calendar e autenticação via magic link para clientes.

## 📋 Índice

- [Visão Geral](#visão-geral)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Pré-requisitos](#pré-requisitos)
- [Instalação e Configuração](#instalação-e-configuração)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Como Usar o Sistema](#como-usar-o-sistema)
- [Integração Google Calendar](#integração-google-calendar)
- [Segurança](#segurança)
- [Troubleshooting](#troubleshooting)

---

## 🎯 Visão Geral

O **AgendaIR** é um sistema web completo que permite:

- ✅ **3 níveis de autenticação**: Cliente (magic link), Funcionário (usuário/senha), Administrador (usuário/senha)
- ✅ **Agendamento inteligente**: Validação de disponibilidade com Google Calendar
- ✅ **Upload de documentos**: Sistema seguro para envio de RG, CPF, comprovantes, etc.
- ✅ **Gestão completa**: CRUD de funcionários, clientes, documentos e agendamentos
- ✅ **Interface amigável**: Bootstrap 5 responsivo com Portuguese UI

---

## 💻 Tecnologias Utilizadas

- **Framework**: ASP.NET Core 8.0 MVC
- **Banco de Dados**: PostgreSQL
- **ORM**: Entity Framework Core
- **Autenticação**: Cookie Authentication
- **Senha Hash**: BCrypt.Net-Next
- **Frontend**: Bootstrap 5, jQuery
- **Integração**: Google Calendar API v3

### Pacotes NuGet Principais

```xml
- Npgsql.EntityFrameworkCore.PostgreSQL (8.0.0)
- Microsoft.EntityFrameworkCore.Design (8.0.0)
- BCrypt.Net-Next (4.0.3)
- Google.Apis.Calendar.v3 (1.68.0.3400)
```

---

## 📦 Pré-requisitos

Antes de começar, certifique-se de ter instalado:

### 1. .NET 8 SDK

**Windows:**
- Baixe em: https://dotnet.microsoft.com/download/dotnet/8.0
- Execute o instalador e siga as instruções

**Linux (Ubuntu/Debian):**
```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

**Verificar instalação:**
```bash
dotnet --version
# Deve mostrar: 8.0.x
```

### 2. PostgreSQL

O banco de dados já está configurado. **Não é necessário instalar localmente**.

**Connection String configurada:**
```
Host=200.162.138.26;Port=5020;Database=AgendaIr;Username=postgres;Password=#Rir@dm$;Pooling=true;
```

### 3. Visual Studio Code (Recomendado)

- Baixe em: https://code.visualstudio.com/
- Instale a extensão **C# Dev Kit**

**OU**

### 3. Visual Studio 2022

- Baixe em: https://visualstudio.microsoft.com/
- Selecione o workload "ASP.NET and web development"

### 4. Git

- Download: https://git-scm.com/downloads

---

## 🚀 Instalação e Configuração

Siga estes passos **exatamente** na ordem para configurar o projeto:

### Passo 1: Clonar o Repositório

Abra o terminal/prompt de comando e execute:

```bash
git clone https://github.com/PauloMartinsAnjos/AgendaIR.git
cd AgendaIR
```

### Passo 2: Restaurar Pacotes

```bash
dotnet restore
```

> **O que isso faz?** Baixa todas as bibliotecas necessárias do NuGet

### Passo 3: Aplicar Migrations (Criar Banco de Dados)

```bash
dotnet ef database update
```

> **O que isso faz?** Cria todas as tabelas no PostgreSQL e insere dados iniciais (usuário admin e documentos padrão)

**⚠️ Importante:** Se o comando `dotnet ef` não for reconhecido, instale a ferramenta:

```bash
dotnet tool install --global dotnet-ef
```

### Passo 4: Compilar o Projeto

```bash
dotnet build
```

Você deve ver: `Build succeeded.`

### Passo 5: Executar o Projeto

```bash
dotnet run
```

Você verá algo como:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Passo 6: Acessar o Sistema

Abra seu navegador e acesse:

```
http://localhost:5000
```

### Passo 7: Primeiro Login (Administrador)

Use as credenciais padrão criadas automaticamente:

- **Usuário:** `admin`
- **Senha:** `Admin@123`

> **⚠️ IMPORTANTE:** Após o primeiro login, recomenda-se criar um novo administrador e deletar ou alterar a senha do admin padrão.

---

## 📁 Estrutura do Projeto

```
AgendaIR/
├── Controllers/              # Controladores MVC
│   ├── AuthController.cs     # Autenticação (magic link + login)
│   ├── FuncionariosController.cs  # CRUD de funcionários
│   ├── ClientesController.cs      # CRUD de clientes
│   ├── DocumentosController.cs    # CRUD de documentos
│   ├── AgendamentosController.cs  # Sistema de agendamento
│   └── HomeController.cs          # Página inicial
├── Models/                   # Modelos de dados
│   ├── Funcionario.cs
│   ├── Cliente.cs
│   ├── DocumentoSolicitado.cs
│   ├── Agendamento.cs
│   ├── DocumentoAnexado.cs
│   └── ViewModels/          # Modelos para views
├── Views/                   # Views Razor
│   ├── Auth/               # Login e autenticação
│   ├── Funcionarios/       # CRUD funcionários
│   ├── Clientes/           # CRUD clientes
│   ├── Documentos/         # CRUD documentos
│   ├── Agendamentos/       # Agendamentos
│   ├── Home/               # Página inicial
│   └── Shared/             # Layout e componentes
├── Services/               # Serviços
│   ├── MagicLinkService.cs      # Geração de tokens
│   ├── FileUploadService.cs     # Upload de arquivos
│   └── GoogleCalendarService.cs # Integração Google
├── Data/                   # Banco de dados
│   └── ApplicationDbContext.cs
├── wwwroot/               # Arquivos estáticos
│   ├── css/
│   ├── js/
│   ├── lib/              # Bootstrap, jQuery
│   └── uploads/          # Documentos enviados
├── Migrations/           # Migrations EF Core
├── appsettings.json     # Configurações
└── Program.cs           # Configuração da aplicação
```

---

## 📖 Como Usar o Sistema

### 👨‍💼 Para Administradores

#### 1. Fazer Login

1. Acesse: `http://localhost:5000/Auth/Login`
2. Use: `admin` / `Admin@123`
3. Você será redirecionado para a página de agendamentos

#### 2. Cadastrar um Funcionário

1. No menu, clique em **"Funcionários"** → **"Novo Funcionário"**
2. Preencha o formulário:
   - **Nome**: Nome completo
   - **Email**: Email do funcionário
   - **Username**: Usuário para login (ex: `joao.silva`)
   - **Senha**: Senha forte (mín. 6 caracteres)
   - **CPF**: CPF no formato 000.000.000-00
   - **Email Google Calendar**: (Opcional) Email da conta Google
   - **É Administrador?**: Marque se for admin
3. Clique em **"Salvar"**

> **Dica:** O CPF é formatado automaticamente enquanto você digita!

#### 3. Cadastrar um Cliente

1. No menu, clique em **"Clientes"** → **"Novo Cliente"**
2. Preencha o formulário:
   - **Nome**: Nome completo do cliente
   - **Email**: Email do cliente
   - **Telefone**: Telefone/WhatsApp (ex: (11) 98888-7777)
   - **CPF**: CPF do cliente
   - **Funcionário Responsável**: Selecione o funcionário
3. Clique em **"Salvar"**
4. **IMPORTANTE:** Após salvar, você verá uma tela com:
   - ✅ Magic Link gerado
   - 📋 Botão para copiar o link
   - 📱 Botão para compartilhar via WhatsApp

**Exemplo de Magic Link:**
```
http://localhost:5000/Auth/LoginMagic?token=a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
```

#### 4. Enviar Magic Link ao Cliente

**Opção 1: Copiar e Colar**
1. Clique em "Copiar Link"
2. Abra o WhatsApp Web
3. Cole e envie para o cliente

**Opção 2: WhatsApp Direto (Mobile)**
1. Clique em "Compartilhar via WhatsApp"
2. Selecione o contato
3. Envie

#### 5. Gerenciar Documentos Solicitados

1. Menu **"Documentos"** → **"Listar Documentos"**
2. Para adicionar novo documento:
   - Clique em **"Novo Documento"**
   - Preencha nome e descrição
   - Marque se é obrigatório
   - Clique em **"Salvar"**

**Documentos Padrão Pré-cadastrados:**
- ✅ RG (Frente e Verso) - *Obrigatório*
- ✅ CPF - *Obrigatório*
- ✅ Comprovante de Residência - *Obrigatório*
- 📄 Informe de Rendimentos - *Opcional*

#### 6. Visualizar Todos os Agendamentos

1. Menu **"Agendamentos"**
2. Use os filtros:
   - Por funcionário
   - Por status (Pendente, Confirmado, Concluído, Cancelado)
   - Por período de datas
3. Clique em um agendamento para ver detalhes

---

### 👨‍💻 Para Funcionários

#### 1. Fazer Login

1. Acesse: `http://localhost:5000/Auth/Login`
2. Use seu **username** e **senha** fornecidos pelo admin
3. Você será redirecionado para seus agendamentos

#### 2. Cadastrar um Cliente

**Como funcionário, você só pode cadastrar clientes para VOCÊ mesmo**

1. Menu **"Clientes"** → **"Novo Cliente"**
2. Preencha os dados do cliente
3. O campo "Funcionário Responsável" já vem preenchido com SEU nome (não editável)
4. Clique em **"Salvar"**
5. Copie o magic link e envie via WhatsApp

#### 3. Visualizar Seus Agendamentos

1. Menu **"Agendamentos"**
2. Você verá **APENAS** os agendamentos dos seus clientes
3. Use filtros por status e data

#### 4. Gerenciar um Agendamento

1. Na lista, clique em **"Detalhes"** no agendamento
2. Você pode:
   - Ver dados do cliente
   - Baixar documentos anexados
   - Alterar status (Pendente → Confirmado → Concluído)
   - Adicionar observações
   - Cancelar o agendamento

#### 5. Baixar Documentos do Cliente

1. Em **"Detalhes"** do agendamento
2. Na seção "Documentos Anexados"
3. Clique no ícone de download de cada documento

---

### 👤 Para Clientes

#### 1. Acessar o Sistema (Via Magic Link)

1. Receba o link via WhatsApp do seu funcionário
2. Clique no link
3. Você será **automaticamente logado** no sistema
4. Será redirecionado para "Meus Agendamentos"

> **⚠️ Importante:** Não compartilhe seu magic link! Ele é pessoal e dá acesso à sua conta.

#### 2. Fazer um Novo Agendamento

1. Clique em **"Novo Agendamento"**
2. Você verá:
   - Seu nome (não editável)
   - Funcionário responsável (não editável - já está atribuído)

3. **Selecione Data e Hora:**
   - Use o calendário para escolher o dia
   - Horários disponíveis: Segunda a Sexta, 8h às 18h
   - Slots de 1 hora
   - ❌ Horários já ocupados aparecem desabilitados

4. **Anexar Documentos:**
   - Você verá a lista de documentos necessários
   - Documentos **obrigatórios** têm uma tag vermelha
   - Clique em "Escolher arquivo" para cada documento
   - Tipos aceitos: **PDF, JPG, PNG**
   - Tamanho máximo: **10MB por arquivo**

5. **Validação:**
   - Você **só pode agendar** se anexar TODOS os documentos obrigatórios
   - O botão "Agendar" fica desabilitado até isso

6. Clique em **"Confirmar Agendamento"**

#### 3. Visualizar Seus Agendamentos

1. Menu **"Meus Agendamentos"**
2. Você verá todos os seus agendamentos com:
   - Data e hora
   - Status (Pendente, Confirmado, Concluído, Cancelado)
   - Funcionário responsável

#### 4. Cancelar um Agendamento

1. Em "Meus Agendamentos"
2. Clique em **"Cancelar"**
3. **Regra:** Você só pode cancelar se faltar **mais de 24 horas**
4. Confirme o cancelamento

---

## 📅 Integração Google Calendar

### Por Que Google Calendar?

A integração permite que:
- ✅ Agendamentos sejam criados automaticamente no calendário do funcionário
- ✅ Validação de disponibilidade antes de confirmar agendamento
- ✅ Atualizações em tempo real
- ✅ Lembretes automáticos por email

### ⚠️ Configuração Necessária

**IMPORTANTE:** A integração Google Calendar está **parcialmente implementada**. Para ativar completamente, siga:

#### Passo 1: Criar Projeto no Google Cloud Console

1. Acesse: https://console.cloud.google.com/
2. Clique em **"Novo Projeto"**
3. Nome: `AgendaIR`
4. Clique em **"Criar"**

#### Passo 2: Ativar Google Calendar API

1. No menu, vá em **"APIs e Serviços"** → **"Biblioteca"**
2. Busque por: `Google Calendar API`
3. Clique em **"Ativar"**

#### Passo 3: Criar Credenciais OAuth 2.0

1. Vá em **"APIs e Serviços"** → **"Credenciais"**
2. Clique em **"Criar Credenciais"** → **"ID do cliente OAuth 2.0"**
3. Tipo de aplicativo: **"Aplicativo da Web"**
4. Nome: `AgendaIR Web`
5. URIs de redirecionamento autorizados:
   ```
   http://localhost:5000/signin-google
   ```
6. Clique em **"Criar"**

#### Passo 4: Baixar Credenciais

1. Após criar, clique em **Download JSON**
2. Renomeie o arquivo para: `credentials.json`
3. Coloque na **raiz do projeto AgendaIR/**

#### Passo 5: Configurar appsettings.json

O arquivo já está configurado, mas verifique:

```json
{
  "GoogleCalendar": {
    "ApplicationName": "AgendaIR",
    "CredentialsPath": "credentials.json"
  }
}
```

#### Passo 6: Descomentar Código

No arquivo `/Services/GoogleCalendarService.cs`, você encontrará código comentado com:

```csharp
/* IMPLEMENTAÇÃO COMPLETA (comentada - requer configuração OAuth):
...
*/
```

**Descomente** essas seções após configurar o OAuth.

#### Passo 7: Testar

1. Reinicie a aplicação
2. Crie um agendamento
3. Na primeira vez, você será redirecionado para autorizar o acesso ao Google Calendar
4. Após autorizar, eventos serão criados automaticamente!

---

## 🔒 Segurança

### Senha Hash com BCrypt

Todas as senhas são armazenadas com **BCrypt hash**, nunca em texto puro.

```csharp
// Ao criar funcionário
string senhaHash = BCrypt.Net.BCrypt.HashPassword(senha);

// Ao fazer login
bool senhaCorreta = BCrypt.Net.BCrypt.Verify(senhaDigitada, senhaHashArmazenada);
```

### Magic Token

Os tokens são gerados com:
- **GUID (128 bits)** - Identificador único aleatório
- **Timestamp** - Momento exato da geração

Resultado: Token praticamente impossível de adivinhar.

### Proteção CSRF

Todos os formulários usam `@Html.AntiForgeryToken()` para prevenir ataques CSRF.

### Upload de Arquivos

Validações implementadas:
- ✅ Tamanho máximo: 10MB
- ✅ Tipos permitidos: .pdf, .jpg, .jpeg, .png
- ✅ Nome único gerado automaticamente
- ✅ Armazenamento organizado por agendamento

### Autorização por Nível

```csharp
// Verifica se usuário é admin
var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";

// Verifica tipo de usuário
var userType = User.FindFirst("UserType")?.Value;
// Valores: "Cliente", "Funcionario"
```

---

## 🐛 Troubleshooting

### Erro: "Unable to connect to database"

**Solução:**
1. Verifique se a connection string em `appsettings.json` está correta
2. Teste conectividade:
   ```bash
   ping 200.162.138.26
   ```
3. Verifique firewall/antivírus

### Erro: "dotnet ef command not found"

**Solução:**
```bash
dotnet tool install --global dotnet-ef
```

### Erro: "Build failed" com erros de NuGet

**Solução:**
```bash
dotnet clean
dotnet restore
dotnet build
```

### Erro: "Port 5000 already in use"

**Solução:**

**Windows:**
```bash
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

**Linux:**
```bash
lsof -i :5000
kill -9 <PID>
```

Ou mude a porta em `Properties/launchSettings.json`

### Erro: Upload de arquivo não funciona

**Solução:**
1. Verifique permissões da pasta `wwwroot/uploads/`
2. Tamanho do arquivo < 10MB
3. Extensão permitida (.pdf, .jpg, .jpeg, .png)

### Não consigo fazer login com admin/Admin@123

**Solução:**
1. Verifique se aplicou as migrations:
   ```bash
   dotnet ef database update
   ```
2. Verifique se o seed data foi criado (usuário admin deve existir no banco)

### Google Calendar não está funcionando

**Solução:**
1. Verifique se criou projeto no Google Cloud Console
2. Verifique se ativou Google Calendar API
3. Verifique se credentials.json está na raiz do projeto
4. Verifique se descomentou o código em GoogleCalendarService.cs

---

## 📞 Suporte

Se precisar de ajuda adicional:

1. **Issues GitHub**: https://github.com/PauloMartinsAnjos/AgendaIR/issues

---

## 🎓 Para Desenvolvedores Iniciantes

### Conceitos Importantes

#### O que é MVC?

**MVC** = Model-View-Controller

- **Model** (Models/): Representa os dados (tabelas do banco)
- **View** (Views/): É a interface (páginas HTML)
- **Controller** (Controllers/): Lógica que conecta Model e View

**Exemplo:**
```
Usuário clica em "Login" → 
AuthController.Login() (Controller) → 
Busca usuário no banco (Model) → 
Retorna página de sucesso (View)
```

#### O que é Entity Framework?

É um **ORM** (Object-Relational Mapping):
- Converte objetos C# em comandos SQL
- Você manipula objetos, não escreve SQL

```csharp
// Em vez de: SELECT * FROM Clientes WHERE Id = 1
var cliente = await _context.Clientes.FindAsync(1);
```

#### O que são Migrations?

São "versões" do banco de dados:
- Cada mudança nas Models gera uma migration
- `dotnet ef migrations add NomeDaMudanca` → Cria migration
- `dotnet ef database update` → Aplica no banco

#### O que é Dependency Injection?

É quando você **injeta** dependências em vez de criar:

```csharp
// ❌ Ruim - criando manualmente
var service = new MagicLinkService();

// ✅ Bom - injetando via construtor
public class AuthController : Controller
{
    private readonly MagicLinkService _service;
    
    public AuthController(MagicLinkService service)
    {
        _service = service; // Injetado automaticamente
    }
}
```

Configurado em `Program.cs`:
```csharp
builder.Services.AddScoped<MagicLinkService>();
```

### Próximos Passos

1. ✅ Execute o projeto e explore a interface
2. ✅ Crie um funcionário e um cliente
3. ✅ Teste o fluxo completo de agendamento
4. ✅ Leia o código dos Controllers com atenção aos comentários
5. ✅ Experimente modificar views (arquivos .cshtml)
6. ✅ Tente adicionar um novo campo em algum Model (vai precisar de migration!)

---

**Desenvolvido com ❤️ para facilitar agendamentos de declaração de IR**
