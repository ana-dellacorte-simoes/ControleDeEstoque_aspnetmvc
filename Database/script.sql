
IF DB_ID('ControleEstoqueDB') IS NULL CREATE DATABASE ControleEstoqueDB;
GO
USE ControleEstoqueDB;
GO

IF OBJECT_ID('dbo.Produtos','U') IS NULL
CREATE TABLE dbo.Produtos (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Nome            VARCHAR(100) NOT NULL,
    QuantidadeTotal INT NOT NULL DEFAULT 0,
    Ativo           BIT NOT NULL DEFAULT 1,
    CONSTRAINT CK_Produto_Quantidade CHECK (QuantidadeTotal >= 0)
);
GO

IF OBJECT_ID('dbo.Reservas','U') IS NULL
CREATE TABLE dbo.Reservas (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    ProdutoId     INT NOT NULL,
    SolicitacaoId VARCHAR(50) NOT NULL,
    Quantidade    INT NOT NULL,
    DataCriacao   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Ativa         BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Reserva_Produto FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos(Id),
    CONSTRAINT CK_Reserva_Quantidade CHECK (Quantidade > 0)
);
GO

IF OBJECT_ID('dbo.Bloqueios','U') IS NULL
CREATE TABLE dbo.Bloqueios (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ProdutoId   INT NOT NULL,
    Quantidade  INT NOT NULL,
    Motivo      VARCHAR(200) NOT NULL DEFAULT '',
    DataCriacao DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Ativo       BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Bloqueio_Produto FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos(Id),
    CONSTRAINT CK_Bloqueio_Quantidade CHECK (Quantidade > 0)
);
GO

/* Histórico de liberações (reserva/bloqueio nunca é apagado) */
IF COL_LENGTH('dbo.Reservas','Desfecho') IS NULL
    ALTER TABLE dbo.Reservas ADD Desfecho VARCHAR(20) NULL, DataLiberacao DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.Bloqueios','Desfecho') IS NULL
    ALTER TABLE dbo.Bloqueios ADD Desfecho VARCHAR(20) NULL, DataLiberacao DATETIME2 NULL;
GO

/* Trilha de movimentações (auditoria / "relatório de movimentação") */
IF OBJECT_ID('dbo.Movimentacoes','U') IS NULL
CREATE TABLE dbo.Movimentacoes (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    ProdutoId   INT NOT NULL,
    Tipo        VARCHAR(30) NOT NULL,
    Quantidade  INT NOT NULL,
    Referencia  VARCHAR(150) NULL,
    DataCriacao DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Mov_Produto FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos(Id),
    CONSTRAINT CK_Mov_Quantidade CHECK (Quantidade > 0)
);
GO

/* Índices */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Produtos_Nome')
    CREATE UNIQUE INDEX UX_Produtos_Nome ON dbo.Produtos(Nome);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Reservas_Produto_Ativa')
    CREATE INDEX IX_Reservas_Produto_Ativa ON dbo.Reservas(ProdutoId, Ativa) INCLUDE (Quantidade);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Bloqueios_Produto_Ativo')
    CREATE INDEX IX_Bloqueios_Produto_Ativo ON dbo.Bloqueios(ProdutoId, Ativo) INCLUDE (Quantidade);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Mov_Data')
    CREATE INDEX IX_Mov_Data ON dbo.Movimentacoes(Id DESC);
GO
