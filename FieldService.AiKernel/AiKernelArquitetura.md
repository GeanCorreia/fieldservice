┌─────────────────────────────────────────────────────────────────────────────┐
│                           FRONTEND (React Client)                           │
└───────────────┬──────────────────────┬──────────────────────┬───────────────┘
│                      │                      │
▼                      ▼                      ▼
┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐
│  Módulo SUPERSET │   │   Módulo FORM    │   │ Módulo ASSISTANT │
│  (Analytics/BI)  │   │  (Workflow/Forms)│   │  (Chat General)  │
└─────────┬────────┘   └─────────┬────────┘   └─────────┬────────┘
│                      │                      │
│   Conhecem seus      │   Conhecem seus      │   Conhecem seus
│   próprios MCPs      │   próprios MCPs      │   próprios MCPs
│                      │                      │
└──────────────────────┼──────────────────────┘
│
▼
┌──────────────────────────────────────────────┐
│                   AiKernel                   │
│      (FiledService.AiKernel / Motor de IA)      │
│                                              │
│  - Guarda Secrets e API Keys da OpenAI       │
│  - Dispara Runs com TruncationStrategy       │
│  - Gerencia Threads e CacheHits              │
│  - Executa a chamada HTTP/Transport do LLM   │
└──────────────────────┬───────────────────────┘
│ (HTTPS / OpenAI SDK)
▼
┌──────────────┐
│  OpenAI/Google/Anthropic API  │
└──────────────┘

Utiliza Microsoft.Extensions.AI

┌─────────────────────────────────────────────────────────────────────────────────┐
│ AIKERNEL (Estado de Roteamento da Sessão)                                       │
│                                                                                 │
│  ThreadId ───► { TenantId, UserId, McpId, Tools: [ CallToolResult ] }           │
└─────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────┐
│ ENTIDADE: Thread (Gerenciada no HybridCache + PostgreSQL)                       │
│                                                                                 │
│  - Id: Guid                                                                     │
│  - UserId: Guid                                                                 │
│  - TenantId: Guid                                                               │
│  - McpId: string                                                                │
│  - ProviderType: AIProvider (Enum: OpenAI, Gemini, Anthropic)                       │ │
│  - LastAccessedAt: DateTimeOffset                                               │
   - IEnumerable<ThreadRequestEvent> RequestEvents                                    │
└───────────────────────────────────────┬─────────────────────────────────────────┘
│
▼ (Relacionamento 1 : N)
┌─────────────────────────────────────────────────────────────────────────────────┐
│ ENTIDADE: ThreadRequestEvent (Auditoria / Faturamento)                            │
│                                                                                 │
│  - Id: Guid                                                                     │
│  - ThreadId: Guid (FK)                                                          │
│  - ProviderRequestId: string  ◄── [Id do Run/ChatCompl. retornado pela LLM]    │
│  - PromptTokens: int                                                            │
│  - CompletionTokens: int                                                        │
│  - CachedTokens: int          ◄── [Essencial para provar o desconto de CacheHit] │ │
│  - OccurredAt: DateTimeOffset                                                    │
└─────────────────────────────────────────────────────────────────────────────────┘

classDiagram
direction LR

    class ThreadStartRequest{
        +Guid UserId
        +Guid TenantId
        +string McpId
        +IEnumerable~CallToolResult~ Tools
        
    }
    class Thread {
        +Guid Id
        +Guid UserId
        +Guid TenantId
        +string McpId
        +AIProvider ProviderType
        +DateTimeOffset LastAccessedAt
        +IEnumerable~ThreadRequestEvent~ RequestEvents
    }

    class ThreadRequestEvent {
        +Guid Id
        +Guid ThreadId
        +string ProviderRequestId
        +int PromptTokens
        +int CompletionTokens
        +int CachedTokens
        +DateTimeOffset OccurredAt
    }

    class Mcp {
        +Guid Id
        +string McpId
        +AIProvider Provider
        +string Model
        +
    }

    class McpDto{
        +string McpId
        +AIProvider Provider
        +string Model
        +IENumeberable~CallToolResult~ Tools
    }

    class Provider {
        +Guid Id
        +AIProvider Name
        +Guid SecretKeyId
        +IEnumerable~ProviderCostEvent~ CostEvents
        +GetCost(DateTimeOffset at) decimal
    }

    class ProviderCostEvent {
        +Guid Id
        +Guid ProviderId
        +decimal Cost
        +DateTimeOffset OccurredAt
    }

    class AIProvider {
        <<enumeration>>
        OpenAI
        Gemini
        Anthropic
    }






namespace SaaS.Core.AiKernel.Abstractions;

using Microsoft.Extensions.AI;
using SaaS.Core.AiKernel.Models;

public interface IAiKernelService
{
Task<Guid> StartThreadAsync(
ThreadStartRequest request,
CancellationToken cancellationToken = default);

    /// <summary>
    /// Fase 2: Envia uma mensagem (suportando texto, imagens e anexos via Microsoft.Extensions.AI)
    /// </summary>
    Task<ChatCompletion> SendMessageAsync(
        Guid threadId, 
        ChatMessage message, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ou, se precisar enviar um lote/histórico de mensagens na mesma requisição:
    /// </summary>
    Task<ChatCompletion> SendMessagesAsync(
        Guid threadId, 
        IEnumerable<ChatMessage> messages, 
        CancellationToken cancellationToken = default);

    Task EndThreadAsync(
        Guid threadId, 
        CancellationToken cancellationToken = default);
}

namespace Shared.Dtos.AiMessages;

public record ChatMessageResponseDto
{
public required Guid Id { get; init; }
public required string Role { get; init; } // "assistant", "user", "system"
public required string Content { get; init; }
public required DateTimeOffset CreatedAt { get; init; }

    // Opcional: Metadados úteis para o Front (como ID do provedor ou tokens consumidos se o admin precisar ver)
    public string? ProviderRequestId { get; init; }
}

namespace SaaS.Core.AiKernel.Abstractions;

public interface IAiToolExecutor
{
/// <summary>
/// Identifica qual módulo é (ex: "superset", "form")
/// </summary>
string McpId { get; }

    /// <summary>
    /// Executa a ferramenta solicitada pela LLM utilizando a lógica interna do módulo
    /// </summary>
    Task<object?> ExecuteToolAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken = default);
}

O Ponto Central (AiKernel em .NET 8):

Continua centralizando a inteligência do chat, o gerenciamento de threads no HybridCache, o histórico em PostgreSQL, o controle de tokens/auditoria e a comunicação com a LLM via Microsoft.Extensions.AI.

Quando o usuário manda uma mensagem, o AiKernel recebe no header o X-Tenant-Id e o token de autenticação do usuário.

A Camada de MCP Dinâmico (Node.js):

Você terá a aplicação em Node.js rodando como o orquestrador/proxy de ferramentas.

Para cada TenantId, ela gerencia ou direciona a execução para o contêiner/instância específica do MCP do Superset daquele cliente (que já sobe apontando para a base correta do tenant via Docker).

O token do usuário é repassado de ponta a ponta, garantindo que o Row-Level Security (RLS) e as permissões nativas do Superset sejam respeitadas rigorosamente.

Rota Única via Ingress do ACA (Tudo na mesma porta)
Se você quiser simplificar ainda mais e usar a mesma URL do Superset para tudo, o próprio ACA (ou um Nginx/Proxy leve dentro do contêiner) pode fazer o roteamento de path:

Requisições para /api/... vão para o Gunicorn (Superset na porta 8088).

Requisições para /mcp/... vão para o servidor MCP (na porta 5008).

Dessa forma, o AiKernel só precisa saber a URL base do ACA do tenant (ex: [https://tenant-a.azurecontainerapps.io](https://tenant-a.azurecontainerapps.io)) e appendando /mcp no final ([https://tenant-a.azurecontainerapps.io/mcp](https://tenant-a.azurecontainerapps.io/mcp))

PostgreSQL MCP Server (geralmente referenciado no ecosistema como @modelcontextprotocol/server-postgres