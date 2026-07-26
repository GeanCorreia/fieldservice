# TemplateUI (Camada de UI no Domínio)

## Objetivo

`TemplateUI` representa a configuração de interface de uma `TemplateVersion`.

Ele **não valida dados de negócio** (isso fica em `Field` + `ValidationRule`), mas define **como os campos devem ser apresentados e comportados na UI**.

---

## Estrutura atual

### Agregado raiz

- `TemplateUI`
  - Único nível com `Id` (pensado para MongoDB).
  - Contém os blocos de configuração:
    - `Layout`
    - `Presentation`
    - `Visibility`
    - `Editability`
    - `DefaultValueStrategy`
    - `UxOptions`
  - Possui `Clone()` para copiar a configuração entre versões.

### Seções de configuração

- `TemplateUILayout`
  - Define organização estrutural (ex.: colunas e seções).
  - Propriedades:
    - `Columns`
    - `Sections` (`TemplateUISection`)

- `TemplateUISection`
  - Agrupa campos por seção.
  - Propriedades:
    - `Key`
    - `Title`
    - `FieldIds`
    - `CollapsedByDefault`

- `TemplateUIPresentation`
  - Define apresentação geral e por campo.
  - Propriedades:
    - `Title`
    - `Subtitle`
    - `Fields` (`TemplateUIFieldPresentation`)

- `TemplateUIFieldPresentation`
  - Overrides visuais por campo.
  - Propriedades:
    - `FieldId`
    - `Label`
    - `HelperText`
    - `Placeholder`
    - `Icon`

- `TemplateUIVisibility`
  - Regras condicionais de visibilidade.
  - Propriedade:
    - `Rules` (`TemplateUIConditionRule`)

- `TemplateUIEditability`
  - Regras condicionais de edição (readonly/editável).
  - Propriedade:
    - `Rules` (`TemplateUIConditionRule`)

- `TemplateUIConditionRule`
  - Regra condicional simples entre campos.
  - Propriedades:
    - `TargetFieldId`
    - `SourceFieldId`
    - `Operator`
    - `Value`

- `TemplateUIDefaultValueStrategy`
  - Estratégias de valor padrão por campo.
  - Propriedade:
    - `Rules` (`TemplateUIDefaultValueRule`)

- `TemplateUIDefaultValueRule`
  - Estratégia para um campo específico.
  - Propriedades:
    - `FieldId`
    - `Strategy`
    - `Expression`
    - `Constant`

- `TemplateUIUxOptions`
  - Opções globais de UX.
  - Propriedades:
    - `UseTabs`
    - `UseStepper`
    - `ShowSummary`
    - `AllowSectionCollapse`

---

## Como integra com versionamento

`TemplateVersion` possui:

- `TemplateUI Ui { get; private set; }`

No fluxo de atualização de campos (`Template.UpdateFields(...)`):

1. Se receber `templateUi`, usa `templateUi.Clone()`.
2. Se não receber, reutiliza `currentVersion.Ui.Clone()`.
3. Nova `TemplateVersion` é criada com essa UI.

Ou seja, a UI fica **versionada junto com os campos**.

---

## Estado atual (proposital)

No momento o modelo está em um nível **genérico/base**, sem engine de avaliação de condições nem validações profundas de consistência cruzada.

Exemplos de validações futuras:

- garantir que `FieldIds` da seção existem em `TemplateVersion.Fields`;
- validar operadores suportados (`==`, `!=`, `contains`, etc.);
- validar estratégias conhecidas em `DefaultValueStrategy`;
- evitar duplicidade de regras conflitantes.

---

## Direção de evolução sugerida

1. Criar enums para `Operator` e `Strategy` (evitar strings soltas).
2. Criar validadores de consistência da UI no domínio.
3. Adicionar métodos de alteração controlada no agregado `TemplateUI` (em vez de set indireto).
4. Definir contrato claro de serialização Mongo (nomes/campos opcionais/defaults).
