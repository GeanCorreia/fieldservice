namespace FieldService.Superset.Interfaces;

internal interface SupersetProvisioningService
{
// {
//     [ SupersetProvisioningOrchestrator ]
//     │
//     ├──► [ SupersetDatabaseService ]
//     ├──► [ SupersetDatasetService ]
//     ├──► [ SupersetSecurityService ]
//     └──► [ SupersetDashboardService ]
//     Responsabilidade de Cada Serviço
//     SupersetDatabaseService
//
//         Foco exclusivo: Conexões de banco de dados (Database Connections).
//
//     Responsabilidades: Cadastrar, atualizar ou verificar a saúde da conexão do PostgreSQL principal/réplica no Superset.
//
//         SupersetDatasetService
//
//         Foco exclusivo: Fontes de dados e metadados de tabelas/views (Datasets & Virtual Tables).
//
//     Responsabilidades: Criar datasets virtuais via SQL, definir apelidos amigáveis para colunas, marcar campos ocultos/sensíveis e cadastrar métricas calculadas (SUM, AVG).
//
//     SupersetSecurityService
//
//         Foco exclusivo: Papéis, usuários internos e regras de acesso (Roles & Permissions).
//
//     Responsabilidades: Criar e sincronizar Roles customizadas (ex: Tenant_SelfService_Role) e vincular quais Datasets cada papel pode visualizar ou explorar.
//
//         SupersetDashboardService
//
//     Foco exclusivo: Layouts, painéis e componentes visuais (Dashboards & Charts).
//
//     Responsabilidades: Exportar/importar dashboards via JSON/ZIP programaticamente e clonar templates de dashboards padrão para novos tenants.
//
//         SupersetProvisioningOrchestrator
//
//         Foco exclusivo: Orquestração do fluxo de provisionamento (Workflow Orchestration).
//
//     Responsabilidades: Executar os serviços na ordem correta durante a inicialização do app (IHostedService) ou ao criar um novo módulo no sistema (ex: 1. Valida Banco -> 2. Cria Datasets -> 3. Sincroniza Permissões).
}