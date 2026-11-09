/* Dados de exemplo (opcional). Rode após script.sql. */
USE ControleEstoqueDB;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Produtos)
BEGIN
    INSERT INTO dbo.Produtos (Nome, QuantidadeTotal) VALUES
      ('Notebook Pro 14', 30), ('Monitor 27"', 13), ('Teclado Mecânico', 80), ('Headset USB', 5);
    INSERT INTO dbo.Movimentacoes (ProdutoId, Tipo, Quantidade, Referencia)
      SELECT Id, 'Entrada', QuantidadeTotal, 'Carga inicial' FROM dbo.Produtos;
    INSERT INTO dbo.Reservas (ProdutoId, SolicitacaoId, Quantidade) VALUES (1,'PED-1001',10),(2,'PED-1002',2);
    INSERT INTO dbo.Bloqueios (ProdutoId, Quantidade, Motivo) VALUES (1,10,'Lote em inspeção'),(2,3,'Avaria no transporte');
END
GO
