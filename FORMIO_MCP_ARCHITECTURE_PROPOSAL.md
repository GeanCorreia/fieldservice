# FormIo MCP Cloud Architecture Proposal

## Overview

Este documento propoe uma arquitetura completa para hospedar dois MCPs relacionados a Form.io em nuvem, com roteamento centralizado via backend usando `Yarp.ReverseProxy.Transforms`.

Objetivo pratico:
- `DeveloperMcp`: ajudar usuario a criar/editar formularios sem precisar conhecer URLs, endpoints ou detalhes de GET/POST.
- `FormExecutionMcp`: executar automacoes de formulario (acoes, fluxos, auto preenchimento), seguindo recursos oficiais de execucao Form.io.

A proposta entra em opcoes, tradeoffs, melhores praticas e caminho recomendado para tomada de decisao.

---

## Contexto e Premissas

### O que estamos assumindo

- Sua aplicacao ja tem autenticacao e contexto de tenant/user.
- O frontend fala com seu backend, nao diretamente com Form.io MCP.
- Cada requisicao precisa ser tenant-safe.
- MCPs devem subir sob demanda para otimizar custo.
- Backend vai atuar como API Gateway para esconder rotas internas.

### O que nao e foco

- Modelagem visual detalhada de UX do form builder.
- Dependencia de um cloud provider especifico (vamos listar opcoes multi-cloud).

---

## Conceitos Rapidos (MCP aplicado ao seu caso)

No seu cenario, pense no MCP como um servidor de capacidades/ferramentas que expoe operacoes estruturadas para:
- criar/especificar formulario (`DeveloperMcp`)
- executar logica operacional em runtime (`FormExecutionMcp`)

O backend central decide:
- quem pode chamar
- para qual tenant
- qual MCP recebera a chamada
- quais limites e politicas aplicar

---

## Arquitetura de Alto Nivel

```text
[User UI]
   |
   | JWT/session
   v
[FieldService Backend]
   |  (AuthZ, tenant claims, audit, rate limit, policy)
   |  + YARP Reverse Proxy + Transforms
   |
   +--> [DeveloperMcp Runtime Pool]  (on-demand)
   |
   +--> [FormExecutionMcp Runtime Pool] (on-demand)

MCPs -> Form.io APIs/Resources -> retornam resposta normalizada
```

Principio chave:
- Usuario nunca monta URL de form runtime.
- Backend injeta contexto (`tenantId`, `userId`, correlation id, scopes).
- MCPs ficam isolados e sem exposicao publica direta.

---

## Opcao 1 - Dois MCPs separados (recomendado)

### Como funciona

- `DeveloperMcp` e `FormExecutionMcp` rodam como servicos independentes.
- Cada um escala de forma autonoma conforme carga.
- YARP roteia por path, header ou policy.

### Vantagens

- Separacao clara de responsabilidade.
- Escalabilidade independente (builder vs runtime tem padroes de uso diferentes).
- Menor risco de regressao cruzada.
- Deploy e rollback isolados.

### Desvantagens

- Mais componentes para operar.
- Pode haver duplicacao de utilitarios (auth client, observabilidade).

### Quando escolher

- Quase sempre no seu caso, especialmente por haver naturezas distintas entre criacao e execucao.

---

## Opcao 2 - MCP unico com modulos (alternativa simplificada)

### Como funciona

- Um MCP unico com dois dominios internos: `developer` e `execution`.

### Vantagens

- Operacao inicial mais simples.
- Menos deployment units.

### Desvantagens

- Acoplamento alto.
- Escala igual para workloads diferentes.
- Incidentes de um dominio podem afetar o outro.

### Quando escolher

- Prova de conceito curta, time pequeno, baixa carga.

---

## Opcao 3 - MCP por tenant (isolamento maximo)

### Como funciona

- Instancia logica (ou fisica) por tenant para um ou ambos MCPs.

### Vantagens

- Isolamento forte e previsibilidade por tenant.
- Facil para tenants enterprise com requisitos de compliance.

### Desvantagens

- Custo e complexidade operacional altos.
- Provisionamento e lifecycle muito mais complexos.

### Quando escolher

- Multi-tenant com contratos enterprise severos (dados, residency, throughput reservado).

---

## Runtime em Nuvem sob Demanda - Opcoes

## 1) Kubernetes + HPA/KEDA

### Vantagens
- Controle fino de autoscaling.
- Bom para workloads estaveis e picos previsiveis.
- Networking e policy maduros.

### Desvantagens
- Curva operacional maior.
- Custo base de cluster mesmo em baixa demanda.

## 2) Serverless containers (Azure Container Apps / Cloud Run / Fargate)

### Vantagens
- Excelente para escala ate zero.
- Menor overhead operacional.
- Boa relacao custo/uso intermitente.

### Desvantagens
- Cold start pode impactar latencia inicial.
- Menos controle low-level.

## 3) Functions + MCP adapter

### Vantagens
- Custo agressivo em baixa carga.
- Deploy rapido.

### Desvantagens
- Pode nao encaixar bem em conexoes longas/estado de sessao.
- Limites de runtime podem atrapalhar operacoes mais pesadas.

Recomendacao pratica:
- Comecar com serverless containers para MCP (escala a zero + menor operacao).
- Migrar para Kubernetes apenas se volume/controle exigir.

---

## Integracao com YARP (ponto central da sua estrategia)

## Objetivo

Interceptar chamadas no backend e encaminhar para MCP correto, sempre anexando contexto de seguranca e auditoria.

## Rotas sugeridas

- `/mcp/dev/*` -> `DeveloperMcp`
- `/mcp/exec/*` -> `FormExecutionMcp`

## Transformacoes recomendadas

No proxy:
- Ler claims com `ClaimsResolver.GetUserId(User)` e `ClaimsResolver.GetTenantId(User)`.
- Injetar headers internos:
  - `X-Tenant-Id`
  - `X-User-Id`
  - `X-Correlation-Id`
  - `X-Request-Id`
- Opcional: injetar token interno assinado curto (service token) para o MCP validar origem.

## Boas praticas com YARP

- Nunca confiar em `X-Tenant-Id` vindo do cliente.
- Sempre sobrescrever headers sensiveis no gateway.
- Usar allowlist de headers para downstream.
- Timeouts por rota (dev vs exec podem ser diferentes).
- Circuit breaker e retry com cautela (idempotencia).

---

## Design do DeveloperMcp

## Missao

Permitir criacao e evolucao do formulario com assistencia, sem expor detalhes de infra para usuario final.

## Capabilities esperadas

- Criar schema inicial via prompt/assistente.
- Validar consistencia de schema.
- Aplicar templates da sua aplicacao.
- Salvar/publicar versoes de formulario.

## Regras de dominio para reduzir erro humano

- Usuario define semantica, nao endpoint.
- URL, GETs, filtros e binds de dados ficam centralizados no backend/frontend.
- Aplicar guardrails:
  - nomenclatura de campos
  - tipos permitidos
  - limites de profundidade
  - policy de componentes permitidos

## Opcoes de MCP

- Oficial Form.io MCP (se disponivel e estavel para seu stack): menor risco de compatibilidade futura.
- MCP comunitario consolidado: pode ter features rapidas, mas exige diligencia forte de seguranca e manutencao.

## Tradeoff oficial vs comunidade

- Oficial:
  - Pro: alinhamento roadmap, suporte mais previsivel.
  - Contra: evolucao pode ser mais lenta.
- Comunidade:
  - Pro: flexibilidade e velocidade.
  - Contra: risco de manutencao, breaking changes, qualidade desigual.

---

## Design do FormExecutionMcp

## Missao

Executar runtime de automacoes: acoes, fluxo e autofill conforme documentacao de execucao Form.io.

## Capabilities esperadas

- Resolver regras dinamicas por evento de formulario.
- Integrar fontes de dados para autofill.
- Executar workflows condicionais com observabilidade.

## Regras essenciais

- Toda execucao deve portar contexto tenant/user.
- Logs de decisao (por que campo foi auto preenchido) para auditoria.
- Limites de tempo e fallback seguro quando dependencia externa falhar.

---

## Seguranca e Compliance

## Controles minimos

- AuthN no backend; MCP recebe apenas chamadas autenticadas internamente.
- AuthZ no backend antes do proxy (permissoes por acao).
- mTLS entre backend e MCP quando possivel.
- Secrets em vault (nao em appsettings).
- Rotacao de tokens/chaves.

## Tenant isolation

- Sempre propagar `tenantId` vindo de claim validada.
- MCP valida coerencia do tenant em toda operacao.
- Nunca permitir lookup sem escopo de tenant.

## Auditoria

Registrar em toda chamada:
- request id/correlation id
- user id
- tenant id
- acao (create/update/execute/autofill)
- recurso afetado (form id/submission id)
- resultado (success/fail + motivo)

---

## Observabilidade

## Logs

- Structured logs JSON.
- Campos obrigatorios: `timestamp`, `service`, `route`, `tenantId`, `userId`, `requestId`, `durationMs`, `status`.

## Metrics

- RPS por MCP
- erro 4xx/5xx por rota
- cold starts
- tempo p50/p95/p99
- taxa de timeout/circuit open

## Tracing

- OpenTelemetry end-to-end (frontend -> backend -> MCP -> Form.io downstream).

---

## Confiabilidade e Resiliencia

- Timeout especifico por endpoint.
- Retry apenas para operacoes idempotentes.
- Circuit breaker para dependencias externas.
- Bulkhead por tipo de workload (dev vs exec).
- Dead-letter para eventos async (se usar fila para execucao).

---

## Dados e Persistencia

## Estrategia recomendada

- Fonte de verdade de formularios/submissoes continua no backend/data layer existente.
- MCP atua como camada de inteligencia/execucao, nao como owner de dados principais.

## Beneficios

- Mantem consistencia com sua auditoria atual.
- Evita lock-in forte no MCP.

---

## Contratos de API internos

Definir contratos versionados entre backend e MCP:

- `v1/developer/create-form`
- `v1/developer/validate-form`
- `v1/execution/run-actions`
- `v1/execution/autofill`

Boas praticas:
- esquema JSON estrito
- validacao server-side
- versionamento sem quebrar consumidores

---

## Performance e Custo

## Ponto chave

Seu maior ganho de custo vira de escala a zero e roteamento central com politica forte.

## Otimizacoes praticas

- Cache de metadados de schema para rotas de leitura.
- Warm pools pequenos em horario de pico para reduzir cold start.
- Pre-computar templates frequentes no `DeveloperMcp`.

---

## Roadmap de Implementacao (fases)

## Fase 1 - Fundacao

- Implementar rotas YARP para `/mcp/dev/*` e `/mcp/exec/*`.
- Injetar headers de contexto via transforms.
- Subir MCPs em ambiente nao-producao com auth interna.

## Fase 2 - Seguranca e observabilidade

- mTLS interno (ou private networking + service auth).
- OpenTelemetry + dashboards + alertas.
- Politicas de timeout/retry/circuit breaker.

## Fase 3 - Funcionalidade

- Integrar fluxo completo de criacao de form com guardrails.
- Integrar autofill/acoes/fluxos de execucao.
- Testes e2e com multiplos tenants.

## Fase 4 - Escala e custo

- Ajustar autoscaling thresholds.
- Definir warm strategy para reduzir cold start.
- Testes de carga e chaos testing.

---

## Matriz de Decisao Rapida

| Decisao | Opcao A | Opcao B | Recomendacao |
|---|---|---|---|
| Separacao de MCP | Dois servicos | MCP unico | Dois servicos |
| Runtime cloud | Serverless containers | Kubernetes puro | Serverless no inicio |
| Integracao | YARP no backend | chamada direta frontend->MCP | YARP no backend |
| Isolamento tenant | shared + policy | por-tenant infra | shared + policy no inicio |
| MCP provider | oficial Form.io | comunidade consolidada | oficial quando possivel |

---

## Riscos e Mitigacoes

## Risco: cold start impactar UX
- Mitigar com warm instances em horario util.
- Pré-aquecer no login de usuario premium/admin.

## Risco: divergencia entre MCP e regras do dominio
- Mitigar com camada de policy no backend antes do proxy.
- Testes de contrato e validadores de schema obrigatorios.

## Risco: vazamento cross-tenant
- Mitigar com claims obrigatorias + header overwrite + validacao no MCP.
- Testes automatizados de isolamento.

## Risco: lock-in em implementacao MCP
- Mitigar com interface interna estavel entre backend e MCP.
- Adaptadores por provider.

---

## Recomendacao Final

Arquitetura recomendada para seu caso:

1. Dois MCPs separados (`DeveloperMcp`, `FormExecutionMcp`).
2. Hospedagem em serverless containers com escala sob demanda.
3. Backend como unico ponto de entrada com YARP + transforms.
4. Contexto de tenant/user sempre via claims resolvidas no backend.
5. Auditoria e observabilidade end-to-end como requisito de primeiro nivel.

Essa estrategia te da:
- boa seguranca multi-tenant
- flexibilidade para evoluir provider MCP
- custo controlado em baixa demanda
- organizacao clara entre criacao e execucao de formularios

---

## Checklist para decidir amanha

- Qual cloud alvo inicial (Azure, GCP, AWS)?
- Vai iniciar com MCP oficial Form.io ou comunitario?
- Qual latencia maxima aceitavel para cold start?
- Precisa isolamento por tenant agora ou depois?
- Quais fluxos de auditoria sao obrigatorios no MVP?

Com essas respostas, voce consegue fechar um desenho de implementacao com baixo retrabalho.

