Guia de Reconfiguração do Container Superset e MCP

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