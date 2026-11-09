using Microsoft.Data.SqlClient;

namespace ControleEstoque.DAO
{
    public class ConexaoBD
    {
        private readonly string connectionString;

        public ConexaoBD(IConfiguration configuracao)
        {
            // Em produção: variável de ambiente ConnectionStrings__DefaultConnection, user-secrets ou cofre. Nunca versionar senhas.
            connectionString = configuracao.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("String de conexão não encontrada.");
        }

        public SqlConnection CriarConexao() => new SqlConnection(connectionString);
    }
}
