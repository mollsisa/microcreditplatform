# Microsserviços - Cadastro, Proposta de Crédito e Cartão de Crédito

## Visão Geral
Este projeto implementa três microsserviços em .NET 8.0 que se comunicam via RabbitMQ para orquestrar o fluxo de:
1. Cadastro de Cliente
2. Geração de Proposta de Crédito
3. Emissão de Cartão de Crédito

Todos os dados de clientes são persistidos em PostgreSQL.  
O sistema foi projetado com resiliência em mente, garantindo que falhas de comunicação sejam tratadas via mensagens de evento.

---

## Como Executar

### 1. Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Docker](https://www.docker.com/) e [Docker Compose](https://docs.docker.com/compose/)

### 2. Subir infraestrutura
No diretório /infra, execute:

```bash
docker-compose up -d
```

Isso iniciará:
- RabbitMQ (porta 5672, painel em http://localhost:15672)
- PostgreSQL (porta 5432)

### 3. Rodar os microsserviços
Cada microsserviço está em uma pasta separada:

```bash
cd src/CustomerService
dotnet run --urls http://locashost:5080
```

```bash
cd src/CreditProposalSerice
dotnet run --urls http://locashost:5081
```

```bash
cd src/CreditCardService
dotnet run --urls http://locashost:5082
```

### 4. Testando
Cadastrar um cliente:

```bash
Invoke-RestMethod -Uri "http://localhost:5080/customers" `
  -Method Post `
  -Headers @{ "Content-Type" = "application/json" } `
  -Body '{"name":"Melissa","document":"123123","email":"melissa@hotmail.com"}'
```

ou 

```bash
curl -X POST http://localhost:5080/customers -H "Content-Type: application/json" -d '{"name":"Melissa","document":"123123","email":"melissa@hotmail.com"}'
```

Esse comando irá retornar o CustomerId. Espere alguns segundos e execute o comando abaixo, trocando o CustomerId pelo o que o comando anterior retornou, para verificar o status do cliente:

```bash
Invoke-RestMethod -Uri "http://localhost:5080/customers/CustomerId"
```

ou

```bash
curl http://localhost:5080/customers/CustomerId
```

Fluxo esperado:
1. Cliente salvo no banco PostgreSQL
2. Evento CustomerRegistered enviado para o RabbitMQ
3. Serviço CreditProposalSerice processa o evento e envia: CreditProposalApproved ou CreditProposalRejected
4. Se aprovado, serviço CreditCardService emite o cartão e envia: CardIssued ou CardIssuanceFailed
4. Serviço CustomerService atualiza o status do cliente com base no resultado

---

## Resiliência
- Cada microsserviço possui retries e dead-letter queue configurados no RabbitMQ.
- Falhas em `CreditProposalSerice` ou `CreditCardService` geram eventos de erro consumidos pelo serviço de `CustomerService`.
- Logs centralizados permitem monitorar falhas.