# Docker Compose - FieldService Infrastructure

Este arquivo configura os serviços necessários para o projeto FieldService usando Docker Compose.

## Serviços

### MongoDB 7.0
- **Porta**: 27017
- **Usuário**: `admin`
- **Senha**: `admin123`
- **Database**: `fieldservice`
- **URL de Conexão**: `mongodb://admin:admin123@localhost:27017/fieldservice?authSource=admin`

### Redis 7.2
- **Porta**: 6379
- **Senha**: `redis123`
- **URL de Conexão**: `localhost:6379,password=redis123`
- **Política de Memória**: LRU com limite de 256MB

### RabbitMQ 3.13
- **Porta AMQP**: 5672
- **Porta Management**: 15672 (acesso em `http://localhost:15672`)
- **Usuário**: `guest`
- **Senha**: `guest`
- **URL de Conexão**: `amqp://guest:guest@localhost:5672/`

## Quickstart

### Iniciar todos os serviços
```bash
docker-compose up -d
```

### Parar os serviços
```bash
docker-compose down
```

### Remover volumes (limpar dados)
```bash
docker-compose down -v
```

### Ver logs
```bash
# Todos os serviços
docker-compose logs -f

# Serviço específico
docker-compose logs -f mongodb
docker-compose logs -f redis
docker-compose logs -f rabbitmq
```

### Verificar status dos serviços
```bash
docker-compose ps
```

## Verificação de Conectividade

### MongoDB
```bash
# Dentro do container
docker-compose exec mongodb mongosh -u admin -p admin123

# Ou use uma ferramenta como MongoDB Compass
# mongodb://admin:admin123@localhost:27017/?authSource=admin
```

### Redis
```bash
docker-compose exec redis redis-cli -a redis123
> PING
```

### RabbitMQ
```bash
# Web UI: http://localhost:15672
# Username: guest
# Password: guest

# CLI
docker-compose exec rabbitmq rabbitmqctl status
```

## Variáveis de Ambiente

Todas as configurações podem ser personalizadas através do arquivo `.env`:

```env
MONGODB_PORT=27017
MONGODB_ROOT_USER=admin
MONGODB_ROOT_PASSWORD=admin123
MONGODB_DATABASE=fieldservice

REDIS_PORT=6379
REDIS_PASSWORD=redis123

RABBITMQ_PORT=5672
RABBITMQ_MANAGEMENT_PORT=15672
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_VHOST=/
```

## Integração com a Aplicação

### Appsettings.json (Exemplo)
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:admin123@localhost:27017/fieldservice?authSource=admin",
    "Database": "fieldservice"
  },
  "Redis": {
    "ConnectionString": "localhost:6379,password=redis123"
  },
  "RabbitMQ": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/"
  }
}
```

## Limpeza e Troubleshooting

### Se porta já está em uso
Modifique a porta no `.env` ou use `docker-compose down` para liberar.

### Reset completo
```bash
# Parar e remover tudo
docker-compose down -v

# Remover images também
docker-compose down -v --rmi all

# Reiniciar do zero
docker-compose up -d
```

### Logs de erro
```bash
docker-compose logs --tail=100 mongodb
docker-compose logs --tail=100 redis
docker-compose logs --tail=100 rabbitmq
```
