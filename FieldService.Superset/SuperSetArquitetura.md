Guia de Reconfiguração do Container Superset e MCP

`FieldService.Cloud.Entities.Container` é a entidade concreta compartilhada,
com dados do provedor, status, configuração e janela de execução. O construtor
recebe `resourceName`, persistido em `Name`; não há herança nem `GetResourceName`.
Superset define seu prefixo em `SupersetTenant.ContainerResourceName(Guid)`.
Outras aplicações usam a mesma entidade e fornecem seus próprios nomes.

Cloud é dono da tabela `Container` e de suas gravações via `IContainerRepository`.
O contexto Superset mapeia essa tabela com `ExcludeFromMigrations()` para manter
`SupersetTenant.Container`, filtros e `Include`, mas recusa alterações de containers
em `SaveChanges`. Os dois contextos precisam apontar para o mesmo banco/schema.
A busca de provisionamento usa tenant e nome, permitindo diferentes aplicações
no mesmo tenant, inclusive antes de existir o registro de `SupersetTenant`.

Na atualização do banco, aplique primeiro as migrations de `CloudDbContext`
(incluindo `AddCloudContainer`) e depois as de `SupersetDbContext`
(incluindo `UseCloudContainer`), com os workers parados durante a transição.
A segunda migration copia os containers legados, preserva IDs e configuração,
preenche os nomes e troca a FK antes de remover a tabela antiga.
Para rollback, reverta Superset antes de Cloud. Migrations históricas continuam
referenciando a tabela antiga para preservar instalações existentes e bancos novos.

O provisionamento grava o container em Cloud antes do checkpoint em Superset.
Não há transação compartilhada entre os contextos: se o checkpoint falhar, o retry
reencontra o container por tenant/nome e reutiliza seu ID.

O core de infraestrutura é registrado por `AddCloudModule`: `IAzureContainerService`
compartilha o `ArmClient`, resolve subscription/resource group/environment por
`AzureIdentityOptions` e executa consulta, criação, atualização, resolução de FQDN
e leitura de status dos Container Apps. `IAzureContainerConfigurationService`
centraliza as regras HTTP/cron e a aplicação idempotente da escala.
O deployment e o job de atualização de Superset injetam esse serviço de Cloud
diretamente, passando `SupersetOptions.Provisioning.KedaCronTimezone`.
`IContainerProcessingLock` mantém os handles distribuídos em um singleton,
com chaves separadas por aplicação. O timeout de aquisição é um tempo de espera,
não uma expiração do lock; os handles devem ser liberados em `finally`, mesmo
quando a operação é cancelada.

Os serviços Superset existentes delegam a infraestrutura a esses contratos.
Continuam no Superset o prefixo dos recursos/locks, as opções de provisionamento,
a imagem, o bootstrap, as variáveis de ambiente, a obtenção de secrets e conexão
do banco do tenant, o cache/repositório e os checks de saúde/autenticação.
Cloud não depende de Superset. As regras e limites de escala existentes foram
preservados nesta extração.

No provisionamento do banco, `CreateSupersetDataInfraJobConsumer` é o dono do
lock do tenant até a persistência do checkpoint. `SupersetDataBaseService` não
readquire esse mesmo lock dentro da operação.

As origens do frontend são configuradas em `ApplicationAccount:AllowedFrameAncestors`,
compartilhadas entre os módulos via `ApplicationAccountOptions` do módulo Shared.
As origens CORS são validadas pelo
`HttpCoreMiddleware` no pipeline HTTP, inclusive para o proxy e o SignalR.
O container Superset mantém CORS desativado e configura apenas a incorporação
em iframe: a mesma lista alimenta `ALLOWED_FRAME_ANCESTORS`,
usado pelo CSP `frame-ancestors` junto com `'self'`, sem depender do módulo
`FieldService.Http`. Use origens HTTP/HTTPS exatas, sem caminhos ou curingas,
para que a lista também possa ser usada por CORS com credenciais.
Alterações na lista de iframe exigem reconfigurar os containers existentes.

Este documento estabelece o blueprint arquitetural e as instruções detalhadas para reconfigurar o container multi-tenant do Apache Superset, incorporando nativamente o servidor Superset MCP (Model Context Protocol) no mesmo ambiente de execução dentro do Azure Container Apps (ACA).

1. Visão Geral da Arquitetura

Para evitar a complexidade de gerenciar proxies externos de roteamento ou containers MCP órfãos, a arquitetura adota o modelo de Co-location (Container Sibling): cada tenant possui sua instância isolada no Azure Container Apps rodando dois processos principais simultaneamente:

Apache Superset: Executando via Gunicorn na porta padrão de aplicação (ex: 8088), responsável pela interface web, painéis e APIs REST.

Superset MCP Server: Executando em segundo plano (background) na porta de serviço interna (ex: 5008), expondo as ferramentas de IA padronizadas pelo protocolo MCP.

Vantagens Principais:

Isolamento Absoluto por Tenant: Como cada tenant possui seu próprio ACA, o isolamento de dados, banco de metadados e schemas é garantido pela própria infraestrutura da Azure.

Propagação Nativa de Segurança: O AiKernel (.NET 8) repassa o token JWT/Auth nativo do usuário logado no Superset via header HTTP. O MCP valida esse token, respeitando rigorosamente o Row-Level Security (RLS) e as permissões de painel de cada usuário.

Roteamento Simplificado: O AiKernel precisa apenas consultar a URL base do ACA daquele tenant na sessão e direcionar as chamadas de ferramentas para a rota /mcp.

2. Reconfiguração do Script de Bootstrap do Container

O gerador de script C# (SupersetContainerBootstrapScript) precisa ser atualizado para instalar o pacote do Superset MCP e iniciá-lo em segundo plano logo antes de subir o Gunicorn.

Exemplo de Adaptação no Script (BuildBootstrapCommand):

// ... [Configuração prévia do superset_config.py, migrations e bootstrap de permissões] ...

echo ">>> Instalando e iniciando o Superset MCP Server..."
pip install superset-mcp || pip install "git+https://github.com/apache/superset.git#subdirectory=superset-mcp" || true

# Sobe o servidor MCP do Superset em background na porta interna 5008
mcp-server-superset --port 5008 &

echo ">>> Iniciando o Gunicorn para o Superset principal..."
exec gunicorn \
--bind "0.0.0.0:{{containerPort}}" \
--workers "{{gunicornWorkers}}" \
--timeout "{{gunicornTimeoutSeconds}}" \
"superset.app:create_app()"


3. Configuração de Portas e Ingress no Azure Container Apps (ACA)

Para unificar o acesso sem que o AiKernel precise gerenciar números de portas internos complexos, configure o Ingress do Azure Container Apps utilizando regras baseadas em caminho (Path-based routing):

Exemplo de Configuração de Ingress (YAML):

ingress:
external: true
targetPort: 8088 # Porta padrão para o tráfego web principal (Superset)
rules:
- path: /mcp*
backendPort: 5008 # Direciona rotas /mcp para o servidor MCP interno


Endereçamento Resultante:

URL Base do Superset (UI / API): https://tenant-xyz.azurecontainerapps.io

Endpoint do MCP (Ferramentas de IA): https://tenant-xyz.azurecontainerapps.io/mcp

4. Checklist de Implementação e Validação

[ ] Atualização do Geretador C#: Modificar o método BuildBootstrapCommand para incluir a instalação do superset-mcp via pip.

[ ] Execução em Background: Garantir que o comando mcp-server-superset --port 5008 & execute antes do comando exec gunicorn.

[ ] Configuração do Ingress no ACA: Ajustar o template do ACA para rotear o caminho /mcp para a porta 5008.

[ ] Integração no AiKernel (.NET 8):

[ ] Ler o McpEndpointUrl da thread ativa (HybridCache / PostgreSQL).

[ ] Injetar o token do usuário no header da requisição da ferramenta:

requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userSupersetToken);


[