using Microsoft.EntityFrameworkCore;
using AgendaIR.Models;

namespace AgendaIR.Data
{
    /// <summary>
    /// ApplicationDbContext é a classe que representa o banco de dados
    /// Ela herda de DbContext, que é a classe base do Entity Framework
    /// Aqui definimos todas as tabelas (DbSet) do nosso banco de dados
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        // Construtor que recebe as opções de configuração
        // Isso permite configurar a connection string e outras opções
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet representa uma tabela no banco de dados
        // Cada DbSet permite fazer operações CRUD (Create, Read, Update, Delete)
        
        /// <summary>
        /// Tabela de Funcionários
        /// </summary>
        public DbSet<Funcionario> Funcionarios { get; set; }

        /// <summary>
        /// Tabela de Clientes
        /// </summary>
        public DbSet<Cliente> Clientes { get; set; }

        /// <summary>
        /// Tabela de Documentos Solicitados (globais)
        /// </summary>
        public DbSet<DocumentoSolicitado> DocumentosSolicitados { get; set; }

        /// <summary>
        /// Tabela de Agendamentos
        /// </summary>
        public DbSet<Agendamento> Agendamentos { get; set; }

        /// <summary>
        /// Tabela de Documentos Anexados
        /// </summary>
        public DbSet<DocumentoAnexado> DocumentosAnexados { get; set; }

        /// <summary>
        /// Tabela de Tipos de Agendamento
        /// </summary>
        public DbSet<TipoAgendamento> TiposAgendamento { get; set; }

        /// <summary>
        /// Tabela de Participantes de Agendamento
        /// </summary>
        public DbSet<AgendamentoParticipante> AgendamentoParticipantes { get; set; }

        /// <summary>
        /// OnModelCreating é chamado quando o Entity Framework está criando o modelo do banco
        /// Aqui podemos configurar relacionamentos, índices, restrições, etc.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar todas as propriedades DateTime para usar timestamp WITHOUT time zone
            // Isso evita conversão automática de timezone pelo PostgreSQL
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetColumnType("timestamp without time zone");
                    }
                }
            }

            // Configurar índice único para Username de Funcionário
            // Isso garante que não existam dois funcionários com o mesmo username
            modelBuilder.Entity<Funcionario>()
                .HasIndex(f => f.Username)
                .IsUnique();

            // Configurar índice único para MagicToken de Cliente
            // Cada cliente deve ter um token único
            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.MagicToken)
                .IsUnique();

            // Configurar relacionamento Cliente -> Funcionário
            // Um funcionário pode ter vários clientes
            // Um cliente pertence a apenas um funcionário
            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Funcionario)
                .WithMany(f => f.Clientes)
                .HasForeignKey(c => c.FuncionarioId)
                .OnDelete(DeleteBehavior.Restrict); // Não permite deletar funcionário se tiver clientes

            // Configurar relacionamento Agendamento -> Cliente
            modelBuilder.Entity<Agendamento>()
                .HasOne(a => a.Cliente)
                .WithMany(c => c.Agendamentos)
                .HasForeignKey(a => a.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar relacionamento Agendamento -> Funcionário
            modelBuilder.Entity<Agendamento>()
                .HasOne(a => a.Funcionario)
                .WithMany(f => f.Agendamentos)
                .HasForeignKey(a => a.FuncionarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar relacionamento DocumentoAnexado -> Agendamento
            modelBuilder.Entity<DocumentoAnexado>()
                .HasOne(da => da.Agendamento)
                .WithMany(a => a.DocumentosAnexados)
                .HasForeignKey(da => da.AgendamentoId)
                .OnDelete(DeleteBehavior.Cascade); // Se deletar agendamento, deleta documentos anexados

            // Configurar relacionamento DocumentoAnexado -> DocumentoSolicitado
            modelBuilder.Entity<DocumentoAnexado>()
                .HasOne(da => da.DocumentoSolicitado)
                .WithMany(ds => ds.DocumentosAnexados)
                .HasForeignKey(da => da.DocumentoSolicitadoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar relacionamento TipoAgendamento -> DocumentosSolicitado
            modelBuilder.Entity<DocumentoSolicitado>()
                .HasOne(d => d.TipoAgendamento)
                .WithMany(t => t.DocumentosSolicitados)
                .HasForeignKey(d => d.TipoAgendamentoId)
                .OnDelete(DeleteBehavior.SetNull); // Se tipo for deletado, documento vira genérico

            // Configurar relacionamento TipoAgendamento -> Agendamento
            modelBuilder.Entity<Agendamento>()
                .HasOne(a => a.TipoAgendamento)
                .WithMany(t => t.Agendamentos)
                .HasForeignKey(a => a.TipoAgendamentoId)
                .OnDelete(DeleteBehavior.Restrict); // Não pode deletar tipo se houver agendamentos

            // Seed data: Criar usuário administrador inicial
            // Este é o primeiro usuário que pode acessar o sistema
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Método que insere dados iniciais no banco de dados
        /// Cria o usuário administrador padrão
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Criar senha hash para o admin
            // A senha será "Admin@123"
            string senhaHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");

            // Inserir funcionário administrador
            modelBuilder.Entity<Funcionario>().HasData(
                new Funcionario
                {
                    Id = 1,
                    Nome = "Administrador do Sistema",
                    Email = "admin@agendair.com",
                    Username = "admin",
                    SenhaHash = senhaHash,
                    CPF = "000.000.000-00",
                    IsAdmin = true,
                    Ativo = true,
                    DataCriacao = new DateTime(2026, 1, 1)
                }
            );

            // Inserir alguns documentos solicitados padrão
            modelBuilder.Entity<DocumentoSolicitado>().HasData(
                new DocumentoSolicitado
                {
                    Id = 1,
                    Nome = "RG (Frente e Verso)",
                    Descricao = "Envie o RG frente e verso em um único arquivo PDF ou imagem",
                    Obrigatorio = true,
                    Ativo = true,
                    DataCriacao = new DateTime(2026, 1, 1)
                },
                new DocumentoSolicitado
                {
                    Id = 2,
                    Nome = "CPF",
                    Descricao = "Envie uma cópia do CPF",
                    Obrigatorio = true,
                    Ativo = true,
                    DataCriacao = new DateTime(2026, 1, 1)
                },
                new DocumentoSolicitado
                {
                    Id = 3,
                    Nome = "Comprovante de Residência",
                    Descricao = "Conta de luz, água ou telefone dos últimos 3 meses",
                    Obrigatorio = true,
                    Ativo = true,
                    DataCriacao = new DateTime(2026, 1, 1)
                },
                new DocumentoSolicitado
                {
                    Id = 4,
                    Nome = "Informe de Rendimentos",
                    Descricao = "Informe de rendimentos do ano anterior",
                    Obrigatorio = false,
                    Ativo = true,
                    DataCriacao = new DateTime(2026, 1, 1)
                }
            );

            // Seed de tipos de agendamento
            modelBuilder.Entity<TipoAgendamento>().HasData(
                new TipoAgendamento { Id = 1, Nome = "Declaração IR", Descricao = "Declaração de Imposto de Renda", Ativo = true, DataCriacao = new DateTime(2026, 1, 1) },
                new TipoAgendamento { Id = 2, Nome = "Declaração IR Retificadora", Descricao = "Retificação de declaração de IR", Ativo = true, DataCriacao = new DateTime(2026, 1, 1) },
                new TipoAgendamento { Id = 3, Nome = "Consultoria Tributária", Descricao = "Consultoria sobre questões tributárias", Ativo = true, DataCriacao = new DateTime(2026, 1, 1) },
                new TipoAgendamento { Id = 4, Nome = "Abertura de MEI", Descricao = "Abertura de Microempreendedor Individual", Ativo = true, DataCriacao = new DateTime(2026, 1, 1) },
                new TipoAgendamento { Id = 5, Nome = "Contabilidade Mensal", Descricao = "Serviços contábeis mensais", Ativo = true, DataCriacao = new DateTime(2026, 1, 1) },
                new TipoAgendamento { Id = 6, Nome = "Regularização Fiscal", Descricao = "Regularização de pendências fiscais", Ativo = true, DataCriacao = new DateTime(2026, 1, 1) },
                new TipoAgendamento { Id = 7, Nome = "Planejamento Tributário", Descricao = "Planejamento estratégico tributário", Ativo = true, DataCriacao = new DateTime(2026, 1, 1) },
                new TipoAgendamento { Id = 8, Nome = "Outros", Descricao = "Outros serviços", Ativo = true, DataCriacao = new DateTime(2026, 1, 1) }
            );
        }
    }
}
